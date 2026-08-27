# Player Guide

How to install the shard launcher, run it, and fix common problems.

## First time

Do these steps once, in order.

1. Install Ultima Online (the classic client). Note the install folder -
   it must contain `client.exe`.
2. Install ClassicUO. Note its install folder - it must contain
   `ClassicUO.exe`.
3. Download `ShardLauncher.exe` from the shard's patch service. Ask
   shard staff for the download link if you do not have it. The
   launcher is a single file. You do not need any other file next to
   it.
4. Run `ShardLauncher.exe`.

### Windows SmartScreen

`ShardLauncher.exe` is not signed with a commercial code-signing
certificate. Windows may show "Windows protected your PC" the first
time you run it. Click **More info**, then click **Run anyway**. This
warning is normal for a small, self-built tool like this launcher. It
does not mean the file is unsafe by itself - it means Windows does not
recognize the publisher.

### What the launcher asks on first run

The launcher opens two folder pickers, in this order:

1. "Select your Ultima Online installation folder (the folder
   containing client.exe)." Pick the folder from step 1 above.
2. "Select your ClassicUO installation folder (the folder containing
   ClassicUO.exe)." Pick the folder from step 2 above.

If you pick a folder that does not contain the right file, the
launcher shows a dialog titled **Invalid folder** and asks again:

- "That folder does not contain client.exe. Pick the folder with your
  Ultima Online installation."
- "That folder does not contain ClassicUO.exe. Pick the folder with
  your ClassicUO installation."

If you close a folder picker without choosing a folder, the launcher
shows "A required folder was not selected. The launcher cannot
continue." and stops. Run the launcher again and pick both folders to
continue.

The launcher saves both folders. It does not ask again on later runs,
unless the saved folder no longer contains the right file.

## Every time after

1. Run `ShardLauncher.exe`.
2. The launcher checks the patch service for updates, downloads
   anything changed, and verifies it. This is automatic - you do not
   press anything.
3. The launcher starts ClassicUO for you.

A normal run shows a short log:

```
Checking for updates...
Up to date (manifest version <n>).
Starting ClassicUO...
ClassicUO started.
```

If the shard published new files, you see "Downloading `<file>`
(`<size>` bytes)..." lines before "Starting ClassicUO...". This is
normal - wait for it to finish.

The **Play** button re-runs the same update-and-start sequence. Use it
to try again after fixing a problem (for example, after Windows
SmartScreen or after freeing up disk space).

## Error messages

Every error message stops the launcher before it starts ClassicUO, and
appears in a dialog titled **Cannot start** (except the two folder
picker errors above, which use their own dialogs).

| Message | What it means | What to do |
|---|---|---|
| "Could not reach the patch service at `<url>`: ..." | Your internet connection is down, or the shard's patch service is offline. | Check your connection. Try again later. If it keeps failing, ask shard staff if the patch service is up. |
| "Could not verify the manifest signature: ..." | The downloaded manifest is malformed or corrupted in transit. | Try again. If it keeps failing, contact shard staff. |
| "The manifest signature is not valid. Refusing to update or start the client." | The manifest the patch service sent was not signed by the shard's real key. This can mean tampering. | Do not proceed. Contact shard staff before trying again. |
| "The manifest could not be parsed." | The patch service sent a manifest file that is not valid JSON. | Contact shard staff - this is a server-side problem. |
| "This launcher is version `<n>`, but the shard requires at least version `<m>`. Download the new launcher: `<url>`" | Your launcher is too old for this shard's current files. | Download the new `ShardLauncher.exe` from the link shown, and replace your old one. |
| "The downloaded manifest is version `<n>`, but this launcher already applied version `<m>`. Refusing to roll back assets. This may mean the patch service is being tampered with; try again later or contact the shard's staff." | The patch service is offering older files than you already have, and the shard owner did not sign this as an authorized recovery. | Do not proceed. Contact shard staff. |
| "Downloaded file `<path>` does not match its manifest hash." | A downloaded file got corrupted in transit. | Try again. If it keeps failing, contact shard staff. |
| "Could not download `<path>`: ..." | The download failed partway (connection drop, disk full, permission problem). | Check your disk space and connection. Try again. |
| "ClassicUO installation folder is not set." | Your saved ClassicUO folder setting is missing or empty. | Reset the launcher to a clean state (see below) and pick the folder again. |
| "ClassicUO.exe was not found at `<path>`." | ClassicUO was moved, renamed, or uninstalled since you picked its folder. | Reinstall or move ClassicUO back, or reset the launcher (see below) to pick the folder again. |
| "Unexpected error: ..." | An error the launcher did not expect. | Try again. If it keeps failing, contact shard staff with the exact message. |

## Where the launcher keeps its files

Everything the launcher owns lives under:

```
%LOCALAPPDATA%\ServUOShard\
```

- `settings.json` - your saved UO folder, ClassicUO folder, and the
  last manifest version you applied.
- `assets\` - downloaded shard files, cached so they are not
  re-downloaded every run.
- `uofilesoverride.txt` - the list ClassicUO reads to find shard-made
  override files.

### Resetting to a clean state

Close the launcher, then delete the whole `%LOCALAPPDATA%\ServUOShard\`
folder. The next launcher run asks for your UO and ClassicUO folders
again, and re-downloads every shard file.

## The launcher never touches your Ultima Online install folder

The launcher only reads `client.exe` from that folder, to confirm it
is the right one, and passes the folder's path to ClassicUO. It never
writes, copies, or deletes anything inside your Ultima Online install
folder. Files the shard delivers go into `%LOCALAPPDATA%\ServUOShard\`
and into your ClassicUO folder's `Data\Client\` and `Data\Plugins\`
subfolders instead.
