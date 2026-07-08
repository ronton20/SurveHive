# SurveHive — TODO / Ideas

Backlog of planned features and ideas — a running wishlist to pull from. Items struck
through with a *(done…)* note are already implemented (Phases 0–5A); the rest are still
open. Roughly grouped; order within a group is not priority.
See `README.md` for what already exists, `CHANGELOG.md` for the release history, and
`CLAUDE.md` for coding standards.

---

> **Active execution plan:** `PLAN.md` is the phased, do-one-thing-at-a-time breakdown of
> everything below, in priority order. Start there. This file remains the backlog/wishlist
> and the rationale; `PLAN.md` is the sequencing. When a phase lands, tick it here too.

## Combat System Overhaul 2.0 — the three power-up lanes (top priority; PLAN Phase 1)

The level-up offer is being restructured from one flat skill pool into **three distinct
lanes**, each with its own selection cap and its own card banner/label so the player reads at
a glance what kind of pick it is. This supersedes the old TODO #18 "Passive / Active / Magic"
category split — the lanes below *are* the categories; **element** (fire / poison / electric /
frost / honey / physical) stays as an orthogonal tag on top for #19 set effects.

- **PASSIVES** — enhancements to the *player itself* (never the attacks). Move speed, max HP,
  **armor (new — flat/% damage-taken reduction)**, attack power, attack speed, **ability
  cooldown**, **ability damage/power (new — scales Abilities)**, crit chance, crit damage,
  lifesteal, pickup magnet. **Cap: 5 distinct passives**; once 5 are owned, no *new* passive
  is offered (owned ones keep leveling). Existing basic-attack-shaping passives (projectile
  count, attack range) migrate *out* of this lane into Enhancements.
- **ENHANCEMENTS (new lane)** — modifiers to the *basic auto-attack*, scaling with attack
  power + level. **Cap: 3 distinct.** Projectile +1 (each projectile hits softer, but ~1.5×
  total damage), lifesteal-on-attack, **ignite (new — attacks apply burn)**, other elemental
  procs, **piercing shot (moved off the active list — attacks deal less damage but pierce)**,
  attack range, and any other basic-attack modifier. Needs a data-driven modifier layer over
  `AutoAttack`/`Projectile`.
- **ABILITIES** — the "active" auto-firing skills, separate from the basic attack, scaling
  with level + ability power. **Cap: 5 distinct.** Honey bomb, chain lightning, the 360°
  stinger burst (**make it pierce too**), exploding stinger, poison cloud, etc. — add more per
  element as elements land. (These are today's `ActiveSkillSO` weapons.)

Every offer card gets a **category banner/label** (Passive / Enhancement / Ability) plus the
element cue from #18. Placeholder status/element icons now live in
`Assets/ThirdParty/FantasyStatusIcons/` (slice + wire until custom art replaces them).

**Power-up readability (follow-on UX — help players strategize around the lane caps):**
- **Lane counter on the offer card** — under each card's lane banner, show a `owned/cap`
  counter (e.g. `1/5` passives, `2/3` enhancements, `4/5` abilities) so the player can see at a
  glance how much room a lane still has before committing a pick. The controller already tracks
  owned-per-lane counts for gating (`LaneEligibility`); surface them on the banner.
- **Owned power-ups list in the pause menu** — a panel listing every power-up the player
  currently owns this run, grouped by lane, each with its current level (and ideally its
  element cue), so mid-run they can review their build. Reads the same per-run level state the
  level-up controller holds; the pause menu already exists (Phase 4C).

## Boss & Wave Drama (PLAN Phase 2)

- **Pre-spawn warnings** — 5 seconds before a strong wave or a boss/miniboss spawns, show a
  warning banner counting in until it arrives (the timeline currently fires spawns instantly).
- **Impactful miniboss kill** — a miniboss death grants a **guaranteed lucky power-up (+2
  levels, still random)** plus a burst of EXP, so it feels like a real reward beat.
- **Boss/miniboss death sequence** — on any boss/miniboss death, drop into **slow-motion** for
  the death animation, make the **player invulnerable** for its duration, add a **shockwave +
  screen shake**, and hold all downstream events (rewards, victory, timeline resume) until the
  animation finishes.

## Suggested implementation order (open items)

A recommended sequence for the *remaining* work, ordered by dependency and payoff. Item
numbers refer to the backlog entries below. Nothing here is binding — it's the path that
unlocks the most with the least rework. **The Combat 2.0 overhaul and Boss/Wave Drama above
now lead this order (PLAN Phases 1–2); the chain below follows as PLAN Phases 3+.**

**A. Combat Depth 2.0 — the biggest richness-per-effort win.** Do these *in order*; each
one is a prerequisite for the next, and doing them out of order means retrofitting:
1. **#18 Enhancement categories & elements** — the taxonomy (category + element enums on
   the skill SOs) + card badges. Foundation for everything below; also improves build
   readability on its own, so it pays off even if you stop here.
2. **#20 Damage typing (physical vs magic)** — every damage application carries a type.
   Small, mechanical, and unlocks defenses paying off.
3. **#23 Enemy defenses beyond HP** (armor / physical shield / magic shield) — makes damage
   typing *matter* and gives raw-damage builds a reason to diversify.
4. **#19 Elemental set effects** (2/3/4+ same-element bonuses) — the reward payoff for
   committing to an element; depends on #18's tagging.

**B. Enemy variety (#22)** — independent of chain A and the cheapest big *feel* win, since
ranged / bomber / swarm all reuse existing pools + components. Slot it in whenever; it
interleaves well with chain A (e.g. a magic-shielded ranged bee once #20/#23 exist).

**C. Final-polish leftovers** (the tail of the original build push — Phases 0–5A are done):
5. **Difficulty / curve tuning pass** — wants chains A/B in place *and* real playtest
   feedback, so it comes after the systems it balances exist. Target: a first-time player
   dies around **minute 8–12**; a meta-invested player can **clear** the Queen.
6. **Mobile UI overhaul (#14–17) + mobile sanity pass** — the gap for "mobile-ready";
   safe-area anchoring is the most urgent (the notch currently hides HUD meters).
7. **Localization seam** — cheap now, only gets more expensive as UI/string count grows;
   worth doing *before* the mobile UI pass touches all that text anyway.

**D. Content expansion** — additional worlds (Garden / Woods / City / Alien Ship), custom
Aseprite hero/boss art, a real hive tileset/floor. The biggest lift; best once the systems
above are locked so new content drops into a stable framework rather than a moving one.

> Rule of thumb: **A + B deepen the game that exists; C hardens it for release; D grows it.**
> If you only want one more push, do **A** — it's what turns "more numbers" into "more decisions."

---

## Skills & Progression
1. ~~**Skill rarity system** — some skills show up more often than others (weighted by rarity tier, not just the current flat `_weight`).~~ *(done in Phase 2: Common/Rare/Epic tiers drive weighted offers + card frame colors.)*
2. ~~**Skill double-level chance** — small chance a skill level-up grants +2 levels instead of +1; change the skill card background to indicate the lucky roll.~~ *(done in Phase 2: lucky picks with a green card.)*
3. ~~**More skills** — pickup/magnet range, crit chance, lifesteal, projectile pierce, projectile speed, cooldown reduction, etc.~~ *(done in Phase 2: 10 passives incl. magnet, crit chance/damage, lifesteal, CDR; pierce lives on the Piercing Lance active.)*
18. **Power-up categories & elements** — classify every level-up offer into one of the three **lanes** (Passive / Enhancement / Ability — see "Combat System Overhaul 2.0" above) and tag it with an **element** (fire/poison/electric/frost/honey/physical…). Surface both on the choice card: a lane banner/badge and an element-colored frame or gem, so the player reads at a glance what kind of pick it is and what element it feeds. Groundwork for #19 (set effects). Needs a small taxonomy on `SkillDefinitionSO`/`ActiveSkillSO` (lane/category enum + element enum) and card-UI slots for the badges. *(Delivered as PLAN Phase 1A/1B.)*

## Combat Depth
4. ~~**Status effects** — burn, poison, slow, stun, freeze (damage-over-time + stat modifiers with durations).~~ *(done in Phase 2: all five, zero-GC fixed-slot buffers with stacking/freeze-break/stun-DR rules.)*
5. ~~**Elemental abilities** — attacks/skills that apply status effects with a chance (e.g. fire → burn, ice → slow/freeze).~~ *(done in Phase 2: 6 active skills, four of which proc statuses — honey→slow, pollen→poison, static→stun, ember→burn. A frost "Chilling Nectar" freeze applier is designed as skill #7 if the roster needs it.)*
19. ~~**Elemental set effects** — reward committing to an element: owning **2 / 3 / 4+** enhancements of the same element grants escalating set bonuses (e.g. 2× fire → +burn duration, 4× fire → burn also spreads). Depends on the element tagging from #18. Design the thresholds + per-element bonuses as data (a `SetBonusSO` per element with tiered effects), track owned counts per element on the player, and show active set tiers somewhere on the HUD.~~ *(done as PLAN Phase 3C: 6 `SetBonusSO` assets (2/3/4-piece tiers) amplify the element's status potency/duration — physical sharpens the basic attack; `ElementSets` service + HUD tier line.)*
20. ~~**Damage typing (physical vs magic)** — split damage into **physical** and **magic** so defenses (#23) and elements can interact with it. Auto-attack/stingers = physical; magic skills (honey/pollen/ember/static) = magic. Every damage application carries a type; `DamageService` and status DoTs stamp it. Prerequisite for #18 elements paying off defensively and for #23.~~ *(done as PLAN Phase 3A: `DamageType` carried by every `TakeDamage`/`DealDamage`; abilities stamp per-asset type matching their card element, DoTs are magic, contact/enemy projectiles physical.)*

## Waves & Enemies
6. ~~**Escalating spawns** — increase enemy spawn rate/count as the run progresses (distinct from the current per-minute stat scaling).~~ *(done in Phase 3: stage spawn-rate curve 1×→3.5×.)*
7. ~~**Strong / Dangerous waves** — periodic waves with significantly higher enemy counts and possibly special spawn patterns/formations.~~ *(done in Phase 3: surround ring at 25%, directional flood at 75%.)*
8. ~~**Bosses** — mini-bosses and a per-world final boss (e.g. Beehive: Queen's Royal Guard minibosses → Queen Bee).~~ *(done in Phase 3: Royal Guard charge miniboss + 3-pattern Queen Bee; killing her wins the run.)*
22. ~~**More enemy archetypes** — beyond the current chase-and-touch bees, add behavior variety: **ranged** (keeps distance, fires projectiles — reuse the `EnemyProjectile` pool), **suicide bombers** (rush the player and explode in an AoE on contact/death), and **swarms** (large packs of weak, fast, low-HP enemies that pressure by numbers). Each is a new behavior component + `EnemyStatsSO` rank, slotted into the wave/stage spawn tables.~~ *(done as PLAN Phase 4: **Spitter Bee** 4A — magic-shielded, `RangedAttack` kites to a firing band and fires telegraphed pooled stingers, 90s; **Bomber Bee** 4B — `BomberAttack` rushes, fuses, AoE blast on fuse or death, 150s; **Swarmling** 4C — `SwarmMovement` wobble + packSize spawns in the wave table, packs of 6 at 60s.)*
23. ~~**Enemy defenses beyond HP** — give enemies defensive layers so raw damage isn't the only answer (and damage typing #20 matters): **armor** (flat or % reduction to physical damage), **physical shield** (absorbs a pool of physical damage, magic bypasses it), **magic shield** (absorbs magic damage, physical bypasses it), and combinations for elites/bosses. Model as an ordered damage-mitigation pipeline on the enemy (shield → armor → HP) reading the incoming damage type; show shield/armor state via a tint or overlay bar. Depends on #20.~~ *(done as PLAN Phase 3B: `EnemyDefense` pipeline on every enemy via the typed absorber/mitigator seams; QueensGuard magic shield + armor, Royal Guard physical shield + armor, Queen both + armor; health-bar fill tints by active shield type.)*

## Run Structure
9. ~~**Stage progress bar** — a per-stage timeline: Strong Waves at 25% and 75%, mini-boss at 50%, final boss at 100%.~~ *(done in Phase 3: HUD bar with siren/skull/crown markers.)*
10. ~~**Item drops** — potion / honey jar (heal), magnet (pull all pickups), shield, screen-nuke, etc.~~ *(done in Phase 3: Honey Jar / Magnet / Wax Shield / Royal Bomb, drop tables on enemy stats.)*

## Mobile UI Overhaul
*(from Device Simulator testing 2026-07-04 — PC-first for now, tackle alongside the Phase 5 mobile sanity pass)*
14. **Safe area support** — the notch/punch-hole hides HUD meters (health/EXP bars, timer); all HUD anchors must respect `Screen.safeArea`.
15. **UI scale pass for small screens** — HUD bars, counters, damage numbers, and card text are all too small at phone DPI; likely a `CanvasScaler` reference-resolution/match rework plus per-element size bumps.
16. **Skill cards relayout for portrait/mobile** — cards should be oriented horizontally (icon left, name/description right) and stacked vertically instead of the current three tall side-by-side columns.
17. **General mobile layout audit** — boss bar/banner, stage progress markers, results screens, and shield/aura indicators checked on tall aspect ratios (19.5:9+) in both orientations.
24. **Mobile sanity pass** — verify joystick + tap flows in the Device Simulator on an **iPhone + a tall Android** profile, confirm HUD respects safe areas, texture memory is sane, and run a profiling pass at target-ish resolution (zero-GC + frame-rate regression check after all additions). *(Companion to #14–17: those fix layout, this verifies it on-device.)*

## Meta & Content
11. ~~**Menus** — main menu, level/world selection, difficulty selection.~~ *(done in Phase 4B: MainMenu boot scene with home/world select/shop/settings-shell panels; Garden+Woods shown locked; difficulty dropdown seam fixed to Normal until a difficulty system exists.)*
12. ~~**Meta progression** — the persistent between-run currency spend + upgrade system and its UI (the `IMetaProgressionStore` seam already exists for this).~~ *(done in Phases 4A+4B: JSON save, six escalating-cost stat upgrades applied at run start, purchase transactions, and the Hive Upgrades shop UI.)*
13. **Art & polish** — ~~real sprites, animations, VFX, and proper UI elements to replace placeholders~~ *(done in Phase 1: PixelFantasy bee rig + animations, death VFX, pixel UI kit + BoldPixels TMP. Remaining: hive floor/tileset, custom Aseprite hero/boss art.)*

---

## Release & polish wishlist *(added 2026-07-07 after the Phase 3 playtest)*
25. **Complete UI overhaul** — fit to PC, enlarge text, smoother animations, click sounds, health bars, etc.
26. **Status effect visual modifiers** — tint enemies in color based on their active status effect. *(A basic priority tint exists in `StatusEffectReceiver`; this wants a proper, readable pass.)*
27. **Enhance status-effect set bonuses** — richer per-element set effects beyond the current potency/duration scaling (e.g. spread-on-death burns, shatter on frozen kills).
28. ~~**Enhance meta shop** — way more modifiers to buy: EXP gain, ability power, cooldown reduction, crit rate (+2%/rank, capped 40%), crit damage, **power-up rerolls** (max 3 per run; each reroll replaces 1 offered card, not all 3; steeply escalating cost — the feature is strong, gate it), item drop rate, and any other bonuses that fit.~~ *(done in Phase 1C, 2026-07-08: all seven shipped — 13-upgrade scrollable shop, per-card in-run rerolls at 400/3.8× growth; cross-shop cost rebalance deferred to the 1A round-2 playtest pass.)*
29. ~~**Asset generation list (ElevenLabs)** — a document listing every asset that needs generating, including sizes, pixel density, colors, usage context — ideally an accurate generation prompt per asset.~~ *(done: `ASSET_GENERATION.md` — surveyed every placeholder/missing asset across visuals and audio, with per-item spec + generation prompt; kept living per the `CLAUDE.md` doc policy.)*
30. ~~**Working stage difficulty** — easy / normal / hard / extreme: scales enemy HP and damage but increases honey gain; with per-difficulty icons (ties into #29).~~ *(done in Phase 1B of the current plan, 2026-07-08: data-driven `DifficultySO` tier table, live world-select dropdown with placeholder icons, save-persisted selection; final icons still tracked in `ASSET_GENERATION.md` §2.7.)*
31. **Premium currency — "Royal Jelly"** — earnable in very small amounts through gameplay; spent on cosmetics/revives. Named/iconed in `ASSET_GENERATION.md` §2.8 (a royal comb cell of jelly, distinct from the common Honey drop).
32. **Character customization** — colors (5 basic), hats, stinger skins, etc.
33. **Achievements system** — including Steam achievements; unlocks cosmetics and rewards premium currency.
34. **Rotating cosmetics shop** — daily rotating cosmetics for purchase.
35. **Codex** — all info on power-ups / set effects / enemies / items / etc. that the player has encountered or picked up.
36. **Enhanced options** — toggles for enemy HP bars, damage numbers, and similar feedback layers.

## Suggestions / additions to consider
- ~~**Pause & settings menu** — in-run pause (audio/vibration/quality toggles); useful early for testing and expected on mobile.~~ *(done in Phase 4C: ESC/HUD-button pause with resume/settings/abandon; settings shared with the main menu, applied live and saved.)*
- ~~**Run stats / results screen** — on death or stage clear, show time survived, kills, level, currency earned (feeds naturally into meta progression).~~ *(done in Phase 3: results block on both death and victory screens; currency banks on both paths.)*
- ~~**Save/load** — persist meta-progression and settings (goes hand-in-hand with #12; decide on a serialization approach, e.g. JSON in `Application.persistentDataPath`).~~ *(done in Phase 4A: versioned JSON at `persistentDataPath`, safe-write, corrupt→fresh-start.)*
- ~~**Object-pool coverage for new spawners** — keep bosses / strong-wave hordes / drops pooled to hold the zero-GC guarantee as counts grow.~~ *(done across Phases 2–3: 24 pool IDs cover both bosses, enemy projectiles, all four item drops, and every skill projectile/zone/VFX — all spawned via `PoolManager`.)*
- **Difficulty curve tuning pass** — once waves, bosses, and meta upgrades exist, do a dedicated balance pass on exp curve, enemy scaling, spawn curve, drop rates, and boss HP. Target: a first-time player dies around **minute 8–12**; a meta-invested player can clear. Document what changed and why.
- ~~**Audio pass** — SFX for hits/level-up/death/pickups and background music per world.~~ *(done in Phase 5A: pooled `AudioService` + CC0 SFX/music for every listed event, credits in `Assets/Audio/CREDITS.md`.)*
- ~~**Damage feedback** — hit flash / knockback / screen shake to make combat feel impactful (cheap wins alongside status effects).~~ *(done in Phase 1: hit flash, knockback, screen shake, hit-stop on elite kills, death VFX.)*
- **Localization seam** — if wider release is a goal, isolate user-facing strings early rather than retrofitting later: all user-facing strings flow through one string table/asset instead of hardcoded literals (actual translation deferred).
- **Bloom / "magic honey" glow pass** — the URP Bloom post-process is set up (Global Volume) but not yet dialled in; tune a high threshold so only deliberately-bright VFX pixels bloom without smearing the pixel art. Optional follow-on: 2D lights for hive-interior mood.
