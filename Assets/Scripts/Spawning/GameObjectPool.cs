using UnityEngine;
using UnityEngine.Pool;

namespace SurveHive.Spawning
{
    public sealed class GameObjectPool
    {
        private readonly ObjectPool<GameObject> _pool;
        private readonly GameObject _prefab;
        private readonly Transform _parent;

        public GameObjectPool(GameObject prefab, Transform parent, int defaultCapacity, int maxSize)
        {
            _prefab = prefab;
            _parent = parent;
            _pool = new ObjectPool<GameObject>(CreateInstance, OnGet, OnRelease, OnDestroyInstance, true, defaultCapacity, maxSize);
            Prewarm(defaultCapacity);
        }

        // ObjectPool's defaultCapacity only pre-sizes its internal stack; it does not
        // create instances. Get-then-Release each one up front so real Instantiate calls
        // happen at load time instead of during gameplay.
        private void Prewarm(int count)
        {
            GameObject[] warmed = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                warmed[i] = _pool.Get();
            }

            for (int i = 0; i < count; i++)
            {
                _pool.Release(warmed[i]);
            }
        }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject instance = _pool.Get();
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        public void Release(GameObject instance)
        {
            _pool.Release(instance);
        }

        private GameObject CreateInstance()
        {
            return Object.Instantiate(_prefab, _parent);
        }

        private static void OnGet(GameObject instance)
        {
            instance.SetActive(true);
        }

        private static void OnRelease(GameObject instance)
        {
            instance.SetActive(false);
        }

        private static void OnDestroyInstance(GameObject instance)
        {
            Object.Destroy(instance);
        }
    }
}
