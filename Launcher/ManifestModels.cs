namespace Launcher;

/// <summary>
/// Mirrors the JSON shapes PatchBuilder writes. Keep in sync with
/// Tools/PatchBuilder/Program.cs.
/// </summary>
internal sealed class Manifest
{
    public int Version { get; set; }
    public int MinLauncherVersion { get; set; }

    /// <summary>
    /// Signed recovery escape hatch for the anti-rollback check in
    /// AssetUpdater.RunAsync. 0 (the default) means "no rollback
    /// authorized" - a normal publish never sets this and old manifests
    /// replayed by an attacker never carry it either, since it is only
    /// meaningful inside a manifest signed by the shard owner's key.
    ///
    /// If the shard owner publishes a broken update and needs to
    /// re-publish an older, known-good manifest, they set this field on
    /// that manifest to the highest LastAppliedManifestVersion a stuck
    /// client may have (typically the broken version's own Version
    /// number, or higher). A client whose already-applied version is
    /// less than or equal to this value is allowed to accept this
    /// manifest even though its own Version is lower than what that
    /// client already applied. A manifest that leaves this at 0 gets no
    /// exemption, so a replayed old-but-validly-signed manifest without
    /// this field stays blocked exactly as before.
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

    /// <summary>One of "override", "cuoData", "plugin", "client".</summary>
    public string Target { get; set; } = "";
}

internal sealed class EmbeddedPublicKey
{
    public string Curve { get; set; } = "nistP256";
    public string X { get; set; } = "";
    public string Y { get; set; } = "";
}

/// <summary>Optional file the publish script writes alongside the manifest, used only to show a download link when the launcher itself is too old.</summary>
internal sealed class LauncherInfo
{
    public int Version { get; set; }
    public string Sha256 { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
}
