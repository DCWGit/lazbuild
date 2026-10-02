# Quest prototype: preview and manual registration demonstration

## Without controllers

When no tracked right controller is available, hold your head steady for three seconds.
A 1:10 miniature appears about 1.3 m ahead and below eye level. It is labelled NOT REGISTERED:
this is only a visual inspection view, not physical construction guidance. Remove and put on
the headset to place it again. Tracking loss, focus loss and recentering hide/reset the preview.
Connecting a tracked right controller returns to the full-scale manual calibration workflow.

## Hand controls

The current worker flow is MENU > JOB > START: full-size example job, gold next target,
blue remaining geometry, spatial instruction labels, DONE / UNDO / SKIP and remaining materials.
The four cards are now JOB / LAYERS / PLACE / CHECK. See WORKER-WORKFLOW.md for controls,
Schematica references, persistence and the distinction between manual progress and verification.

The latest interface starts as a small MENU wheel at bottom centre. Hover the right-hand pointer
over it to fan out PLACE / VIEW / WORK / CHECK cards; pinch a card to reveal six actions above it.
Point elsewhere for 2.5 seconds to fold the menu away, or pinch the wheel to close it immediately.
The panel is centred in the headset frame, with targets within 20 degrees horizontally and 25
degrees vertically of the view centre in the default layout; comfort still needs headset testing.
The panel follows the headset's complete viewing pose. Only the building and its physical
reference markers remain anchored in the room; UI movement does not change model placement.
Text and targets are larger. The right-hand ray and cursor turn purple during a valid pinch;
the hovered button also highlights purple. A Work tour isolates one hanger at a time and marks
it reviewed, not installed. Check displays reference points and records manually entered offsets
only at full scale; no default zero is silently saved. These reports never establish registration.
See PROTOTYPE-NEXT-STEPS.md for the field experiment and remaining engineering gates.

The preview now supports Meta hand tracking with a small panel to your left. Raise your right
hand, aim its cyan pointer at a button and pinch thumb/index together. Release between actions.
The panel offers full scale / miniature, move, rotate, discipline, hanger task, elevation slice,
elevation up/down, place in front, and show all. Pinch a model element away from the panel to
inspect its metadata. For moving, enable Move, then pinch away from the panel and hold while
moving your right hand. Release to leave the model in place; tracking loss cancels movement.

Full-scale placement uses the Quest tracking floor as an approximate elevation reference, not
survey control. All hand-placed geometry remains NOT REGISTERED. Hand controls cannot mark a
model as calibrated or pass a survey checkpoint. The existing controller calibration is separate.
If the panel says to raise your hand but no pointer appears, check the headset's Settings >
Movement tracking > Hand and body tracking. The app declares optional hand tracking support;
the automatic three-second miniature preview still works without hand tracking.

Implementation follows the installed Meta SDK OVRHand pointer/pinch API and official setup:
https://developers.meta.com/horizon/documentation/unity/unity-handtracking-hands-setup

This is a synthetic construction scene with a real manual-registration path. It is not
layout-grade guidance. A successful Android build does not verify passthrough, tracking,
controller mapping, readability, safety or physical accuracy on a headset.

## What the app does

- Uses the installed Meta camera rig and passthrough layer with a transparent background.
- Leaves headset pose tracking under the XR runtime. Only ConstructionWorld is transformed.
- With a tracked controller, hides construction geometry until three controls are captured successfully.
- Captures a tracked right-controller reference, with configurable controller-space probe offset.
- Uses three-point orthonormal frame alignment plus centroid translation, with a residual gate.
  This is a manual demonstrator, not a weighted survey-control adjustment.
- Offers a fourth, held-out point to measure an independent position discrepancy.
- Resets registration on tracking loss, headset removal, recenter/origin change, app pause/focus
  loss, or detected changes to the rig tracking-space transform.
- Supports discipline filtering, hanger-only tasks, elevation slices, and gaze-ray selection.
- Saves session-specific registration events under Application.persistentDataPath as JSONL.
- Does not persist/reuse a physical alignment across application restarts.

The existing sample declares camera/scene permissions for its camera examples. This scene uses
passthrough display and does not request raw camera access or run tag detection. The initial build was installed and
launched on the borrowed Quest 3; the user confirmed room passthrough and instruction text.

## Prepare physical controls

Arrange four independently measured reference points in a safe test area. Default PROJECT
coordinates are metres, with Z up:

| Point | X | Y | Z | Purpose |
|---|---:|---:|---:|---|
| P0 | 0 | 0 | 0 | First calibration point |
| P1 | 2 | 0 | 0 | Second calibration point |
| P2 | 0 | 2 | 0 | Third calibration point |
| P3 | 2 | 2 | 0 | Independent checkpoint, not used in the fit |

In the Unity inspector these appear as `(X,Z,Y)` because Unity Y is up. Change the controls
and checkpoint in QuestRegistrationController to match measured values, never assume a roughly
marked square is exact. The controller's reported reference is not necessarily its visible tip.
The default offset is zero. Establish a repeatable probe reference and calibrate its offset
before interpreting errors. Headset/controller tracking error is included in the physical check.

The default 30 mm fit/check gates are coarse demo rejection thresholds, not product tolerances
or precision specifications. The app never declares construction accuracy, even below a gate.

## Controls

| Input | Action |
|---|---|
| Right primary button (A) | Capture next control; after alignment, capture the independent checkpoint |
| Right secondary button (B) | Clear alignment and hide geometry |
| Left primary button (X) | Cycle discipline |
| Left secondary button (Y) | Toggle hanger-only task |
| Right trigger | Select geometry at the center of the headset view |
| Left stick up/down | Change elevation in metres |
| Left stick click | Toggle elevation slice |

Buttons use Unity XR common usages; confirm actual mappings on the connected controllers.
No hand-tracking interaction is required. Instructions are rendered in the headset, not in a
desktop-only OnGUI panel. The zero-offset controller reference requires special care at capture.

## First device session

1. Confirm campus permission to install development builds on the headset and collect test logs.
2. Enable the institution-approved development connection; inspect connected devices through
   Meta's hzdb tool. Do not bypass device authorization prompts.
3. Install the generated APK once a build has actually succeeded. Package ID:
   `com.spatialbuild.prototype`. Verify the install artifact and build timestamp.
4. First confirm clear passthrough and readable instructions in both eyes. Geometry starts hidden.
5. Capture P0, P1, P2 in that order with the same controller reference. Reject a poor fit.
6. Capture P3 and record the actual discrepancy. Repeat without changing the controls.
7. Inspect alignment from multiple head positions. Test headset removal, recenter, pause and
   tracking loss. Each must hide geometry and require a new calibration.
8. Exercise each filter and selection. Export/pull registration JSONL through the authorized
   device tool. Keep all unsuccessful captures and sessions, not just the best result.

## Physical experiment requirements

Record model revision, headset/OS/SDK versions, control measurements and their uncertainty,
controller-probe calibration, lighting, test volume, session ID, signed XYZ discrepancies,
3D discrepancy, head position, time since calibration and fault/reacquisition behavior.
Report separately: control-fit residual, held-out control error, and perceived/display overlay
error. A controller measurement is not a direct calibration of the eye/display optical chain.
No physical results have been collected yet.

## Build and maintenance

Unity menu: SpatialBuild > Open Quest prototype; SpatialBuild > Build Quest APK.
The build uses a scene-only build list, Android ARM64/IL2CPP, minimum API 32 and target API 34
(installed locally), with a development signature. It is a sideload test build, not a store release.
The original sample scenes and lab are preserved. Build output, when successful:
`Builds/SpatialBuildQuest.apk`. Inspect `spatialbuild-android-build.txt` and the Android log for
the actual outcome; scene preparation alone is not build success.

Automatic optical registration, real-node transport, network-to-headset observation, validated
uncertainty, physical accuracy and an eight-hour power budget remain future work.

## Verified artifact
Build succeeded on October 1, 2026 with zero errors and 331 recorded warnings. Final APK: 92,722,768 bytes, ARM64, package com.spatialbuild.prototype, minimum API 32, target API 34. APK signature verification passed. The package declares VR head tracking and passthrough. Build metadata and SHA-256 are saved beside the APK in Builds/build-verification.json. Registration math and state-transition checks passed using synthetic controls. No Quest was connected during verification; device rendering and physical accuracy are untested.

