# Lime

Lime is a Godot 4.7 C# project focused on reconstructing the reference exploration experience tracked in issue #3.

## M1.0 platform baseline

- Godot **4.7.1 .NET**
- **.NET 9** target framework
- C# 12 with nullable reference types enabled
- Godot **Mobile** renderer
- Phantom Camera **v0.11.0.3**

Phantom Camera is materialized into `addons/phantom_camera/` by the bootstrap script. The generated addon directory is intentionally not committed; the exact upstream version and commit are documented in `third_party/THIRD_PARTY.md`.

## Bootstrap

### Windows / PowerShell

```powershell
./scripts/BootstrapThirdParty.ps1
```

### macOS / Linux

```bash
./scripts/bootstrap-third-party.sh
```

Then open the repository with the **Godot 4.7.1 .NET editor**. The current main scene is the M1.0 platform/plugin smoke scene.

For a C#-only build check:

```bash
dotnet build Lime.csproj
```

## M1.0 smoke scene

`spikes/platform_plugin/PlatformPluginSpike.tscn` validates the Phantom Camera C# wrapper boundary used by Lime:

- typed `AsPhantomCamera3D()` access;
- `FollowTarget` assignment;
- `Priority` round-trip;
- `TeleportPosition()` availability;
- active PCam/host runtime wiring.

CI starts the scene headlessly with `--m1-spike-ci` and exits non-zero if the wrapper contract fails.
