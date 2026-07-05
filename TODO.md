# SurveHive — TODO / Ideas

Backlog of planned features and ideas. Nothing here is implemented yet — this is a
running wishlist to pull from. Roughly grouped; order within a group is not priority.
See `README.md` for what already exists and `CLAUDE.md` for coding standards.

## Skills & Progression
1. ~~**Skill rarity system** — some skills show up more often than others (weighted by rarity tier, not just the current flat `_weight`).~~ *(done in Phase 2: Common/Rare/Epic tiers drive weighted offers + card frame colors.)*
2. ~~**Skill double-level chance** — small chance a skill level-up grants +2 levels instead of +1; change the skill card background to indicate the lucky roll.~~ *(done in Phase 2: lucky picks with a green card.)*
3. ~~**More skills** — pickup/magnet range, crit chance, lifesteal, projectile pierce, projectile speed, cooldown reduction, etc.~~ *(done in Phase 2: 10 passives incl. magnet, crit chance/damage, lifesteal, CDR; pierce lives on the Piercing Lance active.)*
18. **Enhancement categories & elements** — classify every level-up offer into a **category** (Passive / Active Skill / Magic) and tag it with an **element** (fire/poison/electric/frost/honey/physical…). Surface both on the choice card: a category icon/badge and an element-colored frame or gem, so the player can read at a glance what kind of pick it is and what element it feeds. Groundwork for #19 (set effects). Needs a small taxonomy on `SkillDefinitionSO`/`ActiveSkillSO` (category enum + element enum) and card-UI slots for the badges.

## Combat Depth
4. ~~**Status effects** — burn, poison, slow, stun, freeze (damage-over-time + stat modifiers with durations).~~ *(done in Phase 2: all five, zero-GC fixed-slot buffers with stacking/freeze-break/stun-DR rules.)*
5. ~~**Elemental abilities** — attacks/skills that apply status effects with a chance (e.g. fire → burn, ice → slow/freeze).~~ *(done in Phase 2: 6 active skills, four of which proc statuses — honey→slow, pollen→poison, static→stun, ember→burn. A frost "Chilling Nectar" freeze applier is designed as skill #7 if the roster needs it.)*
19. **Elemental set effects** — reward committing to an element: owning **2 / 3 / 4+** enhancements of the same element grants escalating set bonuses (e.g. 2× fire → +burn duration, 4× fire → burn also spreads). Depends on the element tagging from #18. Design the thresholds + per-element bonuses as data (a `SetBonusSO` per element with tiered effects), track owned counts per element on the player, and show active set tiers somewhere on the HUD.
20. **Damage typing (physical vs magic)** — split damage into **physical** and **magic** so defenses (#23) and elements can interact with it. Auto-attack/stingers = physical; magic skills (honey/pollen/ember/static) = magic. Every damage application carries a type; `DamageService` and status DoTs stamp it. Prerequisite for #18 elements paying off defensively and for #23.

## Waves & Enemies
6. ~~**Escalating spawns** — increase enemy spawn rate/count as the run progresses (distinct from the current per-minute stat scaling).~~ *(done in Phase 3: stage spawn-rate curve 1×→3.5×.)*
7. ~~**Strong / Dangerous waves** — periodic waves with significantly higher enemy counts and possibly special spawn patterns/formations.~~ *(done in Phase 3: surround ring at 25%, directional flood at 75%.)*
8. ~~**Bosses** — mini-bosses and a per-world final boss (e.g. Beehive: Queen's Royal Guard minibosses → Queen Bee).~~ *(done in Phase 3: Royal Guard charge miniboss + 3-pattern Queen Bee; killing her wins the run.)*
22. **More enemy archetypes** — beyond the current chase-and-touch bees, add behavior variety: **ranged** (keeps distance, fires projectiles — reuse the `EnemyProjectile` pool), **suicide bombers** (rush the player and explode in an AoE on contact/death), and **swarms** (large packs of weak, fast, low-HP enemies that pressure by numbers). Each is a new behavior component + `EnemyStatsSO` rank, slotted into the wave/stage spawn tables.
23. **Enemy defenses beyond HP** — give enemies defensive layers so raw damage isn't the only answer (and damage typing #20 matters): **armor** (flat or % reduction to physical damage), **physical shield** (absorbs a pool of physical damage, magic bypasses it), **magic shield** (absorbs magic damage, physical bypasses it), and combinations for elites/bosses. Model as an ordered damage-mitigation pipeline on the enemy (shield → armor → HP) reading the incoming damage type; show shield/armor state via a tint or overlay bar. Depends on #20.

## Run Structure
9. ~~**Stage progress bar** — a per-stage timeline: Strong Waves at 25% and 75%, mini-boss at 50%, final boss at 100%.~~ *(done in Phase 3: HUD bar with siren/skull/crown markers.)*
10. ~~**Item drops** — potion / honey jar (heal), magnet (pull all pickups), shield, screen-nuke, etc.~~ *(done in Phase 3: Honey Jar / Magnet / Wax Shield / Royal Bomb, drop tables on enemy stats.)*

## Mobile UI Overhaul
*(from Device Simulator testing 2026-07-04 — PC-first for now, tackle alongside the Phase 5 mobile sanity pass)*
14. **Safe area support** — the notch/punch-hole hides HUD meters (health/EXP bars, timer); all HUD anchors must respect `Screen.safeArea`.
15. **UI scale pass for small screens** — HUD bars, counters, damage numbers, and card text are all too small at phone DPI; likely a `CanvasScaler` reference-resolution/match rework plus per-element size bumps.
16. **Skill cards relayout for portrait/mobile** — cards should be oriented horizontally (icon left, name/description right) and stacked vertically instead of the current three tall side-by-side columns.
17. **General mobile layout audit** — boss bar/banner, stage progress markers, results screens, and shield/aura indicators checked on tall aspect ratios (19.5:9+) in both orientations.

## Meta & Content
11. ~~**Menus** — main menu, level/world selection, difficulty selection.~~ *(done in Phase 4B: MainMenu boot scene with home/world select/shop/settings-shell panels; Garden+Woods shown locked; difficulty dropdown seam fixed to Normal until a difficulty system exists.)*
12. ~~**Meta progression** — the persistent between-run currency spend + upgrade system and its UI (the `IMetaProgressionStore` seam already exists for this).~~ *(done in Phases 4A+4B: JSON save, six escalating-cost stat upgrades applied at run start, purchase transactions, and the Hive Upgrades shop UI.)*
13. **Art & polish** — ~~real sprites, animations, VFX, and proper UI elements to replace placeholders~~ *(done in Phase 1: PixelFantasy bee rig + animations, death VFX, pixel UI kit + BoldPixels TMP. Remaining: real audio, hive floor/tileset, custom Aseprite hero/boss art.)*

---

## Suggestions / additions to consider
- ~~**Pause & settings menu** — in-run pause (audio/vibration/quality toggles); useful early for testing and expected on mobile.~~ *(done in Phase 4C: ESC/HUD-button pause with resume/settings/abandon; settings shared with the main menu, applied live and saved.)*
- ~~**Run stats / results screen** — on death or stage clear, show time survived, kills, level, currency earned (feeds naturally into meta progression).~~ *(done in Phase 3: results block on both death and victory screens; currency banks on both paths.)*
- ~~**Save/load** — persist meta-progression and settings (goes hand-in-hand with #12; decide on a serialization approach, e.g. JSON in `Application.persistentDataPath`).~~ *(done in Phase 4A: versioned JSON at `persistentDataPath`, safe-write, corrupt→fresh-start.)*
- **Object-pool coverage for new spawners** — keep bosses / strong-wave hordes / drops pooled to hold the zero-GC guarantee as counts grow.
- **Difficulty curve tuning pass** — once waves, bosses, and meta upgrades exist, do a dedicated balance pass on exp curve, enemy scaling, and drop rates.
- **Audio pass** — SFX for hits/level-up/death/pickups and background music per world (currently only a placeholder shoot blip).
- ~~**Damage feedback** — hit flash / knockback / screen shake to make combat feel impactful (cheap wins alongside status effects).~~ *(done in Phase 1: hit flash, knockback, screen shake, hit-stop on elite kills, death VFX.)*
- **Localization seam** — if wider release is a goal, isolate user-facing strings early rather than retrofitting later.
