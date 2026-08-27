using System.Runtime.InteropServices;
using CUO_API;

namespace ShardPlugin;

/// <summary>
/// Proves the delivery chain works: launcher downloads this plugin, copies
/// it into ClassicUO's Data/Plugins/, ClassicUO loads it, and it runs. No
/// real game feature - see README.md for what is and is not verified about
/// how ClassicUO finds and calls this entry point.
/// </summary>
public static class PluginMain
{
    // Kept alive for the process lifetime so the native function pointer
    // handed to ClassicUO stays valid.
    private static OnInitialize? _onInitialize;

    /// <summary>
    /// Best-effort entry point: a public static method named Install taking
    /// a PluginHeader by reference, matching the struct's field layout
    /// (verified from cuoapi.dll's IL - see README.md). Whether ClassicUO's
    /// loader actually looks for a method with this exact name was NOT
    /// independently verified.
    /// </summary>
    public static void Install(ref PluginHeader header)
    {
        Log("Install() called - ShardPlugin was loaded and received a PluginHeader.");

        _onInitialize = OnInitializeHandler;
        header.OnInitialize = Marshal.GetFunctionPointerForDelegate(_onInitialize);
    }

    private static void OnInitializeHandler()
    {
        Log("OnInitialize hook fired - ClassicUO called back into ShardPlugin.");
    }

    private static void Log(string message)
    {
        try
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppConstantsShardId);
            Directory.CreateDirectory(dir);
            File.AppendAllText(
                Path.Combine(dir, "plugin.log"),
                $"{DateTime.UtcNow:O} {message}{Environment.NewLine}");
        }
        catch
        {
            // A logging failure must never take the client down.
        }
    }

    // Kept in sync by hand with Launcher/AppConstants.cs's ShardId - the two
    // projects intentionally do not share code.
    private const string AppConstantsShardId = "ServUOShard";
}
