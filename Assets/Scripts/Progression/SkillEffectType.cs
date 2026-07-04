namespace SurveHive.Progression
{
    public enum SkillEffectType
    {
        MoveSpeedPercent,
        MaxHealthFlat,
        AttackRangePercent,
        AttackDamagePercent,
        AttackCooldownPercent,
        ProjectileCountFlat,

        // Appended at the end: enum values are serialized by integer index in skill
        // assets, so new entries must not be inserted before existing ones.
        AttackSpeedPercent,

        // Phase 2 passives.
        MagnetRadiusPercent,
        CritChanceFlat,
        CritDamagePercent,
        LifestealFlat,
        ActiveCooldownPercent,

        // Card that unlocks/levels the ActiveSkillSO referenced by the definition.
        ActiveSkill
    }
}
