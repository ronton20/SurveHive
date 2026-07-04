# SurveHive — Polish & Feature Implementation Plan

> **Progress:** Phases 0–4 ✅ done (Phase 4 finished 2026-07-05). Phase 5 not started.

> Created 2026-07-04 with Ron. This is the agreed roadmap for the next development push.
> Reference this file when starting implementation ("let's do Phase N from PLAN.md").
> Decisions locked in during planning:
> **Art**: use the Unity Store packs now, custom Aseprite art later. **Platform**: PC-first, mobile-ready.
> **Skills**: ~6 active + 10 passive. **Order**: look & feel first. **Bosses**: full Beehive ladder
> (strong waves → miniboss → Queen Bee). **Meta**: flat stat shop, designed for future expansion.
> **Audio**: free/CC0 SFX pass.

---

## 1. Art Direction — the target look

The agreed look, and how each part is achieved:

| Goal | How we get there |
|---|---|
| 2D top-down pixelated | Pixel Perfect Camera (URP 2D), one global PPU, point filtering, no compression on sprites. Side-view sprites that flip left/right on a top-down plane — the standard Vampire Survivors presentation, and exactly how the PixelFantasy monsters are built. |
| High-pace action | Faster base move speed, denser spawns, snappy attack cadence — plus *feel* tech: hit flash, knockback, micro hit-stop, screen shake, damage numbers (already in). |
| Bee-themed skill spectacle | 6 active auto-firing skills, each with its own tinted VFX from the 118-effects bundle (see §4.2). |
| Cool showy animations | PixelFantasy monsters ship with Idle/Run/Attack/Hit/Die clips + a shared Animator controller; VFX bundle provides animated sheets for every skill/impact/death. |
| Slick pixel hive-themed UI | DEVNIK pixel button kit 9-sliced and retinted to a honey palette + BoldPixels TMP font + honeycomb motifs. |

### Rendering foundation (locked decisions)

- **PPU 16** everywhere *(revised in Phase 1: the PixelFantasy sprites turned out to be authored at PPU 16, not 32)*. All imported sprites: `Point (no filter)`, `Compression: None`, mipmaps off.
- **Pixel Perfect Camera** component on the main camera (built into URP 2D): reference resolution **320×180** (integer 6× scale at 1080p), upscale render texture ON for crisp rotation of VFX.
- **Honey/hive palette** (used for UI retints, VFX tints, backgrounds):
  - Honey gold `#FFC30B`, amber `#F5A623`, wax `#E8D8A0`, comb brown `#8C5A2B`, deep hive brown `#3A2416`, danger red `#D9483B`, poison green `#7CB518`, royal purple `#7B2D8B` (queen/royal rank accent).
- **Glow accents**: URP Bloom post-processing (already have a Global Volume) with high threshold so only deliberately-bright VFX pixels bloom — gives "magic honey" pop without smearing the pixel art. Optional later: 2D lights for the hive interior mood.

### Third-party tools (for the "custom later" art phase — not this plan's work, just the recommendation)

- **Aseprite** (~$20, or free if self-compiled) — industry standard for pixel sprites + animation; the project already has `com.unity.2d.aseprite`, so `.aseprite` files import directly with animation clips. Use it for: player hero bee, Queen Bee boss, honey-specific VFX, hive tileset.
- Free alternatives: **Libresprite** / **Pixelorama**.
- **Tilesetter** or Aseprite itself for a honeycomb tileset when the Beehive gets a real floor.
- **ChipTone / jsfxr** (free, browser) for extra retro SFX; **Audacity** for trimming sourced audio.

---

## 2. Asset Triage (what we keep, move, delete) — ✅ DONE (2026-07-04)

New top-level folder: **`Assets/ThirdParty/`** — all store packs live there, our own content stays in `Assets/Sprites|Prefabs|Data|Scripts|...`. Moves are done as folder+`.meta` moves so GUIDs (and thus references) survive.

| Pack | Verdict | Destination / action |
|---|---|---|
| `PixelFantasy/PixelMonsters` + `PixelFantasy/Common` | **KEEP** — bee player/enemies, monster roster for future worlds, 6 bosses, animation clips/controller | `Assets/ThirdParty/PixelFantasy/…` |
| `PixelFantasy/PixelHeroes` | **DELETE** — humanoid hero builder, no use for a bee game | delete after a GUID-reference scan confirms Monsters don't reference it |
| `PixelFantasy/PixelTileEngine` | **DELETE** — side-scroller fantasy tiles, wrong perspective/theme | same guarded delete |
| `118 sprite effects bundle` | **KEEP** — all skill/impact/death VFX | `Assets/ThirdParty/SpriteEffects/` (drop its demo `Scenes/`) |
| `DEVNIK 2D/2D UI PIXEL BUTTONS` | **KEEP** — pixel UI kit (retint to honey palette) | `Assets/ThirdParty/PixelUI/` (drop demo scene) |
| `BoldPixels` | **KEEP** — pixel font + ready TMP SDF asset | `Assets/ThirdParty/Fonts/BoldPixels/` |
| `Layer Lab/2D Icons-PictoIconPack01` | **KEEP (provisional)** — flat white glyphs, not pixel art, but tintable; serve as skill-card icons until custom pixel icons exist | `Assets/ThirdParty/IconsTemp/`; flagged for deletion once custom icons land |

⚠️ Deletion note: these folders are untracked in git — deleting is only recoverable by re-downloading from the Asset Store. The scan-then-delete happens at implementation start, not before plan approval.

**Success criteria (triage):** project compiles with zero console errors after moves/deletes; `SurveHive/Validate Beehive Vertical Slice` passes; no sprite/prefab shows the magenta "missing" look in the Beehive scene; `Assets/` root contains only `ThirdParty/` + our original folders.

> **Phase 0 completion notes (2026-07-04):** all criteria verified via headless Unity (0 compile errors; validator 59/59 PASSED). Beyond the table above: PixelMonsters' `Mounts/` folder, `HorseMounter.cs`, and the `HowToMount`/`Pack1/Bonus` demo scenes were also deleted (they hard-depend on the removed PixelHeroes pack); `EffectManager.asset`'s dangling `FireAudioClip` was nulled; `SurveHive.BuildTools.asmdef` gained a `Unity.RenderPipelines.Universal.2D.Runtime` reference for the Pixel Perfect Camera; the camera pass (PPU 32, 640×360, upscale RT) is applied to `Beehive.unity` and wired into both `Build()` and a new idempotent menu item `SurveHive/Apply Pixel Perfect Camera`; a pre-existing validator failure (LevelUpPanel saved active in the scene) was fixed.

---

## 3. Phase 1 — Look & Feel (art swap + game feel + UI reskin) — ✅ DONE (2026-07-04)

### 3.1 Character art swap

- Player = **YellowBee** (PixelFantasy) with a distinct tint/marking so he reads as "our guy". Enemy ranks are the same bee, differentiated by tint + scale + stats (standard for the genre):
  - Worker Bee — pale/desaturated, scale ~0.9
  - Warrior Bee — orange-red tint, scale ~1.1
  - Queen's Guard (new 3rd trash rank) — royal-purple accent, scale ~1.25
- Build **our own prefabs** (keep the current `EnemyController`/pooling architecture) that use the pack's *sprites + animation clips/controller* — we do not adopt the pack's `Monster.cs` scripts. `EnemyStatsSO` gains sprite/animator/tint/scale fields so ranks stay data-driven.
- Wire animation states: Run while moving, Hit on damage, Die on death (death plays before the pooled despawn), player Attack triggers on fire.
- Sprites flip on X by movement direction (player) / direction to target (enemies).
- Extend `BeehiveSceneBuilder`/`BuildAdditions` so the scene remains regenerable with the new art (per the project's tooling policy).

### 3.2 Game feel ("damage feedback" TODO item)

- **Hit flash**: white flash on sprite for ~0.06s on damage (MaterialPropertyBlock or cached material swap — zero per-hit allocation).
- **Knockback**: brief impulse away from damage source on enemies (mass-scaled so elites budge less).
- **Screen shake**: small camera shake on player-hurt and on big events (miniboss/boss spawn, nuke). Amplitude-clamped so pixel-perfect stays readable.
- **Hit-stop**: 2–3 frame freeze on killing blows to elites+ (skippable on trash to protect pace).
- **Death VFX**: pooled one-shot sprite-sheet effect (from the effects bundle) + fade, replacing enemies just vanishing.
- New reusable pieces: `FlashOnHit`, `CameraShaker`, pooled `SpriteSheetVfx` player (drives sheet frames via `SpriteRenderer.sprite` swap — no Animator per VFX instance, zero-GC).

### 3.3 UI reskin (HUD, level-up cards, death screen)

- Slice the DEVNIK sheet into 9-sliced buttons/panels; author a **honey-tinted variant** of the sheet (one-time recolor — Aseprite or even programmatic tint at import).
- All text switches to **BoldPixels TMP font**.
- Level-up choice cards: honey panel, skill icon (tinted Layer Lab glyph for now), name, `Lv. X → Y`, rarity-colored frame (groundwork for Phase 2 rarity).
- HUD: hex-motif health bar + EXP bar, kill/currency counters, run timer.
- Death screen re-dressed in the same kit.

**Success criteria (Phase 1):**
1. Pressing Play in `Beehive.unity`: every entity (player, all enemy ranks, pickups, projectiles) uses real pixel sprites — zero solid-circle placeholders visible.
2. Player and enemies animate (run/attack/hit/die) and face their movement/target direction.
3. Killing an enemy shows: hit flash on impact, knockback, death VFX. Player taking damage shakes the camera.
4. All UI (HUD, level-up screen, death screen) uses the pixel kit + BoldPixels font; no default Unity UI sprites or LiberationSans anywhere.
5. Pixel Perfect Camera active; sprites render crisp (no bilinear blur) at 1080p in Game view.
6. Profiler over 60s of steady combat (~150 live enemies): **0 B/frame GC allocation** in gameplay code paths; stable 60 fps in-editor.
7. Scene builder regenerates the scene with all of the above intact; validator passes.

> **Phase 1 completion notes (2026-07-04):** delivered via a new idempotent pass `SurveHive/Apply Phase 1 Look & Feel` (`Phase1LookAndFeelBuilder`). Criteria 1–5 and 7 verified headlessly: the validator (extended with ~50 Phase 1 checks) passes 126/126, and a new **PlayMode smoke test** (`Assets/Tests/PlayMode`, run via Test Runner) boots the scene and plays ~8s with zero errors. Criterion 6 (Profiler 0 B/frame + 60 fps) can't be measured headlessly — code follows the zero-GC rules; **confirm with an in-editor Profiler session on your next play**. Deviations/extras vs the written plan: **PPU 16 / 320×180** replaced PPU 32 / 640×360 (the pack's true authoring scale); death feedback is a pooled particle **death-poof** (VFX pack, 512 materials bulk-converted to URP) rather than a corpse die-animation, keeping pooled release instant; the DEVNIK kit's gray elements are auto-sliced by pixel-region scanning and tinted at the Image level (no texture retint needed); **Queen's Guard** (3rd trash rank, purple, 1.25×, knockback-resistant, hit-stop on death) shipped now rather than Phase 3; kill counter + run timer HUD added; TMP Essential Resources had to be imported (found by the smoke test — TMP NREs without it); projectile/pickups use small code-generated pixel sprites (stinger dart, nectar mote, honey drop) pending custom art; bloom deferred to a later polish pass.

---

## 4. Phase 2 — Combat Depth (status effects + skill arsenal + rarity) — ✅ DONE (2026-07-04)

### 4.1 Status effect system (TODO #4, #5)

Data-driven, zero-GC: each enemy/player carries a fixed-size struct buffer of active effects (no per-application allocations).

| Effect | Behavior |
|---|---|
| Burn | Damage over time, stacks refresh duration |
| Poison | DoT, stacks increase potency (cap) |
| Slow | Move-speed multiplier for duration |
| Freeze | Hard stop, breaks early on damage threshold |
| Stun | Full stop, no attack, short duration, diminishing returns on elites/bosses |

Elemental application = chance-on-hit carried by skill data (e.g. fire skill: 25% burn). Enemies show a cheap visual state cue (tint shift + looping mini-VFX from the bundle).

### 4.2 Active skill arsenal (~6, VS-style auto-firing weapons)

New data model: `ActiveSkillSO` (cooldown, damage, projectile/area params, element + proc chance, VFX refs, per-level growth table, level cap). One pooled runtime executor per equipped skill; existing auto-attack becomes the baseline weapon in the same framework.

| Skill | Fantasy | Mechanics | Element/Status | VFX source (bundle) |
|---|---|---|---|---|
| Stinger Barrage | stinger weaponry | radial volley of stingers, count scales per level | — | muzzle flash + slash sprites |
| Piercing Lance | stinger weaponry | high-speed piercing shot through a line of enemies | — | straight slash sheets |
| Honey Splash | honey magic | lobbed glob → AoE puddle that damages + slows | Slow | water-splash sheets tinted honey-gold |
| Pollen Cloud | pollen & toxins | aura around player, DoT inside | Poison | smoke/gas sheets tinted green |
| Static Wings | elemental (electric) | arc that chains between up to N enemies | Stun chance | electricity sheets |
| Ember Sting | elemental (fire) | fiery homing bolt, explodes on hit | Burn | flame + explosion sheets |

(A frost/water "Chilling Nectar" variant is the designed 7th if the roster feels thin in playtesting — Freeze applier, water sheets tinted pale blue.)

### 4.3 Passive skills (grow from 4 → 10) (TODO #3)

Existing: Swift Wings (speed), Thicker Chitin (max HP), Longer Stinger (attack range), Twin Stingers (projectile count).
New: **Nectar Sense** (pickup/magnet radius), **Keen Eye** (crit chance — crits also feed damage-number styling), **Nectar Drain** (lifesteal %), **Hyper Metabolism** (cooldown reduction on actives), **Potent Venom** (damage %), **Deadly Precision** (crit damage multiplier). Player stats grow the needed fields (crit chance, crit damage, lifesteal, CDR, magnet radius).

### 4.4 Skill offering upgrades (TODO #1, #2)

- **Rarity tiers** (Common/Rare/Epic) replace the flat `_weight`; tier sets offer probability and card frame color.
- **Lucky double-level**: small chance a pick grants +2 levels; card gets a distinct "lucky" background so the roll is visible at offer time.

**Success criteria (Phase 2):**
1. All 6 active skills acquirable in a run; each visibly distinct in motion + VFX; each levels up with the stated growth and stops being offered at cap.
2. Status effects observably work: honey-slowed enemies move slower, poisoned/burned tick damage numbers, stunned enemies halt — each with a visible cue on the enemy.
3. All 10 passives function measurably (e.g. Nectar Sense visibly widens pickup drift range; lifesteal heals on hit; Potent Venom raises damage numbers; Deadly Precision raises crit damage numbers).
4. Rarity: over 20 level-ups, Epic skills appear noticeably less often than Common (weights logged/verifiable); lucky picks show the special card and grant +2.
5. Zero-GC holds: no per-frame or per-proc allocations from skills/status effects (Profiler-verified during a skill-heavy run).
6. EditMode tests cover: status-effect stacking/expiry math, rarity weighting distribution, skill level growth tables.

> **Phase 2 completion notes (2026-07-04):** delivered via `SurveHive/Apply Phase 2 Combat Depth` (`Phase2CombatDepthBuilder`), verified headlessly: validator **192/192 PASSED**, EditMode tests **20/20**, PlayMode smoke test green (it now equips three actives and clicks through level-up offers mid-run). Architecture: pure-logic `StatusEffectBuffer` (fixed slots, zero-alloc) behind a `StatusEffectReceiver` per enemy (tint cue + colored DoT numbers; per-enemy looping status VFX skipped as unnecessary at PPU 16 — tint reads clearly); all player damage centralized in `DamageService` (crit roll, lifesteal, styled popups); `ActiveSkillSO` 5-level growth tables + one `ActiveSkillManager` with per-behavior executors; generic pooled `SkillProjectile` (pierce/homing/explode/lob), `AreaEffectZone` puddle, `ZapArcVfx` chain segments; 8 new pools (16 total). Deviations vs the written plan: the **baseline auto-attack stays its own `AutoAttack` component** rather than folding into the skill framework (it already reads all PlayerStats and gains crit/lifesteal via `DamageService` — folding it in bought nothing); status buffers exist on **enemies only** for now (no enemy applies statuses to the player yet — the buffer is entity-agnostic when that comes); rarity **replaces** the old flat `_weight` (field kept for serialization); "Chilling Nectar" (7th skill) not needed yet — roster feels full with 6. Criterion 5 (zero-GC) **confirmed by Ron in an in-editor Profiler session (2026-07-04)**: during a skill-heavy run the only per-frame GC Alloc was inside URP's own `RenderSingleCameraInternal` (engine/editor render path, not gameplay code) — all gameplay script rows at 0 B. This also closes Phase 1's outstanding Profiler criterion.

---

## 5. Phase 3 — Run Structure (waves, bosses, drops, results) — ✅ DONE (2026-07-04)

> **Execution split (2026-07-04, agreed with Ron):** Phase 3 ships as three independently
> verified + committed sub-phases so a session/token budget running out never strands
> half-done work — resume from whichever sub-phase is unchecked:
> - **3A — Stage timeline** ✅ (2026-07-04, validator 212/212, EditMode 25/25, PlayMode green): `StageConfigSO` (duration, escalating spawn-rate curve, timeline
>   events), `StageDirector` + pure `StageTimeline` crossing logic (EditMode-tested), strong
>   waves at 25%/75% (surround-ring + directional-flood formations via `EnemySpawner.SpawnAt`),
>   HUD stage progress bar with siren/skull/crown event markers. Miniboss/boss events fire but
>   only raise notifications until 3B.
> - **3B — Bosses** ✅ (2026-07-04, validator 263/263, EditMode 25/25, PlayMode 2/2 incl. a boss-flow test that fast-forwards the timeline, kills the Queen, and asserts victory; Queen body = royal-tinted BossPack1 BlueDragon until custom art): Queen's Royal Guard miniboss (telegraphed charge), Queen Bee (summon
>   workers / radial stinger burst / charge sweep, enemy projectile pool), boss HP bar, spawn
>   banner + shake, Queen death = victory path.
> - **3C — Drops + results** ✅ (2026-07-04, validator 284/284, EditMode 29/29, PlayMode 2/2): pooled item drops (Honey Jar / Magnet / Wax Shield / Royal
>   Bomb) with drop tables, results screen on death & victory (time, kills, level, currency
>   banked), restart flow, README/TODO refresh.
> All three extend one `Phase3RunStructureBuilder` pass + the validator; each ends with
> headless validator/tests green and a commit.

### 5.1 Stage timeline (TODO #6, #7, #9)

- Stage defined by a `StageConfigSO`: total duration (e.g. 15 min), escalating spawn-rate curve (distinct from existing stat scaling), and timeline events:
  - **25% & 75%** — Strong Waves: burst spawns with formation patterns (surround ring, directional flood).
  - **50%** — miniboss: **Queen's Royal Guard**.
  - **100%** — final boss: **Queen Bee**.
- **HUD stage progress bar** with event markers (waves/skull/crown icons) so the player sees what's coming.

### 5.2 Bosses (TODO #8)

- Miniboss (Queen's Royal Guard): big bee (tint+scale), high HP, one special: telegraphed charge attack. Announced with banner + screen shake.
- **Queen Bee** (world boss): distinct big monster (best-fit BossPack1 body, royal tint, until custom art), 2–3 telegraphed patterns: summon corrupted workers, radial stinger burst, charging sweep. Boss HP bar on HUD.
- Killing the Queen = **world clear** → victory results screen (first winnable run in the game). Bosses/strong-wave hordes fully pooled (TODO: object-pool coverage).

### 5.3 Item drops (TODO #10)

Pooled world drops from elites/events: **Honey Jar** (heal %), **Magnet** (vacuum all pickups), **Wax Shield** (absorb next N hits), **Royal Bomb** (screen nuke with big VFX). Drop tables on enemy/wave data.

### 5.4 Run results (suggestion adopted)

On death **or** victory: results screen — time survived, kills, level reached, currency earned (banked to the wallet → feeds Phase 4 meta). Restart/continue from there.

**Success criteria (Phase 3):**
1. A full run plays start-to-finish: strong waves fire at 25%/75%, miniboss at 50%, Queen at 100%, matching the progress bar markers.
2. Queen fight: all her patterns telegraph before hitting; she's beatable by a decently-built run; killing her shows victory results.
3. Spawn rate demonstrably escalates (enemy count at minute 10 ≫ minute 1) with frame rate stable and zero-GC intact at peak horde + boss + VFX load.
4. All 4 drop types spawn, do their effect, and return to pools.
5. Results screen shows correct stats for both death and victory paths; currency banks correctly.
6. Validator extended to cover stage config + boss prefabs; scene builder can regenerate everything.

> **Phase 3 completion notes (2026-07-04):** delivered in three verified commits (3A/3B/3C, see the
> execution-split checklist above) via `SurveHive/Apply Phase 3 Run Structure`. Final state:
> validator **284/284**, EditMode **29/29**, PlayMode **2/2** — including a boss-flow test that
> fast-forwards the live stage clock, asserts the Royal Guard at 50% and Queen at 100%, kills her,
> and checks the victory results screen. Deviations/decisions: stage duration is **10 min** (not
> 15) pending the Phase 5 tuning pass; the Queen's body is the **BossPack1 dragon** with a royal
> tint (per the plan's "best-fit body until custom art"); success criterion 2 ("beatable by a
> decently-built run") and criterion 3 (frame rate at peak load) still want a real human playthrough
> — everything else is machine-verified. Drop rates: trash 1–3%, Queen's Guard 12%, bosses 100%.
> The Wax Shield registers as a damage absorber on HealthComponent (new `IDamageAbsorber` seam).

---

## 6. Phase 4 — Meta & Menus (TODO #11, #12 + save/load + pause) — ✅ DONE (2026-07-05)

> **Execution split (2026-07-04):** like Phase 3, Phase 4 ships as three independently
> verified + committed sub-phases so a session/token budget running out never strands
> half-done work — resume from whichever sub-phase is unchecked:
> - **4A — Save/load + meta shop core (no UI)** ✅ (2026-07-04, validator 353/353, EditMode
>   52/52, PlayMode 2/2; PlayMode tests redirect the save to a temp file so they never touch
>   the real one): versioned JSON `SaveData` schema with
>   safe-write (temp file + swap) and corrupt→fresh-start handling; `MetaUpgradeSO`
>   assets for the 6 flat stats (Max Health / Damage / Move Speed / Attack Speed /
>   Magnet / Currency Gain) with escalating cost math; persistent store implementing
>   the extended `IMetaProgressionStore` (bank, spend, ranks, settings, best-run
>   stats); purchased ranks applied to the player at run start; EditMode tests
>   (save round-trip, corrupt save, cost/effect math, spend transactions).
>   Purchases are machine-testable through the store API — shop UI arrives in 4B.
> - **4B — Menus & scene flow** ✅ (2026-07-05, validator 437/437, EditMode 52/52, PlayMode
>   3/3 incl. a menu-flow test: shop buy disabled at 0 honey → bank → purchase → start run →
>   purchased max-HP rank verified on the player; layout verified via verify-driver
>   screenshots in landscape): MainMenu bootstrap scene (Play / Hive Upgrades /
>   Settings / Quit) in the pixel kit, world select (Beehive playable, Garden+
>   locked, difficulty dropdown seam), Hive Upgrades shop UI over the 4A store,
>   results screens gained RETRY / HIVE buttons routing back; builder generates the
>   menu scene from scratch (idempotent-by-regeneration) and registers build-settings
>   scenes (menu boots first). Tap-anywhere restart removed (it would race the new
>   buttons) — R stays as the keyboard shortcut. Settings panel is a shell until 4C.
> - **4C — Pause menu + settings** ✅ (2026-07-05, validator 466/466, EditMode 52/52,
>   PlayMode 4/4 incl. a pause test: freeze verified on the run clock, settings change
>   persisted to the save file, resume restores, and the menu refuses to open over
>   another pause owner): in-run pause via ESC or a HUD button (resume / settings /
>   abandon-to-menu, abandoning banks the honey) with a full freeze — all
>   spawners/cooldowns run on scaled time, so `timeScale = 0` stops everything;
>   shared `SettingsPanelUI` block (music + SFX sliders, vibration and quality
>   cycle-buttons) built into both the main-menu settings panel and the pause menu,
>   applying live (SFX drives `AudioListener.volume`; music stored for the Phase 5
>   audio service) and persisting through the 4A save.
> All three extend one `Phase4MetaAndMenusBuilder` pass + the validator; each ends
> with headless validator/tests green and a commit.

- **Save/load foundation**: JSON via `JsonUtility` at `Application.persistentDataPath` (versioned schema, safe-write). Persists: meta currency, purchased upgrades, settings, best-run stats.
- **Meta progression — flat stat shop** implementing the existing `IMetaProgressionStore` seam: permanent ranks of Max Health / Damage / Move Speed / Attack Speed / Magnet / Currency Gain, escalating costs. *Designed for expansion*: upgrades are data assets (`MetaUpgradeSO` list), store interface stays abstract, so a future hive-tree redesign only replaces UI + data, not consumers.
- **Menus**: Main Menu (Play / Hive Upgrades / Settings / Quit) → world select screen (Beehive playable, Garden+ locked, difficulty dropdown seam) → run. All in the pixel UI kit.
- **Pause menu** in-run: resume/settings/abandon (audio sliders, vibration + quality toggles for mobile-later).
- Scene structure: lightweight bootstrap/menu scene + world scenes; run results screen routes back to menu.

**Success criteria (Phase 4):**
1. Full loop: Menu → run → die/win → results → currency visibly spendable in the shop → next run measurably stronger (e.g. buy +Max HP, see higher starting HP).
2. Quit the app entirely; relaunch: meta purchases, currency, settings all persist.
3. Pause freezes the run completely (no spawns/damage during pause) and settings changes apply live.
4. Corrupt/missing save handled gracefully (fresh start, no exceptions).
5. EditMode tests: save round-trip, upgrade cost/effect math, wallet transactions.

> **Phase 4 completion notes (2026-07-05):** delivered in three verified commits (4A/4B/4C, see
> the execution-split checklist above) via `SurveHive/Apply Phase 4 Meta & Menus`. Final state:
> validator **466/466**, EditMode **52/52**, PlayMode **4/4**; menu/shop/pause layouts verified
> visually through verify-driver screenshots (`VerifyShots/`). Success criteria: 1 is covered
> end-to-end by the menu-flow PlayMode test (buy +Max HP → run starts with higher HP); 2's
> mechanism (file-backed JSON, lazy reload) is machine-verified — a literal quit-and-relaunch
> check is a 10-second human confirmation; 3–5 are machine-verified. Deviations/decisions:
> settings use **cycle-buttons** for vibration/quality instead of toggles/dropdown (simpler
> code-built UI, same seam); the difficulty dropdown is present but locked to Normal (no
> difficulty system yet — Phase 5 tuning decides); **tap-anywhere restart removed** in favor of
> RETRY/HIVE buttons (R key stays); abandoning a run banks its honey; the persistent store
> invalidates its cache on save-path changes so tests can never leak state into a real session.

---

## 7. Phase 5 — Audio & Final Polish

- **Audio service**: pooled `AudioSource` pool behind an interface (per coding standards), zero-GC playback, SFX events: hit, kill, pickup, level-up, skill fires (per-skill), player hurt, death, victory, UI clicks, boss stingers.
- **Free/CC0 sourcing pass**: Kenney audio packs, freesound.org, ChipTone-generated extras; one looping Beehive music track (menu + in-run variants if available). All license notes recorded in `Assets/Audio/CREDITS.md`.
- **Difficulty/curve tuning pass** (TODO suggestion): dedicated balancing of EXP curve, enemy scaling, spawn curve, drop rates, boss HP — target: a first-time player dies around minute 8–12; a meta-invested player can clear.
- **Mobile sanity pass**: joystick + tap flows verified in Device Simulator, HUD respects safe areas, texture memory sane, and a profiling pass on target-ish resolution.
- **Localization seam** (TODO suggestion, cheap now): all user-facing strings flow through one string table/asset instead of hardcoded literals — actual translation deferred.

**Success criteria (Phase 5):**
1. Every listed event has a sound; music loops seamlessly; sliders in settings control SFX/music independently and persist.
2. `CREDITS.md` lists source + license for every audio file.
3. Device Simulator (iPhone + a tall Android profile): all UI usable and inside safe areas, joystick controls the bee correctly.
4. Tuning pass documented (what changed and why) and README's status section updated.
5. Zero-GC + frame-rate criteria from earlier phases still pass after all additions (regression check).

---

## 8. Cross-cutting rules (apply to every phase)

- **Coding standards**: everything per `CLAUDE.md` — zero-GC hot paths, `[SerializeField] private`, cached components, pooled everything, interfaces for cross-system seams.
- **README is a living doc**: update it in the same session as any scope/mechanics change (each phase ends with a README refresh; TODO.md items get checked off/removed as they land).
- **Scene tooling**: `BeehiveSceneBuilder` (+ additive passes) and the validator are extended alongside every phase so the scene stays regenerable — never hand-edit-only.
- **Tests**: EditMode tests for pure logic (status effects, rarity, curves, save, shop math) in a `SurveHive.Tests.EditMode` asmdef; run via Test Runner before calling a phase done.
- **Definition of done per phase** = all success criteria demonstrated + validator green + tests green + README updated + committed.

## 9. Suggested order & sizing

| Phase | Content | Rough size |
|---|---|---|
| 0 ✅ | Asset triage + pixel-perfect camera foundation — done 2026-07-04 | small |
| 1 ✅ | Art swap, game feel, UI reskin — done 2026-07-04 | large |
| 2 ✅ | Status effects, 6 actives, 10 passives, rarity — done 2026-07-04 | large |
| 3 ✅ | Stage timeline, bosses, drops, results — done 2026-07-04 | large |
| 4 ✅ | Save, meta shop, menus, pause — 4A ✅ 2026-07-04, 4B+4C ✅ 2026-07-05 | medium |
| 5 | Audio, tuning, mobile sanity, localization seam | medium |

Phases are sequential by design (each builds on the previous), but 4 and 5 can swap if you want sound earlier.
