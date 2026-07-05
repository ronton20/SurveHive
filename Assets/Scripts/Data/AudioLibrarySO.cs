using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurveHive.Data
{
    /// <summary>
    /// Maps every <see cref="SfxId"/>/<see cref="MusicId"/> to its clip(s).
    /// SFX entries support a clip pool + pitch range so repeated events (hits,
    /// pickups) don't sound identical every time. Lookup dictionaries are built
    /// once (lazily) rather than per-call.
    /// </summary>
    [CreateAssetMenu(menuName = "SurveHive/Audio Library", fileName = "AudioLibrary")]
    public sealed class AudioLibrarySO : ScriptableObject
    {
        [Serializable]
        public struct SfxEntry
        {
            public SfxId id;
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume;
            public float pitchMin;
            public float pitchMax;
            // Minimum seconds between plays of this id (unscaled). Collapses
            // bursts (e.g. an AoE hitting a whole horde) into a readable texture
            // instead of a machine-gun wall. 0 = no throttle.
            public float minInterval;
        }

        [Serializable]
        public struct MusicEntry
        {
            public MusicId id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [SerializeField] private SfxEntry[] _sfx;
        [SerializeField] private MusicEntry[] _music;

        private Dictionary<SfxId, SfxEntry> _sfxLookup;
        private Dictionary<MusicId, MusicEntry> _musicLookup;

        public bool TryGetSfx(SfxId id, out SfxEntry entry)
        {
            BuildLookupsIfNeeded();
            return _sfxLookup.TryGetValue(id, out entry);
        }

        public bool TryGetMusic(MusicId id, out MusicEntry entry)
        {
            BuildLookupsIfNeeded();
            return _musicLookup.TryGetValue(id, out entry);
        }

        private void BuildLookupsIfNeeded()
        {
            if (_sfxLookup != null)
            {
                return;
            }

            _sfxLookup = new Dictionary<SfxId, SfxEntry>(_sfx.Length);
            for (int i = 0; i < _sfx.Length; i++)
            {
                _sfxLookup[_sfx[i].id] = _sfx[i];
            }

            _musicLookup = new Dictionary<MusicId, MusicEntry>(_music.Length);
            for (int i = 0; i < _music.Length; i++)
            {
                _musicLookup[_music[i].id] = _music[i];
            }
        }

        private void OnEnable()
        {
            // Cleared instead of built here: builds lazily on first real lookup so
            // the editor-time asset (no clips assigned yet mid-authoring) never throws.
            _sfxLookup = null;
            _musicLookup = null;
        }
    }
}
