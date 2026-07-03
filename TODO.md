# SurveHive — TODO / Ideas

Backlog of planned features and ideas. Nothing here is implemented yet — this is a
running wishlist to pull from. Roughly grouped; order within a group is not priority.
See `README.md` for what already exists and `CLAUDE.md` for coding standards.

## Skills & Progression
1. **Skill rarity system** — some skills show up more often than others (weighted by rarity tier, not just the current flat `_weight`).
2. **Skill double-level chance** — small chance a skill level-up grants +2 levels instead of +1; change the skill card background to indicate the lucky roll.
3. **More skills** — pickup/magnet range, crit chance, lifesteal, projectile pierce, projectile speed, cooldown reduction, etc.

## Combat Depth
4. **Status effects** — burn, poison, slow, stun, freeze (damage-over-time + stat modifiers with durations).
5. **Elemental abilities** — attacks/skills that apply status effects with a chance (e.g. fire → burn, ice → slow/freeze).

## Waves & Enemies
6. **Escalating spawns** — increase enemy spawn rate/count as the run progresses (distinct from the current per-minute stat scaling).
7. **Strong / Dangerous waves** — periodic waves with significantly higher enemy counts and possibly special spawn patterns/formations.
8. **Bosses** — mini-bosses and a per-world final boss (e.g. Beehive: Queen's Royal Guard minibosses → Queen Bee).

## Run Structure
9. **Stage progress bar** — a per-stage timeline: Strong Waves at 25% and 75%, mini-boss at 50%, final boss at 100%.
10. **Item drops** — potion / honey jar (heal), magnet (pull all pickups), shield, screen-nuke, etc.

## Meta & Content
11. **Menus** — main menu, level/world selection, difficulty selection.
12. **Meta progression** — the persistent between-run currency spend + upgrade system and its UI (the `IMetaProgressionStore` seam already exists for this).
13. **Art & polish** — real sprites, animations, VFX, and proper UI elements to replace placeholders.

---

## Suggestions / additions to consider
- **Pause & settings menu** — in-run pause (audio/vibration/quality toggles); useful early for testing and expected on mobile.
- **Run stats / results screen** — on death or stage clear, show time survived, kills, level, currency earned (feeds naturally into meta progression).
- **Save/load** — persist meta-progression and settings (goes hand-in-hand with #12; decide on a serialization approach, e.g. JSON in `Application.persistentDataPath`).
- **Object-pool coverage for new spawners** — keep bosses / strong-wave hordes / drops pooled to hold the zero-GC guarantee as counts grow.
- **Difficulty curve tuning pass** — once waves, bosses, and meta upgrades exist, do a dedicated balance pass on exp curve, enemy scaling, and drop rates.
- **Audio pass** — SFX for hits/level-up/death/pickups and background music per world (currently only a placeholder shoot blip).
- **Damage feedback** — hit flash / knockback / screen shake to make combat feel impactful (cheap wins alongside status effects).
- **Localization seam** — if wider release is a goal, isolate user-facing strings early rather than retrofitting later.
