using System.Security.Cryptography;
using System.Text.Json;

namespace Launcher;

/// <summary>
/// Downloads the signed manifest, verifies it, syncs changed files into the
/// local cache, and stages them where ClassicUO expects them. Every step
/// that fails throws UpdateException, which the caller treats as "refuse to
/// start the client".
/// </summary>
internal sealed class AssetUpdater(LauncherSettings settings, Action<string> log)
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri(AppConstants.PatchServiceBaseUrl) };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<Manifest> RunAsync(CancellationToken ct)
    {
        Manifest manifest = await FetchManifestAsync(ct);

        if (manifest.MinLauncherVersion > AppConstants.LauncherVersion)
        {
            string downloadUrl = await TryGetLauncherDownloadUrlAsync(ct);
            throw new UpdateException(
                $"This launcher is version {AppConstants.LauncherVersion}, but the shard requires at least " +
                $"version {manifest.MinLauncherVersion}. Download the new launcher: " +
                (downloadUrl.Length > 0 ? downloadUrl : AppConstants.PatchServiceBaseUrl));
        }

        if (manifest.Version < settings.LastAppliedManifestVersion)
        {
            // A signed manifest can carry an explicit, shard-owner-authorized
            // recovery exemption (AllowRollbackFrom) for exactly this case:
            // a broken publish needs to be undone by re-publishing an older,
            // known-good manifest. Because the exemption lives inside the
            // signed bytes, only the shard owner's private key can grant it -
            // a replayed old-but-validly-signed manifest that never set this
            // field stays blocked, same as before.
            bool recoveryAuthorized = manifest.AllowRollbackFrom > 0
                && settings.LastAppliedManifestVersion <= manifest.AllowRollbackFrom;

            if (!recoveryAuthorized)
            {
                throw new UpdateException(
                    $"The downloaded manifest is version {manifest.Version}, but this launcher already applied version " +
                    $"{settings.LastAppliedManifestVersion}. Refusing to roll back assets. This may mean the patch service " +
                    "is being tampered with; try again later or contact the shard's staff.");
            }

            log($"Manifest version {manifest.Version} is lower than the applied version " +
                $"{settings.LastAppliedManifestVersion}, but the shard owner signed it as an authorized recovery " +
                $"(allowRollbackFrom={manifest.AllowRollbackFrom}). Rolling back.");
        }

        Directory.CreateDirectory(LauncherSettings.AssetsCacheDirectory);

        var overrideLines = new List<string>();

        foreach (ManifestFileEntry file in manifest.Files)
        {
            string cachedPath;

            try
            {
                cachedPath = ResolveSafePath(LauncherSettings.AssetsCacheDirectory, file.Path);
            }
            catch (UpdateException ex)
            {
                log($"Skipping '{file.Path}': {ex.Message}");
                continue;
            }

            if (!await FileMatchesAsync(cachedPath, file.Sha256))
            {
                log($"Downloading {file.Path} ({file.Size} bytes)...");
                await DownloadAndVerifyAsync(file, cachedPath, ct);
            }

            switch (file.Target)
            {
                case "override":
                    if (!string.IsNullOrEmpty(file.OverrideKey))
                    {
                        if (!IsValidOverrideKey(file.OverrideKey))
                        {
                            log($"Skipping override for '{file.Path}': override key '{file.OverrideKey}' is not a valid bare filename.");
                            break;
                        }

                        if (ContainsLineBreak(cachedPath))
                        {
                            log($"Skipping override for '{file.Path}': resolved cache path contains a line break.");
                            break;
                        }

                        overrideLines.Add($"{file.OverrideKey}={cachedPath}");
                    }
                    break;

                case "cuoData":
                    CopyIntoClassicUo(cachedPath, "Data", "Client", Path.GetFileName(file.Path));
                    break;

                case "plugin":
                    CopyIntoClassicUo(cachedPath, "Data", "Plugins", Path.GetFileName(file.Path));
                    break;

                case "client":
                    // Reserved for future use; ClientAssets/ ships no "client" target today.
                    break;
            }
        }

        Directory.CreateDirectory(LauncherSettings.RootDirectory);
        File.WriteAllLines(LauncherSettings.OverridesFilePath, overrideLines);

        settings.LastAppliedManifestVersion = manifest.Version;
        settings.Save();

        return manifest;
    }

    private async Task<Manifest> FetchManifestAsync(CancellationToken ct)
    {
        byte[] manifestBytes;
        byte[] signature;

        try
        {
            manifestBytes = await _http.GetByteArrayAsync("manifest.json", ct);
            signature = await _http.GetByteArrayAsync("manifest.sig", ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new UpdateException($"Could not reach the patch service at {AppConstants.PatchServiceBaseUrl}: {ex.Message}");
        }

        bool ok;
        try
        {
            ok = ManifestVerifier.Verify(manifestBytes, signature);
        }
        catch (Exception ex)
        {
            throw new UpdateException($"Could not verify the manifest signature: {ex.Message}");
        }

        if (!ok)
        {
            throw new UpdateException("The manifest signature is not valid. Refusing to update or start the client.");
        }

        Manifest? manifest = JsonSerializer.Deserialize<Manifest>(manifestBytes, JsonOptions);
        return manifest ?? throw new UpdateException("The manifest could not be parsed.");
    }

    private async Task<string> TryGetLauncherDownloadUrlAsync(CancellationToken ct)
    {
        try
        {
            string json = await _http.GetStringAsync("launcher.json", ct);
            LauncherInfo? info = JsonSerializer.Deserialize<LauncherInfo>(json, JsonOptions);
            return info?.DownloadUrl ?? "";
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Combines a manifest-supplied relative path with a base directory and
    /// checks the result stays inside that base directory. Rejects a rooted
    /// path (e.g. "C:\Windows\evil.dll") and a ".." segment that escapes.
    /// </summary>
    private static string ResolveSafePath(string baseDirectory, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            throw new UpdateException($"Manifest path '{relativePath}' is not a valid relative path.");
        }

        string combined = Path.Combine(baseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        string fullBase = Path.GetFullPath(baseDirectory);
        string fullPath = Path.GetFullPath(combined);

        string baseWithSeparator = fullBase.EndsWith(Path.DirectorySeparatorChar)
            ? fullBase
            : fullBase + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(baseWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new UpdateException($"Manifest path '{relativePath}' resolves outside the expected directory.");
        }

        return fullPath;
    }

    /// <summary>
    /// Combines a manifest-supplied relative path with the configured patch
    /// service base address and checks the result still targets that same
    /// scheme, host and port. HttpClient treats a request string that parses
    /// as an absolute URI as overriding BaseAddress, so without this check a
    /// manifest entry with a scheme (or a protocol-relative "//host/path")
    /// could redirect the download to an arbitrary host.
    /// </summary>
    private Uri BuildDownloadRequestUri(string relativePath)
    {
        Uri baseUri = _http.BaseAddress!;
        Uri combined;

        try
        {
            combined = new Uri(baseUri, relativePath);
        }
        catch (UriFormatException ex)
        {
            throw new UpdateException($"Manifest path '{relativePath}' is not a valid URI segment: {ex.Message}");
        }

        if (!string.Equals(combined.Scheme, baseUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(combined.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase) ||
            combined.Port != baseUri.Port)
        {
            throw new UpdateException($"Manifest path '{relativePath}' resolves outside the configured patch service.");
        }

        return combined;
    }

    /// <summary>
    /// Checks that a manifest-supplied override key is a plausible bare
    /// filename: no path separators, no "=", and no control characters
    /// (in particular no CR or LF). This key is written unescaped as the
    /// left-hand side of a "key=path" line in uofilesoverride.txt, so an
    /// unchecked key could inject or corrupt other lines in that file.
    /// </summary>
    private static bool IsValidOverrideKey(string overrideKey)
    {
        if (string.IsNullOrWhiteSpace(overrideKey))
        {
            return false;
        }

        foreach (char c in overrideKey)
        {
            if (c == '=' || c == '/' || c == '\\' || char.IsControl(c))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks a value for CR or LF before it is written as the right-hand
    /// side of a "key=path" line in uofilesoverride.txt.
    /// </summary>
    private static bool ContainsLineBreak(string value)
    {
        return value.Contains('\r') || value.Contains('\n');
    }

    private static async Task<bool> FileMatchesAsync(string path, string expectedSha256)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        await using FileStream stream = File.OpenRead(path);
        byte[] hash = await SHA256.HashDataAsync(stream);
        string hex = Convert.ToHexString(hash).ToLowerInvariant();
        return string.Equals(hex, expectedSha256, StringComparison.OrdinalIgnoreCase);
    }

    private async Task DownloadAndVerifyAsync(ManifestFileEntry file, string destinationPath, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        string tempPath = destinationPath + ".download";

        try
        {
            Uri requestUri = BuildDownloadRequestUri(file.Path);

            using (HttpResponseMessage response = await _http.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                response.EnsureSuccessStatusCode();
                await using FileStream fileStream = File.Create(tempPath);
                await response.Content.CopyToAsync(fileStream, ct);
            }

            if (!await FileMatchesAsync(tempPath, file.Sha256))
            {
                throw new UpdateException($"Downloaded file '{file.Path}' does not match its manifest hash.");
            }

            File.Move(tempPath, destinationPath, overwrite: true);
        }
        catch (Exception ex) when (ex is not UpdateException)
        {
            throw new UpdateException($"Could not download '{file.Path}': {ex.Message}");
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private void CopyIntoClassicUo(string sourcePath, params string[] relativeParts)
    {
        if (string.IsNullOrWhiteSpace(settings.ClassicUoPath))
        {
            throw new UpdateException("ClassicUO installation folder is not set.");
        }

        string destinationDir = Path.Combine([settings.ClassicUoPath, .. relativeParts[..^1]]);
        Directory.CreateDirectory(destinationDir);
        string destinationPath = Path.Combine(destinationDir, relativeParts[^1]);
        File.Copy(sourcePath, destinationPath, overwrite: true);
    }
}
