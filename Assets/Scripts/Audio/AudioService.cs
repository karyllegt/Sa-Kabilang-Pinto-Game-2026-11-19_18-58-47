using Skp.Core;
using Skp.Save;
using UnityEngine;

namespace Skp.Audio
{
    /// <summary>
    /// Music and the mixer, on the GameRoot. Two music sources crossfade between tracks; the Music group's
    /// exposed cutoff and pitch glide with the dimension; the Settings toggles mute the Music and SFX groups;
    /// pause ducks the music and silences game sounds (menu sounds stay). All fades use unscaled time.
    /// Sound effects are played by <see cref="SfxPlayer"/> through the same groups.
    /// </summary>
    public class AudioService : MonoBehaviour
    {
        public static AudioService Instance { get; private set; }

        public AudioTuning Tuning { get; private set; }
        /// <summary>The track the audible source plays (tests).</summary>
        public AudioClip PlayingClip => _sources[_active].clip;

        AudioSource[] _sources;
        int _active;
        float _fade = 1f; // 0 to 1 toward _active
        MusicTrack _track = MusicTrack.None;

        // current (smoothed) and target mixer values
        float _musicDb, _sfxDb, _gameDb, _cutoff, _pitch;
        bool _snap = true;

        void Awake()
        {
            Instance = this;
            Tuning = Resources.Load<AudioTuning>(AudioTuning.ResourcePath);
            if (Tuning == null) Tuning = ScriptableObject.CreateInstance<AudioTuning>();
            _sources = new AudioSource[2];
            for (int i = 0; i < 2; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.loop = true;
                s.spatialBlend = 0f;
                s.volume = 0f;
                s.ignoreListenerPause = true;
                s.outputAudioMixerGroup = Tuning.musicGroup;
                _sources[i] = s;
            }
            _cutoff = Tuning.openCutoffHz;
            _pitch = 1f;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        GameSettings Settings => GameRoot.Instance != null && GameRoot.Instance.Settings != null ? GameRoot.Instance.Settings.Data : null;

        /// <summary>Target levels from the settings and the pause duck.</summary>
        public void Targets(out float musicDb, out float sfxDb, out float gameDb, out float cutoffHz, out float pitch)
        {
            var t = Tuning;
            var s = Settings;
            bool music = s == null || s.music, sfx = s == null || s.sfx;
            musicDb = music ? t.musicDb + (Music.Ducked ? t.pauseMusicDuckDb : 0f) : t.muteDb;
            sfxDb = sfx ? t.sfxDb : t.muteDb;
            gameDb = Music.Ducked ? t.pauseGameDuckDb : 0f;
            Music.DimensionSound(Music.Dimension, t, out cutoffHz, out pitch);
            musicDb = Mathf.Max(musicDb, t.muteDb);
            gameDb = Mathf.Max(gameDb, t.muteDb);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            var t = Tuning;

            if (Music.Current != _track) StartTrack(Music.Current);
            if (_fade < 1f)
            {
                _fade = t.trackCrossfadeSeconds > 0 ? Mathf.Min(1f, _fade + dt / t.trackCrossfadeSeconds) : 1f;
                // equal power: no dip in loudness halfway through
                _sources[_active].volume = Mathf.Sin(_fade * Mathf.PI * 0.5f);
                var other = _sources[1 - _active];
                other.volume = Mathf.Cos(_fade * Mathf.PI * 0.5f);
                if (_fade >= 1f) { other.Stop(); other.clip = null; }
            }

            Targets(out float mDb, out float sDb, out float gDb, out float cut, out float pitch);
            if (_snap)
            {
                _musicDb = mDb; _sfxDb = sDb; _gameDb = gDb; _cutoff = cut; _pitch = pitch;
                _snap = false;
            }
            else
            {
                float k = t.levelFadeSeconds > 0 ? Mathf.Clamp01(dt / t.levelFadeSeconds) : 1f;
                _musicDb = Approach(_musicDb, mDb, 80f * k);
                _sfxDb = Approach(_sfxDb, sDb, 80f * k);
                _gameDb = Approach(_gameDb, gDb, 80f * k);
                float g = t.dimensionGlideSeconds > 0 ? Mathf.Clamp01(dt / t.dimensionGlideSeconds) : 1f;
                // the cutoff glides in octaves, so the sweep sounds even
                float lc = Mathf.Log(_cutoff), lt = Mathf.Log(Mathf.Max(10f, cut));
                _cutoff = Mathf.Exp(Approach(lc, lt, Mathf.Abs(Mathf.Log(t.openCutoffHz) - Mathf.Log(Mathf.Max(10f, t.spiritCutoffHz))) * g));
                _pitch = Approach(_pitch, pitch, Mathf.Abs(1f - t.invertedPitch) * g);
            }
            Apply();
        }

        static float Approach(float v, float target, float step) =>
            v < target ? Mathf.Min(target, v + step) : Mathf.Max(target, v - step);

        void Apply()
        {
            var m = Tuning.mixer;
            if (m == null) return;
            m.SetFloat(AudioTuning.MusicVolume, _musicDb);
            m.SetFloat(AudioTuning.SfxVolume, _sfxDb);
            m.SetFloat(AudioTuning.GameVolume, _gameDb);
            m.SetFloat(AudioTuning.MusicCutoff, _cutoff);
            m.SetFloat(AudioTuning.MusicPitch, _pitch);
        }

        void StartTrack(MusicTrack track)
        {
            _track = track;
            var clip = Tuning.Track(track);
            var cur = _sources[_active];
            if (clip != null && cur.clip == clip) return;
            _active = 1 - _active;
            var next = _sources[_active];
            next.clip = clip;
            next.volume = 0f;
            if (clip != null) next.Play();
            _fade = 0f;
        }

        /// <summary>Jump to the targets now (tests, and the first frame).</summary>
        public void Settle()
        {
            _snap = true;
            if (Music.Current != _track) StartTrack(Music.Current);
            if (_fade < 1f)
            {
                _fade = 1f;
                _sources[_active].volume = 1f;
                var other = _sources[1 - _active];
                other.volume = 0f; other.Stop(); other.clip = null;
            }
            Update();
        }

        /// <summary>Current smoothed mixer values (tests).</summary>
        public float MusicDb => _musicDb;
        public float SfxDb => _sfxDb;
        public float GameDb => _gameDb;
        public float CutoffHz => _cutoff;
        public float Pitch => _pitch;
    }
}
