# SurveHive — Developer Handbook / Project Map

> A navigational map of the codebase: every system, the scripts that implement it, and the
> assets/data it reads. Companion to the design docs — this one is about **where things live and
> how they wire together**, not what the game feels like.
>
> - `README.md` — game vision, story, and per-feature design (the *what* and *why*).
> - `PLAN.md` — phased execution plan for the open backlog.
> - `TODO.md` — backlog/wishlist and rationale.
> - `CHANGELOG.md` — release history.
> - `ASSET_GENERATION.md` — art/audio placeholder backlog.
> - `CLAUDE.md` — coding standards (mobile zero-GC, encapsulation, SOLID).
>
> **Note:** `CLAUDE.md`'s "Project Overview" still calls this a greenfield template with "no
> gameplay scripts yet" — that is stale. The game is a fully playable Beehive vertical slice.

---

## 1. Opening & running

- **Unity 6000.5.2f1** (must match `ProjectSettings/ProjectVersion.txt`), URP 2D.
- **Boot scene:** `Assets/Scenes/MainMenu.unity` → Play for the full menu → run loop.
- **Jump into a run:** `Assets/Scenes/Beehive.unity`.
- `Assets/Scenes/SampleScene.unity` is the leftover URP template — unused, safe to ignore.
- **Tests:** `Window > General > Test Runner`, or headless via `unity.sh test <EditMode|PlayMode> [filter]`.

---

## 2. Repository layout

| Path | Purpose |
|---|---|
| `Assets/Scripts/` | All runtime gameplay code — single `SurveHive.Runtime` assembly, organized by domain folder (see §4). |
| `Assets/Editor/` | Editor-only code: property drawers + the `Assets/Editor/BuildTools/` scene-generation pipeline (§6). |
| `Assets/Scenes/` | `MainMenu` (boot), `Beehive` (the run), `SampleScene` (unused template). |
| `Assets/Prefabs/` | Pooled gameplay prefabs: `Enemies/`, `Projectiles/`, `Skills/`, `VFX/`, `Pickups/`, `Drops/`, `UI/DamageNumber`. |
| `Assets/Data/` | ScriptableObject assets — the data-driven backbone (§5). |
| `Assets/Sprites/`, `Assets/Audio/`, `Assets/Settings/` | Generated/authored art, synthesized SFX + music, URP renderer/volume/input assets. |
| `Assets/Tests/` | `EditMode/` (pure logic) + `PlayMode/` (scene smoke/boss/balance) — separate asmdefs (§8). |
| `Assets/ThirdParty/` | Asset-store packs only (PixelFantasy monsters, SpriteEffects VFX, PixelUI kit, BoldPixels font, temp icons). Our content references these, never edits them. |
| `Tools/Audio/synth.py` | Procedural SFX generator (Phase 5A). |
| `graphify-out/` | Generated knowledge-graph snapshot (`GRAPH_REPORT.md`, `graph.html`). Regenerate with `/graphify`. |

**Assemblies:** `SurveHive.Runtime` (Assets/Scripts) · `SurveHive.Editor` + `SurveHive.BuildTools`
(Assets/Editor) · `SurveHive.Tests.EditMode` / `SurveHive.Tests.PlayMode` · `PixelFantasy` (vendored).

---

## 3. Architecture at a glance

The game is **data-driven** (behavior parameterized by ScriptableObjects), **pooled** (no runtime
`Instantiate` except the pool itself), and wired through **interface seams** for testability (DIP).

**Scene singletons / service locators** — a small set of statically-reachable services registered
at scene start rather than a DI container:

| Access point | Registered by | Holds |
|---|---|---|
| `PlayerContext` (static) | `PlayerBootstrap` | live `PlayerStats`, `HealthComponent`, `Transform` |
| `PoolManager` (component singleton) | `GameBootstrap` | all object pools, keyed by `PoolIds` |
| `EnemyRegistry` | scene | live enemy list for nearest-target queries |

**Bootstrapping:** `GameBootstrap` (`Core/GameBootstrap.cs`) prewarms pools from a serialized
`PoolPrewarmEntry[]` (poolId → prefab → counts). `PlayerBootstrap` (`Player/PlayerBootstrap.cs`)
registers the player into `PlayerContext`. Anything dealing damage on the player's behalf
(projectiles, skills, pickups) reads `PlayerContext` instead of scanning the scene.

**Interface seams (the abstractions to depend on, not the concretes):**

| Interface | File | Implementations |
|---|---|---|
| `IMovementInputSource` | `Input/` | keyboard / on-screen joystick |
| `IAudioService` | `Core/IAudioService.cs` | `AudioService` (+ `HeadlessAudioMute`) |
| `IMetaProgressionStore` | `Core/IMetaProgressionStore.cs` | persistent + runtime store SOs |
| `IDamageable` / `IDamageAbsorber` / `IDamageMitigator` | `Health/` | health, shields, armor |

---

## 4. Systems map (folder → what it does → key scripts)

### Input — `Assets/Scripts/Input/`
Movement abstracted behind `IMovementInputSource`; `PlayerInputController` picks the source by
`InputSourceMode`. Mobile = `OnScreenJoystickInputSource` (+`OnScreenJoystickUI`), PC =
`KeyboardMoveInputSource`. Attacks are automatic — input is movement only.

### Player — `Assets/Scripts/Player/`
`PlayerBootstrap` (registers `PlayerContext`) · `PlayerMovement` (has an `Initialize` seam the
balance bot injects into) · `PlayerStats` (all live stat multipliers) · `PlayerShield` /
`ArmorMitigator` (defensive `IDamageAbsorber`/`IDamageMitigator`) · `PlayerDeathHandler` ·
`PlayerHitFeedback` · `CameraFollow` · `MetaUpgradeApplier` (applies purchased meta upgrades at run start).

### Health & damage typing — `Assets/Scripts/Health/`
`HealthComponent` is the `IDamageable`; damage flows shield → armor → HP via ordered
`IDamageAbsorber`/`IDamageMitigator` layers. `DamageType` (physical/magic), `DamageOnContact`.

### Combat — `Assets/Scripts/Combat/`
`AutoAttack` fires at the `NearestEnemyTargeter` (zero-GC). `DamageService` is the central damage
choke point — applies type, rolls **crit** (gold numbers), lifesteal, and status. `Projectile`,
`BasicAttackPayload`, `CombatMath`, `DamagePopupSpawner`.
- **Active skills** — `Combat/Skills/`: `ActiveSkillManager` runs all 8 auto-firing weapons from
  fixed arrays; delivery types (`SkillProjectile`, `BouncingOrbProjectile`, `ExpandingWave`,
  `AreaEffectZone`, `ZapArcVfx`). Set signatures in `ElementalSetSignatures`.
- **Status effects** — `Combat/Status/`: `StatusEffectBuffer` (fixed-slot, zero-alloc) behind
  `StatusEffectReceiver`; `StatusEffectType`, `StatusTintPalette` (signature tints via the flash shader).

### Enemies — `Assets/Scripts/Enemies/`
One shared `EnemyController` + shared bee rig; archetype behavior is a small layered component:
`SwarmMovement` (swarmling), `RangedAttack`+`RangedSteering` (spitter), `ChargeAttack` (miniboss),
`BomberAttack` (bomber). `EnemyDefense` (shield/armor pipeline), `EnemyLoot`, `EnemyProjectile`,
`ExpRewardCalculator`, `QueenBossController`. Prefabs in `Assets/Prefabs/Enemies/`, data in
`Assets/Data/…/EnemyStatsSO`.

### Spawning & pooling — `Assets/Scripts/Spawning/`, `Assets/Scripts/Core/`
`EnemySpawner` + `GameObjectPool` / `PoolManager` / `PoolIds` (the *only* runtime `Instantiate`).
`GameBootstrap` prewarms pools. `PooledVfx` (`View/`) self-releases.

### Stage / run structure — `Assets/Scripts/Stage/`
`StageDirector` drives a 10-minute `StageConfigSO` timeline (`StageTimeline`): spawn-rate ramp,
strong waves, `BossSpawner` for the Royal Guard miniboss (`ChargeAttack`) at 50% and the Queen at
100%. `BossDeathSequence` + `HitStop` + `GamePause` handle the cinematic beats.

### Progression — `Assets/Scripts/Progression/`
`PlayerExperience` (leveling, curve from `LevelCurveSO`) → level-up offers via `SkillOfferSelector`
(rarity-weighted, `RerollLogic`) constrained by `PowerUpLane` + `LaneEligibility` (lane caps).
`SkillEffectApplier` applies picks to `PlayerStats`; `ElementSets` counts pieces and amplifies
statuses (`ElementPalette`, `SkillElement`, `SetBonusSO`). `MetaShop`/`MetaUpgradeMath`,
`DifficultyUnlocks`, `SkillStatPreview` (readable "10 → 11" card numbers).

### Pickups & currency — `Assets/Scripts/Pickups/`, `Assets/Scripts/Currency/`
`PickupItem`/`PickupMotion` (one shared magnet system) · `ItemDrop` (Honey Jar / Magnet / Wax
Shield / Royal Bomb) · `ExpOrbTiers` (merge + restyle by value) · `RunCurrencyWallet`.

### Meta-progression & save — `Assets/Scripts/Persistence/`, `Assets/Scripts/Data/`
Versioned JSON save (`SaveData`, `SaveDataSerializer`, `SaveFileStore`, temp-file swap) at
`Application.persistentDataPath`. `MetaProgressionState` + the store SOs
(`PersistentMetaProgressionStoreSO` / `RuntimeMetaProgressionStoreSO`) sit behind
`IMetaProgressionStore`. Upgrades are `MetaUpgradeSO` assets applied by `MetaUpgradeApplier`.

### UI & HUD — `Assets/Scripts/UI/`
HUD: `HealthBarUI`, `ExpBarUI`, `CurrencyCounterUI`, `KillCounterUI`, `RunTimerUI`,
`StageProgressBarUI`, `SetTierHUD`, enemy/boss bars, `WaveWarningBanner`, `BossBannerUI`.
Screens/flow: `MainMenuController`, `DifficultySelectUI`, `MetaShopUI`+`MetaShopCardUI`,
`LevelUpUIController`, `RunResultsUI`, `PauseMenuController`, `SettingsPanelUI`, `OwnedPowerUpsView`.
**Almost all UI is generated directly into the scenes by the editor builders — see §6 and §7.**

### Localization — `Assets/Scripts/Core/Loc.cs` (+ `LocKeys`, `LocDefaults`, `Data/StringTableSO`)
UI-chrome strings resolve through `Loc.Get(LocKeys.X)`: authored `StringTable` Resources asset →
code `LocDefaults` → raw key. Keys are `const`s in `LocKeys`; English lives in `LocDefaults` and
is authored into `Assets/Resources/StringTable.asset` by the idempotent `LocalizationBuilder`
pass. **SO-authored content (skill/upgrade/set names + descriptions, enemy display names) stays
on its SO** — the table is chrome only. Add a key → add its `LocDefaults` default (a test enforces
the pairing) → re-run the builder. Translation deferred; the asset is the future locale surface.

### Audio — `Assets/Scripts/Core/AudioService.cs`
Scene-scoped `AudioService` behind `IAudioService`: round-robin SFX pool + one music loop, driven
by settings sliders. `SfxId`/`MusicId` map 1:1 to clips in `AudioLibrarySO`. SFX synthesized by
`Tools/Audio/synth.py`; sources credited in `Assets/Audio/CREDITS.md`.

### View / game-feel — `Assets/Scripts/View/`
`HitFlash` (custom `SurveHive/SpriteFlash` URP shader via MaterialPropertyBlock), `CameraShaker`,
`CharacterAnimator`, `DeathAnimation` (plays the rig's death frames on an inert corpse), `PooledVfx`.

---

## 5. Data-driven assets (`Assets/Scripts/Data/` SO definitions → `Assets/Data/` instances)

| ScriptableObject | Drives |
|---|---|
| `EnemyStatsSO` | Per-rank enemy stats, tint/scale, shields, drop chances. |
| `WaveSpawnerConfigSO` / `StageConfigSO` | Spawn table (packs, caps, ramp) + the run timeline. |
| `LevelCurveSO` | EXP thresholds + per-level base-stat bumps. |
| `SkillDefinitionSO` / `ActiveSkillSO` / `SkillDatabaseSO` | Power-up lanes, 5-level growth tables, elements, rarity. |
| `SetBonusSO` | Per-element 2/3/4-piece set tiers + signature. |
| `MetaUpgradeSO` / `MetaProgressionStoreSO` | Permanent shop upgrades + banked state. |
| `DifficultySO` | Easy→Extreme tier scaling table. |
| `AudioLibrarySO` | `SfxId`/`MusicId` → clip mapping. |

Enums/value types alongside: `MetaStatType`, `SetSignatureType`, `DifficultyTier`, `SfxId`, `MusicId`.

---

## 6. The editor builder pipeline — how scenes are generated

The Beehive/MainMenu scenes, their prefabs, data assets, generated sprites/audio, and the input
actions were **generated by code**, not hand-authored, as an ordered chain of *idempotent* passes
in `Assets/Editor/BuildTools/`:

`BeehiveSceneBuilder` (full build) → `Phase1LookAndFeelBuilder` → `Phase2CombatDepthBuilder` →
`Phase3RunStructureBuilder` → `Phase4MetaAndMenusBuilder` → `Phase5AudioBuilder`, plus additive
passes (`CombatOverhaulBuilder`, `EnemyVarietyBuilder`, `DifficultyBuilder`,
`MetaShopExpansionBuilder`, `SetSignatureBuilder`, `LocalizationBuilder`, `HeroBee*SkinBuilder`).
Validated by `BeehiveSceneValidator`; `PlayModeVerifyDriver` drives a headless play capture.

> ⚠️ **Builder caution (from `PLAN.md` and project memory):** the scenes/data have since been
> **hand-tuned**. Do **not** re-run `BeehiveSceneBuilder.Build()` or early phase builders casually —
> they clobber tuned data assets. Extend via new additive idempotent passes or targeted edits, and
> keep `BeehiveSceneValidator` green. Also: in builder passes, load assets **after**
> `OpenScene(Single)` — pre-switch instances wire as `fileID 0` silently.

**Consequence to understand:** because UI is emitted by builders straight into the scene, the two
scenes are large serialized blobs (MainMenu ≈ 671 KB, Beehive ≈ 487 KB), the builders are the de
facto source of truth for UI layout, and hand-edits in the Editor risk being lost on the next
builder run. This is the central tension behind the refactor targets in §9.

---

## 7. Runtime → screen flow

1. **MainMenu** (`MainMenuController`): Home → World Select (`DifficultySelectUI`, earned tiers) →
   Hive Upgrades shop (`MetaShopUI`) → Settings (`SettingsPanelUI`). Selection persists to save.
2. **Beehive** run: `GameBootstrap` prewarms pools; `PlayerBootstrap` registers `PlayerContext`;
   `StageDirector` starts the timeline; `EnemySpawner` drips enemies.
3. **Combat loop:** `AutoAttack` → `DamageService` → `HealthComponent`; kills drop EXP/currency
   (`ExpOrbTiers`, `RunCurrencyWallet`) and sometimes `ItemDrop`s.
4. **Level-up:** `PlayerExperience` freezes time and shows `LevelUpUIController` (3 lane-gated,
   rarity-weighted offers, rerollable) → `SkillEffectApplier`.
5. **Milestones:** strong waves, Royal Guard miniboss (50%), Queen (100%) → `BossDeathSequence`.
6. **End:** death or Queen kill → `RunResultsUI`; honey banks to `IMetaProgressionStore`; RETRY / HIVE.

---

## 8. Tests (`Assets/Tests/`)

- **EditMode** (pure logic): status-effect math, rarity distribution, skill growth tables, save
  round-trip/corruption, meta-shop transactions, lane eligibility, ranged steering, enemy defense,
  stage timeline, element sets.
- **PlayMode**: `BeehiveSmokeTest` (boot → equip → click level-ups), `BossFightTest`,
  `PauseMenuTest`, and the `[Explicit]` `BalanceRunTest` (unattended 6× runs with a kiting bot via
  `PlayerMovement.Initialize`; `unity.sh test PlayMode SurveHive.Tests.BalanceRunTest`).

---

## 9. Refactor targets (current, tracked against your review)

1. ~~**Shop cards are baked, not prefabbed or data-driven.**~~ ✅ **Done.** The shop now spawns
   cards at runtime from `Assets/Prefabs/UI/MetaShopCard.prefab` + `Assets/Data/Meta/MetaUpgradeCatalog.asset`
   (a `MetaUpgradeCatalogSO`) under a `GridLayoutGroup` (`MetaShopUI.cs`). Adding an upgrade is now
   just: create its `MetaUpgradeSO`, drop it in the catalog — no scene or builder edit. The
   migration was applied by the additive `ShopDataDrivenBuilder` (menu:
   *SurveHive → Refactor Shop To Data-Driven Grid*); `ShopVerifyDriver` captures the grid.
2. **UI is not prefabbed.** Gameplay entities are well-prefabbed (`Assets/Prefabs/Enemies|Skills|VFX|…`)
   and pooled, but UI has only `Prefabs/UI/DamageNumber.prefab` — every HUD/menu/card lives inline
   in the scenes. Extract reusable prefabs (shop row, level-up card, stat bar, menu button).
3. **Manual layout instead of layout groups.** Builders hand-compute `anchoredPosition`/`RowPitch`;
   Unity `GridLayoutGroup`/`VerticalLayoutGroup`/`ContentSizeFitter` would replace it.
4. **Stale `CLAUDE.md` overview** ("greenfield / no scripts yet") — update to reality.

See the review notes accompanying this handbook for the full best-practices audit.
