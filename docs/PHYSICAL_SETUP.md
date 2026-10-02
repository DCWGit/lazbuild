# First measured registration trial

The software is ready for a controlled target trial. It cannot claim construction accuracy until this procedure is performed and documented. The current Dental Clinic IFC coordinate system is a model engineering frame; it is not surveyed into the user's room or any jobsite.

1. Print `Tools/targets/SpatialBuild-3-Control-Targets.svg` on US Letter paper at **100% / actual size**. Disable fit-to-page. Verify the printed 100 mm line with a ruler. The sheet contains IDs 0, 1 and 2 from `tagStandard41h12`. The images come from a pinned official AprilRobotics release; see `SOURCE_ATTRIBUTION.md`.
2. Mount each tag flat on a rigid, stationary surface with a clear white margin. Measure the edge between its **black and white detection borders** on the printed page in two directions; enter the measured size in metres as `markerSizeM`. The nominal SVG image is 80 mm across its entire nine-cell image; the detection border is inside it. Do not enter 80 mm without measuring the actual print.
3. Define one explicit local project coordinate frame: origin, X direction, Y direction, Z up, and metre units. Establish the 3D centers of all three markers in that frame with an appropriate independent instrument. Place them noncollinearly; one straight line is insufficient. Record mounting surface, movement protection, control measurement uncertainty, time, operator, and the marker-to-datum offset. Tape-measure setup is suitable for an exploratory trial, not a millimetre construction claim.
4. A model placement experiment requires a documented transform from the Dental Clinic's IFC engineering coordinates to that measured project frame. `project.json` currently declares identity `bimToProject`; only use it if the three target coordinates are explicitly chosen in that same engineering frame. Otherwise determine and record the rigid BIM-to-project transform and project origin, regenerate/validate the manifest, and rebuild. A visual alignment by eye is only a blue inspection preview.
5. Fill `Tools/targets/measured-centers-template.csv` with the measured centers and printed border sizes, then run `python Tools/targets/create_profile.py <your.csv> --version <new-version> --basis "<instrument and coordinate-frame description>"`. This creates `Assets/SpatialBuild/Resources/DentalClinic/control-profile.json` with the exact project revision, unique IDs, center datums, and `physicalValidationPassed=false`. The point fit does not require tag plane orientation. The generator marks a center as `surveyed=true` only because you supplied measured project-frame coordinates; that flag alone is never a construction-accuracy claim. If the physical datum is offset from the tag center, edit `datumFromMarker` with the measured offset and document it.
6. Rebuild the Quest APK. In field mode, use `PROJECT → LOAD`, then `CONTROL → START`. Grant the headset camera permission if requested. Scan all three IDs within the configured 20-second calibration window. `DEGRADED` with a red model means the references fit, but physical validation is pending. `INVALID` means the controls disagree; inspect/mend the moved reference and explicitly restart calibration. Loss of absolute observations hides the model. Blue `INSPECT` is a scaled visual preview only.
7. Place at least one independent checkpoint that was **not used** in the fit. Compare the displayed design point with an independently observed physical position, record repeated measurements and conditions in `Tools/measurements/physical-checkpoints-template.csv`, then run `python Tools/measurements/analyze.py <your.csv> --output <results.json>`. Record instrument calibration/uncertainty and the trial's actual tolerance. Repeat after leaving/returning, intentional occlusion, recentering and moving a control. No existing synthetic report or single residual substitutes for this trial.

Profile structure (illustrative placeholders, **not** measured coordinates):

```json
{
  "schemaVersion": "spatialbuild.control.v1",
  "projectId": "dental-clinic",
  "projectRevision": "copy the exact project.json revision",
  "calibrationVersion": "measured-setup-001",
  "surveyBasis": "describe the instrument, datum and project-frame tie",
  "controls": [{
    "nodeId": "control-1", "datumDescription": "center of the printed black/white border",
    "calibrationVersion": "measured-setup-001", "family": "tagStandard41h12",
    "markerId": 0, "markerSizeM": 0.062,
    "projectFromDatum": {"position": {"x": 0, "y": 0, "z": 0}, "rotation": {"x": 0, "y": 0, "z": 0, "w": 1}},
    "datumFromMarker": {"position": {"x": 0, "y": 0, "z": 0}, "rotation": {"x": 0, "y": 0, "z": 0, "w": 1}},
    "surveyed": false, "physicalValidationPassed": false, "orientationCalibrated": false
  }]
}
```

The example's 0.062 m and zero position are placeholders; measure and replace them. Three distinct entries are required before calibration can begin. An independent physical test is needed before any layout-grade claim.
