using SurveHive.Core;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SurveHive.View
{
    /// <summary>
    /// Plays the rig's "Death" sprite frames when the enemy dies, then releases
    /// the pooled instance. Drives the SpriteResolver directly because the pack's
    /// shared Animator controller has no death state on its base layer. While
    /// playing, the corpse is inert: collider off, physics off, animator off.
    /// </summary>
    public sealed class DeathAnimation : MonoBehaviour
    {
        private static readonly string[] FrameLabels = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        private const string DeathCategory = "Death";

        [SerializeField] private SpriteResolver _resolver;
        [SerializeField] private Animator _animator;
        [SerializeField] private Collider2D _collider;
        [SerializeField] private Rigidbody2D _rigidbody;
        // Extra visuals to hide while the corpse plays out (e.g. the health bar).
        [SerializeField] private GameObject _hideOnDeath;
        [SerializeField] private int _frameCount = 6;
        [SerializeField] private float _framesPerSecond = 12f;
        [SerializeField] private float _holdLastFrameSeconds = 0.25f;

        private int _poolId;
        private bool _playing;
        private float _elapsed;
        private int _shownFrame;

        public bool IsPlaying => _playing;

        public void Play(int poolId)
        {
            _poolId = poolId;
            _playing = true;
            _elapsed = 0f;
            _shownFrame = 0;

            _animator.enabled = false;
            _collider.enabled = false;
            _rigidbody.simulated = false;

            if (_hideOnDeath != null)
            {
                _hideOnDeath.SetActive(false);
            }

            _resolver.SetCategoryAndLabel(DeathCategory, FrameLabels[0]);
        }

        private void OnEnable()
        {
            // Pooled reuse: restore the living configuration.
            _playing = false;
            _animator.enabled = true;
            _collider.enabled = true;
            _rigidbody.simulated = true;

            if (_hideOnDeath != null)
            {
                _hideOnDeath.SetActive(true);
            }
        }

        private void Update()
        {
            if (!_playing)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            float frameDuration = 1f / _framesPerSecond;
            int frame = Mathf.Min((int)(_elapsed / frameDuration), _frameCount - 1);
            if (frame != _shownFrame)
            {
                _shownFrame = frame;
                _resolver.SetCategoryAndLabel(DeathCategory, FrameLabels[frame]);
            }

            if (_elapsed >= (_frameCount * frameDuration) + _holdLastFrameSeconds)
            {
                _playing = false;
                if (PoolManager.Instance != null)
                {
                    PoolManager.Instance.Release(_poolId, gameObject);
                }
            }
        }
    }
}
