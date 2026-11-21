using System.Collections.Generic;
using UnityEngine;

namespace Skp.Audio
{
    /// <summary>
    /// Plays <see cref="Sfx"/> (game) and UI cues through pooled 2D AudioSources routed to the mixer's Game and
    /// UI groups (both under SFX, which the Sound effects setting mutes). The same cue never restarts within
    /// <see cref="AudioTuning.sameCueInterval"/>, and the voice pools are fixed, so a burst cannot stack up and clip.
    /// Lives on the GameRoot.
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        SfxBank _bank;
        AudioTuning _t;
        AudioSource[] _game, _ui;
        int _nextGame, _nextUi;
        readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>(64);

        /// <summary>Cues actually started (after the rate limit), for tests.</summary>
        public int Started { get; private set; }
        public int Skipped { get; private set; }

        void Awake()
        {
            _bank = Resources.Load<SfxBank>(SfxBank.ResourcePath);
            _t = Resources.Load<AudioTuning>(AudioTuning.ResourcePath);
            if (_t == null) _t = ScriptableObject.CreateInstance<AudioTuning>();
            _game = Pool(Mathf.Max(1, _t.gameVoices), _t.gameGroup, false);
            _ui = Pool(Mathf.Max(1, _t.uiVoices), _t.uiGroup, true);
            Sfx.Played += PlayGame;
            Skp.UI.UiAudio.Play += PlayUi;
        }

        AudioSource[] Pool(int n, UnityEngine.Audio.AudioMixerGroup group, bool ignorePause)
        {
            var a = new AudioSource[n];
            for (int i = 0; i < n; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                s.ignoreListenerPause = ignorePause;
                s.outputAudioMixerGroup = group;
                a[i] = s;
            }
            return a;
        }

        void OnDestroy()
        {
            Sfx.Played -= PlayGame;
            Skp.UI.UiAudio.Play -= PlayUi;
        }

        void PlayGame(string cue) => Play(cue, _game, ref _nextGame);
        void PlayUi(string cue) => Play(cue, _ui, ref _nextUi);

        void Play(string cue, AudioSource[] pool, ref int next)
        {
            if (_bank == null || !_bank.TryGet(cue, out var e)) return;
            // the mixer's SFX group carries the mute; no early-out here so the rate limit stays honest
            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(cue, out float last) && now - last < _t.sameCueInterval) { Skipped++; return; }
            _lastPlayed[cue] = now;
            // reuse a free voice if there is one, else the oldest
            int pick = -1;
            for (int i = 0; i < pool.Length; i++) { int k = (next + i) % pool.Length; if (!pool[k].isPlaying) { pick = k; break; } }
            if (pick < 0) pick = next;
            next = (pick + 1) % pool.Length;
            var src = pool[pick];
            src.Stop();
            src.clip = e.clip;
            src.volume = e.volume > 0 ? e.volume : _t.defaultCueVolume;
            src.Play();
            Started++;
        }
    }
}
