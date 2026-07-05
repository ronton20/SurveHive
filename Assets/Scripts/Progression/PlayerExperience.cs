using System;
using System.Collections.Generic;
using SurveHive.Data;
using UnityEngine;

namespace SurveHive.Progression
{
    public sealed class PlayerExperience : MonoBehaviour
    {
        [SerializeField] private LevelCurveSO _levelCurve;
        [SerializeField] private float _autoStatBonusPercentPerLevel = 2f;

        private int _currentLevel = 1;
        private float _currentExp;
        private float _expToNextLevel;
        private readonly Queue<int> _pendingLevelUps = new Queue<int>(4);
        // When set, the next level-up offer is shown as a guaranteed lucky (+2) pick
        // (Phase 2B miniboss reward). Consumed by the level-up controller.
        private bool _nextOfferLucky;

        public event Action<float, float> OnExpChanged;
        public event Action<int> OnLevelUp;

        public int CurrentLevel => _currentLevel;

        public float AutoStatBonusPercentPerLevel => _autoStatBonusPercentPerLevel;

        private void Awake()
        {
            _expToNextLevel = _levelCurve.GetExpForLevel(_currentLevel);
        }

        public void AddExperience(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            _currentExp += amount;

            while (_currentExp >= _expToNextLevel)
            {
                _currentExp -= _expToNextLevel;
                _currentLevel++;
                _expToNextLevel = _levelCurve.GetExpForLevel(_currentLevel);
                _pendingLevelUps.Enqueue(_currentLevel);
            }

            OnExpChanged?.Invoke(_currentExp, _expToNextLevel);

            if (_pendingLevelUps.Count > 0)
            {
                OnLevelUp?.Invoke(_pendingLevelUps.Dequeue());
            }
        }

        /// <summary>
        /// Phase 2B: miniboss kill reward — a guaranteed level-up whose offer is
        /// marked lucky (+2), plus a burst of bonus EXP toward the following level.
        /// </summary>
        public void GrantMinibossReward(float bonusExp)
        {
            _nextOfferLucky = true;

            // Guaranteed free level (this becomes the lucky pick).
            _currentLevel++;
            _expToNextLevel = _levelCurve.GetExpForLevel(_currentLevel);
            _pendingLevelUps.Enqueue(_currentLevel);

            // Burst of EXP on top may grant further (normal) levels.
            _currentExp += Mathf.Max(0f, bonusExp);
            while (_currentExp >= _expToNextLevel)
            {
                _currentExp -= _expToNextLevel;
                _currentLevel++;
                _expToNextLevel = _levelCurve.GetExpForLevel(_currentLevel);
                _pendingLevelUps.Enqueue(_currentLevel);
            }

            OnExpChanged?.Invoke(_currentExp, _expToNextLevel);
            if (_pendingLevelUps.Count > 0)
            {
                OnLevelUp?.Invoke(_pendingLevelUps.Dequeue());
            }
        }

        /// <summary>Returns and clears the "next offer is lucky" flag (Phase 2B).</summary>
        public bool ConsumeForcedLucky()
        {
            if (!_nextOfferLucky)
            {
                return false;
            }

            _nextOfferLucky = false;
            return true;
        }

        public bool TryDequeuePendingLevelUp(out int level)
        {
            if (_pendingLevelUps.Count > 0)
            {
                level = _pendingLevelUps.Dequeue();
                return true;
            }

            level = 0;
            return false;
        }
    }
}
