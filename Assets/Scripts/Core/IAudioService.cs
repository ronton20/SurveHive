using SurveHive.Data;

namespace SurveHive.Core
{
    /// <summary>
    /// Cross-system audio seam (per CLAUDE.md's DIP guidance) so gameplay code
    /// depends on an abstraction rather than the concrete <c>AudioService</c>.
    /// </summary>
    public interface IAudioService
    {
        /// <summary>Current SFX volume (0-1); for callers that play their own one-shot clips.</summary>
        float SfxVolume { get; }

        void PlaySfx(SfxId id);

        void PlayMusic(MusicId id);

        void StopMusic();

        void SetSfxVolume(float volume01);

        void SetMusicVolume(float volume01);
    }
}
