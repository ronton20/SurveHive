using System.Collections.Generic;
using System.Text;
using SurveHive.Core;
using SurveHive.Data;
using SurveHive.Progression;
using TMPro;
using UnityEngine;

namespace SurveHive.UI
{
    /// <summary>
    /// In-run unlock toast (PLAN 5D): listens to the tracker's static unlock
    /// event, queues, and shows one banner at a time — fade in, hold, fade
    /// out. Runs on unscaled time so hit-stop can't freeze it mid-fade. The
    /// toast strings are built once per unlock (a rare event), never per
    /// frame; Update itself is allocation-free.
    /// </summary>
    public sealed class AchievementToastUI : MonoBehaviour
    {
        private enum Phase
        {
            Hidden,
            FadeIn,
            Hold,
            FadeOut,
        }

        [SerializeField] private CanvasGroup _root;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _rewardText;
        // Resolves cosmetic-reward ids to display names on the reward line.
        [SerializeField] private CosmeticCatalogSO _cosmeticCatalog;
        [SerializeField] private float _fadeInSeconds = 0.25f;
        [SerializeField] private float _holdSeconds = 3f;
        [SerializeField] private float _fadeOutSeconds = 0.5f;

        private readonly Queue<AchievementSO> _queue = new Queue<AchievementSO>();
        private readonly StringBuilder _rewardBuilder = new StringBuilder(64);
        private Phase _phase = Phase.Hidden;
        private float _phaseTime;

        private void Awake()
        {
            if (_root != null)
            {
                _root.alpha = 0f;
            }
        }

        private void OnEnable()
        {
            AchievementTracker.Unlocked += HandleUnlocked;
        }

        private void OnDisable()
        {
            AchievementTracker.Unlocked -= HandleUnlocked;
        }

        private void Update()
        {
            if (_phase == Phase.Hidden)
            {
                if (_queue.Count > 0)
                {
                    ShowNext();
                }

                return;
            }

            _phaseTime += Time.unscaledDeltaTime;
            switch (_phase)
            {
                case Phase.FadeIn:
                    _root.alpha = Mathf.Clamp01(_phaseTime / _fadeInSeconds);
                    if (_phaseTime >= _fadeInSeconds)
                    {
                        SetPhase(Phase.Hold);
                    }

                    break;
                case Phase.Hold:
                    if (_phaseTime >= _holdSeconds)
                    {
                        SetPhase(Phase.FadeOut);
                    }

                    break;
                case Phase.FadeOut:
                    _root.alpha = 1f - Mathf.Clamp01(_phaseTime / _fadeOutSeconds);
                    if (_phaseTime >= _fadeOutSeconds)
                    {
                        SetPhase(Phase.Hidden);
                    }

                    break;
            }
        }

        private void HandleUnlocked(AchievementSO achievement)
        {
            if (achievement != null)
            {
                _queue.Enqueue(achievement);
            }
        }

        private void ShowNext()
        {
            AchievementSO achievement = _queue.Dequeue();
            if (_root == null || achievement == null)
            {
                return;
            }

            if (_titleText != null)
            {
                _titleText.text = Loc.Get(LocKeys.AchievementsToastTitle);
            }

            if (_nameText != null)
            {
                _nameText.text = achievement.DisplayName;
            }

            if (_rewardText != null)
            {
                _rewardText.text = BuildRewardLine(achievement);
            }

            SetPhase(Phase.FadeIn);
        }

        private string BuildRewardLine(AchievementSO achievement)
        {
            _rewardBuilder.Length = 0;
            if (achievement.JellyReward > 0)
            {
                _rewardBuilder.Append(Loc.Get(LocKeys.AchievementsRewardPrefix));
                _rewardBuilder.Append(CurrencyGlyphs.Jelly);
                _rewardBuilder.Append('+');
                _rewardBuilder.Append(achievement.JellyReward);
            }

            if (!string.IsNullOrEmpty(achievement.CosmeticRewardId) && _cosmeticCatalog != null)
            {
                CosmeticSO cosmetic = _cosmeticCatalog.FindById(achievement.CosmeticRewardId);
                if (cosmetic != null)
                {
                    _rewardBuilder.Append(_rewardBuilder.Length > 0
                        ? "  +  "
                        : Loc.Get(LocKeys.AchievementsRewardPrefix));
                    _rewardBuilder.Append(cosmetic.DisplayName);
                }
            }

            return _rewardBuilder.ToString();
        }

        private void SetPhase(Phase phase)
        {
            _phase = phase;
            _phaseTime = 0f;
            if (_root != null)
            {
                _root.alpha = phase == Phase.FadeIn || phase == Phase.Hidden ? 0f : 1f;
            }
        }
    }
}
