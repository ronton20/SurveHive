# SurveHive

> **Living document.** This README tracks the current game vision and design as it evolves. Update it whenever scope, story, or systems change — see `CLAUDE.md` for the update policy.

## Elevator Pitch

A worker bee breaks free of his hive queen's corrupted mind-control and must fight through swarms of his former colony to escape — only to discover the queen wasn't the source of the corruption at all. An alien invasion is brainwashing the world's creatures one species at a time, and our hero must grow from a fleeing survivor into the world's last line of defense.

## Story & Setting

- **The Colony**: The player is a worker bee, one of countless drones loosely bound to the hive queen's will — never questioning it, just doing the job to survive.
- **The Corruption**: The queen's mind is corrupted, twisting her from ruler to weapon. Every bee under her control turns violently aggressive, attacking anything not under her command.
- **The Break**: The player is somehow free of her control. To the corrupted hive, that makes him the nearest, most obvious enemy — his entire colony turns on him.
- **The Escape**: The opening arc is a desperate fight through the beehive itself, surviving hordes of former hivemates to reach the outside world.
- **The Reveal**: Outside the hive, it becomes clear the queen wasn't a unique case — an alien invasion is brainwashing living things across the world in an attempt at conquest. What began as a survival story becomes a fight to push back the invasion (and maybe save the world along the way).

## Gameplay Overview

SurveHive is a **Vampire Survivors-style horde survival game**: the player auto-attacks endless waves of enemies while manually focusing on movement and positioning, growing more powerful over the course of a run and, over multiple runs, permanently stronger via meta-progression.

### Worlds

The game is structured into distinct **Worlds**, each a themed run environment with its own enemy roster and narrative beat:

| World | Theme | Example enemies |
|---|---|---|
| Beehive | Escaping the corrupted colony | Worker bees, warrior bees, queen's guard, queen's royal guard (miniboss), Queen Bee (boss) |
| Garden | Fleeing into the wider world | Corrupted insects |
| Woods | Deeper into corrupted nature | Corrupted animals |
| City | Civilization has fallen too | TBD |
| Alien Ship | Confronting the source | Aliens |

Each world's enemy roster is organized into **ranks** — common trash mobs scale up to tougher variants, then minibosses, then a world-ending boss (e.g. Beehive: worker bee → warrior bee → queen's guard → queen's royal guard → Queen Bee).

### Core Loop (per run)

1. Drop into a world and survive as enemy hordes escalate over time.
2. Auto-attack targets the nearest enemy automatically — no manual aim/shoot.
3. Killing enemies grants **EXP** (leveling) and, from certain enemies, a **run currency**.
4. Leveling up offers a choice of **3 skills/abilities/power-ups**, plus a small automatic bump to base stats.
5. Survive escalating waves, reach world milestones (minibosses, boss), and eventually clear or fall in the world.

### Meta-Progression (roguelite)

Currency earned during a run carries over between runs and is spent on **permanent character upgrades**, making future runs progressively more survivable — the roguelite backbone tying individual runs into a larger campaign of growth.

### Controls

- **Mobile**: on-screen simulated joystick for movement.
- **PC**: choice of WASD or mouse-driven movement (left/right click).
- **Attacks**: fully automatic, always targeting the nearest enemy — the player's manual input is movement/positioning only.

## Development Status

**Milestone 1 (Beehive vertical slice) is implemented and playable in-editor.** Open `Assets/Scenes/Beehive.unity` and press Play.

What exists now:
- Core architecture: input abstraction (WASD, click-to-move, and simulated on-screen joystick behind one interface), zero-GC nearest-enemy auto-targeting, pooled enemies/pickups/projectiles/VFX, EXP/leveling with a 3-choice skill pick-up screen, a run currency wallet, and a `IMetaProgressionStore` seam for future permanent meta-progression (not yet implemented).
- One world, the **Beehive**, with three data-driven enemy ranks (Worker Bee, Warrior Bee, Queen's Guard — same `EnemyController` script and shared bee rig, different `EnemyStatsSO` data driving tint/scale/stats) spawned by an escalating wave spawner.
- **Real pixel art & animation** (Phase 1): player and enemies use the PixelFantasy animated bee rig (idle/run/attack/hit/die clips via a shared Animator + SpriteLibrary/SpriteResolver skinning), rendered through a URP Pixel Perfect Camera at PPU 16 with a 320×180 reference resolution. Sprites flip to face movement/targets.
- **Game feel** (Phase 1): white hit-flash on damage (custom `SurveHive/SpriteFlash` URP shader driven by MaterialPropertyBlock), projectile knockback with per-rank resistance, camera shake on player damage, micro hit-stop on elite kills (deferring to the central `GamePause`), enemy **death animations** (the rig's Death frames play on an inert corpse via `DeathAnimation` driving the SpriteResolver directly, then the pooled instance self-releases) plus a one-shot particle death-poof (the VFX pack's legacy materials were bulk-converted to URP).
- **Hive-themed UI** (Phase 1): DEVNIK pixel UI kit (auto-sliced from its unsplit sheet by scanning pixel regions) tinted in a honey palette, all text on the BoldPixels TMP font (legacy UI.Text fully removed), skill-choice cards with icons, HUD health/EXP bars, currency counter with honey-drop icon, kill counter, and a run timer.
- **Active skill arsenal** (Phase 2): 6 auto-firing weapons acquirable on level-up alongside the baseline stinger auto-attack, each with a 5-level growth table (damage/cooldown/count/area/status chance) in an `ActiveSkillSO` asset and a distinct delivery: **Stinger Barrage** (radial ring of stingers), **Piercing Lance** (high-speed line-piercing shot), **Honey Splash** (lobbed glob → slowing honey puddle), **Pollen Cloud** (poisoning aura around the player with a visible radius ring), **Static Wings** (arc chaining between enemies with a stun chance, drawn with stretched zap sprites), and **Ember Sting** (homing bolt that explodes and burns). One `ActiveSkillManager` on the player runs them all from fixed arrays; projectiles/zones/VFX are fully pooled.
- **Status effects** (Phase 2): burn (DoT, refresh on reapply), poison (DoT, stacking potency with a cap), slow (move-speed multiplier), freeze (hard stop that breaks early on a damage threshold), and stun (full stop + no contact damage, with diminishing returns on elite ranks). Pure-logic `StatusEffectBuffer` (fixed slots, zero allocations) per enemy behind a `StatusEffectReceiver` that ticks DoT with colored damage numbers and tints the sprite as a status cue.
- **10 passive skills**: Swift Wings (move speed), Thicker Chitin (max HP), Longer Stinger (range), Twin Stingers (projectile count), Nectar Sense (pickup magnet radius), Potent Venom (damage %), Keen Eye (crit chance), Nectar Drain (lifesteal), Hyper Metabolism (active-skill cooldown reduction), Deadly Precision (crit damage). Crits roll centrally in `DamageService` (gold oversized damage numbers) and lifesteal heals off every damage source.
- **Rarity & lucky picks** (Phase 2): skills carry a Common/Rare/Epic tier that drives weighted offer selection (weights 1 / 0.4 / 0.15, sampled without replacement) and the card frame color; each offered card has a small chance to roll **lucky** — a distinct green card that grants +2 levels in one pick.
- Skills have per-run **levels**: the choice card shows the growth (e.g. `Lv. 1 → Lv. 2`), and skills that hit their level cap stop being offered. Some stats are capped (e.g. projectile count maxes at 5). Each level-up also grants a small automatic bump to base stats (max health, damage, attack speed — move speed grows only via the Swift Wings power-up).
- Player stats include an **attack speed** multiplier (higher = faster firing) that scales on level-up and is open to future modifiers.
- Enemies scale up over the course of a run — health and contact damage grow per minute (tunable in the wave config), so upgrades stay meaningful.
- EXP/currency pickups drift toward the player within a pickup radius (magnet-style, standard for the genre) instead of requiring a pixel-perfect walk-over.
- Floating health bars above enemies, floating damage-number popups on hit, and a procedurally-generated placeholder shooting sound.
- Death & restart: when the player's health hits zero the run ends — the game freezes, a "YOU DIED" screen appears, and pressing R / tapping reloads the run. Level-up pacing and per-level bonuses are tunable via the `LevelCurve` asset and the level-up controller.
- Audio is still placeholder-only (one synthesized SFX blip); projectile/pickup sprites are small code-generated pixel art (stinger dart, nectar mote, honey drop) pending custom Aseprite art.
- Not yet built: other worlds (Garden/Woods/City/Alien Ship), miniboss/boss ranks and the stage timeline, item drops, run-results screen, the meta-progression currency shop and save/persistence, real audio, and mobile build/platform passes. See `PLAN.md` for the phased roadmap (Phases 0–2 done).

All gameplay scripts live under `Assets/Scripts/` (single `SurveHive.Runtime` assembly). Third-party Asset Store packs (PixelFantasy monsters, sprite VFX, pixel UI kit, fonts, temp icons) live under `Assets/ThirdParty/`. The Beehive scene, its prefabs, ScriptableObject data assets, generated sprites/audio, and the input actions asset were generated by editor tooling at `Assets/Editor/BuildTools/`, applied as an ordered chain of idempotent passes: `SurveHive/Build Beehive Vertical Slice` (full from-scratch build) → `Add Health Bars, Damage Numbers, SFX` → `Add Game Over + Death Handling` → `Apply Pixel Perfect Camera` → `Apply Phase 1 Look & Feel` (art swap, game feel, UI reskin) → `Apply Phase 2 Combat Depth` (active skills, status effects, rarity), verified by `SurveHive/Validate Beehive Vertical Slice` — kept in the project as a reusable way to regenerate or extend the scene as data changes, rather than one-off throwaway scripts. Tests live in `Assets/Tests/`: EditMode (status-effect math, rarity distribution, skill growth tables) and a PlayMode smoke test that boots the scene, equips skills, and clicks through level-ups; run both via the Test Runner or headlessly with `-runTests`.
