# ClientAssets

This folder holds only files the shard makes itself. It never holds an
original Ultima Online client file.

PatchBuilder (see `Tools/PatchBuilder/`) walks this folder, hashes every
file with SHA-256, and writes `manifest.json` for the launcher to read.
The three subfolders map to the three delivery targets in the manifest.

## overrides/

Shard-made files that replace or add to a stock UO data file, for example
a custom `hues.mul`. The launcher writes each file's path into
`uofilesoverride.txt` in the format `overrideKey=<absolute path>`, and
passes that file to ClassicUO with `-uofilesoverride`. ClassicUO reads
the named file at runtime; the original client install on disk is never
touched.

Maps to manifest `target: "override"`. The `overrideKey` is the file's
path relative to `overrides/`, using forward slashes (for example
`hues.mul`).

## cuo-data/

Files the launcher copies into ClassicUO's own `Data/Client/` folder
(client-side config the shard wants every player to share, not a stock
UO data file).

Maps to manifest `target: "cuoData"`.

## plugins/

The compiled ClassicUO plugin(s) built from `Plugin/`. The launcher
copies these into ClassicUO's `Data/Plugins/` folder and starts
ClassicUO with `-plugins <path>`.

Maps to manifest `target: "plugin"`.

## What must never go here

No file with an original Ultima Online client name or extension. That
includes `.mul`, `.idx`, `.uop` data files under any name (`art.mul`,
`gumpart.mul`, `map*.mul`, `staidx*.mul`, ...), `client.exe`, and any
`*.dll`/`*.pdb` that ships inside a stock client or ClassicUO install.

`.gitignore` in this folder blocks those patterns by name, then
allowlists the specific file names the shard is known to produce (for
example `overrides/hues.mul`, because a shard-made hue file legitimately
carries a stock file name). If PatchBuilder or a reviewer is unsure
whether a file is shard-made, it does not belong here.
