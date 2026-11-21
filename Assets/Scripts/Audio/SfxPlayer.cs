using Skp.Core;
using UnityEngine;

namespace Skp.Audio
{
    /// <summary>
    /// Plays <see cref="Sfx"/> and UI cues through a few pooled 2D AudioSources, honoring the Sound effects
    /// setting. Lives on the GameRoot. A simple stand-in until the M8 mixer.
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        const int Voices = 6;
        SfxBank _bank;
        AudioSource[] _sources;
        int _next;

        void Awake()
        {
            _bank = Resources.Load<SfxBank>(SfxBank.ResourcePath);
            _sources = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                _sources[i] = s;
            }
            Sfx.Played += Play;
            Skp.UI.UiAudio.Play += Play;
        }

        void OnDestroy()
        {
            Sfx.Played -= Play;
            Skp.UI.UiAudio.Play -= Play;
        }

        void Play(string cue)
        {
            if (_bank == null || !_bank.TryGet(cue, out var e)) return;
            var settings = GameRoot.Instance != null ? GameRoot.Instance.Settings : null;
            if (settings != null && !settings.Data.sfx) return;
            var src = _sources[_next];
            _next = (_next + 1) % _sources.Length;
            src.PlayOneShot(e.clip, e.volume > 0 ? e.volume : 0.8f);
        }
    }
}
