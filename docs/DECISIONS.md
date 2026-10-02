# Decisions

## DEC-001 — Preserve existing prototype and separate field mode
Date: 2026-10-02
Status: Accepted
Context: Working hand/UI/simulation systems coexist with unregistered previews.
Decision: Preserve developer scenes and add explicit field project/localization path.
Reasoning: Avoid replacing tested input while removing false precision assumptions.
Alternatives considered: rebuild project (rejected).
Consequences: field startup hides geometry until project and localization are available.

## DEC-002 — True coordinates and fail-closed trust
Date: 2026-10-02
Status: Accepted
Context: Unity floats cannot preserve millimetres at survey magnitudes.
Decision: Store survey coordinates/transforms in double precision; subtract a declared project origin before float rendering. All trusted-control failures latch INVALID; never exclude a failed node to retain green.
Reasoning: User product specification and auditable frame math.
Alternatives considered: direct float survey coordinates; permissive fault exclusion (rejected).
Consequences: physical validation remains separate from solver residual and simulated data.

## DEC-003 — Preserve geometry absence as data

Date: 2026-10-02
Status: Accepted
Context: The Dental Clinic MEP IFC contains 16,012 elements but zero `IfcProduct.Representation` values. Architecture and structure are renderable.
Decision: Store MEP GUIDs and metadata with `geometryAvailable=false`; do not synthesize pipes or coordinates. Identify another spatially compatible source before demonstrating MEP layout.
Consequences: this package verifies a real federated catalog and renders architecture/structure, but cannot yet show clinic MEP geometry.

## DEC-004 — Point controls and explicit physical validation

Date: 2026-10-02
Status: Accepted
Context: Printed tags may be mounted on different faces; their rotations may be unknown even when center points are measured.
Decision: Fit the Quest/project rigid transform from three or more known tag centers in double precision. Use marker orientation residuals only when independently calibrated. Keep `VALID` gated behind physical-validation records; simulated input is rejected by the field manager.
Consequences: calibration requires noncollinear measured centers and a declared project-frame tie. Relative fit residuals are diagnostic, not independent accuracy.

## DEC-005 — Pinned source and separate GitHub branch

Date: 2026-10-02
Status: Accepted
Context: IFC sources total about 158 MB and the user's GitHub repository has an unrelated initial commit.
Decision: pin remote IFC revision and hashes, commit normalized derivatives with attribution, and push the implementation to a branch connected to the destination history. Keep the Meta sample remote as upstream reference.
Consequences: the repository can reproduce the import without checking in the large raw source files; no force push or replacement of destination main is needed.
