# Hardware notes

The three-layer optical node is a concept, not built or calibrated: two approximately orthogonal heads plus an independent variable-angle head. Motor command is not an angle measurement. Encoders, range origin offsets, inclinometer reference and optical centering require calibration to a literal mechanical datum (survey socket/prism/reference mark).

First physical prototype: rigid visual target with measured size and a defined marker-to-node datum transform. Three known references and independent checkpoints are needed for the requested experiment. No final sensors, power budget or precision claims have been validated. Quest 3 is the borrowed development platform, not final glasses.

The immediate optics are passive paper AprilTags, not the three-layer laser-node concept. `Tools/targets/SpatialBuild-3-Control-Targets.svg` provides three IDs on a US Letter sheet. Print at actual size, verify the 100 mm line, measure each tag at the detector's black/white border, and mount it rigidly. For software tests, a tape can establish a local experimental frame, but its uncertainty must be recorded; use appropriate survey equipment and control when evaluating construction tolerances. The reference has a literal physical datum: tag center (or a measured offset from it). See `PHYSICAL_SETUP.md`.

Future active node designs must independently measure optical head angles, ranges, level and offsets and solve an overdetermined network. The AprilTag source implements a replaceable localization path; it does not validate laser specifications or establish the eventual 8-hour glasses power budget.
