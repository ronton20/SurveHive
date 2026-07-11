# SurveHive — Asset Generation List

> **Living document.** This is the working list of every art/audio asset that still needs
> to be generated (AI-generated via tools like ElevenLabs for audio, or commissioned/drawn
> in Aseprite for visuals) to replace a placeholder or fill a gap called out in
> `TODO.md`/`README.md`. Update this file in the same session as any change that adds,
> generates, or retires an asset need — tick items off, add new ones as systems grow, and
> delete rows once the final asset is in the project. See `CLAUDE.md` for the update policy.

## How to use this

Each entry has four fields:
- **Status** — 🔴 not started · 🟡 placeholder in place (functional, but not final) · 🟢 spec'd/prompt ready, generation in progress
- **Spec** — canvas tier + size (see Design System below) and palette.
- **Usage context** — where in the game it appears and what it replaces.
- **Generation prompt** — a ready-to-paste prompt for an AI generation tool (image model for
  visuals, [ElevenLabs](https://elevenlabs.io) Sound Effects/Music for audio) or a brief for a
  human artist.

---

## 0. Design system (read this before generating or prompting anything)

Everything below exists to keep the finished game feeling like **one cohesive object**
instead of a pile of mismatched placeholders — consistent scale, consistent silhouette
language, and no redundant regeneration of what's really just a recolor.

### 0.1 Scale tiers — every asset snaps to one of these, no exceptions

Mixing icon sizes (the old draft had 48px/64px icons sitting next to 32px ones) is exactly
what makes a game feel cheap. One tier system, used everywhere:

| Tier | Canvas | Used for |
|---|---|---|
| **Pickup** | 16×16 | EXP orb/mote, currency drop, item drops (Honey Jar, Magnet, Wax Shield, Royal Bomb), basic stinger dart, all elemental attack VFX (ember bolt, frost shard, static arc, honey glob, pollen puff, honey puddle, lance dart) |
| **Character** | 32×32 per frame | player rig, trash/elite enemy rigs, future-world enemy rigs |
| **Boss** | 64×64 per frame | miniboss/boss rigs (Royal Guard, Queen Bee) — exactly 2× Character, never its own arbitrary size |
| **Tile** | 16×16 base tile | tilesets (Beehive floor, future-world tilesets) |
| **Icon** | 32×32 | **every** icon-style UI element: power-up card icons, element badges, status-effect icons, difficulty icons, the premium-currency icon, achievement badges, UI glyphs — world-space or screen-space, all one size |
| **Chrome** | bespoke, built on a 4px sub-grid | the only intentional exceptions: lane banners (sized to fit their text) and world-select thumbnails (320×180 full-scene illustration cards, matching the game's reference resolution) |

If a current placeholder doesn't match its tier (e.g. today's pickups are odd sizes like
7×7 or 12×5, a holdover from ad hoc code-generated art), the replacement should still target
the clean tier size — minor prefab rescaling on import is expected and cheap.

### 0.2 Prompt template

Every prompt below follows this shape — copy the pattern when adding new entries:

> "**{{subject}}**, pixel art for SurveHive — a 16-PPU top-down-plane bee/honey
> survival-action game (Vampire-Survivors-style); **{{tier}}** canvas, **{{size}}**;
> hard pixel edges, no anti-aliasing, flat/indexed shading, transparent background.
> {{subject-specific details: palette, pose, mood}}"

### 0.3 Colorblind-safe shape language

Players who can't rely on color need silhouette to carry meaning, so shape is never
decorative — it's information:

1. **The 5 status-effect icons must be mutually unique silhouettes** — no two share a base
   shape, checked within that one set (they appear together over an enemy's head).
2. **The plain droplet shape is reserved exclusively for a future Bleed status effect.**
   Don't use an unadorned droplet as any other icon's *primary* silhouette. A droplet as a
   *minor accent* (e.g. splashing off a honey-glob impact) is fine — the reservation is
   about a droplet being the icon's main read, not about droplets never appearing.
   (World pickups like the honey-drop currency icon are a different context — a
   floating collectible, not a status badge over an enemy — and are exempt.)
3. **Uniqueness applies within each set, not across sets.** It's fine — good, even — for a
   power-up that *causes* a status to echo that status's motif (Burning Stinger's icon can
   include a flame, matching both the Burn status icon and the Fire element badge). That
   repetition teaches the association. The rule only blocks two icons *in the same
   simultaneous-comparison context* from sharing a shape.

### 0.4 Generate a neutral base, tint at runtime — don't regenerate per color

The game already does this for EXP orbs today (a neutral white sprite retinted by value
tier, per `README.md`) and it's the right pattern generally: if an asset's only variation
between "versions" is **color**, generate **one neutral/grayscale (or flat-white) base**
and apply the existing runtime-tint pipeline instead of prompting/paying for N separate
color generations. Reserve full separate-asset generation for variants that differ in
**shape or material**, not just hue (e.g. the hero's cosmetic stinger skins — steel vs.
crystal — genuinely need different rendering, not just a tint).

---

## 1. Player & Beehive world — replacing active placeholders

#### 1.1 Hero bee animation rig — new direction: humanoid bee-person
- **Status:** 🟢 live on the Player, design approved. The insect-bodied hero (character
  `698b84ba-2ef7-4d05-9380-38484f70f3a9`) that was briefly live on the Player was rejected
  2026-07-08 ("i dont like it, i think i want to go in a different direction"). **New
  concept: the bee evolves into a humanoid version of itself.** Male variant designed and
  approved (character `4696ce65-b8f1-4d37-a34b-8cc3fe4a5ea6`, v3 mode) — yellow skin, black
  hair, antler-like antennae, shirt, pants, insect wings, and a large, clearly-visible
  yellow/black striped bee-tail stinger. User confirmed: *"yeah i actually like this
  design."* Female variant + full customization still pending (see §2.10 for the
  now-grounded customization slot list).
  - 5 clips generated from the same approved character (no regeneration, per user's explicit
    "MAKE SURE TO KEEP HIS CURRENT LOOK AND FEEL"): idle (4f)/hit (6f)/death (7f) sourced from
    the **south** rotation, run (6f)/attack (9f) from the **west** rotation — user's own
    choice of which angle reads each motion best. Attack has no PixelLab template (a
    tail-curl-between-the-legs-then-fire motion), so it's a custom v3 action description:
    curl → extend/unfurl → release projectile, matching the user's brief exactly (their
    fallback — drop the expansion beat if it's too many frames — wasn't needed, fit in 9).
  - **South-vs-west facing mismatch resolved in code, per user's choice** ("add code support
    for 2 facings" over regenerating art or leaving it unresolved): `CharacterAnimator.cs`
    gained a per-rig `_idleUsesMirroredFacing` flag (default `true`, preserving every
    existing rig's old behavior — idle keeps mirroring to whichever way the character last
    faced). The Player's rig sets it `false`, since Idle's front-facing (south) pose
    shouldn't flip based on stale movement direction the way Run/Attack's side-facing (west)
    pose still does. Confirmed via 92/92 EditMode tests passing + scene validator PASSED +
    visual drive screenshots.
  - Wired via a new `Assets/Editor/BuildTools/HeroBeePersonSkinBuilder.cs` — builds a
    dedicated `Assets/Sprites/HeroBeePerson/HeroBeePersonGenerated.asset` SpriteLibraryAsset
    (Idle/Run/Attack/Death categories; the shared Attack.anim/Death.anim clips only resolve 6
    labels each, so the 9-frame attack is down-sampled to 6 evenly-spread frames and the
    7-frame death drops its last frame) and points **only** the Player at it — enemies
    untouched, same reversible pattern as the rejected insect rig before it.
  - The old insect rig (`698b84ba-...`, `Assets/Sprites/HeroBee/`, `HeroBeeGenerated.asset`)
    is left fully intact, not deleted — enemies never used it, and it may still be useful
    later (e.g. a "pre-evolution" form, if that becomes a real mechanic rather than just
    flavor).
- **Spec:** Character tier, 32×32 per frame — same tier as before, only the design changed.
  idle/run/attack/hit/die clips, PPU 16, point-filtered, no compression.
- **Usage:** the game's protagonist, visible constantly. Same SpriteLibrary/SpriteResolver
  skinning mechanism as before (see `reference/style-guide.md`'s "Integrating a
  Character-tier rig" section in the `create-sprite` skill) — swap the Player's
  `SpriteLibraryAsset`, don't touch the shared Animator/clips.
- **Generation prompt (male base body):** "A male humanoid bee-person hero — yellow skin,
  short black hair, small branching antler-like antennae on his head, wearing a simple
  shirt and pants, two small translucent insect wings on his back, and a large, prominent
  bee-abdomen tail extending from his lower body with bold yellow-and-black stripes,
  tapering to a sharp point — this striped tail is his stinger weapon and must be clearly
  visible, not a minor detail. Confident heroic stance. Pixel art for SurveHive — a 16-PPU
  top-down-plane bee/honey survival-action game (Vampire-Survivors-style); Character tier,
  32×32 per frame; hard pixel edges, no anti-aliasing, flat/indexed shading, transparent
  background."

#### 1.2 Queen Bee boss art
- **Status:** 🟡 placeholder (royal-tinted `BossPack1` dragon body)
- **Spec:** Boss tier, 64×64 per frame, same clip set as trash bees plus a distinct "pattern" telegraph frame
- **Usage:** final Beehive boss (`QueenBossController`), 3-pattern fight (summon / radial burst / charge sweep); highest-visibility custom-art need in the game
- **Generation prompt:** "A corrupted queen bee boss, ornate crown/thorax pattern, menacing but still readable as a bee, pixel art for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game (Vampire-Survivors-style); Boss tier, 64×64 per frame; hard pixel edges, no anti-aliasing, flat/indexed shading, transparent background. Royal purple and honey-gold coloring; idle/attack/telegraph/hit/die frames."

#### 1.3 Beehive tileset / honeycomb floor
- **Status:** 🔴 not started (README calls this out explicitly as missing)
- **Spec:** Tile tier, 16×16 base tile, floor/wall/edge/corner autotile variants
- **Usage:** Beehive scene background/level geometry (currently flat background, no tileset)
- **Generation prompt:** "A seamless honeycomb floor tileset with floor/wall/edge/corner autotile variants, pixel art for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game (Vampire-Survivors-style); Tile tier, 16×16 base tile; hard pixel edges, no anti-aliasing, flat/indexed shading. Warm comb-brown and wax-cream palette, hexagonal honeycomb texture."

#### 1.4 Basic-attack stinger dart
- **Status:** 🟡 placeholder (code-generated, `Stinger.png`, currently 12×5px)
- **Spec:** Pickup tier, 16×16 canvas (thin dart shape need not fill the canvas)
- **Usage:** the auto-attack projectile, fired constantly every run
- **Generation prompt:** "A bee stinger dart projectile, pointed dark tip fading to a honey-gold shaft, side-facing, pixel art for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game (Vampire-Survivors-style); Pickup tier, 16×16 canvas; hard pixel edges, no anti-aliasing, flat/indexed shading, transparent background."

#### 1.5 EXP pickups (mote / orb)
- **Status:** 🟡 placeholder (code-generated, `ExpMote.png` / `ExpOrb.png` / `ExpPickup.png`)
- **Spec:** Pickup tier, 16×16 canvas — **one neutral/white base only** (per §0.4: the game already retints this by stored value at runtime — green → cyan → orange → royal purple — don't generate 4 color variants)
- **Usage:** EXP drops from every kill, the single most frequently on-screen sprite
- **Generation prompt:** "A single neutral white glowing orb pickup with a soft radial glow and one bright highlight pixel, pixel art for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game (Vampire-Survivors-style); Pickup tier, 16×16 canvas; hard pixel edges, no anti-aliasing, flat/indexed shading, transparent background. Neutral/desaturated — will be tinted per value tier at runtime, do not bake in color."

#### 1.6 Currency pickup (honey drop)
- **Status:** 🟡 placeholder (code-generated, `CurrencyPickup.png` / `HoneyDrop.png`)
- **Spec:** Pickup tier, 16×16 canvas, honey-gold/amber palette
- **Usage:** run-currency drops that bank into meta progression
- **Generation prompt:** "A honey droplet pickup, teardrop shape, honey-gold fill with amber shading and a single white highlight, pixel art for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game (Vampire-Survivors-style); Pickup tier, 16×16 canvas; hard pixel edges, no anti-aliasing, flat/indexed shading, transparent background."

#### 1.7 Item drops — Honey Jar, Magnet, Wax Shield, Royal Bomb
- **Status:** 🟡 placeholder (code-generated, `HoneyJar.png` / `MagnetItem.png` / `WaxShield.png` / `RoyalBomb.png`)
- **Spec:** Pickup tier, 16×16 canvas each, distinct silhouette per item so they read at a glance mid-combat
- **Usage:** elite/boss item drops (heal / pickup-vacuum / block-3-hits / screen-nuke)
- **Generation prompt:** "A set of four item icons for a bee-themed survival game: (1) a honey jar with a wax-cream lid, (2) a small red horseshoe magnet, (3) a translucent wax shield hexagon, (4) a royal-purple bomb with a lit fuse, pixel art for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game (Vampire-Survivors-style); Pickup tier, 16×16 canvas each; hard pixel edges, no anti-aliasing, flat/indexed shading, transparent background. Consistent chunky style across all four."

#### 1.8 Active-skill projectiles/zones
- **Status:** 🟡 placeholder (code-generated: `EmberBolt.png`, `FrostRing.png`, `ZapSegment.png`, `HoneyGlob.png`, `PollenAuraZone.png`, `HoneyPuddleZone.png`, `LanceDart.png`)
- **Spec:** Pickup tier, 16×16 canvas each (aura/zone sprites are scaled up at runtime from this base), element-colored per §1.9's palette
- **Usage:** the active-skill weapons' visible attacks (Ember Sting, Frost Nova, Static Wings, Honey Splash/Bomb, Pollen Cloud, Stinger Barrage)
- **Generation prompt:** "A set of elemental VFX sprites for a bee-themed action game — an ember bolt (orange/red trail), a frost ring shard (pale blue crystalline), a static/lightning arc segment (electric yellow-white), a honey glob projectile (amber, glossy — a droplet accent flying off the splash is fine, it's a minor detail not the primary shape), a pollen aura cloud (poison-green particle puff), a honey puddle zone (translucent amber pool), a lance dart (physical steel-grey barb), pixel art for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game (Vampire-Survivors-style); Pickup tier, 16×16 canvas each; hard pixel edges, no anti-aliasing, flat/indexed shading, transparent background. Each readable at small size over cluttered combat."

#### 1.8b Set-signature effect VFX (Phase 2B)
- **Status:** 🟡 placeholder (reuses existing zone/damage-number visuals — no dedicated art)
- **Spec:** Pickup tier, 16×16 base (zones scaled up at runtime), element-colored per §1.9
- **Usage:** the 4-piece set signatures (PLAN 2B). Currently: the **Virulence** toxic pool and **Sticky Sweet** slow slick reuse `HoneyPuddleZone.png` runtime-tinted green/gold; the **Deep Chill** shatter has **no VFX at all** (only AoE damage numbers); Wildfire spread / Overcharge stun-arc rely on the target's status tint. A dedicated **frost-shatter burst** (icy shard-spray, ~24×24) and distinct **toxic-pool** + **honey-slick** zone sprites would sell these clearly.
- **Generation prompt:** "A frost-shatter burst VFX (pale-blue crystalline shards spraying outward from a point), a translucent toxic-green poison pool zone, and a glossy amber honey-slick zone, pixel art for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game; Pickup tier, hard pixel edges, no anti-aliasing, flat/indexed shading, transparent background; readable over cluttered combat."

---

## 2. Icons & UI

All entries in this section are **Icon tier — 32×32**, no exceptions (see §0.1). Every
power-up gets its own unique, theme-consistent silhouette (§0.3) rather than a generic
palette-swapped RPG glyph.

#### 2.1 Power-up card icons — Passives (cap 5 owned, 10 exist)

One unique 32×32 icon per entry, honey-gold/amber/comb-brown palette with element-tinted
accents where relevant, dark outline, flat shading, transparent background.

| Power-up | Icon shape |
|---|---|
| Swift Wings (move speed) | a single swept wing with speed-lines |
| Thicker Chitin (max HP) | a segmented exoskeleton carapace plate |
| Waxen Plating (armor) | a hexagonal wax-comb shield |
| Potent Venom (damage %) | two crossed stinger fangs |
| Keen Eye (crit chance) | a faceted compound-eye circle with a target dot at its center |
| Deadly Precision (crit damage) | a stinger piercing dead-center through a target ring |
| Nectar Drain (lifesteal) | a stinger siphoning a swirl of nectar up from a small flower bloom |
| Nectar Sense (magnet radius) | a horseshoe magnet with radiating pull-arcs |
| Hyper Metabolism (cooldown reduction) | a winged hourglass |
| Royal Focus (ability power) | a small crown with a starburst spark above it |

- **Status:** 🟡 placeholder (icon slot exists on `SkillDefinitionSO`, currently borrows from placeholder icon sheets)
- **Generation prompt:** "A set of 10 icon badges for a bee-themed RPG power-up card game — [insert the 10 shapes from the table above, one per icon], pixel art for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game (Vampire-Survivors-style); Icon tier, 32×32 each; hard pixel edges, no anti-aliasing, flat/indexed shading, transparent background. Honey-gold/amber/comb-brown palette with element-tinted accents, dark outline, thick readable silhouettes, no two icons sharing a base shape."

#### 2.2 Power-up card icons — Enhancements (cap 3 owned, 7 exist)

| Power-up | Icon shape |
|---|---|
| Twin Stingers | two parallel stinger darts side by side |
| Longer Stinger | one elongated stinger dart with a length arrow |
| Piercing Stinger | a stinger dart punching through two rings in a line |
| Burning Stinger | a stinger wreathed in a small flame (echoes the Burn status/Fire element, intentionally — see §0.3.3) |
| Poison Stinger | a stinger dripping with a small skull mark (echoes the Poison status/element) |
| Frost Stinger | a stinger encased in an ice shard (echoes the Freeze status/Frost element) |
| Shock Stinger | a stinger crackling with a small lightning bolt (echoes the Stun status/Electric element) |

- **Status:** 🟡 placeholder (same icon-slot situation as passives)
- **Generation prompt:** "A set of 7 icon badges for a bee-themed RPG basic-attack modifier card game — [insert the 7 shapes from the table above, one per icon], pixel art for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game (Vampire-Survivors-style); Icon tier, 32×32 each; hard pixel edges, no anti-aliasing, flat/indexed shading, transparent background. Honey-gold/amber/comb-brown palette with element-tinted accents (flame orange, poison green, frost blue, electric yellow) on the elemental four, dark outline, thick readable silhouettes."

#### 2.3 Power-up card icons — Abilities (cap 5 owned, 8 exist)

| Power-up | Icon shape |
|---|---|
| Stinger Barrage | a radial ring of stingers around a center point |
| Honey Splash | an amber splash blob mid-impact |
| Pollen Cloud | a puffy cloud with a small flower at its center |
| Static Wings | two wings with an arcing bolt crackling between them |
| Ember Sting | a comet-like bolt with a flame tail |
| Frost Nova | a radial burst of ice shards from a center point |
| Ball Lightning | a crackling sphere/orb |
| Honey Bomb | a round honey-glazed bomb with a lit fuse, honeycomb-hex pattern on the casing (distinguishes it from the plain purple Royal Bomb item drop) |

- **Status:** 🟡 placeholder (same icon-slot situation as passives). Note: `PiercingLanceCard.asset` still exists as data but the Piercing Lance ability was retired per `README.md` — no icon needed unless it's revived.
- **Generation prompt:** "A set of 8 icon badges for a bee-themed RPG active-ability card game — [insert the 8 shapes from the table above, one per icon], pixel art for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game (Vampire-Survivors-style); Icon tier, 32×32 each; hard pixel edges, no anti-aliasing, flat/indexed shading, transparent background. Honey-gold/amber/comb-brown palette with element-tinted accents, dark outline, thick readable silhouettes."

#### 2.4 Element badges
- **Status:** 🟡 placeholder (color-only via `ElementPalette`, no dedicated icon glyph yet)
- **Spec:** Icon tier, 32×32, 6 icons
- **Usage:** element cue on every level-up card and the set-bonus tier lines
- **Shapes:** physical → a steel-grey stinger sliver · fire → a flame · poison → a skull ·
  electric → a lightning bolt · frost → a snowflake/ice crystal · **honey → a honeycomb
  hexagon cell** (not a droplet — droplet is reserved per §0.3.2)
- **Generation prompt:** "A set of six elemental gem/badge icons for a game UI: physical (steel-grey stinger sliver), fire (orange flame), poison (green skull), electric (yellow lightning bolt), frost (pale-blue snowflake), honey (golden honeycomb hexagon cell — not a droplet), pixel art for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game (Vampire-Survivors-style); Icon tier, 32×32 each; hard pixel edges, no anti-aliasing, flat/indexed shading, transparent background. Consistent gem-shaped frame around each symbol, no two sharing a base shape."

#### 2.5 Status-effect icons
- **Status:** 🟡 placeholder (`Assets/ThirdParty/FantasyStatusIcons/StatusIcons.png`, generic sheet)
- **Spec:** Icon tier, 32×32, 5 icons — mutually unique silhouettes (§0.3.1)
- **Usage:** status-effect indicator on enemy health bars / status receiver tint cue
- **Shapes:** burn → a flame · poison → a skull · slow → a snail shell (not a clock —
  clocks/hourglasses are reserved for cooldown-related power-up icons, see §2.1) ·
  freeze → an ice crystal · stun → spinning stars. **Reserved, not built yet: bleed → a
  droplet.**
- **Generation prompt:** "A set of five status-effect icons: burn (flame), poison (skull), slow (snail shell), freeze (ice crystal), stun (spinning stars), pixel art for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game (Vampire-Survivors-style); Icon tier, 32×32 each; hard pixel edges, no anti-aliasing, flat/indexed shading, transparent background. Bold readable silhouettes, dark outline, no two icons sharing a base shape — colorblind players must be able to tell them apart by shape alone."

#### 2.6 UI glyphs (settings, pause, home, shop, volume, etc.)
- **Status:** 🟡 placeholder (`Assets/ThirdParty/IconsTemp/Icons/PictoIcon_64/`, generic picto pack, currently 64×64 — resize down to match the Icon tier)
- **Spec:** Icon tier, 32×32, covers the ~15 glyphs actually used (home, shop, settings/gear, pause, volume/mute, resume, quit, retry, trophy, lock, etc.)
- **Usage:** main menu, pause menu, settings panel, world-select lock icon
- **Generation prompt:** "A set of UI glyph icons matching a honey/hive theme: home (hive), shop (honey jar with coin), settings (gear), pause (two bars), volume/mute (speaker), resume (play triangle), quit (door), retry (circular arrow), trophy, padlock, pixel art for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game (Vampire-Survivors-style); Icon tier, 32×32 each; hard pixel edges, no anti-aliasing, flat/indexed shading, transparent background. Honey-gold/comb-brown palette, thick outlines, same visual weight as the game's power-up icons — this is UI chrome, not a separate design language."

#### 2.7 Difficulty icons
- **Status:** 🟡 placeholder (Phase 1B landed 2026-07-08 — the picker is live, borrowing PictoIcon feather/star/sword/skull; swap by assigning the 4 `icon` slots on `Assets/Data/Progression/DifficultySettings.asset`, no code/scene change needed)
- **Spec:** Icon tier, 32×32, 4 icons (easy / normal / hard / extreme)
- **Usage:** world-select difficulty picker (dropdown rows + caption)
- **Generation prompt:** "A set of four difficulty badge icons — easy (a single leaf/feather), normal (a bee silhouette), hard (crossed stingers), extreme (a crowned skull), escalating visual intensity, pixel art for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game (Vampire-Survivors-style); Icon tier, 32×32 each; hard pixel edges, no anti-aliasing, flat/indexed shading, transparent background. Honey-gold to danger-red accent progression across the four."

#### 2.8 Premium currency icon — Royal Jelly
- **Status:** 🟡 placeholder — the currency is live (Phase 5B) and a **code-generated placeholder icon now renders everywhere** (2026-07-11): `CurrencyGlyphsBuilder` draws a procedural gold-rimmed cream comb cell into the right half of `Assets/Sprites/CurrencyGlyphs.png` (64×32 sheet, honey drop ×4 on the left) and any UI text inlines it via `<sprite name="jelly">` (TMP default sprite asset). **To land final art: overwrite the PNG's right 32×32 cell** (the builder never regenerates an existing file). Concept: **Royal Jelly**, the rare substance fed only to a queen — reads instantly as "the special one" against common Honey, and fits the game's existing royal-naming convention (Queen's Guard, Royal Guard, Royal Bomb, Royal Focus, royal-purple palette).
- **Spec:** Icon tier, 32×32 (right cell of `CurrencyGlyphs.png`)
- **Shape:** a hexagonal royal comb cell filled with pearly-white/cream jelly, gold rim highlight — **not** a droplet (droplet is reserved per §0.3.2)
- **Usage:** shop-header balance readout, results line, and all future jelly pricing (5C/5E) — one sprite-sheet cell feeds every surface
- **Generation prompt:** "A premium currency icon: a hexagonal royal comb cell filled with pearly-white/cream royal jelly, gold rim highlight, pixel art for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game (Vampire-Survivors-style); Icon tier, 32×32; hard pixel edges, no anti-aliasing, flat/indexed shading, transparent background. Reads as rarer/more special than the common honey-drop currency icon — not a droplet shape."

#### 2.9 Achievement badges
- **Status:** 🔴 not started (TODO #33 — achievements system not yet implemented)
- **Spec:** Icon tier, 32×32 — **one neutral badge per achievement type** (crown for boss kills, skull for deaths, hourglass for survival time, stinger for kill counts, etc.), each with a plain unfilled ring; the gold/silver/bronze tier is a **runtime tint on the ring only** (per §0.4), not three baked variants per badge
- **Usage:** achievements panel + unlock toast
- **Generation prompt:** "A starter set of ~12 achievement badge icons, circular medal shape with a honeycomb border and a plain unfilled outer ring, each with a distinct central symbol (crown for boss kills, skull for deaths, hourglass for survival time, stinger for kill counts, etc.), pixel art for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game (Vampire-Survivors-style); Icon tier, 32×32 each; hard pixel edges, no anti-aliasing, flat/indexed shading, transparent background. Leave the outer ring a neutral/plain color — the gold/silver/bronze tier is applied as a runtime tint, do not bake it in."

#### 2.10 Character cosmetics
- **Status:** 🟡 placeholder — the customization **system** shipped in Phase 5C (2026-07-11):
  a Hive Style panel selling 5 body tints (runtime shader tint — no art needed), 3 hats, and
  3 stinger skins. The hat/stinger sprites are **code-generated pixel placeholders** in
  `Assets/Sprites/Cosmetics/` (HatCrown / HatTopHat / HatDaisy / StingerGold / StingerCrystal /
  StingerThorn, PPU 16, drawn by `CosmeticsBuilder` — **only when the PNG is missing**, so
  final art can simply overwrite the files; attach offsets are hand-tunable on the
  `Assets/Data/Cosmetics/*.asset` rosters and survive builder re-runs).
- **Overlay sprite specs (current placeholder set, replace 1:1):** Sprite tier, tiny — hats
  ~12×6 px sitting on the head (crown: gold with ruby/sapphire jewels; top hat: near-black
  with amber band; daisy: white petals, amber center, leaf). Stingers (playtest follow-up
  2026-07-11) re-skin the **auto-attack projectile**: 3 shape sprites (**Needle / Barb /
  Blade**, ~12×5–13×7 px, pointing right, drawn **neutral near-white/gray** — the color
  variant is a runtime tint, so one sprite covers Amber/Sapphire/Venom). Hard pixel edges,
  transparent background, PPU 16, drawn to read at 1× zoom on the 32 px hero.
- **Future — "transformable" color sprites (TODO #37):** the 5C color slot is a whole-body
  tint for now (user-accepted placeholder). Once the 6A hero art lands, colors should become
  real sprite transformations — per-region recolors over the slots below — via palette
  masks/swaps, not a flat multiply.
- **Recolor-slot design for the final bee-person art (§1.1), recorded earlier** — when the 6A
  humanoid rig lands, the color slot should evolve from a whole-body tint into per-region
  recolors, per the user looking at the approved art ("for now i dont think we need those"
  — said of building these as generated assets; the confirmed future slots are):
  - **Base color** — the yellow body/skin
  - **Stripes color** — the black stinger/body stripes
  - **Antlers color** — the yellow dots at the antler tips
  - **Wings color**
  (Supersedes an earlier speculative list — shirt/pants/hair/shoes color, hair style — drafted
  before there was an approved design to check it against; drop that list, this is current.)
- **Spec (recolor slots):** all four are almost certainly pure recolors → one neutral base +
  runtime tint per §0.4, not regenerated variants. The `_Tint` channel the 5C color slot uses
  is the natural home; per-region tinting needs shader/palette work sized with the final art.
  Female variant (also §1.1) needs designing before this can extend to both.
- **Usage:** the Hive Style panel (live since 5C) + the in-run hero overlays

#### 2.11 Meta-shop upgrade icons (13 exist)
- **Status:** 🟡 placeholder (`_icon` slot exists on `MetaUpgradeSO`; each of the 13 upgrades
  currently borrows a generic picto from `Assets/ThirdParty/IconsTemp/Icons/PictoIcon_128` —
  Heart / Sword / Speedmeter / Hammer / Magnetic / Money / Star / Thunder / Time / Target /
  Star_Circle / Gift / Random). Wired by the Phase 3B-1 `MetaShopTabsBuilder`.
- **Spec:** Icon tier, 32×32, 13 icons — one per permanent shop upgrade, shown both in the shop's
  category grid and the detail pane. Group visually by tab so a player reads the category at a
  glance: **Combat** (Damage, Attack Speed, Crit Chance, Crit Damage, Ability DMG, Cooldown Cut),
  **Survival** (Max HP, Move Speed, Pickup Range), **Utility** (Honey Gain, EXP Gain, Item Drop
  Rate, Rerolls). Reuse existing motifs where they exist (the Honey-Gain icon should echo the
  honey-drop currency; crit icons can echo the Keen Eye / crit power-up motifs).
- **Usage:** Hive Upgrades shop (tab grid + detail pane)
- **Generation prompt:** "A set of 13 icon badges for a bee-themed survival game's permanent
  upgrade shop: max health (heart/honeycomb heart), damage (stinger/sword), move speed (winged
  boot), attack speed (blurred wing), pickup range (magnet), honey gain (honey jar/coin), EXP
  gain (star), ability power (radiant hex), cooldown cut (winged hourglass), crit chance
  (targeting eye), crit damage (cracked impact star), item drop rate (gift/loot bag), rerolls
  (shuffling arrows / dice), pixel art for SurveHive — a 16-PPU top-down-plane bee/honey
  survival-action game (Vampire-Survivors-style); Icon tier, 32×32 each; hard pixel edges, no
  anti-aliasing, flat/indexed shading, transparent background. Honey-gold/amber/comb-brown
  palette, thick readable silhouettes, dark outline, no two icons sharing a base shape."

#### 2.12 Codex glyphs (item entries + set-bonus glyph)
- **Status:** 🟡 placeholder (Phase 5A landed 2026-07-10 — the codex panel is live; its four
  item-entry icons borrow generic pictos — Flask_01 / Magnetic / Defense / Bomb — authored on
  `Assets/Data/Progression/CodexCatalog.asset` by `CodexBuilder`, and set-bonus entries share
  one Sparkle picto tinted per element at runtime)
- **Spec:** Icon tier, 32×32. The four item icons should simply **reuse §1.7's final item-drop
  art** (honey jar / magnet / wax shield / royal bomb) once that lands — update the catalog's
  `Icon` slots, no code change. The set glyph wants one neutral emblem (e.g. a honeycomb hex
  frame) designed to read through the runtime element tint per §0.4; alternatively reuse §2.4's
  element badges per set and drop the tint.
- **Usage:** codex Items/Sets tabs (grid cells + detail pane). Power-up entries reuse the card
  icons (§2.1–2.3) and enemy entries reuse the live world sprites — no extra art needed there.
- **Generation prompt:** "A single neutral set-bonus emblem: a honeycomb hexagon frame with a
  radiant core, designed in light greys/white so a runtime color tint reads cleanly, pixel art
  for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game
  (Vampire-Survivors-style); Icon tier, 32×32; hard pixel edges, no anti-aliasing,
  flat/indexed shading, transparent background."

---

## 3. Future world content (Phase D — lower priority, build once systems are stable)

#### 3.1 Garden world
- **Status:** 🔴 not started
- **Spec:** Character tier (32×32/frame) enemy rig set + Tile tier (16×16) tileset
- **Usage:** second world; "corrupted insects" per README
- **Generation prompt:** "A corrupted insect enemy rig set (ant/beetle/wasp variants) and a matching overgrown-garden tileset, pixel art for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game (Vampire-Survivors-style); Character tier, 32×32 per frame (enemies) / Tile tier, 16×16 base tile (tileset); hard pixel edges, no anti-aliasing, flat/indexed shading, transparent background. Sickly green corruption glow accents."

#### 3.2 Woods world
- **Status:** 🔴 not started
- **Spec:** Character tier enemy rig set + Tile tier tileset
- **Usage:** third world; "corrupted animals" per README
- **Generation prompt:** "A corrupted forest-animal enemy rig set (fox/owl/boar variants) and a matching dark-twisted-forest tileset, pixel art for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game (Vampire-Survivors-style); Character tier, 32×32 per frame (enemies) / Tile tier, 16×16 base tile (tileset); hard pixel edges, no anti-aliasing, flat/indexed shading, transparent background. Purple corruption glow accents."

#### 3.3 City world
- **Status:** 🔴 not started (enemy roster still TBD per README)
- **Spec:** Character tier enemy rig set + Tile tier tileset
- **Usage:** fourth world; theme/enemies not yet designed
- **Generation prompt:** *(hold — design the enemy roster first; README lists this world's enemies as TBD)*

#### 3.4 Alien Ship world
- **Status:** 🔴 not started
- **Spec:** Character tier enemy rig set + Tile tier tileset
- **Usage:** final world; "aliens", confronting the corruption's source
- **Generation prompt:** "A bio-mechanical alien enemy rig set and a matching alien-ship-interior tileset, pixel art for SurveHive — a 16-PPU top-down-plane bee/honey survival-action game (Vampire-Survivors-style); Character tier, 32×32 per frame (enemies) / Tile tier, 16×16 base tile (tileset); hard pixel edges, no anti-aliasing, flat/indexed shading, transparent background. Cold metallic surfaces with a purple/violet corruption bio-glow."

---

## 4. Audio assets (ElevenLabs / AI-generation candidates)

*(Not revisited yet this pass — flagged by the user as "fine for now," to be refined later.)*

#### 4.1 Missing active-skill SFX — Frost Nova, Honey Bomb, Ball Lightning
- **Status:** 🔴 not started (these 3 actives shipped in Combat 2.0 after the Phase 5A audio pass; `SfxId` has no entries for them yet — see `Assets/Scripts/Data/SfxId.cs`)
- **Spec:** 2 variants each (`_00`/`_01.wav`, matching the existing skill-SFX convention), ~0.3–0.8s one-shots
- **Usage:** fires every time the player has these actives equipped — currently silent
- **Generation prompt (ElevenLabs Sound Effects):**
  - Frost Nova: "a short crystalline ice-shatter burst, high-pitched shimmering crack, retro chiptune-adjacent, 0.4 seconds"
  - Honey Bomb: "a thick viscous honey splat with a low sticky thud, warm and syrupy, 0.5 seconds"
  - Ball Lightning: "an electric crackle-zap with a rising static charge, bright arcade-y sizzle, 0.4 seconds"

#### 4.2 Per-world music loops — Garden, Woods, City, Alien Ship
- **Status:** 🔴 not started (only `menu_loop.ogg` and `beehive_loop.mp3` exist)
- **Spec:** seamless loop, 60–120s, matching the CC0 tracks' energy level (upbeat-but-tense adventure)
- **Usage:** background music per world once built
- **Generation prompt (ElevenLabs Music):**
  - Garden: "an upbeat but slightly eerie chiptune-adventure loop, plucky and organic, hints of unease under the melody, seamless loop, no vocals"
  - Woods: "a darker, tense chiptune-adventure loop, sparse and foreboding, low drones under a minimal melody, seamless loop, no vocals"
  - City: "a tense, industrial-tinged chiptune loop, driving rhythm, fallen-civilization mood, seamless loop, no vocals"
  - Alien Ship: "an otherworldly, dissonant electronic loop, cold synth pads with an unsettling undertone, seamless loop, no vocals"

#### 4.3 Boss/miniboss music cues
- **Status:** 🔴 not started (Royal Guard + Queen fights currently reuse the Beehive ambient loop)
- **Spec:** seamless loop, 60–90s, higher intensity than the world loop
- **Usage:** plays during miniboss/boss encounters (timeline freezes for these fights, per README)
- **Generation prompt:** "an intense chiptune boss-battle loop, driving percussion, urgent rising melody line, royal/regal undertone (bells or fanfare motif), seamless loop, no vocals"

#### 4.4 Achievement/unlock fanfare
- **Status:** 🔴 not started (TODO #33 dependency)
- **Spec:** one-shot, ~1.5s
- **Usage:** achievement-unlock toast
- **Generation prompt:** "a short triumphant chiptune fanfare, bright ascending arpeggio, celebratory, 1.5 seconds"

#### 4.5 Ambient hive bed (optional layer)
- **Status:** 🔴 not started (nice-to-have, not blocking)
- **Spec:** seamless loop, low-mixed background layer under `beehive_loop.mp3`
- **Usage:** subtle environmental texture (buzzing/wing-hum bed) under the Beehive music
- **Generation prompt:** "a low, seamless ambient bed of distant bee wing-buzz and hive murmur, subtle and non-melodic, meant to sit quietly under music, seamless loop"

#### 4.6 Narrative voice-over (stretch — story beats only)
- **Status:** 🔴 not started (optional; game currently has no voiced lines)
- **Spec:** short spoken lines, 2–5s each, per key story beat (the Break, the Escape, the Reveal — see README Story & Setting)
- **Usage:** intro cinematic / world-transition narration, if the game ever adds voice
- **Generation prompt (ElevenLabs Voice/TTS):** "a weathered, weary narrator voice, grim and atmospheric, reading a short line about a hive turned against its own kin — mid-range male or androgynous voice, minimal processing, no music bed"

---

## Change log for this document

- **2026-07-08 (wired live)** — the humanoid Hero Bee-Person (§1.1) is now live on the
  Player, replacing the rejected insect rig. Added `CharacterAnimator._idleUsesMirroredFacing`
  (default `true`, backward-compatible with every existing rig) so a character's Idle pose
  can be sourced from a different facing than Run/Attack without mirroring incorrectly — the
  Player opts in to the new `false` behavior; nothing else changed. New
  `HeroBeePersonSkinBuilder.cs` builds the SpriteLibraryAsset and wires the Player only,
  same reversible enemies-untouched pattern as before. Verified via 92/92 EditMode tests,
  scene validator PASSED, and visual drive screenshots.
- **2026-07-08 (approved + generated)** — the humanoid bee-person male design (§1.1) is
  approved by the user ("yeah i actually like this design"). Generated and staged all 5
  animation clips from the same approved character (idle/hit/death from its south rotation,
  run/attack from its west rotation — user's choice per motion) at `Assets/Sprites/
  HeroBeePerson/`. The attack clip is a custom v3 action (no template covers a
  tail-curl-and-fire motion) matching the user's brief; their fallback instruction to drop
  the "expand" beat if it needed too many frames wasn't needed — it fit in 9. Confirmed the
  customization slot list against the actual approved art (§2.10): base/stripes/antlers/wings
  color, replacing the earlier speculative list. Not yet wired into Unity — flagged an open
  question (south vs. west facing split doesn't match the game's single-facing-plus-flip
  system) for whenever integration happens.
- **2026-07-08 (design pivot)** — hero direction changed after live playtest review: the
  insect-bodied bee (§1.1) is rejected ("i dont like it, i think i want to go in a different
  direction"). New concept: the bee evolves into a humanoid version of itself, eventually
  male + female + fully customizable, starting with one male example. Rewrote §1.1 with the
  male design brief (yellow skin, black hair, antler-antennae, shirt/pants, wings, a large
  visible striped bee-abdomen tail as the stinger weapon) and a custom attack animation brief
  (tail curls forward between the legs, optionally expands, releases a projectile). Recorded
  the planned customization slots (body/hair/stinger-stripe/shirt/pants/shoes color, hair
  style) in §2.10 as a design placeholder, not yet built. The old insect rig and its
  SpriteLibraryAsset are left intact, unused rather than deleted.
- **2026-07-07** — initial list created (TODO #29), surveyed against current placeholders in `Assets/Sprites/`, `Assets/ThirdParty/`, `Assets/Audio/`, and `Assets/Data/`.
- **2026-07-07 (generation)** — added the `create-sprite` skill (`.claude/skills/create-sprite/`) wrapping the new PixelLab MCP server: looks up an entry here, maps its tier to the right PixelLab tool with canonical style params (`reference/style-guide.md`), generates, and stages output without silently overwriting live assets. First use: generated §1.1's Hero Bee rig — took 3 attempts (user-reviewed each time via an Artifact preview page) before landing an actual bee instead of "a human in a bee costume"; standard mode's human-skeleton generation couldn't be talked out of a human silhouette even with custom insectoid proportions, `v3` mode could. Final rig: base pose + idle/run/attack/hit/die clips, single direction (mirrored to face right) — confirmed the game has no multi-directional facing, just a horizontal flip. 14 total generations spent across all 3 attempts; staged at `Assets/Sprites/_Generated/HeroBee/`, not yet imported/wired. Lessons captured in `reference/style-guide.md` for future insectoid-cast generations.
- **2026-07-07 (integration)** — wired §1.1's Hero Bee art onto the Player only, via a new idempotent `HeroBeeSkinBuilder.cs` builder pass. Moved the staged frames from `_Generated/HeroBee/` to their permanent home `Assets/Sprites/HeroBee/`; built a dedicated `HeroBeeGenerated.asset` SpriteLibraryAsset instead of touching the shared `YellowBee.asset`, so enemies are completely unaffected and the swap is trivially reversible. Confirmed live via headless build + validator + visual drive screenshots. User is still undecided on the final art ("not sure if I like it, for now rig it") — kept the old rig fully intact rather than deleting anything.
- **2026-07-07 (revision)** — added a Design System section (§0): one scale-tier system for all icons/sprites, a shared prompt template, colorblind-safe shape rules (poison → skull, honey → hexagon not droplet, droplet reserved for a future Bleed status), and a tint-don't-regenerate rule for pure recolors (EXP orb, hero cosmetics, achievement tier rings). Assigned unique themed icon shapes to every passive/enhancement/ability (25 power-ups total, confirmed against the actual `Assets/Data/Skills/*.asset` roster). Premium-currency concept left open pending a naming decision.
