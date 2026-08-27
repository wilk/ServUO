using System.Reflection;
using System.Text.Json;

namespace Launcher;

/// <summary>
/// Shard-specific constants. See Docs/PatchServer.md for the VPS side of
/// these addresses. The real shard address is never tracked in the repo -
/// see Launcher/ShardConfig.local.json.example and Launcher.csproj's
/// EnsureShardConfig target.
/// </summary>
internal static class AppConstants
{
    private const string ShardConfigResourceName = "Launcher.ShardConfig.local.json";

    /// <summary>Folder name under %LOCALAPPDATA% the launcher owns.</summary>
    public const string ShardId = "ServUOShard";

    /// <summary>This launcher build's own version. Compared against manifest.json's minLauncherVersion.</summary>
    public const int LauncherVersion = 1;

    /// <summary>
    /// Patch service base address. Plain HTTP - the shard has no DNS name.
    /// Integrity comes from the manifest signature, not TLS. Read from the
    /// embedded ShardConfig.local.json resource (see Launcher.csproj); a
    /// build with no local config falls back to an obvious placeholder.
    /// </summary>
    public static string PatchServiceBaseUrl { get; }

    /// <summary>Game server address passed to ClassicUO.</summary>
    public static string GameServerIp { get; }

    public const int GameServerPort = 2593;

    /// <summary>UO client version string passed to ClassicUO's -clientversion.</summary>
    public const string ClientVersion = "7.0.108.0";

    static AppConstants()
    {
        (PatchServiceBaseUrl, GameServerIp) = LoadShardConfig();
    }

    private static (string PatchServiceBaseUrl, string GameServerIp) LoadShardConfig()
    {
        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ShardConfigResourceName);
        if (stream is null)
        {
            // ShardConfig.local.json was not embedded. The build should
            // already have failed on this (see EnsureShardConfig in
            // Launcher.csproj); this is a last-resort fallback so a
            // launcher built without the check does not silently point at
            // a real-looking address.
            return ("http://SHARD-ADDRESS-NOT-CONFIGURED.invalid/", "SHARD-ADDRESS-NOT-CONFIGURED");
        }

        using var reader = new StreamReader(stream);
        string json = reader.ReadToEnd();

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        ShardConfig config = JsonSerializer.Deserialize<ShardConfig>(json, options)
            ?? throw new InvalidOperationException("ShardConfig.local.json could not be parsed.");

        return (config.PatchServiceBaseUrl, config.GameServerIp);
    }

    private sealed class ShardConfig
    {
        public string PatchServiceBaseUrl { get; set; } = "";
        public string GameServerIp { get; set; } = "";
    }
}
