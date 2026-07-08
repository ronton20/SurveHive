---
name: create-sprite
description: Generate one visual asset from ASSET_GENERATION.md via the PixelLab MCP server — look up its spec/prompt/tier, generate with consistent style params, stage the result in the project, and update the doc. Use whenever the user wants to generate, create, or produce game art/sprites for SurveHive (not audio — that's a separate, not-yet-built workflow).
---

# Generating a sprite from ASSET_GENERATION.md

This turns one row of `ASSET_GENERATION.md` into an actual sprite via the `pixellab` MCP
server, without producing art that feels bolted-on. Read `reference/style-guide.md` before
your first `create_*` call in a session — it holds the canonical params (view/outline/
shading/size formulas per tier) that keep every generated asset visually cohesive. Don't
improvise those params inline; the whole point of this skill is *not* re-deriving style
choices per call.

## Step 1 — Find the entry

Locate the row in `ASSET_GENERATION.md` the user means (by section number like "1.1", or by
name/keyword search). Pull out:
- **Tier** (Pickup/Character/Boss/Tile/Icon/Chrome) — from its Spec line, per §0.1.
- **Spec** — exact size, palette.
- **Usage context** — what it replaces / where it's used.
- **Generation prompt** — the ready-made prompt text; reuse its content but restructure the
  call into the MCP tool's actual parameters (don't just paste the whole prompt string into
  a `description` field — pull out palette/pose details as their own params where the tool
  supports them, e.g. `view`/`outline`/`shading`/`proportions`).
- **Current status** emoji.

If the entry covers a *set* of icons (e.g. §2.1's 10 passive icons in one prompt), generate
them as separate calls — see style-guide.md's note on this.

## Step 2 — Map tier to tool + params

Per `reference/style-guide.md`:
- **Character / Boss** → `create_character` (+ `animate_character` for clips)
- **Pickup / Icon** → `create_1_direction_object` (top-down view)
- **Tile** → `create_topdown_tileset`
- **Chrome** → not yet validated; stop and ask the user which tool fits before generating.

Check `reference/anchors.json` for an existing style anchor in the same tier family and
pass it as a style/reference image where the tool supports it (objects only — the character
tool takes no image input, so character consistency instead comes from using identical
params + shared palette vocabulary every time).

## Step 3 — Budget check before spending

Call `get_balance`. Standard-mode character bodies and single objects are 1 generation each
— cheap. Animating a character is **1 generation per direction per clip** in template/v3
mode — this adds up fast (5 clips × 8 directions = 40 generations, potentially an entire
trial balance). Before calling `animate_character`, compute the total cost and:
- If it's a small fraction of `generations_remaining`, proceed.
- If it's a large fraction (rule of thumb: >25%), stop and tell the user the exact
  breakdown (clips × directions × cost) and let them decide how to scope it down (fewer
  directions, fewer clips first, or explicit go-ahead to spend it all).
- Never pass `mode="pro"` or `confirm_cost=true` without the user explicitly confirming the
  cost shown to them first — this mirrors the tool's own built-in safeguard.

## Step 4 — Generate, poll, review

Kick off the generation, then poll with `get_character`/`get_object` (they report
processing/failed/completed with progress+ETA). Once completed, look at the preview image
and sanity-check it against the spec (right silhouette, right palette, readable at actual
size) before moving on. If it's clearly wrong, regenerate rather than accepting a bad
result — don't spend the user's generations twice on a review pass they didn't ask for,
but do flag if something looks off before staging it.

## Step 5 — Stage the output (don't silently overwrite live assets)

Download images with `scripts/fetch.sh <url> <path>`. Save to
`Assets/Sprites/_Generated/<AssetName>/` — **not** directly over the live placeholder path
(e.g. not straight over `Assets/Sprites/Player.png`) — because:
- A generated character rig is a multi-direction/multi-frame result; the live placeholder
  is often a single static sprite. They're not drop-in compatible without an import/slicing
  pass (splitting sheets into Sprites, building AnimationClips, wiring the Animator/
  SpriteLibrary).
- Swapping a constantly-visible asset (the hero, its projectile) is exactly the kind of
  hard-to-reverse, visible change that warrants the user looking at it first.

Write a small `manifest.json` alongside the staged files recording: the PixelLab
character_id/object_id(s), the params used, the date, and which ASSET_GENERATION.md entry
this fulfills. This is what lets a later session resume/extend the same character instead
of regenerating from scratch.

If this generation is a good future style anchor (first of its tier family, or explicitly
the project's "canonical" look), update `reference/anchors.json` with its id.

## Step 6 — Update ASSET_GENERATION.md (same session, per CLAUDE.md policy)

- Don't delete the row yet if the asset is only staged, not wired into the actual
  game (prefab/Animator/SpriteResolver) — that's a separate integration step. Instead
  update its **Status** line to note generation happened and where it's staged, e.g.:
  `**Status:** 🟢 generated, staged at Assets/Sprites/_Generated/HeroBee/ — pending in-editor integration`
- Only delete the row once the final art is actually placed and wired in-project (the doc's
  own stated convention — "delete rows once the final asset is in the project").
- Add a line to the **Change log** section at the bottom noting what was generated and when.

## Step 7 — Report and hand off

Tell the user: what was generated, where it's staged, PixelLab generation cost spent,
remaining balance, and what the integration step would involve if they want to do it next
(you can offer to do it, but importing/wiring is a distinct follow-up, not implied by "generate
the sprite").
