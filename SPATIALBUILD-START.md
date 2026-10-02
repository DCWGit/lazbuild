# SpatialBuild Unity workspace

Open this folder as an existing project in Unity Hub using Unity 6000.6.3f1.
Android Build Support, SDK/NDK and OpenJDK were found in that editor installation.

The starting point is Meta's Unity-PassthroughCameraApiSamples at commit
`f9c382190907e20232a74c5ecf6513df47bc13b8`. Original sample licenses are retained.
SpatialBuild assets were copied from `../outputs/precision-bim/unity/Assets`.

The sample's Meta MRUK/Core 85 packages fail compilation on Unity 6.6 because
they call removed integer instance-ID APIs. MRUK is now pinned to 207.0.0,
which depends on Core 207.0.0. Unity upgraded its own packages during import;
the resolved versions are recorded in Packages/manifest.json and packages-lock.json.

## Desktop inspection

Open `Assets/SpatialBuild/Scenes/SpatialBuildInspection.unity` once setup completes,
or use the `SpatialBuild > Create desktop inspection scene` editor menu.
Press Play to load the fixture: room context, one pipe and five hangers.
The amber geometry is inspection-only and is not aligned to your room.

## Limits

A desktop compile does not verify headset rendering, hand interaction, camera
access, registration, tracking stability or physical accuracy. The saved inspection
scene is not an Android deployment scene. A Quest rig and device validation are next.
The optional pinch adapter is gated by SPATIALBUILD_META_XR and is not enabled yet.

The batch import and scene setup logs are `spatialbuild-import.log` and
`spatialbuild-setup.log`. The first records the original SDK incompatibility.

## Verified setup result
Unity 6000.6.3f1 completed batch import, C# compilation and inspection-scene creation with exit code 0. Meta MRUK/Core 207.0.0 resolved the original compile errors. Runtime visuals and Android/headset builds have not yet been tested.

