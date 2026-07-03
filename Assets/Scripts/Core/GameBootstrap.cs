using System;
using UnityEngine;

namespace SurveHive.Core
{
    [Serializable]
    public struct PoolPrewarmEntry
    {
        public int poolId;
        public GameObject prefab;
        public int prewarmCount;
        public int maxSize;
    }

    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private PoolPrewarmEntry[] _pools;
        [SerializeField] private Transform _poolParent;

        private void Awake()
        {
            // Sibling component accessed directly rather than via PoolManager.Instance:
            // Awake order between components is not guaranteed by Unity, even on the
            // same GameObject, so the static singleton may not be assigned yet here.
            if (!TryGetComponent(out PoolManager poolManager))
            {
                return;
            }

            for (int i = 0; i < _pools.Length; i++)
            {
                PoolPrewarmEntry entry = _pools[i];
                poolManager.RegisterPool(entry.poolId, entry.prefab, _poolParent, entry.prewarmCount, entry.maxSize);
            }
        }
    }
}
