# Experiment log

## Existing headset demonstration (qualitative)
Experiment: passthrough and local anchoring / hand UI.
Date: 2026-10-01 through 2026-10-02, conversation-confirmed.
Objective: render and interact with room-fixed preview.
Hardware: borrowed Quest 3, no controllers.
Software commit: uncommitted prototype at audit; preserved in baseline commit.
Environment: user room, not surveyed.
Procedure: user moved head, viewed preview and used hand controls.
Ground truth: none.
Measurements: qualitative user reports only.
Results: user confirmed model stays fixed and hand controls work; requested UI revisions.
Mean error / Median / RMSE / P95 / Maximum / Standard deviation: NOT MEASURED.
Failure observations: large panel was too far left; replaced by bottom wheel.
Conclusion: local tracking and usability demonstration, not absolute accuracy evidence.
Next experiment: measured reference registration and independent checkpoints.

Physical tests 1-15 from PRODUCT_SPEC.md have NOT been performed. Existing synthetic reports are not physical measurements.

## 2026-10-02 software integration

Objective: verify real IFC conversion, coordinate framing, fail-closed state logic, APK packaging.
Inputs: pinned Dental Clinic IFCs; synthetic coordinate fixtures with a one-million-metre origin and moved control; no physical checkpoint observations.
Results: 19,988 unique GUID-linked elements; 3,933 renderable arc/str meshes and 16,012 MEP metadata-only elements. Import source hashes, geometry offsets and indices validated; generated JSON and mesh chunks reproduced byte-for-byte with serial tessellation. .NET coordinate checks, Python pipeline checks and Unity import checks passed. Final field APK signature and camera/hand permissions verified, installed on connected Quest 3 and launched with no immediate crash signal.
Measured physical position error: **NOT MEASURED**. Synthetic fit residuals are implementation tests and do not estimate headset or tag accuracy.
Failure/limitation: source MEP IFC has no shape representations. The headset was not worn for a new field-mode interaction test. No printer/reference setup or independent instrument was available during this run.
