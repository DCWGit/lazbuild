# SpatialBuild AI handoff — 2026-10-02

## Objective

Continue the existing Quest project toward the physical-control, real-IFC construction AR system in `PRODUCT_SPEC.md`. Preserve the working developer hand UI/simulator. Never present a synthetic residual as physical accuracy.

## Verified state

- Existing Quest passthrough, room-fixed developer model and hand controls were confirmed by the user before this work. The current field startup APK built, its camera/hand permissions and signature were verified, and it installed and launched without an immediate crash on the connected Quest 3. User testing of the new field menu, camera detection and physical registration is pending.
- The Dental Clinic catalog has 19,988 unique IFC elements and 3,933 renderable architectural/structural meshes. The 16,012 MEP source elements have no shape representation. `Tools/bim/validate_import.py` passes source hashes and all mesh offsets/indices. A one-thread export repeated byte-for-byte on the generated JSON and mesh chunks.
- `dotnet run --project Tests/CoordinateChecks/CoordinateChecks.csproj` passes coordinate composition, large origins, point fit with differently mounted tags, stale/synthetic rejection, moved-reference invalidation, degeneracy and camera-crop checks. Python pipeline tests and Unity's imported-project/legacy checks also pass.
- No measured project controls, printed-target trial, independent checkpoint observations, or physical accuracy statistics are available. The committed control profile is intentionally empty, so field calibration cannot become green by default.

## Principal code

- `Assets/SpatialBuild/Coordinates/`: double frames, rigid matrices, Unity float boundary.
- `Assets/SpatialBuild/Bim/`: manifest and IFC mesh view.
- `Assets/SpatialBuild/Localization/`: camera detector, control profile, point fit, trust state.
- `Assets/SpatialBuild/Quest/FieldProjectController.cs`: field startup/menu, source integration, event log.
- `Assets/SpatialBuild/Quest/QuestRegistrationController.cs` and `HandPreviewControls.cs`: existing developer demo.
- `Packages/jp.keijiro.apriltag/`: pinned, BSD-licensed native AprilTag package with measured-intrinsics overload.
- `Tools/bim/`: pinned fetch, conversion, validation. Source IFCs are ignored in `SourceData/`; normalized model and attribution are committed.
- `Tools/targets/`: pinned official marker images and printable letter SVG.
- `Tools/measurements/`: independent physical-checkpoint schema and statistics.

## Rebuild

Open with Unity 6000.6.3f1 or run `SpatialBuild.QuestPrototypeSetup.Build` in batch mode for Android. The resulting local `Builds/SpatialBuildQuest.apk` is ignored by Git. Current APK SHA-256: `3d57643436275483d160f9e592c0d30f4f1b01adba4704a0ef83b284c35cec92`. It retains `horizonos.permission.HEADSET_CAMERA` and hand tracking permission. Follow repo `AGENTS.md` and use `hzdb` with Unity's SDK ADB path for device actions. A Meta package postprocessing callback emits a nonfatal `UriFormatException` when parsing a manifest path with spaces; the build completed and APK signature verified. Fix if it becomes a build failure.

## Immediate next work

1. Print and measure stable targets using `docs/PHYSICAL_SETUP.md`. Establish three centers and an independent checkpoint in a declared project coordinate frame. Record the reference instrument and its uncertainty.
2. Populate `Assets/SpatialBuild/Resources/DentalClinic/control-profile.json` with measured controls and a new calibration version. Keep `physicalValidationPassed=false` until independent checkpoints are documented. Build and run camera detection and registration on the physical headset.
3. Record repeated independent checkpoint measurements in the CSV template; analyze with `Tools/measurements/analyze.py`. Verify moved-marker red latch, stale/occluded behavior, relocalization, recenter, latency and cross-session drift.
4. Profile the full IFC scene draw calls, memory and camera readback latency, then prioritize a trade/workface subset. Find a spatially valid MEP model if pipe layout is the first task; the current clinic MEP file cannot render.

## Git

User's destination: `https://github.com/DCWGit/lazbuild`, configured as remote `spatialbuild`; Meta sample remains `origin` and must not receive user work. Destination `main` initially contained only `# lazbuild`. Keep the implementation on its own branch without force pushing or replacing `main`. See `git log` for commits.
