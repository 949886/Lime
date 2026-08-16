# M1.0 Platform & Plugin Spike

This spike validates the foundational runtime stack before the M1 production scene hierarchy is created.

## Scope

- Godot 4.7.1 .NET
- `net9.0`
- C# 12 with nullable reference types enabled
- PC default renderer: Forward+
- Mobile renderer override: Mobile
- Phantom Camera 0.11.0.3
- Phantom Camera typed C# wrapper
- Windows export smoke
- Android `arm64-v8a` export smoke
- iOS is intentionally out of scope for the current project phase

## Runtime assertions

`PlatformPluginSpike.cs` verifies that the checked-in Phantom Camera C# wrapper can:

1. assign and read `FollowTarget`;
2. assign and read `Priority`;
3. activate the highest-priority PCam;
4. call `TeleportPosition()` without a string-based API workaround;
5. move the real `Camera3D` when its follow target moves;
6. switch control between two PCams using priorities.

The spike also validates the configured renderer split:

- `rendering/renderer/rendering_method = forward_plus`
- `rendering/renderer/rendering_method.mobile = mobile`

## Local smoke

Build C# first:

```bash
dotnet restore
dotnet build
```

Run interactively:

```bash
godot --path .
```

Run as an automated headless assertion:

```bash
godot --headless --path . -- --m1-spike
```

The automated command exits with code `0` on success and `1` on a failed assertion.

## Export presets

The checked-in `export_presets.cfg` contains M1.0 smoke presets for:

- `Windows`
- `Android`

The Android package identifier `com.limegame.lime` is a spike-stage placeholder and is not the final product identifier.

## Exit criteria

M1.0 is complete when the current branch proves all of the following:

- `dotnet build` succeeds on .NET 9;
- the headless Phantom Camera runtime smoke succeeds;
- Windows export succeeds;
- Android export succeeds with the Godot 4.7 Android toolchain;
- no Lime-owned runtime code needs untyped string calls to access the Phantom Camera APIs used by M1.
