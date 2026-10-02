You are continuing development of my existing Unity / Meta Quest 3 project:

`SpatialBuild-Quest`

This is an existing working project.

DO NOT restart it.

DO NOT regenerate it from scratch.

DO NOT replace working systems unless necessary.

DO NOT spend large amounts of time brainstorming the product, redesigning the idea, or adding speculative features.

I have already defined the product direction.

Your job is to inspect the existing project, preserve what works, implement the architecture described below, test it, document what actually works, commit the work cleanly, and keep the repository understandable to another engineer or another ChatGPT conversation.

The goal is to minimize wasted development effort.

---

# 1. CORE PRODUCT

SpatialBuild is a construction AR system where a BIM model behaves like a **survey-registered virtual hologram of the building**.

Imagine an empty construction site.

Survey control establishes the real project coordinate system.

SpatialBuild knows where the building belongs relative to that survey control.

A worker puts on AR glasses.

The worker should see the building occupying the exact physical location where it is supposed to exist.

The worker should be able to:

- walk toward the building
- walk around columns
- walk through future walls
- stand underneath future pipes and ducts
- view reinforcing steel inside structural elements when that data exists
- switch between architectural, structural, civil, mechanical, electrical, plumbing, fire protection, and other BIM disciplines
- move throughout the site while the virtual building remains stationary in project coordinates

The experience should feel like walking around a holographic version of the building.

The virtual building MUST NOT move with the headset.

The worker moves through the building coordinate system.

The building does not move through the worker's coordinate system.

---

# 2. TARGET USERS

Initial priority:

1. layout crews
2. field engineers
3. skilled trades

Eventually:

- general construction labor
- superintendents
- QA/QC
- inspectors
- commissioning teams
- robotics
- autonomous equipment
- scanners
- layout tools
- drones

But do not optimize V1 around those future cases.

V1 should prove the fundamental spatial-registration system.

---

# 3. CURRENT PROJECT STATE

The current project reportedly already contains:

- Quest 3 passthrough
- room-anchored construction geometry
- controller-free hand tracking
- pointing
- pinch interaction
- cyan/purple interaction feedback
- bottom menu wheel
- miniature/full-scale model mode
- model move
- model rotate
- model lock
- construction filters
- elevation layers
- individual tasks
- remaining-piece filtering
- Schematica-like job guide
- hanger anchors
- pipe demo
- spatial instructions
- Done / Undo / Skip
- saved progress
- simulated positioning network
- configurable measurement error
- coordinate solving
- diagnostics
- moved-node simulation
- manual calibration code
- field-check logs
- automated checks
- development documentation

Verify all of this from the actual repository.

Do not assume the summary is perfect.

Preserve existing working features unless they interfere with the new architecture.

---

# 4. WHAT IS NOT A PRIORITY RIGHT NOW

Do not spend substantial new development effort on:

- tooltips
- material information popups
- installation task sequencing
- Done / Undo workflows
- AI assistant
- voice assistant
- automatic installation verification
- drill tracking
- tool tracking
- worker productivity analytics
- schedule optimization
- realistic materials
- visual polish
- animations
- construction robots
- custom production glasses
- production industrial design

Keep existing implementations if useful.

Do not delete them merely because they are not currently prioritized.

The immediate focus is:

**REAL BIM → PROJECT COORDINATES → PHYSICAL CONTROL → QUEST REGISTRATION → DRIFT CORRECTION → TRUST MONITORING → MEASURED ACCURACY**

---

# 5. FUNDAMENTAL SYSTEM PHILOSOPHY

The Quest is NOT the authoritative source of absolute project position.

Quest provides:

- high-frequency relative head movement
- visual-inertial tracking
- orientation
- rendering
- camera observations
- passthrough
- hand tracking

Survey/project control provides:

- absolute spatial truth

The intended system concept is:

```text
SURVEY CONTROL
      ↓
SPATIAL CONTROL NETWORK
      ↓
PROJECT COORDINATE SYSTEM
      ↓
ABSOLUTE LOCALIZATION REFERENCES
      ↓
LOCALIZATION ENGINE
      ↑
QUEST VISUAL-INERTIAL TRACKING
      ↓
CORRECTED HEADSET POSE
      ↓
BIM RENDERING
```

The Quest should provide smooth motion between absolute corrections.

External/project control should prevent unchecked accumulated drift.

---

# 6. COORDINATE SYSTEMS

The system must explicitly distinguish at least:

### BIM SPACE

Coordinates contained in BIM/IFC source data.

### PROJECT SPACE

The actual surveyed coordinate system of the construction project.

### QUEST LOCAL SPACE

Quest's current internal tracking coordinate system.

### HEAD / CAMERA SPACE

Current headset and camera pose.

Do not collapse these into one Unity transform hierarchy.

Conceptually maintain:

```text
T_BIM_PROJECT

T_PROJECT_QUEST

T_QUEST_HEAD
```

Rendering ultimately behaves like:

```text
BIM
↓
PROJECT
↓
QUEST LOCAL
↓
HEADSET VIEW
```

All important transforms must be:

- explicit
- inspectable
- testable
- logged
- reversible where applicable

Do not hide critical coordinate math inside arbitrary nested GameObjects.

---

# 7. HOLOGRAM PRINCIPLE

If a column's design center exists at:

```text
X = 48.324 m
Y = 17.912 m
Z = 3.657 m
```

in project coordinates, then the virtual column must occupy that same project-space location.

When the worker moves, only the viewing/head transformation changes.

The column does not move.

The same must be true for:

- walls
- pipes
- ducts
- conduits
- equipment
- rebar
- penetrations
- hangers
- structural steel
- slabs
- foundations
- civil elements
- any other modeled object

---

# 8. REAL BIM DATA

Stop treating the manually created pipe/hanger demonstration as the primary model.

Keep it for debugging if useful.

Use a real IFC project as the primary spatial test.

Recommended initial dataset:

**IFC-Bench Dental Clinic**

Use the architectural, structural, and MEP model content.

The reason is that it lets SpatialBuild test:

- real BIM hierarchy
- multiple disciplines
- real geometry
- real coordinates
- element metadata
- realistic model scale

Do not manually rebuild the Dental Clinic model in Unity.

Use the actual IFC files.

---

# 9. IFC IMPORT ARCHITECTURE

Do not permanently make the rest of SpatialBuild depend directly on raw IFC objects.

Implement or evolve an import pipeline:

```text
IFC
↓
IFC importer/parser
↓
normalized SpatialBuild model
↓
project coordinate representation
↓
Unity rendering representation
```

Create a normalized internal representation conceptually similar to:

```text
SpatialProject
    ProjectMetadata
    CoordinateSystem
    Discipline[]
    SpatialElement[]
```

and:

```text
SpatialElement
    id
    sourceGuid
    discipline
    category
    type
    geometryReference
    localTransform
    projectTransform
    metadata
    sourceRevision
```

Preserve IFC GUIDs.

Use meters internally.

Do not fabricate missing BIM information.

---

# 10. BIM DISCIPLINES

At minimum support:

```text
ARCHITECTURAL
STRUCTURAL
CIVIL
MECHANICAL
ELECTRICAL
PLUMBING
FIRE_PROTECTION
OTHER
```

All disciplines must share the SAME project coordinate system.

Switching discipline must NOT create separate manually aligned models.

Example:

A worker standing beside a future column chooses:

```text
STRUCTURAL
```

and sees structural geometry.

Then switches to:

```text
ELECTRICAL
```

and electrical geometry appears in the same physical building coordinate frame.

Then:

```text
PLUMBING
```

and plumbing appears in the same frame.

---

# 11. DEFAULT FIELD EXPERIENCE

When SpatialBuild launches and no project is selected:

Show passthrough.

Show only a minimal menu.

Do not display arbitrary BIM geometry.

Provide a project-selection control.

The project selection should conceptually feel similar to choosing a Wi-Fi network.

Example:

```text
AVAILABLE PROJECTS

Dental Clinic
UIUC Test Room
Project Alpha
Project Beta
```

When a project is selected:

load:

- project manifest
- BIM data
- project coordinate system
- control-node configuration
- calibration information
- current BIM revision

Manual model movement should remain only as a developer/calibration tool.

It should not be the normal production workflow.

---

# 12. PROJECT MANIFEST

Each project should eventually be loadable through a deterministic project manifest.

A simple JSON-based prototype is acceptable.

Conceptually store:

```text
projectId
projectName
coordinateSystem
projectOrigin
BIM revision
BIM source files
discipline mappings
units
control-node configuration
model transform
calibration version
```

Do not build an elaborate database yet.

The point is repeatability.

---

# 13. BIM REVISION CONTROL

Record which BIM revision is loaded.

Do not silently replace one revision with another.

The system should eventually be able to report:

```text
PROJECT:
Dental Clinic

MODEL REVISION:
...

CONTROL CALIBRATION:
...

LOADED:
...
```

Simple metadata is enough for now.

---

# 14. SPATIAL CONTROL NODE CONCEPT

Long-term SpatialBuild will use modular physical devices called:

**SPATIAL CONTROL NODES**

These are physical surveying/localization infrastructure.

They are intended to:

1. connect SpatialBuild to surveyed project coordinates
2. establish or verify site geometry
3. detect moved control
4. provide absolute spatial references
5. eventually provide localization infrastructure for other devices

Do NOT treat them merely as Unity anchors.

---

# 15. NODE PHYSICAL DATUM

Each node must have an exact physical datum.

For example:

```text
NODE_01 project XYZ
```

must correspond to a literal known point on the physical device.

Possible future datum:

- machined survey socket
- marked center point
- prism center
- precision base reference
- mechanical datum

Do not define it vaguely as:

"the middle of the box."

Every sensor offset must ultimately relate back to this datum.

---

# 16. THREE-LAYER NODE CONCEPT

The current physical concept contains three independently useful measurement layers.

Conceptually:

```text
      ┌───────────────────┐
      │ ORTHOGONAL HEAD A │
      │ optical / laser   │
      ├───────────────────┤
      │ VARIABLE HEAD     │
      │ optical / laser   │
      ├───────────────────┤
      │ ORTHOGONAL HEAD B │
      │ optical / laser   │
      ├───────────────────┤
      │ COMPUTE           │
      │ IMU               │
      │ RADIO             │
      │ BATTERY           │
      ├───────────────────┤
      │ PRECISION DATUM   │
      └─────────┬─────────┘
                ▼
            survey point
```

Do not assume this final mechanical design is already solved.

This is the intended concept.

---

# 17. ORTHOGONAL HEADS

Two measurement heads are envisioned as approximately perpendicular directions.

Example:

```text
              NODE B
                 ●
                 ↑
                 │
                 │ dAB
                 │
NODE C ● ←──── NODE A
            dAC
```

The desired relationship may be approximately 90°.

BUT:

Do NOT assume the physical device is perfectly 90° because of mechanical construction.

Actual angular relationships must eventually be:

- measured
- encoded
- calibrated

Potential future measurements include:

```text
horizontal angle
vertical angle
distance
timestamp
uncertainty
measurement quality
```

---

# 18. VARIABLE ANGLE HEAD

The middle measurement layer should eventually rotate independently.

It should measure arbitrary angles.

Examples could include:

- 45°
- 60°
- 43.718°
- any other geometry

It is intended to provide additional geometric constraints.

Example:

```text
A → B

A → C

A → D at measured arbitrary angle
```

This helps create a redundant network rather than depending on only the minimum number of measurements.

---

# 19. OVERCONSTRAINED NODE NETWORK

The long-term node network should prefer redundancy.

Conceptually:

```text
A → B
A → C
A → D

B → A
B → C
B → D

C → A
C → B
C → D
```

Not every measurement must always be available.

The solver should eventually use redundant observations to detect:

- moved nodes
- incorrect distance measurements
- angular errors
- calibration failures
- blocked sight lines
- inconsistent geometry
- sensor faults

Do not build a full survey-adjustment package unless needed yet.

Architect the system so this can be added later.

---

# 20. NODE MOVEMENT SAFETY RULE

The current product decision is intentionally conservative:

## IF A TRUSTED CONTROL NODE MOVES, THE ENTIRE PRECISION BIM TURNS RED.

Do NOT silently remove the bad node and continue showing green during V1.

Example:

```text
EXPECTED A → C:
24.381 m

MEASURED:
24.406 m

RESIDUAL:
+25 mm

CONTROL NETWORK:
INVALID
```

Then:

```text
REGISTRATION STATE = INVALID
```

and the BIM turns red.

Future fault-tolerant exclusion may be added later.

Not now.

---

# 21. REGISTRATION STATES

Implement an explicit state machine.

At minimum:

```text
UNINITIALIZED
CALIBRATING
VALID
DEGRADED
INVALID
LOST
```

User-facing interpretation:

```text
GREEN
=
trusted

RED
=
not trusted

UNKNOWN / HIDDEN
=
insufficient positioning information
```

Do NOT show green simply because graphics visually appear aligned.

---

# 22. QUEST ABSOLUTE LOCALIZATION PROBLEM

The node network may know where the BUILDING belongs.

The headset must still know where it is relative to that project coordinate system.

For V1, use a practical optical reference approach.

The Quest cameras should detect known physical reference targets attached to control nodes.

Possible target technologies:

- AprilTag
- ArUco
- another reliable fiducial system

Each marker corresponds to a known node.

Example:

```text
TAG 17
↓
NODE_03
↓
known project XYZ
↓
known marker size
↓
known marker-to-node transform
```

Use:

- Quest camera frame
- camera intrinsics
- camera extrinsics
- timestamp
- camera pose
- physical target geometry

to produce an observation relating:

```text
QUEST SPACE
↔
PROJECT SPACE
```

Do not permanently hardwire the overall architecture to AprilTags.

They are only a prototype localization source.

---

# 23. LOCALIZATION SOURCE ARCHITECTURE

Implement a replaceable localization architecture.

Conceptually:

```csharp
public interface ILocalizationSource
{
    PoseMeasurement GetMeasurement();
}
```

Potential implementations:

```text
QuestTrackingSource
FiducialObservationSource
ManualSurveySource
SimulatedNodeSource
ExternalOpticalSource
TotalStationSource
UwbSource
FutureControlNodeSource
```

Then use a central:

```text
LocalizationManager
```

to combine or select measurements.

Do not let one prototype sensor become permanently embedded throughout the codebase.

---

# 24. MEASUREMENT STRUCTURE

Every localization measurement should eventually include:

```text
source
timestamp

position
orientation

position uncertainty
orientation uncertainty

quality

valid / invalid
```

Do not treat all measurements as equally reliable.

Every measurement must identify its source.

Examples:

```text
QUEST_VIO
FIDUCIAL_CAMERA
MANUAL_SURVEY
SIMULATED_NODE
EXTERNAL_OPTICAL
TOTAL_STATION
UWB
```

---

# 25. QUEST ROLE

Quest provides smooth local tracking.

Conceptually:

```text
QUEST VIO
fast
smooth
relative movement
      +
ABSOLUTE CONTROL OBSERVATIONS
slower
project-space truth
      ↓
LOCALIZATION ESTIMATE
      ↓
CORRECTED PROJECT-SPACE HEAD POSE
```

Do NOT repeatedly snap or reset the building every time a marker is observed if that produces visible jumps.

Implement the simplest mathematically sound correction method first.

Do not immediately build a massive sensor-fusion or Kalman-filter system unless necessary.

But ensure the architecture allows future filtering.

---

# 26. FUTURE LOCALIZATION OPTIONS

Do not implement all of these now.

Architecture should permit:

- external optical tracking
- rigid marker target attached to glasses
- multiple node cameras triangulating headset pose
- infrared tracking
- robotic total station
- survey prism tracking
- UWB
- environment geometry matching
- LiDAR
- custom glasses sensors
- BIM geometry matching

Quest 3 is not assumed to be final hardware.

---

# 27. IMPORTANT WORKER EXPERIENCE

The intended experience is NOT:

"lasers constantly point at the worker."

The intended experience is:

```text
PUT GLASSES ON

↓

PROJECT REGISTERS

↓

BUILDING APPEARS

↓

WORKER WALKS AROUND

↓

QUEST HANDLES SMOOTH LOCAL MOVEMENT

↓

ABSOLUTE REFERENCES PERIODICALLY CORRECT DRIFT

↓

BUILDING REMAINS FIXED
```

Positioning infrastructure should disappear into the background.

---

# 28. INITIAL REGISTRATION FLOW

Prototype startup should evolve toward:

```text
PROJECT SELECTED

↓

SEARCHING FOR CONTROL

↓

NODE_01 DETECTED
NODE_02 DETECTED
NODE_03 DETECTED

↓

SOLVING PROJECT REGISTRATION

↓

CHECKING RESIDUALS

↓

CONTROL VALID

↓

REGISTRATION ERROR ESTIMATE:
...

↓

SPATIALBUILD READY
```

Normal workers should not need to understand the math.

Developer diagnostics should expose the details separately.

---

# 29. REGISTRATION RESIDUALS

When solving registration from multiple references, do not return only a transform.

Calculate residuals.

Example:

```text
NODE_01 residual:
...

NODE_02 residual:
...

NODE_03 residual:
...

RMSE:
...

MAX RESIDUAL:
...
```

A mathematical solution should be rejectable if observations disagree excessively.

Thresholds should be configurable.

Do not invent claims of construction-grade accuracy.

---

# 30. LARGE SURVEY COORDINATES

This is extremely important.

Real civil/survey project coordinates can be numerically very large.

Unity uses single-precision floating point for normal world transforms.

Do NOT blindly place Unity GameObjects directly at enormous survey coordinates and assume millimeter precision will remain intact.

Investigate this explicitly.

Preferred architecture concept:

```text
TRUE PROJECT / SURVEY COORDINATES
        ↓
HIGH-PRECISION REPRESENTATION
        ↓
LOCAL PROJECT ORIGIN
        ↓
UNITY-LOCAL COORDINATES
```

Preserve true project coordinates separately.

Use a nearby local origin for rendering.

Possible future techniques may include:

- double-precision project coordinates
- floating origin
- origin rebasing
- local tangent/project frames

Choose the simplest robust strategy suitable for the prototype.

Document it.

---

# 31. TRANSFORM TESTING

Spatial math must be heavily tested.

Write deterministic tests for:

- translation
- rotation
- inverse transforms
- chained transforms
- BIM → project
- project → Quest
- Quest → project
- round-trip conversion
- unit conversion
- local-origin conversion
- origin shifting

Example invariant:

```text
P_project
→ Quest
→ Project
≈ P_project
```

within acceptable numerical tolerance.

Coordinate math should be among the best-tested components.

---

# 32. CALIBRATION ARCHITECTURE

Do not scatter calibration constants throughout scripts.

Create an explicit calibration/configuration layer.

Future calibration may include:

- marker physical size
- marker-to-node-datum transform
- camera parameters
- node orientation
- laser origin offsets
- encoder zero offsets
- angle offsets
- survey coordinates
- sensor offsets

Calibration data should be:

- inspectable
- serializable
- versioned
- associated with experiments

---

# 33. TIMESTAMPS

Localization data is time-dependent.

Every external measurement should contain a timestamp.

Do not combine old measurements with new measurements as though they were simultaneous.

Architecture should eventually be capable of rejecting stale measurements.

Document the timing source being used.

---

# 34. REAL VS SIMULATED POSITIONING

The existing project reportedly contains simulated node positioning.

Keep it.

But clearly separate it from real localization.

A simulated localization source must be labeled:

```text
SIMULATED
```

Never allow simulated measurements to silently contribute to a real-world accuracy result.

Accuracy reports must state exactly which localization sources were active.

Development mode and field mode should be clearly separated.

---

# 35. FIELD MODE VS DEVELOPER MODE

Developer/calibration mode may include:

- manual model movement
- manual rotation
- fake node data
- error injection
- coordinate overlays
- diagnostics
- debug menus
- measurement visualization

Field mode should eventually contain only necessary worker controls.

Do not delete existing debugging tools.

Organize them so they cannot be confused with real measurement.

---

# 36. VISUAL TRUST SYSTEM

When registration/control is valid:

render BIM guidance GREEN.

When trusted control becomes invalid:

render the ENTIRE precision BIM RED.

If position is no longer trustworthy at all:

hide precision guidance or show an UNKNOWN state rather than indefinitely displaying stale geometry.

Possible messages:

```text
CONTROL INVALID
DO NOT USE FOR LAYOUT
```

or:

```text
POSITIONING UNAVAILABLE
```

Do not hide failures.

---

# 37. ACCURACY PHILOSOPHY

Never claim:

- millimeter accurate
- ±5 mm
- ±3 mm
- construction grade

unless experiments prove it.

The prototype must MEASURE its accuracy.

Possible long-term targets can be investigated.

Do not assume them.

---

# 38. PHYSICAL ACCURACY TEST

Create known physical test points.

Example:

```text
P1
P2
P3
P4
P5
```

Their real physical/project coordinates must be independently known.

SpatialBuild renders corresponding virtual locations.

Record:

```text
timestamp
targetId

designX
designY
designZ

observedX
observedY
observedZ

errorX
errorY
errorZ

error3D

visibleNodes
localizationSources
registrationState

timeSinceCorrection
distanceSinceCorrection
calibrationVersion
softwareCommit
```

Calculate:

- mean
- median
- RMSE
- 95th percentile
- maximum error
- standard deviation where useful

Never invent measurements.

---

# 39. REQUIRED TESTS

Eventually perform:

### TEST 1
stationary headset

### TEST 2
walk 5 m

### TEST 3
walk 20 m

### TEST 4
walk away and return

### TEST 5
turn around repeatedly

### TEST 6
temporarily lose node visibility

### TEST 7
regain node visibility

### TEST 8
move one trusted reference

### TEST 9
restore/recalibrate reference

### TEST 10
restart application

### TEST 11
restart headset

### TEST 12
different lighting

### TEST 13
partial node occlusion

### TEST 14
multiple visible references

### TEST 15
single visible reference

Record actual results.

---

# 40. FIRST MAJOR DEMONSTRATION

The next important demo should be:

### REAL BUILDING HOLOGRAM TEST

Use:

- Quest 3
- real IFC project
- at least 3 physical control references
- known reference positions
- passthrough camera
- Quest tracking

Demo sequence:

1. Launch SpatialBuild.
2. Select Dental Clinic.
3. Detect physical control references.
4. Solve project registration.
5. Validate residuals.
6. BIM appears in project coordinates.
7. Structural discipline can be shown.
8. Walk around structural geometry.
9. Switch to mechanical.
10. Mechanical geometry remains in the exact same building frame.
11. Switch to another discipline.
12. Walk away.
13. Walk back.
14. Reobserve control.
15. Measure accumulated registration error.
16. Correct drift.
17. Deliberately move one trusted reference.
18. Detect the inconsistency.
19. Entire model turns red.
20. Display control failure.
21. Restore/recalibrate control.
22. Return to green.
23. Export accuracy log.

If this works reliably, the system has moved beyond a generic BIM viewer.

---

# 41. FUTURE NODE HARDWARE

Do NOT immediately build the final custom three-layer control node.

The first real-world localization prototype can use something much simpler:

```text
rigid physical base
+
precisely defined datum
+
large visual fiducial
+
known project coordinate
```

Use this to measure the actual registration limitations.

Then decide what custom hardware is required.

Possible future node components include:

- microcontroller
- wireless communication
- precision angle encoders
- laser distance module
- IMU
- electronic level/inclinometer
- cameras
- infrared sensors
- battery
- USB-C
- RGB status light
- buzzer
- survey mount
- magnetic mount
- adjustable feet

Do not choose final hardware without measured justification.

---

# 42. FUTURE NODE SETUP CONCEPT

The envisioned long-term workflow is:

```text
SURVEYOR ESTABLISHES CONTROL

↓

NODE A PLACED

↓

NODE B PLACED

↓

NODE C PLACED

↓

NODE NETWORK MEASURES
DISTANCES + ANGLES

↓

NETWORK SOLVER

↓

COMPARE WITH PROJECT GEOMETRY

↓

VALIDATE

↓

BUILDING COORDINATE FRAME ACTIVE
```

This is a future design direction.

Do not overbuild it before the Quest registration experiment.

---

# 43. LONG-TERM PLATFORM

Eventually the same project coordinate infrastructure may serve:

```text
AR GLASSES
ROBOTS
DRONES
SCANNERS
LAYOUT TOOLS
AUTONOMOUS EQUIPMENT
QA SYSTEMS
```

All of them could consume the same project coordinate system.

This is not part of the immediate implementation scope.

---

# 44. SOFTWARE MODULES

Adapt the actual repository rather than blindly creating these exact folders.

Preferred conceptual architecture:

```text
SpatialBuild.Core

SpatialBuild.Project
    ProjectManager
    ProjectManifest

SpatialBuild.BIM
    IFCImporter
    SpatialProject
    SpatialElement
    DisciplineManager

SpatialBuild.Coordinates
    CoordinateFrame
    RigidTransform
    ProjectTransform
    OriginManager
    TransformValidator

SpatialBuild.Localization
    LocalizationManager
    PoseMeasurement
    LocalizationState
    ILocalizationSource

SpatialBuild.Localization.Quest
    QuestTrackingSource

SpatialBuild.Localization.Fiducials
    FiducialDetector
    FiducialPoseSolver
    FiducialObservationSource

SpatialBuild.Control
    ControlNode
    ControlNetwork
    NetworkMeasurement
    NetworkValidator

SpatialBuild.Rendering
    BIMRenderer
    DisciplineRenderer
    RegistrationVisualizer

SpatialBuild.Diagnostics
    AccuracyLogger
    RegistrationLogger
    ExperimentRunner

SpatialBuild.UI
    ProjectSelector
    DisciplineSelector
    RegistrationStatus
```

Reuse existing systems when appropriate.

---

# 45. CONTROL NODE DATA MODEL

Create a serializable software representation for control references.

Example concept only:

```json
{
  "nodeId": "NODE_01",

  "projectPose": {
    "positionMeters": [0.0, 0.0, 0.0],
    "rotationQuaternion": [0.0, 0.0, 0.0, 1.0]
  },

  "datum": {
    "description": "center of survey reference point"
  },

  "fiducial": {
    "family": "APRILTAG",
    "id": 17,
    "physicalSizeMeters": 0.20
  },

  "calibration": {
    "version": "prototype-001"
  },

  "status": "VALID"
}
```

Adapt to the codebase rather than copying blindly.

---

# 46. GIT + UNITY WORKFLOW

The existing `SpatialBuild-Quest` folder should become or remain the Git repository.

Do NOT create a second duplicate Unity project merely for GitHub.

Intended workflow:

```text
LOCAL COMPUTER

SpatialBuild-Quest/
      │
      ├── Unity opens this folder
      ├── Codex edits this folder when local
      └── Git tracks this folder
               │
               ↓
             GitHub
```

Unity does not receive "pushes."

Unity simply sees the local project files.

If Codex works locally:

```text
CODEX EDITS
↓
UNITY PROJECT CHANGES
↓
TEST
↓
GIT COMMIT
↓
GITHUB PUSH
```

If Codex works remotely:

```text
CODEX
↓
GITHUB COMMIT
↓
LOCAL MACHINE
↓
GIT PULL
↓
UNITY SEES NEW FILES
```

Never blindly pull over uncommitted local Unity work.

---

# 47. GIT SAFETY

Before making substantial changes:

inspect:

```text
git status
```

Preserve uncommitted work.

Do NOT:

- force push
- rewrite history
- run destructive resets
- delete existing branches
- discard user work
- wipe the repository
- recreate Git history

unless explicitly instructed.

Use incremental commits.

---

# 48. GITHUB SETUP

If the project is not already tracked by Git:

1. initialize Git in the existing project root
2. add a Unity-appropriate `.gitignore`
3. preserve source files
4. create a clean initial commit
5. connect to a GitHub repository if credentials/access are available
6. push

If GitHub authentication is unavailable:

do not stop development.

Continue locally.

Document:

```text
GITHUB BLOCKER

Local repository ready:
YES / NO

Remote configured:
YES / NO

Authentication available:
YES / NO

User action required:
...
```

---

# 49. GITIGNORE

Do not commit generated Unity data.

Appropriate exclusions may include:

```text
Library/
Temp/
Logs/
obj/
Build/
Builds/
UserSettings/
MemoryCaptures/
```

Preserve important project data such as:

```text
Assets/
Packages/
ProjectSettings/
docs/
*.meta
```

Inspect the existing `.gitignore` before replacing it.

---

# 50. UNITY META FILES

Do not delete tracked `.meta` files.

Preserve Unity asset GUID relationships.

Inspect current Unity version-control and serialization settings before changing them.

Prefer Git-friendly text serialization where safe.

But do not trigger a massive project rewrite without documenting why.

---

# 51. LARGE FILES

Inspect for large binaries such as:

- IFC files
- FBX models
- textures
- scan data
- videos
- APKs
- point clouds

Do not commit unnecessary generated builds.

If important source files are too large for normal Git:

evaluate Git LFS.

Do not automatically migrate the repository to Git LFS without documenting:

- affected files
- sizes
- reason
- consequences

---

# 52. SECRETS

Never commit:

- passwords
- GitHub tokens
- API keys
- private certificates
- authentication credentials

If discovered:

do not print them into documentation.

Exclude them appropriately.

---

# 53. SHARED AI DOCUMENTATION

The repository should act as the persistent handoff mechanism between:

- Codex
- ChatGPT
- human developers
- future AI sessions

Create or maintain:

```text
docs/
    PRODUCT_SPEC.md
    ARCHITECTURE.md
    AI_HANDOFF.md
    HARDWARE_NOTES.md
    EXPERIMENT_LOG.md
    DECISIONS.md
```

Do not duplicate equivalent documentation if it already exists.

Update existing files where practical.

---

# 54. PRODUCT_SPEC.md

`PRODUCT_SPEC.md` should contain the authoritative product definition.

It should explain:

- survey-registered holographic BIM concept
- target users
- project coordinate philosophy
- Quest role
- node concept
- discipline filtering
- conservative red-on-control-loss behavior
- current prototype priority
- future custom-glasses direction

Do not let later speculative features silently redefine the product.

---

# 55. ARCHITECTURE.md

Document the ACTUAL project architecture after inspecting the repository.

Include:

- Unity version
- Meta XR SDK version
- OpenXR version
- Android requirements
- scenes
- XR rig
- passthrough setup
- project/BIM root
- localization components
- UI
- simulation
- packages
- tests
- build process
- IFC importer
- CV/fiducial library
- coordinate architecture

Also include a scene/prefab map:

```text
Primary scene:
Startup scene:
XR rig:
Passthrough:
BIM root:
Localization root:
Control-node objects:
UI root:
Persistent managers:
Important prefabs:
```

Use actual discovered names.

Do not invent them.

---

# 56. AI_HANDOFF.md

This file is critical.

Maintain:

`docs/AI_HANDOFF.md`

Use:

```text
# SpatialBuild AI Handoff

## Current Goal

...

## What Currently Works

...

## What Was Changed This Session

...

## Files Added

...

## Files Modified

...

## Files Removed

...

## Tests Run

...

## Build Status

...

## Quest Device Status

...

## Known Bugs

...

## Known Technical Limitations

...

## Architecture Decisions

...

## Open Questions

...

## Blockers

...

## Next Recommended Task

...

## Latest Commit

...
```

Update it after every meaningful implementation session.

Do not claim something works unless it was tested.

---

# 57. DECISIONS.md

Maintain an architecture-decision record.

Format:

```text
## DEC-001 — Title

Date:
Status:
Context:
Decision:
Reasoning:
Alternatives considered:
Consequences:
```

Examples:

- use IFC as source format
- use normalized SpatialBuild model
- preserve true survey coordinates separately from Unity-local coordinates
- Quest is not absolute truth
- full model red on trusted-control failure
- fiducials are prototype localization sources only

---

# 58. HARDWARE_NOTES.md

Document:

- node concept
- physical datum
- orthogonal layers
- variable-angle layer
- future distance sensors
- future angle encoders
- future calibration concerns
- hardware experiments
- parts considered
- measured limitations

Do not present untested hardware specifications as fact.

---

# 59. EXPERIMENT_LOG.md

Use:

```text
Experiment:
Date:
Objective:
Hardware:
Software commit:
Environment:
Procedure:
Ground truth:
Measurements:
Results:
Mean error:
Median error:
RMSE:
95th percentile:
Maximum error:
Failure observations:
Conclusion:
Next experiment:
```

Only record actual results.

---

# 60. COMMIT WORKFLOW

Use clear logical commits.

Examples:

```text
docs: add SpatialBuild architecture and handoff documentation

refactor: separate BIM and project coordinate frames

feat: add project manifest model

feat: import real IFC disciplines

feat: add fiducial localization source

feat: solve project registration from control references

feat: add registration trust state

test: add transform round-trip validation

feat: add accuracy experiment logger
```

Do not commit every tiny edit.

Do not create one giant meaningless commit either.

After meaningful milestones:

1. test
2. update docs
3. commit
4. push if possible
5. record commit hash in `AI_HANDOFF.md`

---

# 61. ACCEPTANCE GATES

Do not declare something complete simply because code exists.

### Coordinate architecture complete

Only when transform tests pass.

### IFC pipeline complete

Only when a real IFC project loads correctly.

### Discipline support complete

Only when real discipline data can be toggled without coordinate misalignment.

### Registration complete

Only when physical known references produce repeatable project registration.

### Hologram lock complete

Only when the user moves while BIM remains physically stationary.

### Control monitoring complete

Only when moving a trusted reference causes invalid state.

### Accuracy experiment complete

Only when physical ground truth has been measured and compared.

Keep documentation honest.

---

# 62. PERFORMANCE

Do not optimize prematurely.

Real BIM may be heavy.

Possible later optimization:

- discipline-based loading
- spatial culling
- metadata separation
- mesh batching
- LOD
- geometry streaming

But registration accuracy and coordinate correctness come first.

Document any mesh-processing step that alters transforms or coordinates.

---

# 63. IMPLEMENTATION ORDER

Follow this order unless repository reality requires a small adjustment.

## PHASE 1 — AUDIT

Inspect the repository.

Understand:

- scenes
- scripts
- packages
- current localization
- existing coordinate system
- current model pipeline
- simulated nodes
- logging
- tests
- Git status

Update `AI_HANDOFF.md`.

---

## PHASE 2 — DOCUMENTATION

Create/update:

- PRODUCT_SPEC.md
- ARCHITECTURE.md
- AI_HANDOFF.md
- HARDWARE_NOTES.md
- EXPERIMENT_LOG.md
- DECISIONS.md

Commit.

---

## PHASE 3 — COORDINATE ARCHITECTURE

Explicitly separate:

- BIM coordinates
- project coordinates
- Quest coordinates
- camera/head coordinates

Implement:

- rigid transforms
- origin management
- large-coordinate handling
- conversion tests
- round-trip tests

Commit.

---

## PHASE 4 — REAL IFC

Import the Dental Clinic IFC.

Create normalized SpatialBuild representation.

Preserve IFC GUIDs.

Validate units.

Validate coordinates.

Commit.

---

## PHASE 5 — REAL DISCIPLINE FILTERS

Map real model data into:

- architecture
- structure
- MEP subdivisions where available

Use existing menu interaction if possible.

Commit.

---

## PHASE 6 — PROJECT MANIFEST

Create simple project configuration.

Allow Dental Clinic to be selected and loaded deterministically.

Commit.

---

## PHASE 7 — CONTROL NODE DATA MODEL

Create control-reference configuration.

Initially allow manually entered known coordinates.

Commit.

---

## PHASE 8 — QUEST CAMERA / FIDUCIAL PIPELINE

Use supported Quest camera access.

Implement:

- image acquisition
- timestamp
- calibration access
- fiducial detection
- target ID
- target pose observation

Commit.

---

## PHASE 9 — PROJECT REGISTRATION

Given:

- known target project pose
- observed target Quest/camera pose

solve:

```text
T_PROJECT_QUEST
```

Use multiple references where possible.

Calculate residuals.

Commit.

---

## PHASE 10 — HOLOGRAM LOCK

Apply project registration to the BIM root.

Walk around.

Ensure BIM does not move with the user.

Commit.

---

## PHASE 11 — CONTROL TRUST MONITORING

Continuously compare trusted observations.

If a trusted node/reference moves beyond configurable criteria:

```text
REGISTRATION STATE = INVALID
```

turn the entire precision BIM red.

Commit.

---

## PHASE 12 — DRIFT CORRECTION

When trusted references are observed again:

measure disagreement.

Apply correction without unnecessary visible jumping.

Log:

- correction amount
- time
- references used
- residuals

Commit.

---

## PHASE 13 — ACCURACY LOGGING

Implement real experiment logging.

Export CSV and/or JSON.

Record measurement source and calibration.

Commit.

---

## PHASE 14 — PHYSICAL EXPERIMENT

Run the defined physical accuracy tests.

Do not fake results.

Update `EXPERIMENT_LOG.md`.

---

# 64. WHAT NOT TO DO BEFORE CORE REGISTRATION WORKS

Do not divert effort into:

- AI features
- worker productivity features
- tooltips
- construction scheduling
- voice assistants
- smart drill tracking
- production node enclosure design
- robotics
- elaborate UX
- marketing/demo polish

If choosing between:

A. another visible feature

or

B. better coordinate accuracy / registration / calibration / trust monitoring

choose B.

---

# 65. BLOCKED DECISION FORMAT

If you encounter a genuine architectural decision that cannot safely be inferred:

write this into `AI_HANDOFF.md`:

```text
BLOCKED DECISION:

Context:

Option A:

Option B:

Option C:

Recommended option:

Reason:

What remains independently implementable:
```

Do not stop all work.

Continue unrelated tasks.

---

# 66. FIRST RESPONSE BEFORE MAJOR IMPLEMENTATION

Before changing large parts of the project, inspect the repo and report concisely:

```text
1. Existing systems relevant to SpatialBuild's new architecture

2. What should be preserved

3. What should be refactored

4. Current coordinate implementation

5. Current BIM/model pipeline

6. Current localization implementation

7. Current simulated-node implementation

8. Existing packages/dependencies

9. Missing dependencies

10. Files to create

11. Files to modify

12. Main technical risks

13. Immediate implementation sequence

14. Genuine blockers
```

Then proceed with implementation.

Do not wait for permission after every small step.

Only stop for a genuine blocker.

---

# 67. CREDIT / COMPUTE CONSERVATION

Do not use Codex effort for unnecessary ideation.

Do not spend long sessions:

- re-explaining the product
- brainstorming alternative business ideas
- writing marketing copy
- redesigning the concept
- repeatedly summarizing this prompt
- building speculative features

I want Codex focused on:

- repository inspection
- code implementation
- package integration
- testing
- debugging
- Unity compilation
- Quest builds
- Git commits
- technical documentation

The architectural/product thinking is already contained in this prompt.

---

# 68. END-OF-SESSION REQUIREMENT

Before ending any major implementation session:

1. run appropriate tests
2. build where practical
3. update `AI_HANDOFF.md`
4. update `ARCHITECTURE.md` if architecture changed
5. update `DECISIONS.md` if a decision was made
6. update `EXPERIMENT_LOG.md` if an experiment was run
7. commit the work
8. push to GitHub if access is available
9. record the commit hash
10. summarize the actual verified state
11. identify the next recommended implementation task

The repository must remain understandable without access to this Codex chat.

---

# 69. FINAL DEFINITION OF SUCCESS

The current prototype succeeds when it can demonstrate:

> A real BIM building appears as a virtual structure occupying known physical project coordinates. A person wearing Quest 3 can naturally walk around and through that structure. Different BIM disciplines remain registered to the same physical building frame. External known references establish and periodically correct absolute registration. Quest tracking provides smooth movement between those corrections. Moving a trusted physical reference is detected and causes the entire precision model to become invalid/red. Physical experiments quantify the actual registration error rather than assuming accuracy.

The central research question is:

> Can inexpensive mixed-reality hardware plus external surveyed control references maintain a sufficiently accurate and trustworthy BIM-to-reality registration system for field construction layout?

Build the project to answer that question.

Do not get distracted from it.