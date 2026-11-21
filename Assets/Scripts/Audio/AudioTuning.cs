using UnityEngine;
using UnityEngine.Audio;

namespace Skp.Audio
{
    /// <summary>
    /// Every audio number in one asset (Resources/Audio/AudioTuning): the mixer and its exposed parameters,
    /// the looped music per act, levels, ducking, the dimension filter and the voice limits.
    /// Built by SkpAudioSetup; edit the values in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Sa Kabilang Pinto/Audio Tuning", fileName = "AudioTuning")]
    public class AudioTuning : ScriptableObject
    {
        public const string ResourcePath = "Audio/AudioTuning";

        // exposed mixer parameter names (SkpAudioSetup exposes them under these names)
        public const string MusicVolume = "MusicVolume", SfxVolume = "SfxVolume", GameVolume = "GameVolume",
                            MusicPitch = "MusicPitch", MusicCutoff = "MusicCutoff";

        [Header("Mixer")]
        public AudioMixer mixer;
        public AudioMixerGroup musicGroup, sfxGroup, gameGroup, uiGroup;

        [Header("Music (looped, see SkpAudioSetup)")]
        public AudioClip menu;
        public AudioClip act1, act2, act3, boss;

        [Header("Levels (dB)")]
        [Tooltip("Music group level. Headroom keeps music plus a burst of effects under full scale.")]
        public float musicDb = -9f;
        public float sfxDb = -3f;
        [Tooltip("Muted groups go to this level.")]
        public float muteDb = -80f;
        [Tooltip("Music drop while paused.")]
        public float pauseMusicDuckDb = -12f;
        [Tooltip("Game sounds (not menu sounds) while paused.")]
        public float pauseGameDuckDb = -80f;
        [Tooltip("Seconds for a level change to settle (unscaled time, so it runs while paused).")]
        public float levelFadeSeconds = 0.25f;

        [Header("Music changes")]
        [Tooltip("Seconds of crossfade between two tracks.")]
        public float trackCrossfadeSeconds = 1.2f;

        [Header("Dimensions")]
        [Tooltip("Low-pass cutoff in Ordinary and Inverted (Hz): fully open.")]
        public float openCutoffHz = 22000f;
        [Tooltip("Spirit muffles the music, as if heard from the other side of a door.")]
        public float spiritCutoffHz = 1300f;
        [Tooltip("Inverted pitches the music down.")]
        public float invertedPitch = 0.84f;
        [Tooltip("Seconds to glide between dimension sounds (short enough to follow the door, long enough not to jar).")]
        public float dimensionGlideSeconds = 0.45f;

        [Header("Effects")]
        [Tooltip("Pooled voices for game sounds; the oldest is reused.")]
        public int gameVoices = 8;
        public int uiVoices = 3;
        [Tooltip("The same cue never restarts faster than this (s): no stacking, no clipping.")]
        public float sameCueInterval = 0.05f;
        [Tooltip("Volume when a bank entry gives none.")]
        public float defaultCueVolume = 0.7f;

        public AudioClip Track(MusicTrack t) => t switch
        {
            MusicTrack.Menu => menu,
            MusicTrack.Act1 => act1,
            MusicTrack.Act2 => act2,
            MusicTrack.Act3 => act3,
            MusicTrack.Boss => boss,
            _ => null,
        };
    }
}
