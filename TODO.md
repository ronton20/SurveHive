# SurveHive — TODO / Ideas

Backlog of planned features and ideas. Nothing here is implemented yet — this is a
running wishlist to pull from. Roughly grouped; order within a group is not priority.
See `README.md` for what already exists and `CLAUDE.md` for coding standards.

## Skills & Progression
1. ~~**Skill rarity system** — some skills show up more often than others (weighted by rarity tier, not just the current flat `_weight`).~~ *(done in Phase 2: Common/Rare/Epic tiers drive weighted offers + card frame colors.)*
2. ~~**Skill double-level chance** — small chance a skill level-up grants +2 levels instead of +1; change the skill card background to indicate the lucky roll.~~ *(done in Phase 2: lucky picks with a green card.)*
3. ~~**More skills** — pickup/magnet range, crit chance, lifesteal, projectile pierce, projectile speed, cooldown reduction, etc.~~ *(done in Phase 2: 10 passives incl. magnet, crit chance/damage, lifesteal, CDR; pierce lives on the Piercing Lance active.)*

## Combat Depth
4. ~~**Status effects** — burn, poison, slow, stun, freeze (damage-over-time + stat modifiers with durations).~~ *(done in Phase 2: all five, zero-GC fixed-slot buffers with stacking/freeze-break/stun-DR rules.)*
5. ~~**Elemental abilities** — attacks/skills that apply status effects with a chance (e.g. fire → burn, ice → slow/freeze).~~ *(done in Phase 2: 6 active skills, four of which proc statuses — honey→slow, pollen→poison, static→stun, ember→burn. A frost "Chilling Nectar" freeze applier is designed as skill #7 if the roster needs it.)*

## Waves & Enemies
6. ~~**Escalating spawns** — increase enemy spawn rate/count as the run progresses (distinct from the current per-minute stat scaling).~~ *(done in Phase 3: stage spawn-rate curve 1×→3.5×.)*
7. ~~**Strong / Dangerous waves** — periodic waves with significantly higher enemy counts and possibly special spawn patterns/formations.~~ *(done in Phase 3: surround ring at 25%, directional flood at 75%.)*
8. ~~**Bosses** — mini-bosses and a per-world final boss (e.g. Beehive: Queen's Royal Guard minibosses → Queen Bee).~~ *(done in Phase 3: Royal Guard charge miniboss + 3-pattern Queen Bee; killing her wins the run.)*

## Run Structure
9. ~~**Stage progress bar** — a per-stage timeline: Strong Waves at 25% and 75%, mini-boss at 50%, final boss at 100%.~~ *(done in Phase 3: HUD bar with siren/skull/crown markers.)*
10. ~~**Item drops** — potion / honey jar (heal), magnet (pull all pickups), shield, screen-nuke, etc.~~ *(done in Phase 3: Honey Jar / Magnet / Wax Shield / Royal Bomb, drop tables on enemy stats.)*

## Meta & Content
11. **Menus** — main menu, level/world selection, difficulty selection.
12. **Meta progression** — the persistent between-run currency spend + upgrade system and its UI (the `IMetaProgressionStore` seam already exists for this).
13. **Art & polish** — ~~real sprites, animations, VFX, and proper UI elements to replace placeholders~~ *(done in Phase 1: PixelFantasy bee rig + animations, death VFX, pixel UI kit + BoldPixels TMP. Remaining: real audio, hive floor/tileset, custom Aseprite hero/boss art.)*

---

## Suggestions / additions to consider
- **Pause & settings menu** — in-run pause (audio/vibration/quality toggles); useful early for testing and expected on mobile.
- ~~**Run stats / results screen** — on death or stage clear, show time survived, kills, level, currency earned (feeds naturally into meta progression).~~ *(done in Phase 3: results block on both death and victory screens; currency banks on both paths.)*
- **Save/load** — persist meta-progression and settings (goes hand-in-hand with #12; decide on a serialization approach, e.g. JSON in `Application.persistentDataPath`).
- **Object-pool coverage for new spawners** — keep bosses / strong-wave hordes / drops pooled to hold the zero-GC guarantee as counts grow.
- **Difficulty curve tuning pass** — once waves, bosses, and meta upgrades exist, do a dedicated balance pass on exp curve, enemy scaling, and drop rates.
- **Audio pass** — SFX for hits/level-up/death/pickups and background music per world (currently only a placeholder shoot blip).
- ~~**Damage feedback** — hit flash / knockback / screen shake to make combat feel impactful (cheap wins alongside status effects).~~ *(done in Phase 1: hit flash, knockback, screen shake, hit-stop on elite kills, death VFX.)*
- **Localization seam** — if wider release is a goal, isolate user-facing strings early rather than retrofitting later.
