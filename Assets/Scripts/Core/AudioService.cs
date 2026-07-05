using SurveHive.Data;
using SurveHive.Persistence;
using UnityEngine;

namespace SurveHive.Core
{
    /// <summary>
    /// Scene-scoped audio hub (PLAN.md §7): a small round-robin pool of SFX
    /// <see cref="AudioSource"/>s (so overlapping one-shots don't cut each other
    /// off and can each carry their own pitch jitter) plus one dedicated music
    /// source. Reads persisted volumes at startup and applies live changes from
    /// <see cref="SurveHive.UI.SettingsPanelUI"/>. Zero allocations after Awake.
    /// </summary>
    public sealed class AudioService : MonoBehaviour, IAudioService
    {
        public static IAudioService Instance { get; private set; }

        public float SfxVolume => _sfxVolume;

        [SerializeField] private AudioLibrarySO _library;
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource[] _sfxSources;
        [SerializeField] private PersistentMetaProgressionStoreSO _store;
        [SerializeField] private bool _autoPlayMusic;
        [SerializeField] private MusicId _autoPlayMusicId;

        private int _nextSfxSource;
        private float _sfxVolume = 1f;
        private float _musicVolume = 1f;
        private bool _hasCurrentMusic;
        private MusicId _currentMusic;
        // Last unscaled play time per SfxId, for the per-id min-interval throttle.
        private float[] _lastPlayTime;

        private void Awake()
        {
            Instance = this;
            _lastPlayTime = new float[System.Enum.GetValues(typeof(SfxId)).Length];

            if (_store != null)
            {
                SettingsData settings = _store.Settings;
                _sfxVolume = Mathf.Clamp01(settings.sfxVolume);
                _musicVolume = Mathf.Clamp01(settings.musicVolume);
            }
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }

        private void Start()
        {
            if (_autoPlayMusic)
            {
                PlayMusic(_autoPlayMusicId);
            }
        }

        public void PlaySfx(SfxId id)
        {
            if (_library == null || _sfxSources.Length == 0 || !_library.TryGetSfx(id, out AudioLibrarySO.SfxEntry entry)
                || entry.clips == null || entry.clips.Length == 0)
            {
                return;
            }

            if (entry.minInterval > 0f)
            {
                // Unscaled so the throttle behaves the same during hit-stop/pause.
                float now = Time.unscaledTime;
                int idx = (int)id;
                if (idx >= 0 && idx < _lastPlayTime.Length)
                {
                    if (now - _lastPlayTime[idx] < entry.minInterval)
                    {
                        return;
                    }

                    _lastPlayTime[idx] = now;
                }
            }

            AudioSource source = _sfxSources[_nextSfxSource];
            _nextSfxSource = (_nextSfxSource + 1) % _sfxSources.Length;

            AudioClip clip = entry.clips.Length == 1 ? entry.clips[0] : entry.clips[Random.Range(0, entry.clips.Length)];
            source.pitch = entry.pitchMin >= entry.pitchMax ? Mathf.Max(0.01f, entry.pitchMin) : Random.Range(entry.pitchMin, entry.pitchMax);
            source.volume = entry.volume * _sfxVolume;
            source.clip = clip;
            source.Play();
        }

        public void PlayMusic(MusicId id)
        {
            if (_musicSource == null || (_hasCurrentMusic && _currentMusic == id && _musicSource.isPlaying))
            {
                return;
            }

            if (_library == null || !_library.TryGetMusic(id, out AudioLibrarySO.MusicEntry entry) || entry.clip == null)
            {
                return;
            }

            _musicSource.clip = entry.clip;
            _musicSource.loop = true;
            _musicSource.volume = entry.volume * _musicVolume;
            _musicSource.Play();
            _hasCurrentMusic = true;
            _currentMusic = id;
        }

        public void StopMusic()
        {
            if (_musicSource != null)
            {
                _musicSource.Stop();
            }

            _hasCurrentMusic = false;
        }

        public void SetSfxVolume(float volume01)
        {
            _sfxVolume = Mathf.Clamp01(volume01);
        }

        public void SetMusicVolume(float volume01)
        {
            _musicVolume = Mathf.Clamp01(volume01);
            if (_musicSource != null && _library != null && _hasCurrentMusic
                && _library.TryGetMusic(_currentMusic, out AudioLibrarySO.MusicEntry entry))
            {
                _musicSource.volume = entry.volume * _musicVolume;
            }
        }
    }
}
