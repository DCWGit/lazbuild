# First noise sensitivity sweep

**Simulation only. These are not physical node or headset performance measurements.**

The sweep contains 24 scenarios with 100 seeded trials each: three/four nodes, range sigma
0.5/1/2 mm and angular sigma 0.001/0.005/0.01/0.05 degrees. Other independent one-sigma
inputs remain level 0.002 degrees, encoder 0.001 degrees, receiver centering 0.2 mm.
Range and yaw biases are zero. No trials failed to solve in this sweep.

Selected results at **1 mm range sigma**:

| Angular sigma (degrees) | 3-node RMS (mm) | 4-node RMS (mm) | 3-node P95 max (mm) | 4-node P95 max (mm) |
|---|---:|---:|---:|---:|
| 0.001 | 0.627 | 0.531 | 1.170 | 0.955 |
| 0.005 | 0.968 | 0.864 | 1.686 | 1.450 |
| 0.010 | 1.478 | 1.330 | 2.528 | 2.283 |
| 0.050 | 6.039 | 5.090 | 11.878 | 10.398 |

RMS uses node B and C position errors in both configurations so adding node D does not change
the comparison population. P95 is the nearest-rank percentile of the maximum B/C error in each
trial, not a guaranteed upper bound and not 200 independent observations. Results are conditional
on fixed node A and known station orientations. The node layout is approximately 10 by 8 m.
Adding D changes measurement redundancy and geometry. This is not proof that every fourth-node
placement improves performance by the same amount.

Within this simplified model, angular error becomes a major contributor as it increases, and
the fourth node improves the selected comparisons. Before using these results to choose hardware,
extend the solver/simulator to unknown station orientation, correlated leveling/encoder errors,
calibrated detector geometry, survey-control uncertainty and an actual headset measurement link.

A separate regression test deliberately applies common yaw bias. It produces a rotated network
with small internal residuals and substantial truth error. Internal consistency does not establish
absolute alignment. Physical control checks and a complete uncertainty budget are required.

Data: `synthetic-noise-sweep.csv`. Generator: `../../work/network-verification/Program.cs`
from the repository root context (the generator resides under the original task workspace).
Run the NetworkVerification console project with `--sweep` from that workspace to reproduce.
