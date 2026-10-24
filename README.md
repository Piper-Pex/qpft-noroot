# QpEnhance — un-rooted Quest Pro face/eye enhancements for VRCFaceTracking

Improves the stock Meta Quest Pro face/eye tracking that **Virtual Desktop already
exposes on an un-rooted headset** (including the latest firmware, e.g. v2.9). It plugs
into [VRCFaceTracking](https://github.com/benaclejames/VRCFaceTracking) as a module and
post-processes the signal on the PC — no root, no firmware downgrade, no camera access.

## What it does

Working from the standard FB2 (`XR_FB_face_tracking2`) signal:

- **Gaze smoothing** — a 1€ adaptive filter removes eye-tracker jitter while staying
  responsive, driven from the real per-eye orientation quaternions when available.
- **Openness calibration** — remaps your real closed/open range onto 0..1 (with a gamma
  curve and optional auto-widening) so eyes fully open and blinks fully close instead of
  sitting half-lidded.
- **Cheek puff/suck left-right separation** — a one-sided puff no longer leaks onto the
  other cheek.
- **Brow sensitivity** — amplifies the Quest Pro's subtle brow movement.
- Full face mapping (jaw, lips, cheeks, nose, brows, basic tongue-out) so it is a
  **drop-in replacement** for the stock Virtual Desktop module, not an add-on on top.

## What it cannot do (and why)

These need data Meta filters out *on the headset* before any PC app sees it, so they
require root — which is patched above ~v2.7 and not available on v2.9:

- True independent eye convergence (cross-eyed / vergence depth)
- Camera-level tongue tracking (only coarse "tongue out" is available)
- Real pupil diameter (published as a neutral constant)

See the discussion in `NOTICE` for the boundary between the stock signal and the
root-only channels.

## Layout

```
src/QpEnhance.Core/     Pure algorithm library (no VRCFT / no headset deps). Unit-tested.
src/QpEnhance.Module/   The VRCFaceTracking module: reads Virtual Desktop, runs Core,
                        writes VRCFaceTracking unified data.
src/QpEnhance.ConfigUI/ Standalone slider UI for tuning QpEnhance.config.json live.
tests/QpEnhance.Tests/  Self-contained test runner for Core (no external packages).
```

The split is deliberate: all the enhancement logic lives in `QpEnhance.Core`, which has
no dependency on VRCFaceTracking or the headset and is therefore fully testable on any
machine. The module is a thin adapter.

## Build & test

**Core + tests** (works anywhere with the .NET SDK):

```
dotnet run --project tests/QpEnhance.Tests/QpEnhance.Tests.csproj -c Release
```

**The module** needs two VRCFaceTracking DLLs — see
[`src/QpEnhance.Module/libs/README.md`](src/QpEnhance.Module/libs/README.md) — then:

```
dotnet build src/QpEnhance.Module/QpEnhance.Module.csproj -c Release
```

(The module targets `net10.0-windows` to match VRCFaceTracking 5.4.x, and is not part of
`QpEnhance.sln`, which stays buildable without the VRCFaceTracking DLLs.)

## Install

1. Build the module.
2. Create a folder named after the module's `ModuleId` under VRCFaceTracking's custom
   modules folder, and copy the build output into it:
   `%AppData%\VRCFaceTracking\CustomLibs\<ModuleId>\` containing `QpEnhance.Module.dll`,
   `QpEnhance.Module.deps.json`, `QpEnhance.Module.runtimeconfig.json` and `module.json`.
   The module is one self-contained DLL — `QpEnhance.Core` is compiled into it (see note
   below), so there is **no** `QpEnhance.Core.dll` to copy.
3. Start Virtual Desktop streaming with face/eye tracking enabled, then start
   VRCFaceTracking. QpEnhance appears in its module list and attaches automatically.
   (Run only one provider: deactivate the stock "Virtual Desktop" module.)
4. A `QpEnhance.config.json` is written next to the DLL on first run; edit it and it
   hot-reloads within ~2 seconds.

> **Why one DLL?** VRCFaceTracking 5.4.x loads each module inside a separate sandbox
> process whose assembly loader does not resolve sidecar dependency DLLs placed next to
> the module. A separate `QpEnhance.Core.dll` therefore fails to load at runtime
> ("Could not load file or assembly 'QpEnhance.Core'") and the module hangs on init.
> The module project compiles the Core sources straight in to avoid this; the standalone
> `QpEnhance.Core` project remains for the tests and the config UI.

## Tuning UI

`QpEnhance.ConfigUI` is a tiny standalone Windows app (sliders + checkboxes) that edits
`QpEnhance.config.json` for you. Changes save automatically and the running module
hot-reloads within ~2 seconds, so you can tune live while watching VRCFaceTracking's
**Output** page.

```
dotnet run --project src/QpEnhance.ConfigUI/QpEnhance.ConfigUI.csproj -c Release
```

It has no VRCFaceTracking dependency — it only reads/writes the module's config file,
auto-located under `%AppData%\VRCFaceTracking\CustomLibs\<ModuleId>\`.

## Config (`QpEnhance.config.json`)

| Key | Default | Meaning |
|-----|---------|---------|
| `GazeSmoothing` | `true` | Enable the 1€ gaze filter. |
| `GazeMinCutoff` | `1.2` | Lower = smoother at rest, more lag. |
| `GazeBeta` | `0.25` | Higher = snappier on fast eye movement. |
| `GazeGain` | `1.15` | Push gaze to the corners of its range. |
| `OpennessCalibration` | `true` | Enable openness remapping. |
| `OpenClosedPoint` | `0.15` | Openness value that should read as fully closed. |
| `OpenOpenPoint` | `0.90` | Openness value that should read as fully open. |
| `OpennessGamma` | `1.0` | >1 keeps eyes wider; <1 closes sooner. |
| `OpennessAuto` | `false` | Let the open/closed range widen from observed extremes. |
| `SquintGain` | `1.0` | Gain on lid-tightener squint. |
| `CheekSeparation` | `true` | Enable cheek left/right separation. |
| `CheekStrong` | `false` | Harder contrast for near-tied cheek poses. |
| `BrowSensitivity` | `1.4` | Brow weight multiplier. |

## Status

Core is implemented and unit-tested (51 checks). The module builds clean against the real
VRCFaceTracking 5.4.5 assemblies (`net10.0-windows`, 0 warnings / 0 errors) and is
**confirmed working end-to-end on a Quest Pro over Virtual Desktop**: it loads in the
VRCFaceTracking sandbox, attaches to the Virtual Desktop face-state shared memory, and
drives a VRChat avatar's face live. Tuning the defaults to taste (via the config UI) is
all that's left per user.

> QpEnhance is a **drop-in replacement** for the stock Virtual Desktop module — both read
> the same shared memory and claim the same eye+expression capabilities. Run only one at a
> time: deactivate the "Virtual Desktop" module in VRCFaceTracking's module list, leave
> "QpEnhance" active.

See `NOTICE` for third-party credits.
