using System.Collections.Generic;
using SurveHive.Enemies;
using UnityEngine;

namespace SurveHive.Core
{
    public sealed class EnemyRegistry : MonoBehaviour
    {
        private const int InitialCapacity = 128;

        public static EnemyRegistry Instance { get; private set; }

        private readonly List<EnemyController> _activeEnemies = new List<EnemyController>(InitialCapacity);

        public IReadOnlyList<EnemyController> ActiveEnemies => _activeEnemies;

        public int ActiveCount => _activeEnemies.Count;

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

        public void Register(EnemyController enemy)
        {
            _activeEnemies.Add(enemy);
        }

        public void Unregister(EnemyController enemy)
        {
            _activeEnemies.Remove(enemy);
        }
    }
}
