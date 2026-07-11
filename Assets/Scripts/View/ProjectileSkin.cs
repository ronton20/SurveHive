using UnityEngine;

namespace SurveHive.View
{
    /// <summary>
    /// Applies the equipped <see cref="StingerSkin"/> to this pooled projectile
    /// on spawn (PLAN 5C follow-up). Lives only on the player's auto-attack
    /// Stinger prefab — enemy/skill projectiles are untouched. The prefab's own
    /// sprite/color are cached in Awake and restored when no skin is equipped,
    /// so pooled instances never leak a previous run's look. Zero-GC: two field
    /// assignments per spawn.
    /// </summary>
    public sealed class ProjectileSkin : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        private Sprite _defaultSprite;
        private Color _defaultColor;

        private void Awake()
        {
            _defaultSprite = _renderer.sprite;
            _defaultColor = _renderer.color;
        }

        private void OnEnable()
        {
            if (StingerSkin.OverrideSprite != null)
            {
                _renderer.sprite = StingerSkin.OverrideSprite;
                _renderer.color = StingerSkin.Tint;
            }
            else
            {
                _renderer.sprite = _defaultSprite;
                _renderer.color = _defaultColor;
            }
        }
    }
}
