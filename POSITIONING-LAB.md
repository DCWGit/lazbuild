# SpatialBuild positioning lab

In Unity, stop Play mode and select **SpatialBuild > Open positioning lab**, then press Play.
The original inspection scene is preserved. The lab scene is
`Assets/SpatialBuild/Scenes/SpatialBuildPositioningLab.unity`.

## First experiment

1. Select **Zero noise**. Solved coordinates should agree with simulated truth.
2. Select **Default noise**. Watch the new samples, node errors and formal RMS uncertainty.
3. Select **Freeze baseline**, then **Move C 50 mm**. The network change alarm should latch.
4. Select **Restore C**. The alarm remains until you explicitly freeze a new baseline.
5. Occlude C. The disconnected network becomes UNKNOWN. Restore visibility to solve again.
6. Pause scanning and wait more than four seconds. The provider reports stale observations.
7. Add a fourth node and repeat. Changing the topology resets the baseline.
8. Change shared yaw bias. Small residuals can coexist with wrong absolute coordinates.
9. Export measurements. CSVs contain raw observations and node truth/estimates; JSON records
   the captured settings, not slider values changed after the capture. Outputs are in Unity's
   `Application.persistentDataPath/Experiments`. The panel shows the full path.

The left diagnostics panel scrolls. Orbit buttons turn the development camera around the room.
Discipline buttons, an elevation slice and a hanger-only task view restrict the construction
geometry. Aim the center crosshair and select an object for its metadata.

## Modules

- `Positioning/NetworkMath.cs`: double-precision weighted nonlinear least squares, residuals,
  finite-difference Jacobian, rank check, convergence checks and formal covariance.
- `Positioning/MeasurementSimulation.cs`: seeded Gaussian observations and frozen-baseline monitor.
- `Positioning/PositioningProvider.cs`: provider abstraction and deliberately unavailable hardware stub.
- `Positioning/SimulatedPositioningProvider.cs`: capture lifecycle, optical-stage illustration,
  stale-data handling and explicit calibration.
- `Construction/ConstructionWorld.cs`: lightweight model, metadata, discipline/elevation/task filters.
- `Diagnostics/NetworkDiagnostics.cs`: simulation controls, node/error visuals and exports.
- `Editor/PositioningLabSetup.cs`: scene creation and verification menu.

## Coordinate and uncertainty contract

The solver uses metres and radians in a right-handed project frame: X east, Y north, Z up.
Unity rendering maps this to `(X,Z,Y)`; Unity Y is elevation. Unity float positions are display
values only. Node A is fixed at the origin. Every station's optical orientation is assumed
independently known in that frame. The solver does not solve unknown head orientations,
survey control or a range-only network. Initialization uses observations, never true coordinates.

Every ordered node pair yields range, azimuth and elevation. Gaussian inputs are independent
one-standard-deviation errors, not bounded +/- specifications. Level, encoder and centering are
an approximate independent angular-error budget. Centering uses the small-angle relation
centering distance / target range. A real leveling/encoder model will require correlations,
calibration biases and mechanical geometry. Common range and yaw biases are separate controls.

Formal node uncertainty is sqrt(trace(inverse(J'WJ))) in millimetres: conditional linearized
3D RMS given the fixed datum, known orientations and independent Gaussian model. It is not a
95% bound, not a maximum error, and not uncertainty of the headset or overlay. Node A's zero
formal uncertainty reflects the fixed-datum assumption, not perfect physical knowledge.
Zero-noise mode uses tiny numerical weight floors. Bias controls are excluded from covariance.
The Monte Carlo check applies only to the default independent-noise model.

The baseline alarm compares solved positions to a frozen baseline with a configurable threshold,
and also checks large standardized residuals. It is an experimental detector, not a calibrated
false-alarm probability. It does not uniquely attribute a moved node. Global rigid motion of an
unanchored network cannot be detected by internal measurements alone. Restoring geometry never
silently clears a latched fault. Recalibration is explicit.

## Honest limits

The Rotate/Search/Detect/Center/Lock/Range sequence is a timed illustration. Blue lines are ideal
links, not a beam-interception, detector, encoder-control-loop or laser-safety simulation.
There is no physical laser, hardware transport or measured power/battery model.

ConstructionWorld stays in the explicit demonstration project frame. The provider reports network
coordinates; it does not invent a network-to-headset registration. No solved node estimate silently
moves the construction geometry. A measured control registration and Quest-to-project transform
are separate future steps. All geometry remains inspection-only; no live layout-grade GREEN exists.

This scene is a desktop lab, not a configured Quest passthrough scene. Headset deployment,
physical registration, pose latency, camera observations and real accuracy remain untested.
The eventual product goal remains worker-focused spatial construction instructions and reduced
labor hours per installed unit at a measured tolerance. Custom glasses and the three-layer optical
node mechanism remain design options, not validated requirements.

## Verification completed
Ten numerical verification groups passed in both standalone .NET and Unity. A separate Unity 6.6.3f1 validation project loaded the saved scene in Play mode and verified 13 elements, the solver, movement latching/reset, occlusion and staleness, discipline/task/elevation filters, raycast metadata selection and an offscreen camera render. The render was visually inspected. Unity emitted an editor SearchDatabase startup exception in that isolated validation project; the runtime checks completed successfully. The main project still needs to refresh/import these new assets in the open editor. No Quest build or physical accuracy test was performed.


Main-project integration confirmed: spatialbuild-lab-ready.txt records successful editor compilation and scene preparation at 2026-09-30T21:35:04Z. The open-project setup also ran the numerical verification successfully.

