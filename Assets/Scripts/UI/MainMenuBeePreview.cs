using SurveHive.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SurveHive.UI
{
    /// <summary>
    /// Bottom-right main-menu showcase of the hero's currently-equipped look —
    /// body color tint + hat overlay only (no stinger). Reads the persistent
    /// store on every enable, so returning from the Hive Style panel reflects a
    /// fresh equip. Menu-only, refresh-on-enable; no per-frame work. The overlay
    /// math mirrors <see cref="CosmeticsUI"/> so the hat sits exactly as it does
    /// on the in-run rig.
    /// </summary>
    public sealed class MainMenuBeePreview : MonoBehaviour
    {
        [SerializeField] private MetaProgressionStoreSO _store;
        [SerializeField] private CosmeticCatalogSO _catalog;
        [SerializeField] private Image _bodyImage;
        [SerializeField] private Image _hatImage;
        // Fallback preview pixels per world unit, used only when the body sprite
        // is missing — otherwise derived from the preserve-aspect body fit.
        [SerializeField] private float _previewPixelsPerUnit = 96f;

        private void OnEnable()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_store == null || _catalog == null)
            {
                return;
            }

            if (_bodyImage != null)
            {
                CosmeticSO color = _catalog.FindById(_store.GetEquippedCosmetic((int)CosmeticSlot.Color));
                _bodyImage.color = color != null ? color.Tint : Color.white;
            }

            RefreshHat(_catalog.FindById(_store.GetEquippedCosmetic((int)CosmeticSlot.Hat)));
        }

        private void RefreshHat(CosmeticSO hat)
        {
            if (_hatImage == null)
            {
                return;
            }

            if (hat == null || hat.Sprite == null)
            {
                _hatImage.enabled = false;
                return;
            }

            Sprite sprite = hat.Sprite;
            _hatImage.sprite = sprite;
            _hatImage.color = Color.white;
            var rect = (RectTransform)_hatImage.transform;
            float scale = PreviewPixelsPerUnit();
            rect.sizeDelta = sprite.rect.size / sprite.pixelsPerUnit * scale;
            rect.anchoredPosition = hat.AttachOffset * scale;
            _hatImage.enabled = true;
        }

        // How many preview pixels one Body world unit spans: the preserve-aspect
        // body Image fits its sprite by the tighter axis.
        private float PreviewPixelsPerUnit()
        {
            if (_bodyImage == null || _bodyImage.sprite == null)
            {
                return _previewPixelsPerUnit;
            }

            Sprite body = _bodyImage.sprite;
            Vector2 worldSize = body.rect.size / body.pixelsPerUnit;
            Vector2 rectSize = ((RectTransform)_bodyImage.transform).rect.size;
            return Mathf.Min(rectSize.x / worldSize.x, rectSize.y / worldSize.y);
        }
    }
}
