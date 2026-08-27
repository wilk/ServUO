# Patch Server (VPS)

How the client asset patch service is set up on the VPS. The VPS only
serves static files over plain HTTP - it never builds anything and it
never sees the signing private key. See `Tools/publish-assets.sh` for
what gets copied here and in what order, and `ClientAssets/README.md`
for what the three asset groups mean.

## Why plain HTTP

The shard has no DNS name - it is reached at `http://<SHARD_IP>:<PATCH_PORT>/`.
A real TLS certificate needs a hostname, so HTTPS is not available here.
Integrity does not come from TLS. It comes from the manifest signature:
`Tools/PatchBuilder` signs `manifest.json` with an ECDSA P-256 key, and
the launcher embeds the matching public key and refuses to install
anything if that signature does not check out, or if any downloaded
file's SHA-256 does not match what the signed manifest says. A
man-in-the-middle can see the traffic; it cannot make the launcher
accept a file the shard owner did not sign for.

## Web root

Static files are served from:

```
/srv/shard-patch
```

This holds (mirrors `Tools/publish-assets.conf`'s `REMOTE_WEB_ROOT`):

- `overrides/`, `cuo-data/`, `plugins/` - copies of `ClientAssets/`.
- `manifest.json`, `manifest.sig` - the signed file list.
- `ShardLauncher.exe`, `launcher.json` - the launcher build + its own
  version/hash/download metadata.

## Owning user

The web root is owned by a dedicated, non-root user, e.g. `shard`, the
same user `publish-assets.sh` connects over ssh as
(`REMOTE_USER`/`SSH_KEY_PATH` in `Tools/publish-assets.conf`). nginx
reads the web root; it does not need to write to it. Only that ssh user
(via `publish-assets.sh`, run from the shard owner's own machine) writes
to it.

## nginx

Static file server on port <PATCH_PORT>, nothing else:

```nginx
server {
    listen <PATCH_PORT>;
    server_name <SHARD_IP>;

    root /srv/shard-patch;
    autoindex off;

    location / {
        try_files $uri =404;
    }
}
```

Reload after editing: `sudo nginx -t && sudo systemctl reload nginx`.

## Firewall

Open only the patch service port. Port 80 stays closed - nothing serves
plain web traffic on this box, and there is no rule opening it:

```
sudo ufw allow <PATCH_PORT>/tcp comment 'shard patch service'
```

Do **not** add a rule for port 80. If something later needs it, that is
a separate, deliberate decision - not a side effect of this setup.

## Publishing

Run `Tools/publish-assets.sh` from the shard owner's Windows/WSL
machine (see that script and `Tools/publish-assets.conf.example`). It
builds the Launcher and Plugin, runs PatchBuilder, and rsyncs over ssh -
asset files first, `manifest.json` + `manifest.sig` last, so a partial
publish never leaves a manifest pointing at files that have not arrived
yet.
