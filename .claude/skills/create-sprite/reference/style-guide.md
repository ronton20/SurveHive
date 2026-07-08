# PixelLab generation parameters — canonical, don't improvise per-call

These values exist so every asset generated over many separate sessions still looks like
it belongs to the same game. Pull them from here verbatim; only the `description` text and
size should vary per asset. If a new tier shows up that isn't listed, add it here first
rather than inventing one-off params inline.

## Shared vocabulary (reuse this language in every `description`)

Palette words: "honey-gold", "comb-brown", "wax-cream", "amber", "royal purple" (bosses/royal
items only). Silhouette words: "hard pixel edges, no anti-aliasing, flat/indexed shading".
Always end object/tile descriptions with "transparent background" unless it's a tileset.

## Character / Boss tier → `create_character` + `animate_character`

| Param | Value | Why |
|---|---|---|
| `view` | `low top-down` | ~20° 3/4 RPG angle — matches the tile/world camera. Never `side` or `high top-down` for playable rigs. |
| `outline` | `single color black outline` | matches existing placeholder rigs and the icon set |
| `shading` | `basic shading` | flat-ish, reads at 32px; `medium`/`detailed` gets muddy at this resolution |
| `detail` | `medium detail` | `high detail` fights the low pixel budget |
| `text_guidance_scale` | `8` (default) | leave alone unless a generation drifts wildly off-prompt |
| `mode` | `standard` first | 1 generation. Only escalate to `v3` (2-9 gens) or `pro` (20-40 gens) if standard's result is unusable — **and only after telling the user the cost and getting a go-ahead**, trial/paid balance is finite (check `get_balance`). Confirmed: for non-human cast (bees), `standard` mode's skeleton-based generation always reads as "a person in a costume" no matter how proportions are biased — the skeleton itself is human. `v3` mode isn't locked to that literal human rig and follows an explicit "not a human/person, an actual insect body" description far more faithfully — reach for `v3` directly for any insectoid/creature character rather than burning a `standard` attempt first. `v3` requires `size` to be a **multiple of 4** (validation error otherwise) — round the `canvas/1.4` formula up/down to the nearest 4. `v3` also ignores `n_directions` (always generates 8) and `proportions`/`shading`/`text_guidance_scale` — put all shape intent into the `description` text instead. |
| `n_directions` | `4` (the tool's minimum — `create_character` itself is always 1 generation regardless of this count). Confirmed: `CharacterAnimator.cs` only ever renders a right-facing sprite and flips `localScale.x` for left — there is no multi-directional facing in this game today, for player OR enemies (shared `Controller.controller`/`CharacterAnimator`). So **only animate the `east` direction** in `animate_character` (`directions=["east"]`) — this is what makes a 5-clip rig cost ~5 generations instead of ~40. Don't animate all directions "just in case"; it burns budget on art the game can't display. |
| `size` (px) | `round(canvas / 1.4)` | the tool pads ~40% for animation headroom. Character tier canvas 32 → size ≈ 23. Boss tier canvas 64 → size ≈ 46. |
| `proportions` | **never a preset** (`heroic`/`chibi`/`cartoon`/etc.) for insectoid cast members — those presets are human-body templates and `standard` mode's skeleton-based generation follows them literally, producing a person-in-a-bee-costume instead of an actual bee (confirmed by generating one). Use **custom** proportions biased toward insect anatomy instead: big `head_size` (1.3–1.6, compound eyes dominate the head), short `arms_length`/`legs_length` (0.6–0.7, vestigial limbs), narrow `shoulder_width` (0.6), wide `hip_width` (1.3–1.4, reads as a bulbous striped abdomen). Pair with a description that explicitly rules out the human reading, e.g. "an actual bee creature, not a human in a costume — round fuzzy striped abdomen, big compound eyes covering most of the head, short stubby insect limbs, four small wings on the back". Bump `text_guidance_scale` to ~12 when fighting this bias. |

### Animation clip → PixelLab template mapping

Use `animate_character` in **template mode** first (1 generation/direction) — cheapest,
and gives Unity-standard frame counts for free. Only fall back to `v3` (custom
`action_description`, 1 gen/direction, frame_count 4-16) when no template fits, and `pro`
only with explicit user confirm (20-40 gen/direction).

| Spec clip | Template | Notes |
|---|---|---|
| idle | `breathing-idle` | |
| run | `walking-6-frames` | matches the 6-frame run called out in ASSET_GENERATION.md |
| attack | `lead-jab` or `cross-punch` | pick whichever reads better as a "sting" |
| hit | `taking-punch` | template frame count is fixed by PixelLab, not user-chosen — don't fight it |
| die | `falling-back-death` | |

Template frame counts are decided by PixelLab, not us — if ASSET_GENERATION.md's frame
counts (e.g. "hit 2-frame") don't match what comes back, that's expected; update the doc's
wording after the fact rather than forcing an exact count via v3.

Confirmed against the current rig (`Assets/ThirdParty/PixelFantasy/PixelMonsters/Common/
Animation/*.anim` + `CharacterAnimator.cs`): today's placeholder uses Idle 4 / Run 6 /
Attack 6 / Die 6 frames, and "Hit" isn't a frame swap at all today — it's just a red color
flash (`Hit.anim` animates `m_Color`, no dedicated sprite frames exist). Generate a real Hit
clip anyway per the ASSET_GENERATION.md spec (cheap — 1 generation) so it's available if
`CharacterAnimator`/`Hit.anim` is ever upgraded to use real frames, but flag in the handoff
report that wiring it in is a separate code change, not implied by generating the art.

**Before spending on animations**, multiply clips × directions and check it against
`get_balance`'s `generations_remaining`. If it's a large fraction of what's left, stop and
tell the user the cost breakdown before calling `animate_character`.

## Pickup / Icon tier → `create_1_direction_object` (top-down view)

| Param | Value |
|---|---|
| `view` | `top-down` |
| `size` | tier canvas: 16 for Pickup, 32 for Icon |
| `style_images` | if `reference/anchors.json` has an anchor for this tier family, pass its cached base64/image as style reference so new pickups/icons match existing ones. If not, this generation *becomes* the anchor — save it back to anchors.json. |

Icons (§2 of ASSET_GENERATION.md) and world Pickups (§1.x) are different families —
don't cross-pollinate their anchors (a UI badge and a world sprite read differently even
in the same game). Track them as separate anchor entries: `icon_anchor` and
`pickup_anchor`.

For multi-shape prompts (e.g. §2.1's 10 passive icons in one prompt), prefer generating
them as **separate calls**, one per icon — `create_1_direction_object` takes one
`description` and mixing N distinct shapes into a single prompt string produces
inconsistent results per shape. Reuse the same style anchor across all calls in the set so
they land consistent with each other.

## Tile tier → `create_topdown_tileset`

| Param | Value |
|---|---|
| `view` | `low top-down` — match the Character tier's camera angle, not the tool's `high top-down` default. A game where floor and characters are rendered from different angles reads as broken. |
| `tile_size` | `{width: 16, height: 16}` |
| `outline` | `single color outline` (tiles don't get the character's flat-black outline treatment — matches typical tileset conventions) |
| `shading` | `basic shading` |

## Chrome tier (banners, world-select thumbnails)

Bespoke sizing per §0.1 — no shared tool mapping yet. If asked to generate one of these,
stop and ask the user which PixelLab tool fits (likely `create_ui_asset` for banners), since
none has been validated against this game's look yet.

## Integrating a Character-tier rig without touching shared art

The player and every enemy share one `Controller.controller` + one set of `Idle`/`Runing`/
`Attack`/`Death` `AnimationClip`s, which animate a `SpriteResolver` by category+label
(`Idle` "0".."3", `Run` "0".."5", `Attack` "0".."5", `Death` "0".."5") — the actual sprites
come from whatever `SpriteLibraryAsset` is assigned to that GameObject's `SpriteLibrary`
component. Swapping one character's look **never** means touching the shared clips or
controller: build a new `SpriteLibraryAsset` with the same category/label names pointing at
the new sprites (`SpriteLibraryAsset.AddCategoryLabel(sprite, category, label)`), then call
`Phase1LookAndFeelBuilder.BuildBeeRig(gameObject, flashMaterial, sortingOrder, libraryPath:)`
— it's already parameterized for this. This is how the Hero Bee rig (§1.1) was wired onto
the Player only, leaving `YellowBee.asset`/enemies completely untouched and the swap a
one-line revert. Reuse this pattern for cosmetic skins (§2.10) and any future per-character
art swap — don't invent a new wiring mechanism per character.

If a generated clip has fewer frames than its category needs (e.g. a 3-frame attack against
a 6-label category), pad by reusing existing frames in a deliberate pattern (a ping-pong
out-and-back reads as a considered two-beat attack) rather than spending more generations —
only regenerate if the padding visibly stutters.

## Consistency across a session vs. across sessions

`create_character_state` can derive a *variant* of an already-generated character
(recolor, pose, accessory) while keeping identity — prefer it over a fresh
`create_character` call whenever the ask is "same character but X" (e.g. cosmetic body
colors in §2.10, or a boss that should visually rhyme with the hero).
