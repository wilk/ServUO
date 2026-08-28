namespace Launcher;

internal sealed class MainForm : Form
{
    private readonly TextBox _log = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill
    };

    private readonly Button _playButton = new()
    {
        Text = "Play",
        Dock = DockStyle.Bottom,
        Height = 40
    };

    private LauncherSettings _settings = null!;

    public MainForm()
    {
        Text = $"Shard Launcher (v{AppConstants.LauncherVersion})";
        Width = 640;
        Height = 400;
        Controls.Add(_log);
        Controls.Add(_playButton);

        _playButton.Click += async (_, _) => await RunAsync();
        Load += async (_, _) =>
        {
            EnsureSettings();
            await RunAsync();
        };
    }

    private void Log(string message) => _log.AppendText(message + Environment.NewLine);

    private void EnsureSettings()
    {
        _settings = LauncherSettings.Load() ?? new LauncherSettings();

        if (!IsValidUoPath(_settings.UoPath))
        {
            _settings.UoPath = PromptForFolder(
                "Select your Ultima Online installation folder (the folder containing client.exe).",
                @"C:\Program Files (x86)\Ultima Online Classic",
                IsValidUoPath,
                "That folder does not contain client.exe. Pick the folder with your Ultima Online installation.");
        }

        _settings.Save();
    }

    private static bool IsValidUoPath(string? path) =>
        !string.IsNullOrWhiteSpace(path) && File.Exists(Path.Combine(path, "client.exe"));

    private string PromptForFolder(string description, string initialPath, Func<string, bool> isValid, string invalidMessage)
    {
        string path = initialPath;

        while (true)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = description,
                UseDescriptionForTitle = true,
                SelectedPath = Directory.Exists(path) ? path : ""
            };

            DialogResult result = dialog.ShowDialog(this);
            if (result != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                throw new UpdateException("A required folder was not selected. The launcher cannot continue.");
            }

            if (isValid(dialog.SelectedPath))
            {
                return dialog.SelectedPath;
            }

            MessageBox.Show(this, invalidMessage, "Invalid folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            path = dialog.SelectedPath;
        }
    }

    private async Task RunAsync()
    {
        _playButton.Enabled = false;

        try
        {
            Log("Checking for updates...");
            var updater = new AssetUpdater(_settings, Log);
            Manifest manifest = await updater.RunAsync(CancellationToken.None);
            Log($"Up to date (manifest version {manifest.Version}).");

            Log("Starting ClassicUO...");
            ClassicUoLauncher.Start(_settings);
            Log("ClassicUO started.");
        }
        catch (UpdateException ex)
        {
            Log("ERROR: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Cannot start", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            Log("ERROR: " + ex.Message);
            MessageBox.Show(this, $"Unexpected error: {ex.Message}", "Cannot start", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _playButton.Enabled = true;
        }
    }
}
