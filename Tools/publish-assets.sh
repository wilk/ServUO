#!/usr/bin/env bash
# publish-assets.sh
#
# NOTE on line endings: every other new text file in this issue is written
# with CRLF line endings to match this repo's existing convention. This
# file is the one deliberate exception - it is kept LF-only because CRLF
# line endings inside a bash script (not just the shebang) break variable
# assignments, comparisons and control flow, not only script invocation
# via the shebang. See the issue #51 implementation notes.
#
# Builds the Launcher and the Plugin (Release), runs PatchBuilder over
# ClientAssets/, then publishes everything to the VPS web root with rsync
# over ssh. ClientAssets files are copied FIRST, manifest.json and
# manifest.sig LAST, so a partial/interrupted publish never leaves a
# manifest pointing at files that are not there yet.
#
# Config comes from Tools/publish-assets.conf (gitignored). Copy
# Tools/publish-assets.conf.example to get started.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
CONF_PATH="$SCRIPT_DIR/publish-assets.conf"

if [[ ! -f "$CONF_PATH" ]]; then
    echo "error: $CONF_PATH not found." >&2
    echo "Copy Tools/publish-assets.conf.example to Tools/publish-assets.conf and fill it in." >&2
    exit 1
fi

# shellcheck source=/dev/null
source "$CONF_PATH"

for var in REMOTE_USER REMOTE_HOST SSH_KEY_PATH REMOTE_WEB_ROOT PATCH_SERVICE_URL \
           LAUNCHER_DOWNLOAD_URL MANIFEST_VERSION MIN_LAUNCHER_VERSION SIGNING_KEY_PATH \
           CLIENT_BUILD_DIR; do
    if [[ -z "${!var:-}" ]]; then
        echo "error: $var is not set in $CONF_PATH" >&2
        exit 1
    fi
done

cd "$REPO_ROOT"

echo "==> Building Launcher (Release, win-x64, self-contained, single file)"
dotnet publish Launcher -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

echo "==> Building Plugin (Release)"
dotnet build Plugin -c Release

LAUNCHER_PUBLISH_DIR="$REPO_ROOT/Launcher/bin/Release/net10.0-windows/win-x64/publish"
LAUNCHER_EXE="$LAUNCHER_PUBLISH_DIR/ShardLauncher.exe"
PLUGIN_DLL="$REPO_ROOT/Plugin/bin/Release/net10.0/ShardPlugin.dll"

if [[ ! -f "$LAUNCHER_EXE" ]]; then
    echo "error: launcher build did not produce $LAUNCHER_EXE" >&2
    exit 1
fi

if [[ ! -f "$PLUGIN_DLL" ]]; then
    echo "error: plugin build did not produce $PLUGIN_DLL" >&2
    exit 1
fi

echo "==> Staging plugin into ClientAssets/plugins/"
mkdir -p "$REPO_ROOT/ClientAssets/plugins"
cp -f "$PLUGIN_DLL" "$REPO_ROOT/ClientAssets/plugins/ShardPlugin.dll"

if [[ ! -f "$CLIENT_BUILD_DIR/ClassicUO.exe" ]]; then
    echo "error: CLIENT_BUILD_DIR ($CLIENT_BUILD_DIR) holds no ClassicUO.exe" >&2
    exit 1
fi

if [[ ! -f "$CLIENT_BUILD_DIR/cuo.dll" ]]; then
    echo "error: CLIENT_BUILD_DIR ($CLIENT_BUILD_DIR) holds no cuo.dll" >&2
    exit 1
fi

if [[ ! -f "$CLIENT_BUILD_DIR/LICENSE.md" ]]; then
    echo "error: CLIENT_BUILD_DIR ($CLIENT_BUILD_DIR) holds no LICENSE.md - the ClassicUO fork's" >&2
    echo "BSD 2-Clause notice must ship with the binaries. Copy it there from the client repo." >&2
    exit 1
fi

echo "==> Staging client build into ClientAssets/client/ (skipping *.pdb, pruning removed files)"
mkdir -p "$REPO_ROOT/ClientAssets/client"
rsync -a --delete \
    --exclude='*.pdb' \
    --exclude='.gitkeep' --exclude='.gitignore' --exclude='README.md' --exclude='LICENSE.md' \
    "$CLIENT_BUILD_DIR/" "$REPO_ROOT/ClientAssets/client/"
cp -f "$CLIENT_BUILD_DIR/LICENSE.md" "$REPO_ROOT/ClientAssets/client/LICENSE.md"

STAGING_DIR="$REPO_ROOT/publish"
rm -rf "$STAGING_DIR"
mkdir -p "$STAGING_DIR"

echo "==> Running PatchBuilder"
dotnet run --project Tools/PatchBuilder -c Release -- build \
    --assets "$REPO_ROOT/ClientAssets" \
    --out "$STAGING_DIR" \
    --key "$REPO_ROOT/$SIGNING_KEY_PATH" \
    --version "$MANIFEST_VERSION" \
    --min-launcher-version "$MIN_LAUNCHER_VERSION"

LAUNCHER_SHA256="$(sha256sum "$LAUNCHER_EXE" | awk '{print $1}')"

cat > "$STAGING_DIR/launcher.json" <<JSON
{
  "version": $MIN_LAUNCHER_VERSION,
  "sha256": "$LAUNCHER_SHA256",
  "downloadUrl": "$LAUNCHER_DOWNLOAD_URL"
}
JSON

SSH_OPTS=(-i "$SSH_KEY_PATH" -o StrictHostKeyChecking=accept-new)
REMOTE="${REMOTE_USER}@${REMOTE_HOST}"

echo "==> Publishing ClientAssets/ to $REMOTE:$REMOTE_WEB_ROOT (assets first)"
rsync -av --exclude='.gitkeep' --exclude='.gitignore' --exclude='README.md' \
    -e "ssh ${SSH_OPTS[*]}" \
    "$REPO_ROOT/ClientAssets/" "$REMOTE:$REMOTE_WEB_ROOT/"

echo "==> Publishing launcher .exe and launcher.json"
rsync -av -e "ssh ${SSH_OPTS[*]}" \
    "$LAUNCHER_EXE" "$REMOTE:$REMOTE_WEB_ROOT/ShardLauncher.exe"
rsync -av -e "ssh ${SSH_OPTS[*]}" \
    "$STAGING_DIR/launcher.json" "$REMOTE:$REMOTE_WEB_ROOT/launcher.json"

echo "==> Publishing manifest.json + manifest.sig (LAST - this is what makes the update live)"
rsync -av -e "ssh ${SSH_OPTS[*]}" \
    "$STAGING_DIR/manifest.json" "$REMOTE:$REMOTE_WEB_ROOT/manifest.json"
rsync -av -e "ssh ${SSH_OPTS[*]}" \
    "$STAGING_DIR/manifest.sig" "$REMOTE:$REMOTE_WEB_ROOT/manifest.sig"

echo "==> Done. Patch service: $PATCH_SERVICE_URL"
