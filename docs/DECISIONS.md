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
