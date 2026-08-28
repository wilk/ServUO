# Shard Owner Guide

How to set up and run the client delivery system: the signed patch
service and the player patcher. Run every command from the repo root,
on the build machine (your own PC, not the VPS).

## First time only

Do these steps once, before your first publish.

### 1. Generate the signing key pair

```
dotnet run --project Tools/PatchBuilder -- generate-key --private <path> --public Launcher/publickey.json
```

Put `<path>` somewhere outside the repo, for example
`Tools/PatchBuilder/signing-key.json` (already gitignored) or a folder
outside the repo entirely. This is the private key. Never commit it.
`Launcher/publickey.json` is the matching public key - the patcher
build embeds it, and it is meant to be committed.

### 2. Create your local config files

These two files are gitignored. Create them from the `.example`
templates and fill in real values:

```
cp Launcher/ShardConfig.local.json.example Launcher/ShardConfig.local.json
cp Tools/publish-assets.conf.example Tools/publish-assets.conf
```

In `Launcher/ShardConfig.local.json`, set:

- `patchServiceBaseUrl` - the public address players' patchers fetch
  from, e.g. `http://<SHARD_IP>:<PATCH_PORT>/`.
- `gameServerIp` - your game server's address.

If this file is missing, the patcher build fails with a clear error
(see the `EnsureShardConfig` target in `Launcher/Launcher.csproj`).

In `Tools/publish-assets.conf`, set:

- `REMOTE_USER`, `REMOTE_HOST` - the ssh user and address of your VPS.
- `SSH_KEY_PATH` - absolute path to the private ssh key that reaches
  the VPS.
- `REMOTE_WEB_ROOT` - the web root nginx serves on the VPS (see
  `Docs/PatchServer.md`).
- `PATCH_SERVICE_URL` - must match `patchServiceBaseUrl` above and
  nginx's listen port.
- `LAUNCHER_DOWNLOAD_URL` - where players download
  `ShardPatcher.exe`.
- `MANIFEST_VERSION`, `MIN_LAUNCHER_VERSION` - start both at `1`.
- `SIGNING_KEY_PATH` - path to the private key from step 1.
- `CLIENT_BUILD_DIR` - absolute path to the shard's ClassicUO build
  output (see "Building the client" below).

### 2a. Build the client

The shard ships its own ClassicUO build, not a stock ClassicUO
install. Build it from `github.com/wilk/ClassicUO`, branch
`shard/main`, on Windows, with Git Bash:

```
git clone --recursive -b shard/main https://github.com/wilk/ClassicUO.git
cd ClassicUO/scripts
bash build-naot.sh
```

This publishes the `net472` bootstrap `ClassicUO.exe` and the
NativeAOT `cuo.dll` into a `bin/dist` folder at the repo root, next to
every other file the build produces.

Copy that build's `LICENSE.md` (ClassicUO is BSD 2-Clause, and binary
redistribution must carry the notice) into `bin/dist`, then point
`CLIENT_BUILD_DIR` in `Tools/publish-assets.conf` at `bin/dist`.
`Tools/publish-assets.sh` refuses to publish, with a clear error, if
`ClassicUO.exe`, `cuo.dll`, or `LICENSE.md` is missing from that
folder.

`CLIENT_BUILD_DIR` must point at this build output folder, and at
nothing else. `Tools/publish-assets.sh` copies every file it finds
there (except `*.pdb`) into `ClientAssets/client/`, and that folder is
published to a public web server that every player reaches over plain
HTTP. Never point `CLIENT_BUILD_DIR` at your ClassicUO repository
checkout - a checkout carries `.git`, local build configs, and can
carry a private key, and every one of those files would go public with
the next publish.

Before your first publish, open `CLIENT_BUILD_DIR` and check it holds
only the client build: `ClassicUO.exe`, `cuo.dll`, `LICENSE.md`, and
the other files `build-naot.sh` produced. No `.git` folder, no
unrelated config file, nothing private.

### 3. Set up the VPS

Follow `Docs/PatchServer.md` for the exact nginx and firewall commands.
It covers the web root, the owning user, the nginx config, and the
firewall rule for the patch port.

### 4. First publish

```
Tools/publish-assets.sh
```

This builds the patcher and plugin, stages the client build from
`CLIENT_BUILD_DIR`, runs PatchBuilder, and uploads everything to the
VPS. See "The publish loop" below for what it does in detail.

### 5. How the patcher reaches players

Give players the `LAUNCHER_DOWNLOAD_URL` link (or point them at your
patch service address - the patcher build also writes `launcher.json`
there with the same URL). A player downloads `ShardPatcher.exe`,
runs it, and it does the rest.

## Every update after

The loop for a normal content update:

1. Edit or add a file under `ClientAssets/` (`overrides/`, `cuo-data/`,
   `plugins/`, or `client/` - see `ClientAssets/README.md` for what
   each folder means). A new client build only needs `CLIENT_BUILD_DIR`
   updated; `Tools/publish-assets.sh` re-stages `ClientAssets/client/`
   from it on every run.
2. Raise `MANIFEST_VERSION` in `Tools/publish-assets.conf` by at least
   1. The patcher rejects a manifest whose version is not higher than
      what it already applied, so this step is required, not optional.
3. Run:
   ```
   Tools/publish-assets.sh
   ```
4. Confirm the manifest is live:
   ```
   curl <PATCH_SERVICE_URL>manifest.json
   ```
   Check the `version` field matches the `MANIFEST_VERSION` you just
   set.

### What happens on the player side, and when

`Tools/publish-assets.sh` uploads asset files first, then
`manifest.json` and `manifest.sig` last - so a player never sees a
manifest pointing at a file that has not arrived yet. The next time a
player runs their patcher (there is no background check - it only
checks on launch), it downloads the new manifest, verifies its
signature, downloads any changed file, verifies each file's SHA-256,
and applies it automatically.

## Players who ask about the ClassicUO launcher

Some players prefer to start the game from the third-party ClassicUO
launcher, for its profiles and saved accounts. Point them at "Using
the third-party ClassicUO launcher" in `Docs/PlayerGuide.md`. It
covers the folder their profile needs and the update rule.

Do not repeat the folder, port, or client version here. They come from
`Launcher/AppConstants.cs`, and a copy here would drift out of sync
with the code.

## Raising MIN_LAUNCHER_VERSION

Raise `MIN_LAUNCHER_VERSION` in `Tools/publish-assets.conf` only when
you publish a new `ShardPatcher.exe` that players must upgrade to -
for example, a patcher bug fix, or a change the old patcher cannot
handle safely. Also bump the `LauncherVersion` constant in
`Launcher/AppConstants.cs` to match, and rebuild before publishing.

A player whose patcher version is below `MIN_LAUNCHER_VERSION` sees:

> "This launcher is version `<their version>`, but the shard requires
> at least version `<n>`. Download the new launcher: `<url>`"

and the patcher refuses to update assets or start ClassicUO until
they download the new one.

## Recovery: undoing a bad publish

If a publish shipped broken files, re-publish an older, known-good
`ClientAssets/` state with an explicit rollback exemption. The
patcher normally refuses any manifest with a lower version than what
it already applied - `--allow-rollback-from` is a signed exception to
that check, so only you (holder of the private key) can grant it.

`Tools/publish-assets.sh` does not expose this flag, so run
`Tools/PatchBuilder` directly for a recovery publish:

```
dotnet run --project Tools/PatchBuilder -c Release -- build \
    --assets <path to the known-good ClientAssets state> \
    --out publish \
    --key <SIGNING_KEY_PATH from your conf> \
    --version <the known-good, lower version number> \
    --min-launcher-version <MIN_LAUNCHER_VERSION> \
    --allow-rollback-from <the highest version any player may have applied - usually the broken publish's own version>
```

Then upload `publish/manifest.json` and `publish/manifest.sig` (and
any asset files that need to revert) to the VPS web root yourself, in
the same order `publish-assets.sh` uses: asset files first, manifest
last. Use the `REMOTE_USER`, `REMOTE_HOST`, `SSH_KEY_PATH`, and
`REMOTE_WEB_ROOT` from your `Tools/publish-assets.conf`.

You can check any manifest/signature pair against a public key without
touching the patcher:

```
dotnet run --project Tools/PatchBuilder -- verify --manifest <path> --sig <path> --pubkey Launcher/publickey.json
```

## What must never be committed

- The private signing key (the file at `SIGNING_KEY_PATH`, e.g.
  `Tools/PatchBuilder/signing-key.json`). Gitignored.
- `Tools/publish-assets.conf` - names your VPS address and ssh key
  path. Gitignored.
- `Launcher/ShardConfig.local.json` - names your real shard address.
  Gitignored.
- Any original Ultima Online client file under `ClientAssets/` - no
  `.mul`, `.idx`, `.uop`, `client.exe`, or any `.dll`/`.pdb` that ships
  inside a stock client or ClassicUO install. `ClientAssets/.gitignore`
  blocks these by pattern; see `ClientAssets/README.md`.
- The shard's own ClassicUO binaries under `ClientAssets/client/`
  (`ClassicUO.exe`, `cuo.dll`, and the rest of the build). They stay
  untracked, the same as `ShardPlugin.dll`. Only `client/LICENSE.md` is
  tracked.

## Troubleshooting

- **"error: `<path>`/publish-assets.conf not found."** - You have not
  created the config file yet. Copy
  `Tools/publish-assets.conf.example` to `Tools/publish-assets.conf`
  and fill it in.
- **"error: `<VAR>` is not set in `<path>`/publish-assets.conf"** - One
  of the required config values is empty. Fill it in.
- **"error: CLIENT_BUILD_DIR (`<path>`) holds no ClassicUO.exe"**, **"...
  holds no cuo.dll"**, or **"... holds no LICENSE.md"** - `CLIENT_BUILD_DIR`
  does not point at a complete ClassicUO build. Rebuild the client from
  `github.com/wilk/ClassicUO`, branch `shard/main`, copy its `LICENSE.md`
  into the same folder, and check the path.
- **"error: launcher build did not produce `<path>`"** or **"error:
  plugin build did not produce `<path>`"** - The `dotnet publish` or
  `dotnet build` step failed before it reached PatchBuilder. Scroll up
  in the script output for the real build error.
- **"missing required option `<name>`"** (from PatchBuilder) - A
  required `--flag` is missing from the PatchBuilder command. Check the
  flag names against the usage text (`dotnet run --project
  Tools/PatchBuilder -- --help`).
- **"'`<path>`' is not under overrides/, cuo-data/, plugins/ or client/
  - don't know which manifest target it maps to."** - You added a file
  directly under `ClientAssets/` instead of inside one of its four
  subfolders. Move it into `overrides/`, `cuo-data/`, `plugins/`, or
  `client/`.
- **"assets directory not found: `<path>`"** - The `--assets` path you
  gave PatchBuilder does not exist. Check the path.
- **"could not parse signing key file: `<path>`"** - `SIGNING_KEY_PATH`
  points at a missing or corrupted key file. Regenerate it with
  `generate-key` if you have lost it (players' patchers will then
  need the new `Launcher/publickey.json` rebuilt and republished).
- **"FAIL: signature does NOT verify against the public key."** (from
  `PatchBuilder verify`) - The manifest, signature, or public key file
  do not match each other. Re-run `build` to regenerate a matching
  pair.
