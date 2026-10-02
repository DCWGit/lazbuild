# SpatialBuild: next prototype gates

## Working hardware baseline

The user has confirmed Quest 3 passthrough, room-fixed miniature geometry, and right-hand
pointer/pinch controls. This establishes usability and local tracking only. No physical node
observations or measured project registration are feeding the headset.

## Current iteration: field interface

- Compact, head-relative bottom MENU wheel at 1.15 m; hovering fans four tab cards outward,
  selecting a tab reveals six actions, and pointing away folds the menu down again.
  Controls follow headset position and orientation; the construction model stays room-fixed.
- Cyan pointer becomes purple during a valid pinch, with a larger cursor and button feedback.
- Place: miniature/full scale, drag then lock, 15-degree rotation, reset/reposition.
- View: disciplines, elevation slice and hanger-only view.
- Work: one hanger at a time, previous/next and reviewed status. Review is not installation approval.
- Check: display P0/P1/P2/P3 references and enter a manually measured offset in mm at full scale.
- Trial events saved as field-trial-<session>.jsonl in the Android app's persistent data directory.
  Reports include the preview transform, scale, reference, timestamp and manual source label.
  No report promotes registration status or certifies accuracy. Zero must be explicitly entered.

## Next physical experiment

Obtain a tape measure and removable floor marks. Use a clear test area and establish P0=(0,0),
P1=(2,0), P2=(0,2), P3=(2,2) metres, independently checking lengths and the diagonal.
This layout is a test fixture, not surveyed control. Use full scale and align the preview manually.
The UI's point labels refer to the same coordinates as the existing controller calibration.

Record an observed discrepancy at each point, viewing from at least three positions. Record
initial, 5-minute and 15-minute results without changing placement. Repositioning starts a new
comparison; never pool measurements across different transforms as a single drift series.
Removing the headset, recentering or losing tracking invalidates placement. Keep failed checks.
An apparent screen overlay offset is observer-dependent: record the measurement method and do
not mistake visual alignment or a hand-tracking pose for an external metrology observation.

## Registration implementation gate

Choose an observable physical reference method next: a calibrated controller probe, or camera
fiducials with independently measured locations and a verified camera-to-headset transform.
Do not infer absolute headset pose from node-to-node measurements alone. Survey alignment needs
control observations; headset correction additionally needs observations connecting it to that
frame. Hand gestures are UI input, not precision measurements.

Accept registration only with independent checkpoints, explicit tolerance selected for the
workflow, repeatability across viewpoints and sessions, and measured failure behavior. Maintain
separate network, control-registration, headset-pose and display/probe error terms.

## Worker workflow experiment

Test the hanger tour first as a candidate, not a chosen market. Compare plan-only versus AR
inspection with representative users. Record setup minutes, completion time per target, mistakes,
corrections, fatigue/readability feedback and observed position discrepancies. Account for setup
and re-registration time. Do not perform drilling from the unregistered prototype.

## Node/hardware gate

Continue the existing range/angle/noise sweep and include station orientation, correlated errors,
occlusion, moved stations and network geometry. Select distance/encoder/detector requirements
from an end-to-end error budget only after the physical registration experiment. Start with one
instrumented optical head before a custom multi-layer mechanism. Use measured hardware packets
through RealPositioningProvider; never label simulated covariance as headset accuracy.

## Product gate

Demonstrate reduced labor time at a declared, independently checked tolerance before custom
glasses, all-day battery optimization, or a large BIM import pipeline. Preserve the modular
solver, positioning provider, world transform, model data, UI and trial records already present.
