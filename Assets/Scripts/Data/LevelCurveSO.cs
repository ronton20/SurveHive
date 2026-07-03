using UnityEngine;

namespace SurveHive.Data
{
    [CreateAssetMenu(menuName = "SurveHive/Level Curve", fileName = "LevelCurve")]
    public sealed class LevelCurveSO : ScriptableObject
    {
        [SerializeField] private float _baseExpToLevel2 = 10f;
        [SerializeField] private float _growthFactor = 1.15f;

        public float GetExpForLevel(int level)
        {
            return _baseExpToLevel2 * Mathf.Pow(_growthFactor, level - 1);
        }
    }
}
