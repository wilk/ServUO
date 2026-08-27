using System.Text.Json;

namespace Launcher;

/// <summary>
/// Local, per-player settings. Lives entirely under %LOCALAPPDATA%\ShardId\.
/// The launcher never writes inside the player's UO install folder.
/// </summary>
internal sealed class LauncherSettings
{
    public string UoPath { get; set; } = "";
    public string ClassicUoPath { get; set; } = "";

    /// <summary>Manifest version last applied successfully. Used to reject a replayed, older-but-validly-signed manifest.</summary>
    public int LastAppliedManifestVersion { get; set; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string RootDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppConstants.ShardId);

    public static string AssetsCacheDirectory => Path.Combine(RootDirectory, "assets");

    public static string SettingsPath => Path.Combine(RootDirectory, "settings.json");

    public static string OverridesFilePath => Path.Combine(RootDirectory, "uofilesoverride.txt");

    public static LauncherSettings? Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<LauncherSettings>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(RootDirectory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
    }
}
