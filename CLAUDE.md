# CLAUDE.md

This repo has two sides. Read this before you change anything.

## The two sides

- **Server side** - `Server/`, `Scripts/`, `Config/`, `Data/`, `Ultima/`.
  This is ServUO: the game server and its content.
- **Client side** - `ClientAssets/`, `Launcher/`, `Plugin/`,
  `Tools/PatchBuilder/`, `Tools/publish-assets.sh`, `Docs/`. This is the
  custom launcher and the asset patch service that delivers client files
  to players.

## The core rule

When a change touches one side, ask whether the other side needs a
matching change. Do not assume a server change is invisible to the
client, or that a client change is purely cosmetic.

Concrete cases in this repo:

- A new hue number in `Scripts/` means nothing to a player until a
  matching client hue file ships through `ClientAssets/overrides/hues.mul`
  and gets published. See `ClientAssets/README.md`.
- A client-side rule that no server packet can express (for example, a
  ClassicUO plugin behavior or launcher-side config) belongs in
  `ClientAssets/cuo-data/` or `Plugin/`, not in `Server/` or `Scripts/`.
- A new `ClientAssets/` file does nothing for a player until
  `Tools/publish-assets.sh` runs. See "Delivery" below.

## Which side is authoritative

The server owns every game rule. `Server/` and `Scripts/` are the single
source of truth for what is legal and what happens in the game.

Client assets change only what the player sees or how the client
connects - never what is allowed. A player who runs an old or
hand-edited `ClientAssets/` file must gain no game advantage. The server
must reject or ignore anything a modified client tries that breaks a
game rule.

## Delivery chain

A `ClientAssets/` change is not live until it is published. The shard
owner edits a file, raises `MANIFEST_VERSION`, and runs
`Tools/publish-assets.sh` from the build machine. That script builds the
launcher and plugin, signs a manifest with the private key, and uploads
to the VPS. A player's launcher only checks for updates when it runs -
there is no background push. See `Docs/ShardOwnerGuide.md` and
`Docs/PatchServer.md` for the full chain.

## Never commit these

- The private signing key, e.g. `Tools/PatchBuilder/signing-key.json`.
- `Tools/publish-assets.conf` - names the real VPS address and ssh key
  path.
- `Launcher/ShardConfig.local.json` - names the real shard address.
- `Plugin/CuoApi.local.props` - a local build path.
- Any original Ultima Online client file under `ClientAssets/` - no
  `.mul`, `.idx`, `.uop`, `client.exe`, or any stock `.dll`/`.pdb`.

All of these are gitignored. See the root `.gitignore` and
`ClientAssets/.gitignore`.

## Line endings

This repo uses CRLF. The one deliberate exception is
`Tools/publish-assets.sh` - it is a shell script, and it must keep LF
line endings to run correctly. The script's own header comment records
this.

## Before you open a pull request

- [ ] Did this change touch `Server/` or `Scripts/`? If yes, check
      whether `ClientAssets/` needs a matching update (hues, plugin
      behavior, config).
- [ ] Did this change touch `ClientAssets/`, `Launcher/`, or `Plugin/`?
      If yes, confirm it changes only presentation or connection, never
      a game rule.
- [ ] Does a `ClientAssets/` change need `MANIFEST_VERSION` raised
      before publish? See `Docs/ShardOwnerGuide.md`.
- [ ] Check no gitignored file (signing key, local configs) was staged.
- [ ] New text files use CRLF, unless they are a shell script.
- [ ] Read the relevant guide before you write docs: `README.md`
      (client delivery section), `Docs/PlayerGuide.md`,
      `Docs/ShardOwnerGuide.md`, `Docs/PatchServer.md`,
      `ClientAssets/README.md`, `Plugin/README.md`.
