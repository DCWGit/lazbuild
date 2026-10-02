# Model and marker attribution

Dental Clinic architectural, structural, and MEP IFCs: BSI (2020), “Medical-Dental Test Files,” buildingSMART International, [upstream project](https://github.com/buildingsmart-community/Community-Sample-Test-Files/tree/main/IFC%202.3.0.1%20(IFC%202x3)/Medical-Dental%20Clinic), [IFC-Bench dataset mirror](https://huggingface.co/datasets/sylvainHellin/ifc-bench/tree/main/projects/dental_clinic). Licensed [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). The mirror describes a redacted real two-story clinic. Source download revision and each SHA-256 are pinned in `Tools/bim/sources.json`. The large source IFCs are fetched locally, while normalized mesh/metadata derivatives are kept in this repository with this attribution.

Official tagStandard41h12 images IDs 0, 1, 2: [AprilRobotics/apriltag-imgs](https://github.com/AprilRobotics/apriltag-imgs), revision `f3fd9a7add5bfd82a886fc65240fdb8e3c9ac5a1`, BSD 2-Clause license copied to `Tools/targets/LICENSE.txt`. `Tools/targets/generate.py` converts those exact images into the letter-size vector print sheet.

AprilTag Unity detector: [Keijiro Takahashi's Unity package](https://github.com/keijiro/jp.keijiro.apriltag), revision `fd6dd4698c9c6d2dc4a5e676beeab7f620006c78`, BSD 2-Clause license retained in `Packages/jp.keijiro.apriltag/LICENSE`. SpatialBuild adds a calibrated intrinsics overload; see `FIDUCIAL_VENDOR.md`.

The original Meta Unity Passthrough Camera API sample and its licenses remain in this repository. SpatialBuild uses the installed Meta MRUK camera access API for capture-time pose and intrinsics.
