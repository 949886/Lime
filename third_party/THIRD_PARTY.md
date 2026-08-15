# Third-party dependencies

## Phantom Camera

- Upstream: `ramokz/phantom-camera`
- Pinned release: `v0.11.0.3`
- Pinned commit: `cb6e0966ac305202c47f1d1a81c105966e29da96`
- License: MIT (`third_party/licenses/phantom-camera-LICENSE`)
- Runtime path after bootstrap: `res://addons/phantom_camera/`

Lime does not track the generated addon directory directly. Run `scripts/BootstrapThirdParty.ps1` or `scripts/bootstrap-third-party.sh` after cloning. The scripts fetch the exact release tag above and materialize only the upstream `addons/phantom_camera` directory into the Godot-standard addon path.

The release/commit pin is intentional. Phantom Camera upgrades must be performed explicitly and followed by the M1 platform/plugin smoke tests, including the C# wrapper boundary.
