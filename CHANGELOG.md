# Changelog

All notable changes to **SurveHive** are documented here. The project was built in a
phased development push (Phases 0–5A, below); `TODO.md` carries the open backlog and
suggested next steps. Dates are the day the work landed.

The format is loosely based on [Keep a Changelog](https://keepachangelog.com/).
This project targets mobile (PC-first, mobile-ready) on Unity 6000.5.2f1 (URP 2D).

### Playtest fixes — shop scrolling + difficulty unlock gating (2026-07-08)

Same-day feedback round on Phases 1B/1C.

- **Shop actually scrolls now**: the 1C ScrollRect had no raycast surface of its own, so
  wheel/drag input over the shop never reached it. The viewport gained an invisible
  full-area input surface plus a **visible scrollbar** down the right edge (drag handle +
  "there's more below" cue); sensitivity bumped for mouse wheels.
- **Difficulty tiers are now earned**: Easy/Normal always open; **Hard** unlocks after
  clearing The Beehive on Normal; **Extreme** unlocks after clearing it on Hard **plus**
  clearing the next stage (the Garden) on Normal — so Extreme stays visibly locked until
  more worlds ship. Gates are data on the `DifficultySO` rows (append-only
  `unlockRequirements`); victories record per-stage/per-tier clear flags via
  `RunSession` → the save (v3, migrating v2 saves to an empty clear record).
- **Locked-tier UX**: locked rows read "— LOCKED" in the dropdown; picking one bounces
  back to the previous tier and pins a **tooltip listing the unlock tasks** — met tasks
  green, checked (`[X]`) and struck through, open ones plain `[ ]` — the same tooltip
  hover shows on any locked row. Saves pointing at a now-locked tier fall back to the
  highest unlocked one.
- EditMode tests: gate logic per tier, stage-clear save round-trip, v2→v3 migration,
  mismatched-array sanitizing; validator asserts for the gate data, tooltip/hover wiring,
  scroll surface, and scrollbar.

### Phase 1C — Meta shop expansion: 7 new upgrades + power-up rerolls (2026-07-08)

TODO #28: the honey from 1B gets somewhere to go — the Hive Upgrades shop grows from
6 to **13 permanent upgrades**, including the run-changing reroll mechanic.

- **Six new stat upgrades** (each a `MetaUpgradeSO` + append-only `MetaStatType` entry,
  applied at run start by `MetaUpgradeApplier`): **Wisdom of the Hive** (EXP gain +5%/rank ×8),
  **Queen's Blessing** (ability damage +4%/rank ×8), **Efficient Glands** (active-skill
  cooldowns −3%/rank ×6, floored by the existing 0.4× cap), **Killer Instinct** (crit chance
  **+2%/rank ×20 = 40% cap** on the 1A 0% base — with Keen Eye's 30% a full build hits 70%),
  **Barbed Stingers** (crit damage +5%/rank ×10), and **Forager's Instinct** (item drop rolls
  +10%/rank ×5, multiplying the drop table in `EnemyLoot` via a run-reset static).
- **Power-up rerolls — "Waggle Dance"**: bought rank = per-run stock (max 3), refilled every
  run. On the level-up screen each card gets a REROLL button (plus a remaining-count readout,
  both hidden until a rank is owned); a reroll replaces **that one card** with a fresh
  eligible pick that's never a duplicate of anything on screen, keeps the offer's forced-lucky
  state, and refuses to waste a charge when the pool has nothing else. Cost-gated hard per the
  design mandate: **400 / 1,520 / 5,776** honey (3.8× growth). Pure pick logic lives in
  `RerollLogic` (EditMode-tested).
- **Scrollable shop**: the 2×3 card grid becomes a 2-column, 13-card grid inside a vertical
  `ScrollRect` (drag/wheel), title/balance/back staying fixed. Built additively by
  `MetaShopExpansionBuilder` — existing cards are reparented, not rebuilt.
- Existing six upgrade costs untouched: the cross-shop rebalance folds into 1A round 2 once
  post-nerf income data exists.
- EditMode tests (reroll stock/exclusion/exhaustion semantics, every new stat applying
  through the real applier, drop-rate static resetting between runs) + validator asserts for
  the 13-card shop, the crit/reroll gates, and the reroll UI wiring.

### Phase 1B — Working stage difficulty (2026-07-08)

TODO #30: the Phase-4B difficulty dropdown seam (fixed to Normal) is now a real,
data-driven system.

- **Four tiers — Easy / Normal / Hard / Extreme** — authored as one `DifficultySO` tier
  table (`Assets/Data/Progression/DifficultySettings.asset`): per tier, enemy HP and
  damage multipliers (Easy 0.75×/0.75× → Extreme 2.25×/1.9×), an optional spawn-rate
  multiplier (Hard 1.15×, Extreme 1.3×), and a compensating **honey-gain multiplier**
  (Easy 0.75× → Extreme 2.25×). Tuning is inspector-only data; the additive
  `DifficultyBuilder` pass never overwrites an existing 4-row table.
- **One hook covers every spawn**: the multipliers resolve once at run start
  (`RunSession.SelectedDifficulty` static carries the menu choice across the scene load)
  and apply inside `EnemySpawner.SpawnAt` — regular drip, strong waves, the Royal Guard
  miniboss, and the Queen all scale. Honey compensation multiplies every pickup in
  `RunCurrencyWallet`, stacking with the meta-shop gain upgrade.
- **Live world-select dropdown**: 4 options with placeholder icons (feather / star /
  sword / skull from the temp icon pack — final art specced in `ASSET_GENERATION.md`
  §2.7), populated from the tier table by a new `DifficultySelectUI`; the TMP dropdown
  template got icon slots + row sizing to fit.
- **Save v2**: last-selected difficulty persists (`selectedDifficulty`, clamped on load;
  v1 saves migrate to Normal) and restores on the next boot.
- EditMode tests (tier lookup + fallback, enemy HP/damage scaling through
  `EnemyController.Initialize`, honey stacking, save round-trip/migration/clamping) and
  ~20 new validator asserts pin the wiring in both scenes.

### Phase 1A (round 1) — Balance: honey economy + crit rework (2026-07-08)

First tuning round of the new plan's balance pass, driven by playtest feedback (the
meta shop maxed out in ~6–8 runs and crit was too freely available).

- **Honey income cut ~60%** across every enemy drop table (chance and/or roll ranges):
  Worker/Spitter/Bomber 25%×1–2 → 15%×1, Warrior 35%×2–4 → 30%×1–2, Swarmling 8% → 4%,
  Queen's Guard 60%×2–5 → 40%×1–3, Royal Guard 8–15 → 3–6, Queen 40–60 → 15–25.
  Maxing the current shop should now take roughly 2.5× as many runs; the 1C shop
  expansion adds more sinks on top.
- **Base crit chance 5% → 0%** (script default, scene value, and a new validator
  assert) — all crit now comes from power-ups and, later, the 1C meta upgrade.
- **Keen Eye reworked**: 6 levels × flat +5% → **5 levels totalling 5/10/15/20/30%**
  (the last level jumps +10%). Implemented via an optional per-level magnitude table
  on `SkillDefinitionSO` (`_magnitudePerLevel`, append-only field) threaded through
  `SkillEffectApplier` (now takes the pre-application level) and `SkillStatPreview`,
  so card previews keep showing the exact numbers. EditMode tests pin the curve.
- **Spec locked for 1C** (PLAN/TODO updated): power-up rerolls capped at **3 per run**
  with steeply escalating rank costs (~400 base, ~3.8× growth), and the crit-chance
  meta upgrade set at **+2%/rank, 20 ranks, 40% cap**.
- EXP curve, enemy HP/damage ramp, spawn curve, and boss HP deliberately untouched —
  hand-tuned last round; re-evaluate against post-nerf playtests (1A stays ◐ until
  the die-at-8–12-min / meta-clears-Queen targets are confirmed).

### Phase 4B/4C — Enemy variety: Bomber Bee + Swarmlings (2026-07-07)

Phase 4 (TODO #22) complete — all three behavior archetypes shipped.

- **New enemy rank: the Bomber Bee** (4B) — a hot-orange rusher (rank 1, unlocks 2:30,
  weight 0.25, fast at 3.3 u/s). Inside 1.6u it stops and lights a **fuse** — a rapid
  orange pulse for 0.55s — then detonates a 2.2u AoE blast for 2.5× its run-scaled
  contact damage, with a pack explosion VFX (`BomberBlast` pooled wrapper). **Dying
  detonates it too** (`BomberAttack` subscribes to `OnDied`), so point-blank kills stay
  dangerous; ranged kills and knockback (resistance 0.7 — it shoves easily) are the
  counter-play. Stuns hold the fuse timer rather than defusing it, and the blast
  consumes the bomber through the normal health pipeline (EXP/loot still drop).
- **New enemy rank: the Swarmling** (4C) — a tiny (0.6 scale), fast, 8-HP pale-blue rank
  that arrives in **packs of 6**: `WaveSpawnerConfigSO.WaveEntry` gained a `packSize`
  field (legacy assets hold 0 — `ClampPackSize` reads 0/1 as single, unit-tested) and
  the spawner now spawns the whole pack clustered around the pick point. Each swarmling
  weaves a perpendicular sine wobble with a per-instance phase (`SwarmMovement`), so the
  cluster fans out into a living cloud instead of a stacked column. Unlocks at 1:00,
  weight 0.3 — early pressure by numbers, not stats.
- **Pipeline**: `EnemyVarietyBuilder` extended additively (shared trash-prefab helper +
  per-rank behavior wiring, `BomberBee`/`SwarmlingBee`/`BomberBlastVfx` pools, wave
  entries); validator's Phase 4 block now covers all three ranks via a shared
  per-prefab check (rig, status receiver, health bar, behavior wiring, pools,
  pack sizes).

### Phase 4A — Enemy variety: ranged Spitter Bee (2026-07-07)

First of the three Phase 4 archetypes (TODO #22) — enemies stop being uniformly
chase-and-touch.

- **New enemy rank: the Spitter Bee** — a venom-green ranged bee (rank 1, unlocks at
  1:30, spawn weight 0.3) that **kites to a firing band** instead of chasing to touch
  range: it flees when the player closes inside 4.5u, chases when they escape past 7.5u,
  and orbits sideways in between. On a 2.5s cycle it stops, pulses a pink telegraph
  (0.6s, matching the hostile stinger color), and fires one pooled `EnemyProjectile`
  stinger at the player.
- **Behavior architecture**: `RangedAttack` component (pooled-safe zero-alloc state
  machine mirroring `ChargeAttack`) + `RangedSteering`, a pure static flee/hold/chase
  decision on squared distances, unit-tested in EditMode. Firing respects stuns
  (`IsAttackDisabled`) and only winds up when the shot can actually reach.
- **Projectile damage now scales with the run**: `EnemyController` exposes
  `ScaledContactDamage` (contact damage × the run's damage curve) so spitter shots — and
  any future secondary attacks — grow like touches do.
- **3B interleave**: the Spitter carries a small **magic shield** (15) — physical builds
  pop it on touch, magic builds must chew through the shield first. Weak melee (6 contact)
  makes diving it the counter-play.
- **Pipeline**: new additive `EnemyVarietyBuilder` pass (shared bee rig in venom tint,
  health bar, status receiver, stats asset, wave-table entry, `SpitterBee` pool);
  validator grew a Phase 4A block (stats/rig/wiring/pool/wave checks).

### Playtest fixes — set-effect UX + burst-hit performance (2026-07-07)

Feedback from the first 3C playtest.

- **Fixed all six set names showing on the HUD from run start**: `ElementSets`' static
  state defaulted to "tier I active" for every element until the first recompute, and the
  HUD could enable before the run initialized the service. Tiers now default to inactive,
  re-initialization notifies subscribers, and unconfigured sets can never render.
- **Set state moved off the combat HUD** to where picks are decided: each offer card's
  set progress ("WILDFIRE SET — unlocks: Burns last 30% longer" / "2/3 — at 3: …") renders
  **below the card** so long descriptions never overflow it, the offer panel bottom lists
  active tiers with effects, and the pause build panel gained a SET BONUSES section
  (pieces, active effect, next threshold). Element colors consolidated into `ElementPalette`.
- **Offer panel got a context title and a taller layout**: "LEVEL UP!" normally,
  "MINIBOSS KILLED!" for the guaranteed-lucky reward offer, so players know where the
  popup came from; the panel background expanded vertically (980×760) to hold the title,
  cards, below-card set lines, and the set summary.
- **Fixed frame hitches when piercing volleys hit/kill crowds**: the damage-number pool
  grew by instantiating popup canvases mid-frame and then destroyed the overflow on
  release — every burst paid an instantiate + destroy storm. Damage numbers now use a
  no-grow `TryGet` (overflow numbers are dropped, capped at 48 concurrent) and the death
  VFX pool keeps burst instances instead of destroy-churning (24 prewarmed / 96 kept).

### Combat 2.0 — Elemental set effects (Phase 3C) (2026-07-07)

Committing to an element now grants escalating set bonuses (TODO #19) — the payoff
layer on the 1A element tags, 3A typing, and 3B defenses.

- One **`SetBonusSO` per element** with 2 / 3 / 4-piece tiers (values are totals at the
  tier): Wildfire (fire), Virulence (poison), Overcharge (electric), Deep Chill (frost),
  Sticky Sweet (honey) amplify their status's potency/duration; Sharp Stingers (physical)
  adds +6/12/20% basic-attack damage. Authored by the idempotent `Combat 2.0/3C` builder
  pass into `Assets/Data/SetBonuses/`, registered on the `SkillDatabaseSO`.
- New run-scoped **`ElementSets`** static service: `LevelUpUIController` initializes it per
  run and recounts owned distinct enhancements+abilities per element after every pick;
  per-element multipliers are cached on change so hot-path queries stay zero-GC.
- Bonuses apply at two seams only: `StatusEffectReceiver.ApplyEffect` (every status flows
  through it; burn→fire, poison→poison, stun→electric, freeze+cold→frost, slow→honey) and
  `AutoAttack` (physical set's damage multiplier).
- **HUD set-tier line** (`SetTierHUD`): element-colored "WILDFIRE II · STICKY SWEET I",
  rebuilt only when counts change; element colors unified into a shared `ElementPalette`.
- 7 new EditMode tests (tier thresholds, status routing, multiplier math, change-event
  discipline) + validator checks (6 valid sets covering all elements, HUD wiring).

### Combat 2.0 — Enemy defenses beyond HP (Phase 3B) (2026-07-07)

Elites and bosses now carry defensive layers that make the 3A damage typing matter
(TODO #23): an ordered **shield → armor → HP** pipeline per enemy.

- New pure-logic `EnemyDefense` (typed shield pools + armor) registered as both the
  absorber and mitigator on every enemy's `HealthComponent`: a **physical shield** soaks
  physical only (magic bypasses), a **magic shield** soaks magic only, **armor** %-reduces
  physical damage that got past shields (magic ignores armor). Pools reset on pooled respawn.
- The absorber/mitigator seams are now type-aware and support partial absorption:
  `IDamageAbsorber.Absorb(amount, type) → remainder`, `IDamageMitigator.Mitigate(amount, type)`.
  Player Wax Shield (whole-hit charges) and player armor (reduces both types) keep their
  behavior on the new signatures.
- Per-rank data on `EnemyStatsSO` (`_armorPercent` / `_physicalShield` / `_magicShield`,
  shields scale with the run's health multiplier): Queen's Guard 15% armor + 30 magic
  shield, Royal Guard 15% armor + 250 physical shield, Queen 20% armor + 400/400 shields.
  Workers/warriors carry nothing — early-game hit counts unchanged.
- Enemy health bar tints **steel-blue** while a physical shield holds, **violet** for a
  magic shield, reverting when shields break; fully-soaked hits still flash the hit anim.
- 8 new EditMode tests (type routing, partial absorb, pooled reset, full pipeline order
  through a real `HealthComponent`) + validator checks on the per-rank defense data.

### Combat 2.0 — Damage typing (Phase 3A) (2026-07-06)

Every damage application now carries a **physical/magic** `DamageType` (TODO #20) —
the seam the upcoming enemy defenses (3B) and elemental set effects (3C) read.

- `IDamageable.TakeDamage` and `DamageService.DealDamage` take a `DamageType`; every
  call-site stamps one. Basic attack + enhancement procs, enemy contact, and enemy
  projectiles are **physical**; elemental abilities, status DoTs (burn/poison), and the
  Royal Bomb nuke are **magic**.
- Each ability's type lives on its `ActiveSkillSO` (`_damageType`) and must match its
  offer card's element (physical element ⇔ physical damage) — enforced by a new scene
  validator check and an EditMode test. Stinger Barrage / Piercing Lance are physical;
  the other seven abilities are magic.
- `DamageOnContact` / `EnemyProjectile` expose a serialized damage type (physical by
  default) so future magic-touch/caster enemies are pure data.
- No balance change yet: mitigation still ignores the type until 3B lands.

### Combat 2.0 — Boss & Wave Drama (Phase 2) (2026-07-05)

Makes the run's set-pieces land.

- **Pre-spawn warnings (2A)**: every strong wave and boss/miniboss telegraphs ~5s ahead with
  an upper-centre countdown banner (`StageTimeline.CollectNewlyWarned` lookahead).
- **Impactful miniboss kill (2B)**: killing a miniboss grants a guaranteed lucky (+2) level-up
  offer plus a burst of EXP.
- **Boss death sequence (2C)**: any boss/miniboss death drops into 0.25× slow-motion with the
  player invulnerable, a shockwave + screen shake, and holds the timeline resume / victory /
  reward until the beat finishes. Cooperates with `GamePause` + `HitStop` on the time scale.

### Combat 2.0 — Power-up lanes (Phase 1A–1E) (2026-07-05)

Restructured the flat level-up pool into **three lanes**, each with its own distinct-pick
cap and card banner, tagged with an **element** (physical/fire/poison/electric/frost/honey).

- **Taxonomy & caps (1A/1B)**: `PowerUpLane` (Passive/Enhancement/Ability) + `SkillElement`
  on every skill; each offer card shows a lane banner + element gem. Per-lane distinct caps —
  **Passive 5 / Enhancement 3 / Ability 5** — enforced by the pure `LaneEligibility`: once a
  lane is full, no *new* pick from it is offered, but owned picks keep leveling.
- **Passives (1C)**: added **Armor** (percent damage-taken reduction via an `IDamageMitigator`
  on the player's `HealthComponent`, capped) and **Ability Power** (multiplier on all
  active-skill damage). Projectile-count / attack-range moved out of Passive into Enhancement.
- **Enhancements — new lane (1D)**: a composable modifier layer on the basic attack
  (`BasicAttackPayload` → `Projectile`). **Multishot** damage tradeoff (~1.5× total per extra
  projectile, each softer); **Piercing Stinger** (pierce 2/4/everything over 3 levels, damage
  penalty 30%/20%/0%, extended travel); **Burning** & **Poison Stinger** (on-hit DoT, chance +
  damage/tick both scale per level); **Frost Stinger** (chance to freeze); **Shock Stinger**
  (chance to bounce with damage/chance falloff). Retired the standalone Piercing Lance active.
- **Abilities (1E)**: the radial stinger burst now **pierces**; added **Frost Nova** (radial
  freeze), **Ball Lightning** (radial stun), and **Honey Bomb** (homing explosion + slow),
  reusing existing pools. All ability damage scales with Ability Power.
- **Distinct ability gameplay (1E revamp)**: Frost Nova is an expanding icy ring that chills
  (new persistent **Cold** status); Ball Lightning is a slow player-sized orb that pierces,
  ticks damage, and bounces off the screen edges; Honey Bomb scatters slowing honey zones.
- **Readability (1F)**: each offer card shows its lane's `owned/cap`; the pause menu gains a
  **POWER-UPS** panel listing the run's build grouped by lane with levels + element cues.
- Built additively via the idempotent `SurveHive/Combat 2.0/*` editor passes; EditMode tests
  cover the lane caps, armor/multishot/pierce math, and the active-skill growth tables.

### Phase 5A — Audio Service (2026-07-05)

- **Audio system**: a scene-scoped `AudioService` behind an `IAudioService` seam — a
  round-robin pool of SFX `AudioSource`s (overlapping one-shots layer instead of cutting
  each other off, each with its own pitch jitter) plus one looping music source.
- **`AudioLibrarySO`** maps every `SfxId` / `MusicId` to its clip(s), with per-event
  volume and pitch-range settings; lookups are built once, zero per-call allocation.
- **SFX wired to events**: enemy hit, kill, currency pickup, level-up, player hurt/death,
  victory, UI clicks (via a reusable `UIClickSfx` drop-on), boss stinger bursts, and each
  of the 6 active skills firing. Pollen Cloud's aura tick and EXP-orb pickups are
  deliberately excluded (both fire many times/sec — would flood the mix). Hit/kill carry a
  per-sound min-interval throttle so an AoE hitting a whole horde reads as a texture rather
  than a machine-gun wall.
- **Original, procedurally synthesized SFX** fitting the bee/honey pixel-art theme —
  buzzy amplitude-modulated sawtooths for combat, chiptune arpeggios for level-up/victory,
  honey "bloops" for pickups, distinct gestures per skill — generated by `Tools/Audio/synth.py`
  (pure-stdlib Python). Every clip maps 1:1 to an `SfxId`, so swapping in AI-generated
  (e.g. ElevenLabs) or sourced clips for final polish is drop-a-file + re-run the pass.
- **Music**: a looping CC0 track (OpenGameArt) in the menu and another during runs, driven
  by the settings sliders and imported as streaming to keep memory sane on mobile. Sources +
  licenses recorded in `Assets/Audio/CREDITS.md`.
- Settings sliders (music/SFX) now drive the audio service live and persist through the
  save; added a previously-missing `AudioListener` to both scenes' cameras.

### Docs

- Created this `CHANGELOG.md` from the full commit history (Phases 0 → 5A).
- **Retired `PLAN.md`** — the phased build push it tracked is complete through Phase 5A.
  Its durable content was folded into the living docs: the art-direction reference (target
  look, PPU/resolution, honey palette hex codes, bloom-deferred note, custom-art tool
  recommendations) moved to `README.md`; the remaining Phase 5B work (difficulty tuning
  targets, mobile sanity pass, localization seam) moved to `TODO.md`.
- Restructured `TODO.md`: added a **suggested implementation order** (combat-depth chain →
  enemy variety → final-polish leftovers → content), corrected the stale "nothing
  implemented yet" intro, and marked object-pool coverage done.

### Phase 4 — Meta & Menus (2026-07-04 → 2026-07-05)

- **4A — Save + meta shop core**: versioned JSON save at `Application.persistentDataPath`
  (safe-written via temp-file swap; corrupt/missing → fresh start) persisting banked honey,
  purchased upgrade ranks, settings, and best-run stats. Six `MetaUpgradeSO` stat upgrades
  (Max Health / Damage / Move Speed / Attack Speed / Magnet / Currency Gain) with escalating
  costs, applied permanently at run start via `MetaUpgradeApplier` through the
  `IMetaProgressionStore` seam.
- **4B — Menus & scene flow**: generated `MainMenu` boot scene in the pixel kit — home
  (Play / Hive Upgrades / Settings / Quit), world select (Beehive playable; Garden + Woods
  locked; difficulty dropdown seam), and the Hive Upgrades shop (2×3 card grid with live
  rank/cost/balance and affordability gating). Death/victory screens gained RETRY / HIVE
  buttons; tap-anywhere restart removed in favor of buttons (R still restarts).
- **4C — Pause & settings**: in-run pause via ESC or a HUD button (resume / settings /
  abandon — abandoning banks the run's honey) with a full `timeScale = 0` freeze; a shared
  settings block (music + SFX sliders, vibration + quality cycle-buttons) in both the main
  menu and pause menu, applied live and saved. Pause never opens over another freeze.
- **Tuning + shop redesign**: meaningful upgrade values, grid shop UI, boss-gated timeline
  freeze, and rarer rare/epic offers.

### Phase 3 — Run Structure (2026-07-04)

- **3A — Stage timeline**: `StageConfigSO`-driven 10-minute run with an escalating spawn-rate
  curve (1× → 3.5×) on top of per-minute stat scaling, timeline events, and a HUD progress
  bar with siren/skull/crown markers. Strong waves with formations — surround ring at 25%,
  directional flood at 75%.
- **3B — Bosses**: Queen's Royal Guard miniboss at 50% (telegraphed charge) and the Queen Bee
  final boss at 100% (three telegraphed patterns — summon workers, radial stinger burst,
  charging sweep — with an enemy-projectile pool). Boss HP bar, spawn banner + shake. While a
  boss is alive the timeline and regular spawns freeze. Killing the Queen wins the run.
- **3C — Item drops + results**: pooled world drops — Honey Jar (heal), Magnet (vacuum
  pickups), Wax Shield (absorb N hits), Royal Bomb (screen nuke) — with drop tables on enemy
  stats. Run results screen on both death and victory (time, kills, level, honey banked) with
  restart flow.

### Phase 2 — Combat Depth (2026-07-04)

- **Status effects**: burn, poison, slow, freeze, stun — zero-GC fixed-slot
  `StatusEffectBuffer` per enemy with stacking, freeze-break-on-damage, and stun diminishing
  returns on elites. Visual tint cue + colored DoT numbers.
- **6 active auto-firing skills**: Stinger Barrage, Piercing Lance, Honey Splash (slow),
  Pollen Cloud (poison aura), Static Wings (chain + stun), Ember Sting (homing + burn) — each
  a data-driven `ActiveSkillSO` with a 5-level growth table, run by one `ActiveSkillManager`
  with fully pooled projectiles/zones/VFX.
- **10 passive skills**: Swift Wings, Thicker Chitin, Longer Stinger, Twin Stingers, Nectar
  Sense, Keen Eye (crit), Nectar Drain (lifesteal), Hyper Metabolism (CDR), Potent Venom,
  Deadly Precision. Crits and lifesteal roll centrally in `DamageService`.
- **Rarity & lucky picks**: Common/Rare/Epic tiers drive weighted level-up offers and card
  frame color; a small chance per card rolls "lucky" (+2 levels, distinct green card).

### Phase 1 — Look & Feel (2026-07-04)

- **Real pixel art**: player + enemies use the PixelFantasy animated bee rig (idle/run/
  attack/hit/die) via a shared Animator + SpriteLibrary skinning, rendered through a URP
  Pixel Perfect Camera (PPU 16, 320×180). Sprites flip to face movement/targets. Three enemy
  ranks by tint/scale, plus a Queen's Guard elite.
- **Game feel**: white hit-flash (custom SRP shader via MaterialPropertyBlock), knockback
  with per-rank resistance, camera shake on player damage, micro hit-stop on elite kills,
  enemy death animations + a pooled particle death-poof.
- **Hive-themed UI**: DEVNIK pixel UI kit tinted in a honey palette, all text on the
  BoldPixels TMP font (legacy UI.Text removed), skill-choice cards, HUD health/EXP bars,
  currency counter, kill counter, run timer.

### Phase 0 — Foundation (2026-07-04)

- Triaged Asset Store packs into `Assets/ThirdParty/` (kept PixelFantasy monsters, sprite
  VFX, pixel UI kit, fonts; deleted unused hero/tile packs). Added the URP 2D Pixel Perfect
  Camera pass and idempotent scene-builder tooling.

### Initial — Vertical Slice (2026-07-03)

- Project init on the URP 2D template. Core survivors gameplay: player movement (floating
  touch joystick + keyboard), auto-attack targeting nearest enemy, enemy spawning/chasing,
  health + contact damage, EXP orbs + currency pickups with magnet drift, level-up choices,
  and object pooling — all generated by editor build tooling so the scene stays regenerable.
