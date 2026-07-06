---
name: power-up
description: Author a new SurveHive power-up / level-up card end-to-end — gather the full spec (name, lane, element, appearance, status effect, per-level stats), create the data asset(s) via an idempotent builder pass, register it, and verify. Use whenever the user wants to add, design, or create a new power-up, skill, ability, passive, enhancement, or active weapon.
---

# Creating a new power-up

Power-ups are the level-up offer cards. **Never hand-edit `.asset` YAML** — author them
through an additive, idempotent editor builder pass (find-or-create), the same way
`CombatOverhaulBuilder` does. That keeps them re-runnable and never clobbers tuned data.

## Step 1 — Gather the FULL spec first (do not skip)

Ask the user for anything they haven't already given. Every power-up needs:

- **Name** — display name (e.g. "Frost Nova") and a code id (PascalCase, e.g. `FrostNova`).
- **Lane** — one of: **Passive** (buffs the player itself, cap 5), **Enhancement** (modifies the
  basic auto-attack, cap 3), **Ability** (a new auto-firing active weapon, cap 5).
- **Element** — `Physical` (neutral), `Fire`, `Poison`, `Electric`, `Frost`, or `Honey`. Drives
  the card's element cue and projectile tint. "None" → `Physical`.
- **Rarity** — `Common` / `Rare` / `Epic` (offer weight + card frame).
- **Max level** — how many times it can be taken (0 = unlimited; Passives usually 5, Abilities
  match their level table length).
- **Description** — the one-line card text.
- **Icon** — which sprite (a name to match under `Assets/`, or "placeholder").

Then, by lane:

- **Passive / Enhancement** — the **stat effect** and its per-level **magnitude** (e.g. +10%
  move speed per level; +1 pierce; 25% ignite chance). Must map to a `SkillEffectType`.
- **Ability** — the **appearance & behavior**: firing pattern (ring / homing / lobbed zone /
  nova blast / bouncing orb / aura / chain, …), projectile look/tint, speed, range, and a
  **per-level stats table** (damage, cooldown, count, area). Plus the **status effect** it
  applies if any: which effect (`Burn`/`Poison`/`Slow`/`Freeze`/`Stun`/`Cold`), chance %,
  potency, duration per level.

If the user's idea doesn't fit an existing behavior/effect/element, flag it — see
"Extending the system" — before building.

## Step 2 — Know the data model

- **`SkillDefinitionSO`** (the card, in `Assets/Data/Skills/`): id, displayName, description,
  `_effectType` (`SkillEffectType`), `_lane` (`PowerUpLane`), `_element` (`SkillElement`),
  `_magnitude`, `_maxLevel` (0=∞), `_rarity`, `_icon`, and `_activeSkill` (Ability lane only).
- **`ActiveSkillSO`** (Ability lane only, in `Assets/Data/Skills/Actives/`): `_behavior`
  (`ActiveSkillBehavior`), a `_levels[]` growth table (Damage/Cooldown/Count/Area/StatusChance/
  StatusDuration), delivery (`_projectileTint`/`_projectileSpeed`/`_range`/pool ids), zone
  params, and the status block (`_appliesStatus`/`_statusType`/`_statusPotency`/`_statusDuration`).
- **`SkillDatabaseSO`** (`Assets/Data/Skills/SkillDatabase.asset`): the `_skills[]` offer pool —
  a new card is invisible until registered here.
- Passive/Enhancement effects run through `Progression/SkillEffectApplier.Apply` — the
  `SkillEffectType` you pick must have a case there.

> **Enum caution:** `PowerUpLane`, `SkillElement`, `StatusEffectType`, `SkillEffectType`, and
> `ActiveSkillBehavior` are serialized by **integer index**. Only ever **append** new values —
> never insert or reorder, or you silently re-map every existing asset.

## Step 3 — Author via a builder pass

Copy `reference/NewPowerUpBuilder.cs.template` to
`Assets/Editor/BuildTools/<Name>Builder.cs`, rename the class, delete the lane block you don't
need, and fill in the spec. It uses the same `EnsureSkill` / `EnsureActiveSkill` /
`RegisterInDatabase` find-or-create helpers as `CombatOverhaulBuilder`, so it's safe to re-run.
(For a one-off you may instead add a method to `CombatOverhaulBuilder` alongside the existing
`AddAbilities`/`AddEnhancements`/`AddPassives`, but a self-contained builder is cleaner.)

Run it (background it — Unity batch is 1–3 min):
```
.claude/skills/verify/scripts/unity.sh build <Name>Builder
```

## Step 4 — Extending the system (only if the spec needs it)

- **New stat (Passive/Enhancement):** append a `SkillEffectType` value, add the
  `PlayerStats.IncreaseX(...)` method, and a `case` in `SkillEffectApplier.Apply`.
- **New firing pattern (Ability):** append an `ActiveSkillBehavior` value and implement it in
  `Combat/Skills/ActiveSkillManager` (spawn/fire logic), plus a fire-SFX case; if it needs a new
  projectile/zone prefab, register a pool id. Reuse an existing behavior whenever it fits.
- **New status effect:** append a `StatusEffectType` (its own buffer slot) and handle it in
  `StatusEffectReceiver`/`StatusEffectBuffer`.
- All new C# obeys `CLAUDE.md` (zero-GC, `[SerializeField] private`, `_` fields, explicit Unity
  `== null`, etc.).

## Step 5 — Verify & finish (invoke the `verify` skill)

1. `unity.sh validate` → `validation PASSED`. Add a validator assert if the card is worth
   guarding (see how `BeehiveSceneValidator` checks skill icons).
2. `unity.sh test EditMode` (offer-pool / lane-cap logic) and `PlayMode` for anything gameplay.
3. `unity.sh drive` to eyeball the card + the ability in action (screenshots → `VerifyShots/`).
4. **Docs (CLAUDE.md policy):** add the power-up to `README.md`'s design doc and note it in
   `CHANGELOG.md`, same session.
5. Stop — don't commit until the user OKs, then use the `ship` skill.
