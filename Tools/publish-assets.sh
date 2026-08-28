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

echo "==> Staging client build into ClientAssets/client/ (allowlist only, pruning removed/skipped files)"
mkdir -p "$REPO_ROOT/ClientAssets/client"

# Only these files are legitimate output of the shard's ClassicUO build.
# This is an allowlist, not a denylist: anything CLIENT_BUILD_DIR holds that
# is not named here - a stray signing key, a .env, a .git folder, a local
# debug config - is never staged, whatever it is called. See issue #55.
CLIENT_BUILD_ALLOWLIST=(
    "ClassicUO.exe"
    "ClassicUO.exe.config"
    "cuo.dll"
    "cuoapi.dll"
    "FNA.dll.config"
    "FNA3D.dll"
    "FAudio.dll"
    "SDL3.dll"
    "zlib.dll"
    "libtheorafile.dll"
    "System.Buffers.dll"
    "System.Memory.dll"
    "System.Numerics.Vectors.dll"
    "System.Runtime.CompilerServices.Unsafe.dll"
    "LICENSE.md"
)

RSYNC_CLIENT_INCLUDES=()
for name in "${CLIENT_BUILD_ALLOWLIST[@]}"; do
    RSYNC_CLIENT_INCLUDES+=(--include="/$name")
done

echo "    checking CLIENT_BUILD_DIR against the allowlist:"
while IFS= read -r -d '' entry; do
    name="$(basename "$entry")"
    keep=0
    for allowed in "${CLIENT_BUILD_ALLOWLIST[@]}"; do
        if [[ "$name" == "$allowed" ]]; then
            keep=1
            break
        fi
    done
    if [[ "$keep" -eq 0 ]]; then
        echo "    skipped (not in the client build allowlist): $name"
    fi
done < <(find "$CLIENT_BUILD_DIR" -mindepth 1 -maxdepth 1 -print0)

rsync -a --delete --delete-excluded \
    --filter='P .gitkeep' --filter='P .gitignore' --filter='P README.md' \
    "${RSYNC_CLIENT_INCLUDES[@]}" \
    --exclude='*' \
    "$CLIENT_BUILD_DIR/" "$REPO_ROOT/ClientAssets/client/"

echo "==> Checking the rest of ClientAssets/ for files git does not track"
# Everything under ClientAssets/ is published as-is to a public web root -
# client/ is now allowlisted above, and plugins/ShardPlugin.dll is a build
# output regenerated every run, but overrides/, cuo-data/ and plugins/ are
# otherwise free-form folders. A file dropped there by hand (a stray key, a
# .env, a local config) would ship to every player unless it is caught here.
# git tracking is the allowlist for those folders - see ClientAssets/.gitignore.
UNTRACKED_ASSET_FOUND=0
while IFS= read -r -d '' entry; do
    rel="${entry#"$REPO_ROOT"/}"
    case "$rel" in
        ClientAssets/client/*) continue ;;
        ClientAssets/plugins/ShardPlugin.dll) continue ;;
    esac
    if ! git -C "$REPO_ROOT" ls-files --error-unmatch -- "$rel" >/dev/null 2>&1; then
        echo "    untracked, will not publish: $rel"
        UNTRACKED_ASSET_FOUND=1
    fi
done < <(find "$REPO_ROOT/ClientAssets" -type f -print0)

if [[ "$UNTRACKED_ASSET_FOUND" -eq 1 ]]; then
    echo "error: ClientAssets/ holds file(s) git does not track (listed above)." >&2
    echo "Everything under ClientAssets/ is published to the public web root as-is." >&2
    echo "Remove these files, or git add and commit them, before publishing." >&2
    exit 1
fi

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
