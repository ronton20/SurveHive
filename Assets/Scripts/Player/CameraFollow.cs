using SurveHive.View;
using UnityEngine;

namespace SurveHive.Player
{
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0f, -10f);
        [SerializeField] private CameraShaker _shaker;

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            Vector3 position = _target.position + _offset;
            if (_shaker != null)
            {
                position += _shaker.CurrentOffset;
            }

            transform.position = position;
        }
    }
}
