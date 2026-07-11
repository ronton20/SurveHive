using SurveHive.Data;
using UnityEngine;

namespace SurveHive.View
{
    /// <summary>
    /// Dresses the hero in the equipped cosmetics at spawn (PLAN 5C). The color
    /// slot tints the body through the SpriteFlash shader's _Tint (renderer
    /// color writes get clobbered by animation clips; the property block merges
    /// safely with HitFlash's, which always reads before writing). The hat slot
    /// drives a pre-built overlay child renderer on the Body, so it flips with
    /// the rig's facing. The stinger slot publishes the equipped shape/color to
    /// the static <see cref="StingerSkin"/>, which every pooled auto-attack
    /// projectile reads on spawn. One-shot at Awake — cosmetics can't change
    /// mid-run, so nothing runs per frame.
    /// </summary>
    public sealed class CosmeticApplier : MonoBehaviour
    {
        private static readonly int TintProperty = Shader.PropertyToID("_Tint");

        [SerializeField] private MetaProgressionStoreSO _store;
        [SerializeField] private CosmeticCatalogSO _catalog;
        [SerializeField] private SpriteRenderer _bodyRenderer;
        [SerializeField] private SpriteRenderer _hatRenderer;

        private void Awake()
        {
            // Always reset the static skin first so a run without the applier
            // (or without an equipped stinger) never inherits a stale one.
            StingerSkin.Clear();

            if (_store == null || _catalog == null)
            {
                return;
            }

            ApplyTint(_catalog.FindById(_store.GetEquippedCosmetic((int)CosmeticSlot.Color)));
            ApplyOverlay(_hatRenderer, _catalog.FindById(_store.GetEquippedCosmetic((int)CosmeticSlot.Hat)));

            CosmeticSO stinger = _catalog.FindById(_store.GetEquippedCosmetic((int)CosmeticSlot.Stinger));
            if (stinger != null && stinger.Sprite != null)
            {
                StingerSkin.Set(stinger.Sprite, stinger.Tint);
            }
        }

        private void ApplyTint(CosmeticSO cosmetic)
        {
            if (_bodyRenderer == null || cosmetic == null)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            _bodyRenderer.GetPropertyBlock(block);
            block.SetColor(TintProperty, cosmetic.Tint);
            _bodyRenderer.SetPropertyBlock(block);
        }

        private void ApplyOverlay(SpriteRenderer overlay, CosmeticSO cosmetic)
        {
            if (overlay == null)
            {
                return;
            }

            if (cosmetic == null || cosmetic.Sprite == null)
            {
                overlay.enabled = false;
                return;
            }

            overlay.sprite = cosmetic.Sprite;
            overlay.transform.localPosition = cosmetic.AttachOffset;
            if (_bodyRenderer != null)
            {
                overlay.sortingOrder = _bodyRenderer.sortingOrder + cosmetic.SortingOffset;
            }

            overlay.enabled = true;
        }
    }
}
