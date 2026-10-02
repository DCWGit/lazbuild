# SpatialBuild AI Handoff

## Current Goal
Implement survey-registered real BIM following PRODUCT_SPEC.md. Preserve the working Quest developer demo.

## What Currently Works
Prior user-confirmed: Quest passthrough, room-fixed model, hand pointing/pinch, bottom wheel UI. Latest worker guide compiled, passed synthetic tests and launched without an immediate crash; user has not confirmed that guide. Physical registration and accuracy have not been measured.

## What Was Changed This Session
Audit and documentation baseline. No precision functionality claimed yet.

## Files Added
This docs directory. See subsequent commits for implementation.

## Files Modified
.gitignore excludes local build logs and generated developer credentials.

## Files Removed
None.

## Tests Run
Audit only so far. Prior reports remain at repository root and Reports/.

## Build Status
Prior Android build succeeded, zero errors, 334 warnings. APKs are local ignored output.

## Quest Device Status
Previously connected Quest 3; check live before deployment.

## Known Bugs
No field project selector; startup currently auto-places the developer fixture.

## Known Technical Limitations
No optical localization source, survey control, or physical accuracy measurements. Legacy IFC exporter rejects large coordinates and ignores IFC map conversion. RealPositioningProvider is fail-closed.

## Architecture Decisions
See DECISIONS.md. Project coordinates are not headset coordinates.

## Open Questions
Destination GitHub repository; physical measured control setup.

## Blockers
GitHub: existing repository YES; origin points to Meta upstream, no user destination; gh CLI unavailable, authentication not established. Do not push to upstream. Physical tests require measured references.

## Next Recommended Task
Double-precision coordinate chain, normalized real IFC package, then capture-time optical localization and trust tests.

## Latest Commit
This audit is committed before implementation. Use git log; subsequent handoff update records verified milestone hashes.
