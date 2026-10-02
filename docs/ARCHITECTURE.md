# Actual architecture (audit baseline)

Unity 6000.6.3f1; Meta MRUK/Core 207.0.0; OpenXR 1.18.0; XR Management 4.7.0; Android ARM64 IL2CPP, min SDK 32, target 34.

Primary/startup scene: Assets/SpatialBuild/Scenes/SpatialBuildQuest.unity.
XR rig: official Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab, Stage tracking origin.
Passthrough: scene object Passthrough with OVRPassthroughLayer, transparent camera clear.
BIM/debug root: ConstructionWorld, currently generated example geometry.
Localization root: Manual project registration with QuestRegistrationController.
UI: HandPreviewControls creates the head-relative CurvedPreviewPanel bottom wheel; OVRHand components under rig hand anchors provide pointer/pinch.
Control objects: simulated nodes are generated in SpatialBuildPositioningLab, not real Quest control references.
Persistent managers: none across scenes. Job progress and logs persist in Application.persistentDataPath, not spatial registration.
Other scenes: SpatialBuildInspection and SpatialBuildPositioningLab. Original upstream sample scenes retained.

Construction/: fixture elements, job definition and progress. Quest/: input, UI, preview and manual calibration. Positioning/: double-valued node math, simulation, float Unity manual registration. Diagnostics/: desktop network reports. Runtime/: earlier exported-model viewer and an unimplemented fiducial-source interface.

Existing Python IFC importer lives outside this Git root at ../outputs/precision-bim. It uses IfcOpenShell, NumPy and a normalized export. It must be brought into a reproducible in-repository pipeline; don't depend on the sibling directory in production. IFC map conversion is currently not applied; the exporter rejects large coordinates.

Camera acquisition is available in the installed MRUK PassthroughCameraAccess API but not connected to localization. No CV library integrated at audit time.

Build: Unity -batchmode -quit -projectPath <root> -buildTarget Android -executeMethod SpatialBuild.QuestPrototypeSetup.Build -logFile <path>. That method runs registration, hand, UI and job checks. NetworkVerification covers the simulator. Device operations use hzdb with Unity SDK adb path when needed. Generated logs, APKs and Library are not source control.
