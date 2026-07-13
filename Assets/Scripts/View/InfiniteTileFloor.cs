using UnityEngine;

namespace SurveHive.View
{
    /// <summary>
    /// PLAN 6B — makes the finite honeycomb <see cref="UnityEngine.Tilemaps.Tilemap"/>
    /// read as an endless world-locked floor. Each LateUpdate this snaps the
    /// grid to the follow target (the run camera) rounded to whole tile
    /// <see cref="_period"/> steps: since every honeycomb tile is identical, the
    /// visible pattern stays fixed in world space while the grid quietly
    /// re-centres under the camera — so a modest tile fill covers an unbounded
    /// run without ever exposing the void at the edges.
    ///
    /// Zero-GC: value-type math and one transform write per frame, no
    /// allocations, no per-frame component lookups (the camera is wired at build
    /// time). Runs in LateUpdate after <c>CameraFollow</c> moves the camera; a
    /// one-frame lag is invisible given the fill's large margin over the view.
    /// </summary>
    public sealed class InfiniteTileFloor : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [Tooltip("World size of one tile cell; the snap step (identical tiles hide the jump).")]
        [SerializeField] private float _period = 1f;

        private Transform _transform;

        private void Awake()
        {
            _transform = transform;
        }

        private void LateUpdate()
        {
            if (_target == null || _period <= 0f)
            {
                return;
            }

            Vector3 focus = _target.position;
            float x = Mathf.Round(focus.x / _period) * _period;
            float y = Mathf.Round(focus.y / _period) * _period;

            Vector3 position = _transform.position;
            if (position.x != x || position.y != y)
            {
                position.x = x;
                position.y = y;
                _transform.position = position;
            }
        }
    }
}
