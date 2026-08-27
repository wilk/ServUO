using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PatchBuilder;

/// <summary>
/// Walks ClientAssets/, writes a signed manifest.json + manifest.sig pair.
/// See Docs/PatchServer.md and ClientAssets/README.md for the delivery model.
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        try
        {
            switch (args[0])
            {
                case "generate-key":
                    return GenerateKey(args);
                case "build":
                    return Build(args);
                case "verify":
                    return Verify(args);
                case "-h":
                case "--help":
                    PrintUsage();
                    return 0;
                default:
                    Console.Error.WriteLine($"Unknown command '{args[0]}'.");
                    PrintUsage();
                    return 1;
            }
        }
        catch (PatchBuilderException ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("PatchBuilder - builds and signs the shard's client asset manifest.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  PatchBuilder generate-key --private <path> --public <path>");
        Console.WriteLine("  PatchBuilder build --assets <dir> --out <dir> --key <privateKeyPath>");
        Console.WriteLine("                --version <int> --min-launcher-version <int>");
        Console.WriteLine("                [--allow-rollback-from <int>]");
        Console.WriteLine("  PatchBuilder verify --manifest <path> --sig <path> --pubkey <path>");
        Console.WriteLine();
        Console.WriteLine("--allow-rollback-from <int>");
        Console.WriteLine("    Recovery only. Use when a published update was broken and you are");
        Console.WriteLine("    re-publishing an older, known-good manifest to undo it. Set this to");
        Console.WriteLine("    the highest --version any client may already have applied (usually");
        Console.WriteLine("    the broken manifest's own --version, or higher). Clients that already");
        Console.WriteLine("    applied a version at or below this value accept this manifest even");
        Console.WriteLine("    though its --version is lower than what they applied. Leave unset for");
        Console.WriteLine("    every normal publish - it grants no exemption by default, and it only");
        Console.WriteLine("    means anything because this manifest is signed with your private key.");
    }

    // ---- generate-key ---------------------------------------------------

    private static int GenerateKey(string[] args)
    {
        string privatePath = RequireOption(args, "--private");
        string publicPath = RequireOption(args, "--public");

        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        ECParameters priv = ecdsa.ExportParameters(true);
        ECParameters pub = ecdsa.ExportParameters(false);

        var privateKey = new SigningKey
        {
            Curve = "nistP256",
            D = Convert.ToBase64String(priv.D!),
            X = Convert.ToBase64String(priv.Q.X!),
            Y = Convert.ToBase64String(priv.Q.Y!)
        };

        var publicKey = new PublicKey
        {
            Curve = "nistP256",
            X = Convert.ToBase64String(pub.Q.X!),
            Y = Convert.ToBase64String(pub.Q.Y!)
        };

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(privatePath))!);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(publicPath))!);

        File.WriteAllText(privatePath, JsonSerializer.Serialize(privateKey, JsonOptions));
        File.WriteAllText(publicPath, JsonSerializer.Serialize(publicKey, JsonOptions));

        Console.WriteLine($"Wrote private key: {privatePath}");
        Console.WriteLine($"Wrote public key:  {publicPath}");
        Console.WriteLine("The private key is a secret. Never commit it. Keep it out of the repo.");
        return 0;
    }

    // ---- build ------------------------------------------------------------

    private static int Build(string[] args)
    {
        string assetsDir = RequireOption(args, "--assets");
        string outDir = RequireOption(args, "--out");
        string keyPath = RequireOption(args, "--key");
        int version = int.Parse(RequireOption(args, "--version"));
        int minLauncherVersion = int.Parse(RequireOption(args, "--min-launcher-version"));
        int allowRollbackFrom = int.Parse(OptionalOption(args, "--allow-rollback-from") ?? "0");

        assetsDir = Path.GetFullPath(assetsDir);
        if (!Directory.Exists(assetsDir))
        {
            throw new PatchBuilderException($"assets directory not found: {assetsDir}");
        }

        var files = new List<ManifestFileEntry>();

        foreach (string filePath in Directory.EnumerateFiles(assetsDir, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(assetsDir, filePath).Replace('\\', '/');

            if (ShouldSkip(relative))
            {
                continue;
            }

            string target = ClassifyTarget(relative);
            string overrideKey = target == "override"
                ? relative[(relative.IndexOf('/') + 1)..]
                : string.Empty;

            byte[] bytes = File.ReadAllBytes(filePath);
            string sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

            files.Add(new ManifestFileEntry
            {
                Path = relative,
                Size = bytes.LongLength,
                Sha256 = sha256,
                OverrideKey = overrideKey,
                Target = target
            });

            Console.WriteLine($"  {target,-8} {relative} ({bytes.LongLength} bytes)");
        }

        files.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));

        var manifest = new Manifest
        {
            Version = version,
            MinLauncherVersion = minLauncherVersion,
            AllowRollbackFrom = allowRollbackFrom,
            Files = files
        };

        Directory.CreateDirectory(outDir);
        string manifestPath = Path.Combine(outDir, "manifest.json");
        string sigPath = Path.Combine(outDir, "manifest.sig");

        byte[] manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, JsonOptions);
        File.WriteAllBytes(manifestPath, manifestBytes);

        SigningKey key = LoadSigningKey(keyPath);
        using ECDsa ecdsa = ToEcdsa(key);
        byte[] signature = ecdsa.SignData(manifestBytes, HashAlgorithmName.SHA256);
        File.WriteAllBytes(sigPath, signature);

        Console.WriteLine();
        Console.WriteLine($"{files.Count} file(s) in manifest.");
        Console.WriteLine($"Wrote {manifestPath}");
        Console.WriteLine($"Wrote {sigPath}");
        return 0;
    }

    private static bool ShouldSkip(string relativePath)
    {
        string name = Path.GetFileName(relativePath);
        return name is ".gitkeep" or ".gitignore" or "README.md";
    }

    private static string ClassifyTarget(string relativePath)
    {
        string top = relativePath.Split('/')[0];
        return top switch
        {
            "overrides" => "override",
            "cuo-data" => "cuoData",
            "plugins" => "plugin",
            "client" => "client",
            _ => throw new PatchBuilderException(
                $"'{relativePath}' is not under overrides/, cuo-data/, plugins/ or client/ - " +
                "don't know which manifest target it maps to.")
        };
    }

    // ---- verify -----------------------------------------------------------

    private static int Verify(string[] args)
    {
        string manifestPath = RequireOption(args, "--manifest");
        string sigPath = RequireOption(args, "--sig");
        string pubKeyPath = RequireOption(args, "--pubkey");

        byte[] manifestBytes = File.ReadAllBytes(manifestPath);
        byte[] signature = File.ReadAllBytes(sigPath);

        string pubJson = File.ReadAllText(pubKeyPath);
        var pub = JsonSerializer.Deserialize<PublicKey>(pubJson, JsonOptions)
                  ?? throw new PatchBuilderException($"could not parse public key file: {pubKeyPath}");

        using ECDsa ecdsa = ToEcdsa(pub);
        bool ok = ecdsa.VerifyData(manifestBytes, signature, HashAlgorithmName.SHA256);

        if (ok)
        {
            Console.WriteLine("OK: signature verifies against the public key.");
            return 0;
        }

        Console.Error.WriteLine("FAIL: signature does NOT verify against the public key.");
        return 1;
    }

    // ---- shared helpers -----------------------------------------------------

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static SigningKey LoadSigningKey(string path)
    {
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SigningKey>(json, JsonOptions)
               ?? throw new PatchBuilderException($"could not parse signing key file: {path}");
    }

    private static ECDsa ToEcdsa(SigningKey key)
    {
        var parameters = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            D = Convert.FromBase64String(key.D),
            Q = new ECPoint
            {
                X = Convert.FromBase64String(key.X),
                Y = Convert.FromBase64String(key.Y)
            }
        };
        return ECDsa.Create(parameters);
    }

    private static ECDsa ToEcdsa(PublicKey key)
    {
        var parameters = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = Convert.FromBase64String(key.X),
                Y = Convert.FromBase64String(key.Y)
            }
        };
        return ECDsa.Create(parameters);
    }

    private static string RequireOption(string[] args, string name)
    {
        return OptionalOption(args, name)
               ?? throw new PatchBuilderException($"missing required option {name}");
    }

    private static string? OptionalOption(string[] args, string name)
    {
        for (int i = 1; i < args.Length - 1; i++)
        {
            if (args[i] == name)
            {
                return args[i + 1];
            }
        }

        return null;
    }
}

internal sealed class PatchBuilderException(string message) : Exception(message);

internal sealed class SigningKey
{
    public string Curve { get; set; } = "nistP256";
    public string D { get; set; } = "";
    public string X { get; set; } = "";
    public string Y { get; set; } = "";
}

internal sealed class PublicKey
{
    public string Curve { get; set; } = "nistP256";
    public string X { get; set; } = "";
    public string Y { get; set; } = "";
}

internal sealed class Manifest
{
    public int Version { get; set; }
    public int MinLauncherVersion { get; set; }

    /// <summary>
    /// Signed recovery escape hatch for Launcher/AssetUpdater.cs's
    /// anti-rollback check. 0 (the default) means no rollback is
    /// authorized. Set only via --allow-rollback-from, when re-publishing
    /// an older, known-good manifest to recover from a broken update -
    /// see the --allow-rollback-from usage text above.
    /// </summary>
    public int AllowRollbackFrom { get; set; }

    public List<ManifestFileEntry> Files { get; set; } = [];
}

internal sealed class ManifestFileEntry
{
    public string Path { get; set; } = "";
    public long Size { get; set; }
    public string Sha256 { get; set; } = "";
    public string OverrideKey { get; set; } = "";
    public string Target { get; set; } = "";
}
