---
name: phase
description: Implement the next (or a named) sub-phase from PLAN.md end-to-end — build to the CLAUDE.md standards, verify, mark ✅, and sync the docs. Use whenever the user says "continue with PLAN.md", "start phase N / next step", "do 2c", "finish phase 2", or otherwise drives work off PLAN.md.
---

# Implementing a PLAN.md sub-phase

This is the core SurveHive work loop. `PLAN.md` splits the `TODO.md` backlog into
ordered **sub-phases** (1A, 1B, 2C, …), each a self-contained ship-and-verify unit.
Legend: `☐` not started · `◐` in progress · `✅` done.

## Pick the target

- "next step" / "continue" → the first `☐`/`◐` sub-phase top-to-bottom in `PLAN.md`.
- A named one ("do 2c", "finish phase 2") → that sub-phase (or every remaining one in the phase).
- Read the sub-phase's row + touch-points **and** the referenced `TODO.md` rationale so you
  build what was actually wanted, not just the one-line summary.
- Big phase + limited tokens? The user often asks to split into sub-phases — honor that and
  ship the smallest shippable slice.

## Build it

- Obey `CLAUDE.md` to the letter: mobile zero-GC hot paths, `[SerializeField] private` + `_`
  prefixed fields, cache components in `Awake`, unsubscribe in `OnDisable`/`OnDestroy`,
  explicit `== null` on Unity objects (no `?.`/`??`), `CompareTag`, `sqrMagnitude`, no LINQ in
  runtime, small SOLID components over monolels.
- **Prefer additive, idempotent editor passes / targeted edits.** Do NOT re-run
  `BeehiveSceneBuilder.Build()`/`BuildAdditions()` — it clobbers hand-tuned data assets.
  Later `PhaseNBuilder.Apply()` methods use find-or-create and are safe. If a new asset/scene
  wiring is needed, add a new additive `Apply()` pass rather than editing the from-scratch build.
- Keep the scene validator green — extend it when you add wiring worth asserting.

## Verify (invoke the `verify` skill)

Run the relevant layers via `verify`'s `scripts/unity.sh` — background them:
1. `build <Builder>` if you added/changed a builder pass.
2. `validate` — expect `validation PASSED`, no `[FAIL]`.
3. `test EditMode` (and `PlayMode` for gameplay-facing changes).
4. `drive` when the change is visual and worth eyeballing (screenshots → `VerifyShots/`).

Obsolete-API (CS0619) warnings **fail** the build here — fix them, don't ignore.

## Close out the sub-phase (required, same session)

1. Flip the sub-phase marker to `✅` in `PLAN.md`.
2. **Docs upkeep is mandatory** (CLAUDE.md policy): if scope/story/mechanics/systems changed,
   update `README.md` (the living design doc) in this same session.
3. Add a `CHANGELOG.md` entry for what shipped.
4. Cross-check `TODO.md` — tick or remove anything this sub-phase resolved.

## Stop — do not commit

Ending a phase is **not** permission to commit. Summarize what shipped + verification
results and wait for the user's explicit OK. When they give it, use the `ship` skill.
