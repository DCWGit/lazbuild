# Spatial construction instructions: demo job

## User flow

Open the bottom MENU wheel, choose JOB, then START. The model opens at full size and the next
piece becomes gold. Remaining job geometry is a faint blue ghost. The active target carries its
ID, instruction and material directly in the room. Unrelated building geometry is hidden.

The sample work package contains H184-H188 hanger anchors and the P-284 pipe centerline.
These are example design locations, not approved construction details or a complete installation
sequence. Placement remains manual and unregistered. No drilling or accuracy approval is implied.

- DONE records the user's report, hides that item by default, and advances to an unfinished item.
- UNDO restores the last reported item and its material quantity.
- SKIP changes the active target without recording completion.
- MATERIALS lists remaining quantities based on the example job and the user's reports.
- ONLY NEXT hides all other job objects; SHOW REMAINING brings back the unfinished ghost.
- LAYERS provides all, one elevation band, or all-below modes plus height up/down.
- PLACE pauses the guide before changing the model transform.
- Tracking invalidation pauses the job; reopening it requires START again.

## Reference and adaptation

Schematica's own controls describe hologram placement, material lists, all/layer/all-below
visibility and highlights for blocks still to be placed:
https://github.com/Lunatrius/Schematica/blob/master/src/main/resources/assets/schematica/lang/en_us.lang

This implementation adapts those interaction ideas into original Unity/C# code. No Minecraft
code, textures or assets were copied. Schematica can compare against Minecraft's known block
state; SpatialBuild does not yet have an equivalent observation of the physical site. Therefore
DONE is self-reported, material counts are task-derived, and colors indicate task focus rather
than measured correctness. No printer/automatic physical placement is claimed.

## Data and persistence

Assets/SpatialBuild/Resources/demo-pipe-job.json is the sample work package. ConstructionJob
owns step advancement, remaining-material aggregation and undo. WorkerGuide owns spatial
instructions. ConstructionWorld owns ghost rendering and filters. The job can evolve independently
of hand input, the menu and the positioning provider.

Progress is stored on the headset as job-demo-pipe-v1.json in Application.persistentDataPath.
The job ID versions the work package; progress includes user_reported_not_verified provenance.
It does not store or restore spatial registration. Export progress and field logs before returning
a borrowed headset; reinstalling on a different headset alone does not transfer that local data.

## Remaining gap to actual field guidance

The next required positioning work is to observe known physical control, solve the project-to-
headset transform, and test independent checkpoints. The existing manual-controller solver is
available but the user currently has no controllers. A camera-fiducial or calibrated probe path
needs physical reference measurements; hand placement cannot establish millimetre accuracy.
The node simulator still evaluates network measurements, not the physical headset's absolute pose.

After registration: feed a real BIM work package through the same job schema, add tolerances and
approved installation details, separate observed/verified status from user reports, and measure
time per installed unit against the conventional process. Preserve the preview warning until
positioning uncertainty and independent checks justify layout guidance.
