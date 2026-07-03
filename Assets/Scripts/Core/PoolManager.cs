using System.Collections.Generic;
using SurveHive.Spawning;
using UnityEngine;

namespace SurveHive.Core
{
    public sealed class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        private readonly Dictionary<int, GameObjectPool> _pools = new Dictionary<int, GameObjectPool>();

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RegisterPool(int poolId, GameObject prefab, Transform parent, int prewarmCount, int maxSize)
        {
            if (_pools.ContainsKey(poolId))
            {
                return;
            }

            _pools.Add(poolId, new GameObjectPool(prefab, parent, prewarmCount, maxSize));
        }

        public bool HasPool(int poolId)
        {
            return _pools.ContainsKey(poolId);
        }

        public GameObject Get(int poolId, Vector3 position, Quaternion rotation)
        {
            return _pools[poolId].Get(position, rotation);
        }

        public void Release(int poolId, GameObject instance)
        {
            _pools[poolId].Release(instance);
        }
    }
}
