# [ServUO]

[![Build Status](https://travis-ci.com/ServUO/ServUO.svg?branch=master)](https://travis-ci.com/ServUO/ServUO)
[![GitHub issues](https://img.shields.io/github/issues/servuo/servuo.svg)](https://github.com/ServUO/ServUO/issues)
[![GitHub release](https://img.shields.io/github/release/servuo/servuo.svg)](https://github.com/ServUO/ServUO/releases)
[![GitHub repo size](https://img.shields.io/github/repo-size/servuo/servuo.svg)](https://github.com/ServUO/ServUO/)
[![Discord](https://img.shields.io/discord/110970849628000256.svg)](https://discord.gg/0cQjvnFUN26nRt7y)
[![GitHub contributors](https://img.shields.io/github/contributors/servuo/servuo.svg)](https://github.com/ServUO/ServUO/graphs/contributors)
[![GitHub](https://img.shields.io/github/license/servuo/servuo.svg?color=a)](https://github.com/ServUO/ServUO/blob/master/LICENSE)


ServUO is a community driven Ultima Online Server Emulator written in C#.


### Website

[ServUO]


#### Windows

Run `_windebug.bat` for development, attaching a debugger and/or extended output.

Run `_winrelease.bat` for production environment.


#### Other Platforms

Run `make debug` for development, attaching a debugger and/or extended output.

Run `make` or `make release` for production environment. Writing release is optinal by default


### Linux Dependencies

#### Ubuntu / Debian
```
sudo add-apt-repository ppa:dotnet/backports
sudo apt-get update
sudo apt-get -y install zlib1g mono-complete dotnet-sdk-10.0 dotnet-runtime-10.0
```

#### Arch-based
```
sudo pacman -S make mono dotnet-sdk dotnet-runtime
```

### Summary

1. Starting with the `/Config` directory, make sure to read the readme first, then find and edit `Server.cfg` to set up the essentials.
2. Go through the remaining `*.cfg` files to ensure they suit your needs.
3. For Windows, run `_winrelease.bat` to produce `ServUO.exe`, OSX/Linux users may run `make`.
4. Run `ServUO`
5. ???
6. Profit!


    [ServUO]: <https://www.servuo.dev>

### Client Delivery

The shard ships two things beyond the server: a Windows launcher, and a
patch service that feeds it. Together they let the shard owner push
custom client files (hue tables, ClassicUO plugins, config) to every
player without asking players to download and merge files by hand. The
launcher also starts ClassicUO with the right arguments, so a player
never edits a shortcut or a config file themselves.

#### The three roles

- **Build machine** - the shard owner's own PC. It holds the private
  signing key. It builds the launcher and the plugin, builds the signed
  manifest, and pushes everything to the VPS.
- **VPS (patch service)** - a public server that only serves static
  files over plain HTTP. It never builds anything and never holds the
  private signing key.
- **Player's machine** - runs the launcher. The launcher downloads the
  manifest, checks its signature, downloads any changed file, and
  starts ClassicUO.

#### One-time setup

Do this once, on the build machine, before the first publish.

1. Generate the signing key pair:
   `dotnet run --project Tools/PatchBuilder -- generate-key --private <path> --public Launcher/publickey.json`.
   Keep the private key outside the repo. Never commit it.
2. Copy `Launcher/ShardConfig.local.json.example` to
   `Launcher/ShardConfig.local.json` and fill in the real shard address.
   This file is gitignored; the launcher build embeds it into the
   single `.exe` and fails with a clear error if it is missing.
3. Copy `Tools/publish-assets.conf.example` to `Tools/publish-assets.conf`
   and fill in the VPS address, the ssh key path, and the signing key
   path. This file is gitignored too.
4. Set up the VPS: nginx and the firewall rule for the patch port. See
   `Docs/PatchServer.md` for the exact commands.

#### Publishing an update

Run `Tools/publish-assets.sh` from the build machine. It:

1. Builds the launcher (Release, self-contained, single file).
2. Builds the plugin and stages it into `ClientAssets/plugins/`.
3. Runs `Tools/PatchBuilder` over `ClientAssets/`, which hashes every
   file with SHA-256 and signs the resulting manifest with the private
   key.
4. Uploads to the VPS over ssh, in this order: the asset files first,
   then `manifest.json` and `manifest.sig` last. This order means an
   interrupted publish never leaves a manifest that points at a file
   the VPS does not have yet.

#### ClientAssets/

`ClientAssets/` holds only files the shard makes itself. No original
Ultima Online client file belongs here - no `.mul`, `.idx`, `.uop`,
`client.exe`, or any DLL that ships inside a stock client or ClassicUO
install. Three subfolders, one per delivery method:

- `overrides/` - shard-made files that replace or add to a stock UO
  data file (for example a custom `hues.mul`). The launcher passes
  these to ClassicUO with `-uofilesoverride`; the player's real client
  install is never touched.
- `cuo-data/` - files the launcher copies into ClassicUO's own
  `Data/Client/` folder.
- `plugins/` - the compiled ClassicUO plugin(s) built from `Plugin/`.
  The launcher copies these into ClassicUO's `Data/Plugins/` folder.

See `ClientAssets/README.md` for the full rules.

#### Installing and updating (player side)

1. Download `ShardLauncher.exe` from the shard's patch service and run
   it. It is a single file; no other file is needed next to it.
2. On first run, the launcher asks for the Ultima Online install folder
   and the ClassicUO install folder. It saves both under
   `%LOCALAPPDATA%`.
3. On every run, the launcher checks the patch service for a new
   manifest, downloads any changed file, and verifies each file's
   SHA-256 hash before it applies it.
4. The launcher then starts ClassicUO with the right arguments. A
   player never edits a config file or a shortcut by hand.

#### Trust model

The patch service uses plain HTTP - the shard has no DNS name, so a
real TLS certificate is not available. Integrity does not depend on
TLS. `Tools/PatchBuilder` signs the manifest with an ECDSA P-256 key,
and the launcher embeds the matching public key. The launcher refuses
to install anything if the manifest signature does not check out, or
if a downloaded file's SHA-256 does not match what the signed manifest
says.

#### Reference

- `ClientAssets/` - shard-made client files only, grouped by how the launcher delivers them.
- `Tools/PatchBuilder/` - builds and signs the asset manifest.
- `Launcher/` - the player-facing Windows launcher that updates assets and starts ClassicUO.
- `Plugin/` - the ClassicUO plugin the launcher delivers.
- `Docs/PatchServer.md` - the VPS-side patch service setup.
- `Docs/PlayerGuide.md` - install and run the launcher, as a player.
- `Docs/ShardOwnerGuide.md` - set up and publish updates, as the shard owner.
- `CLAUDE.md` - the two-sides rule: when a change on one side needs a matching change on the other.
