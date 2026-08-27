using System.Diagnostics;

namespace Launcher;

internal static class ClassicUoLauncher
{
    /// <summary>
    /// Starts ClassicUO.exe with the arguments the shard requires. Assumes
    /// -uofilesoverride and -plugins each take a single file path; if the
    /// shard ships more than one plugin file, ClassicUO's own convention for
    /// separating multiple -plugins paths was not verified against this
    /// launcher's build (see Plugin/README.md) - update this if that turns
    /// out to be wrong.
    /// </summary>
    public static void Start(LauncherSettings settings)
    {
        string exePath = Path.Combine(settings.ClassicUoPath, "ClassicUO.exe");

        if (!File.Exists(exePath))
        {
            throw new UpdateException($"ClassicUO.exe was not found at '{exePath}'.");
        }

        var args = new List<string>
        {
            "-ip", AppConstants.GameServerIp,
            "-port", AppConstants.GameServerPort.ToString(),
            "-uopath", settings.UoPath,
            "-clientversion", AppConstants.ClientVersion
        };

        if (File.Exists(LauncherSettings.OverridesFilePath))
        {
            args.Add("-uofilesoverride");
            args.Add(LauncherSettings.OverridesFilePath);
        }

        string pluginsDir = Path.Combine(settings.ClassicUoPath, "Data", "Plugins");
        if (Directory.Exists(pluginsDir))
        {
            string[] plugins = Directory.GetFiles(pluginsDir, "*.dll");
            if (plugins.Length > 0)
            {
                args.Add("-plugins");
                args.Add(string.Join(',', plugins));
            }
        }

        var startInfo = new ProcessStartInfo(exePath)
        {
            WorkingDirectory = settings.ClassicUoPath,
            UseShellExecute = false
        };

        foreach (string arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        Process.Start(startInfo);
    }
}
