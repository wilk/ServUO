# Player Guide

How to install the shard patcher, run it, and fix common problems.

The patcher downloads and verifies the shard's client files, then starts
the game for you. It is not a launcher for ClassicUO in general - use it
to keep your shard files up to date and to play. You may still point the
third-party ClassicUO launcher at the folder the patcher installs, if you
prefer.

## First time

Do these steps once, in order.

1. Install Ultima Online (the classic client). Note the install folder -
   it must contain `client.exe`.
2. Download `ShardPatcher.exe` from the shard's patch service:
   <http://46.62.226.1:8120/ShardPatcher.exe>. The patcher is a
   single file. You do not need any other file next to it.
3. Run `ShardPatcher.exe`.

You do not install ClassicUO yourself. The patcher installs the
shard's own ClassicUO build for you, the first time it runs.

### Warnings from the browser, from Windows and from your antivirus

`ShardPatcher.exe` is not signed with a commercial code-signing
certificate. No cloud reputation database knows this file. Expect a
warning at each of the three steps below. Each one is normal for a
small, self-built tool. None of them means the file is unsafe.

**Your browser may block the download.** Chrome and Edge report an
unsigned executable as not commonly downloaded. Open the browser's
download list, find the blocked file, and choose **Keep** or **Keep
anyway**.

**Windows SmartScreen may block the first run.** Windows shows
"Windows protected your PC". Click **More info**, then click **Run
anyway**. Windows does not recognize the publisher, and it says so.

**Your antivirus may quarantine the file.** An unknown executable that
downloads other executables looks like a generic threat to a scanner
that goes by reputation. The detection name it reports is a generic
one, not a name for this file. If you trust this shard, restore the
file from quarantine and add an exclusion for `ShardPatcher.exe` and
for the folder `%LOCALAPPDATA%\ServUOShard\`.

If you would rather check first, ask shard staff for the file's
SHA-256 and compare it with `certutil -hashfile ShardPatcher.exe
SHA256`.

### What the patcher asks on first run

The patcher opens one folder picker:

1. "Select your Ultima Online installation folder (the folder
   containing client.exe)." Pick the folder from step 1 above.

If you pick a folder that does not contain the right file, the
patcher shows a dialog titled **Invalid folder** and asks again:

- "That folder does not contain client.exe. Pick the folder with your
  Ultima Online installation."

If you close the folder picker without choosing a folder, the patcher
shows "A required folder was not selected. The patcher cannot
continue." and stops. Run the patcher again and pick the folder to
continue.

The patcher saves the folder. It does not ask again on later runs,
unless the saved folder no longer contains `client.exe`.

After the folder picker, the patcher downloads its own ClassicUO
build the same way it downloads any other shard file - see "Every time
after" below. You never install ClassicUO and never pick its folder.

## Every time after

1. Run `ShardPatcher.exe`.
2. The patcher checks the patch service for updates, downloads
   anything changed (including its own ClassicUO build, on first run),
   and verifies it. This is automatic - you do not press anything.
3. The patcher starts ClassicUO for you.

A normal run shows a short log:

```
Checking for updates...
Up to date (manifest version <n>).
Starting ClassicUO...
ClassicUO started.
```

If the shard published new files, you see "Downloading `<file>`
(`<size>` bytes)..." lines before "Starting ClassicUO...". This is
normal - wait for it to finish. On your very first run, this includes
the ClassicUO client itself (about 22 MB), so it takes longer than
later runs.

The **Play** button re-runs the same update-and-start sequence. Use it
to try again after fixing a problem (for example, after Windows
SmartScreen or after freeing up disk space).

## Using the third-party ClassicUO launcher

You do not have to use the patcher's own **Play** button. Some players
prefer the third-party ClassicUO launcher instead, for its profiles,
saved accounts, and autologin.

### Create a profile

Add a profile in the ClassicUO launcher with these fields:

| Field | Value |
|---|---|
| ClassicUO folder | `%LOCALAPPDATA%\ServUOShard\client` (`C:\Users\<your user>\AppData\Local\ServUOShard\client`) |
| Ultima Online folder | the folder from "First time", step 1 |
| Server address | `46.62.226.1` |
| Port | `2593` |
| Client version | `7.0.108.0` |

The **ClassicUO folder** field picks which client runs. The Ultima
Online folder does not decide this. No plugin setting decides this
either. A player who points it anywhere else lands on the wrong
client, with no error message.

The Ultima Online folder holds only the art and the maps. The
ClassicUO folder holds the program. They are never the same folder.

### The patcher still owns updates

The ClassicUO launcher never checks the shard's patch service. It
never downloads a shard file. Run `ShardPatcher.exe` first. It
installs the client. Run `ShardPatcher.exe` again after every shard
update.

If the game misbehaves after a shard update, and you play through the
ClassicUO launcher, your client has likely fallen behind the shard.
Run `ShardPatcher.exe`. It restores every client file to match the
shard, no matter which launcher starts the game.

## Error messages

Every error message stops the patcher before it starts ClassicUO, and
appears in a dialog titled **Cannot start** (except the folder picker
errors above, which use their own dialogs).

| Message | What it means | What to do |
|---|---|---|
| "Could not reach the patch service at `<url>`: ..." | Your internet connection is down, or the shard's patch service is offline. | Check your connection. Try again later. If it keeps failing, ask shard staff if the patch service is up. |
| "Could not verify the manifest signature: ..." | The downloaded manifest is malformed or corrupted in transit. | Try again. If it keeps failing, contact shard staff. |
| "The manifest signature is not valid. Refusing to update or start the client." | The manifest the patch service sent was not signed by the shard's real key. This can mean tampering. | Do not proceed. Contact shard staff before trying again. |
| "The manifest could not be parsed." | The patch service sent a manifest file that is not valid JSON. | Contact shard staff - this is a server-side problem. |
| "This launcher is version `<n>`, but the shard requires at least version `<m>`. Download the new launcher: `<url>`" | Your patcher is too old for this shard's current files. | Download the new `ShardPatcher.exe` from the link shown, and replace your old one. |
| "The downloaded manifest is version `<n>`, but this launcher already applied version `<m>`. Refusing to roll back assets. This may mean the patch service is being tampered with; try again later or contact the shard's staff." | The patch service is offering older files than you already have, and the shard owner did not sign this as an authorized recovery. | Do not proceed. Contact shard staff. |
| "Downloaded file `<path>` does not match its manifest hash." | A downloaded file got corrupted in transit. | Try again. If it keeps failing, contact shard staff. |
| "Could not download `<path>`: ..." | The download failed partway (connection drop, disk full, permission problem). | Check your disk space and connection. Try again. |
| "The shard client is not installed yet. Run the launcher again while connected to the internet so it can download it from `<url>`." | The patcher has not finished installing its own ClassicUO build yet, usually because an earlier run had no internet connection. | Check your connection and run the patcher again. |
| "Unexpected error: ..." | An error the patcher did not expect. | Try again. If it keeps failing, contact shard staff with the exact message. |

## Where the patcher keeps its files

Everything the patcher owns lives under:

```
%LOCALAPPDATA%\ServUOShard\
```

- `settings.json` - your saved UO folder and the last manifest version
  you applied.
- `assets\` - downloaded shard files, cached so they are not
  re-downloaded every run.
- `client\` - the shard's own ClassicUO build: `ClassicUO.exe`,
  `cuo.dll`, its `LICENSE.md`, and the `Data\Client\` and
  `Data\Plugins\` subfolders the shard's files go into.
- `uofilesoverride.txt` - the list ClassicUO reads to find shard-made
  override files.

### Resetting to a clean state

Close the patcher, then delete the whole `%LOCALAPPDATA%\ServUOShard\`
folder. The next patcher run asks for your UO folder again, and
re-downloads every shard file, including the ClassicUO client itself.

## The patcher never touches your Ultima Online install folder

The patcher only reads `client.exe` from that folder, to confirm it
is the right one, and passes the folder's path to ClassicUO. It never
writes, copies, or deletes anything inside your Ultima Online install
folder. Files the shard delivers, and the ClassicUO client itself, go
into `%LOCALAPPDATA%\ServUOShard\` instead. If you already have your
own ClassicUO install elsewhere, the patcher never touches it either -
it starts only its own copy, under `%LOCALAPPDATA%\ServUOShard\client\`.

The Ultima Online folder holds the art and the maps. The client
folder holds the program. They are never the same folder.
