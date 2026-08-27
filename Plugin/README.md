# ShardPlugin

A minimal ClassicUO plugin that proves the delivery chain: the launcher
downloads it, copies it into ClassicUO's `Data/Plugins/`, ClassicUO
loads it, and it writes a log line. No real game feature.

## Building

`Plugin.csproj` references `cuoapi.dll` by absolute path - that DLL ships
inside a ClassicUO install and is never vendored into this repo. The
default path is this shard's documented dev machine location:

```
/mnt/c/Programmi UO/ClassicUOLauncher-win-x64-release/ClassicUO/cuoapi.dll
```

To build against a different copy, either pass
`-p:CuoApiPath=/path/to/cuoapi.dll` on the command line, or copy
`CuoApi.local.props.example` to `CuoApi.local.props` (gitignored) and
edit the path there.

## What was verified against the real cuoapi.dll, and how

`cuoapi.dll` (30496 bytes, from the ClassicUO install at the path above)
was disassembled to IL with `monodis` (`monodis cuoapi.dll`). From that
IL, verified directly:

- The `CUO_API.PluginHeader` struct exists, is a sequential value type,
  and its exact field list and order is: `ClientVersion` (int32), then
  `native int` fields `HWND`, `OnRecv`, `OnSend`, `OnHotkeyPressed`,
  `OnMouse`, `OnPlayerPositionChanged`, `OnClientClosing`,
  `OnInitialize`, `OnConnected`, `OnDisconnected`, `OnFocusGained`,
  `OnFocusLost`, `GetUOFilePath`, `Recv`, `Send`, `GetPacketLength`,
  `GetPlayerPosition`, `CastSpell`, `GetStaticImage`, `Tick`,
  `RequestMove`, `SetTitle`.
- `CUO_API.OnInitialize` is a delegate with signature `void Invoke()`,
  marked `UnmanagedFunctionPointerAttribute` with calling convention
  value `2` (Cdecl).
- `cuoapi.dll` targets `mscorlib` (old .NET Framework-style build), not
  a modern .NET Core/.NET 8+ target.

`PluginMain.cs` uses the real `CUO_API.PluginHeader` and
`CUO_API.OnInitialize` types from the referenced assembly directly (not
a hand-copied redeclaration), so the field layout and delegate signature
used at build time are exactly what the DLL defines.

## What could NOT be verified: the plugin loader's entry point

`cuoapi.dll` only defines the data types a plugin uses (`PluginHeader`,
the delegates, packet helpers). It does not contain the code that scans
a plugin assembly and decides what to call. That loader lives in the
ClassicUO client itself (`cuo.dll` / `ClassicUO.exe`, same install
folder).

Attempted: `monodis cuo.dll` and `ikdasm cuo.dll` on the client's main
`cuo.dll` (14.85 MB). Both failed - `monodis` reported "Error while
trying to process cuo.dll" and `ikdasm` raised
`IKVM.Reflection.BadImageFormatException` while reading the PE headers.
`cuo.dll` is a modern (non-`mscorlib`) build and its metadata is not in
the classic IL layout these two tools expect, most likely because it is
published as NativeAOT or a similarly transformed native module rather
than plain IL - `file` reports it as a PE32+ DLL with no readable
CLR IL metadata, and it is far too large (14.85 MB) to be a small IL
assembly for what it does.

A `strings` search over `cuo.dll` found real evidence of a plugin
loading pipeline: the substrings `IPluginHost`, `LoadPlugin`,
`OnInstall`, `CannotInstall`, `get_PluginHost`, `get_PluginPath`,
`get_Plugins`, and `dOnPluginLoad` all appear in the binary. This
confirms a load path exists and involves some install/host concept, but
does not reveal the exact class name, method name, or signature
ClassicUO's loader looks for via reflection - that would need
disassembling the native code, which was out of scope here.

`ClassicUO`'s `settings.json` (read directly from the same install) has
a `"plugins": []` array and a `"files_override": null` field, which is
consistent with `-plugins` and `-uofilesoverride` each taking one or
more file paths, but the exact delimiter this build expects for more
than one `-plugins` path was not verified. `ClassicUoLauncher.cs` joins
multiple plugin paths with a comma; if that turns out to be wrong for
this ClassicUO build, only that one line needs to change.

**Given that**, `PluginMain.Install(ref PluginHeader header)` is written
as the best-effort, most conventional entry point for this plugin API
shape (a public static method named `Install` taking the header by
reference) - but whether ClassicUO's loader actually looks for a method
with this exact name was not independently confirmed. If the real shard
plugin does not load, check `%LOCALAPPDATA%\ServUOShard\plugin.log`
first (it will simply be missing/empty if `Install` was never called),
then re-run the `cuo.dll` inspection with a proper native disassembler
(e.g. Ghidra) or check the ClassicUO project's own plugin-loading source
for the real entry point convention.
