using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.Media;
#endif

namespace Skp.Tests.Video
{
    /// <summary>
    /// Records the running game to an MP4 (960x540, 30 fps, with the game's own audio through AudioRenderer) without
    /// a Game view, so it works in a batch-mode editor. Game time is locked to 60 frames a second
    /// (<see cref="Time.captureFramerate"/>), so one frame is one simulation tick and the video plays at real speed
    /// however slowly it is made; every second frame is encoded. Overlay canvases are switched to Camera mode on the
    /// scene camera and sorted above every world sprite, so world and UI land in one render texture as the player sees them.
    /// </summary>
    [DefaultExecutionOrder(30000)]
    public class ProgressRecorder : MonoBehaviour
    {
        public const int W = 960, H = 540, GameFps = 60, VideoFps = 30, UiLayer = 5;

        RenderTexture _rt;
        Texture2D _frame;
        Camera _main;
        int _frameNo, _encoded;
        string _stillName;
        readonly List<string> _stills = new List<string>();
        string _stillDir;
        bool _audioOk;
        int _audioChannels;
#if UNITY_EDITOR
        MediaEncoder _enc;
#endif

        public int EncodedFrames => _encoded;
        public bool AudioCaptured => _audioOk;
        public IReadOnlyList<string> Stills => _stills;

        public static ProgressRecorder Begin(string mp4Path, string stillDir)
        {
            var go = new GameObject("ProgressRecorder");
            DontDestroyOnLoad(go);
            var r = go.AddComponent<ProgressRecorder>();
            r.Init(mp4Path, stillDir);
            return r;
        }

        void Init(string mp4Path, string stillDir)
        {
            _stillDir = stillDir;
            Directory.CreateDirectory(stillDir);
            Directory.CreateDirectory(Path.GetDirectoryName(mp4Path));
            Time.captureFramerate = GameFps;
            _rt = new RenderTexture(W, H, 24) { name = "ProgressVideo" };
            _rt.Create();
            _frame = new Texture2D(W, H, TextureFormat.RGBA32, false);
            _audioChannels = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2;
#if UNITY_EDITOR
            if (File.Exists(mp4Path)) File.Delete(mp4Path);
            var v = new VideoTrackAttributes
            {
                frameRate = new MediaRational(VideoFps), width = W, height = H, includeAlpha = false,
                bitRateMode = UnityEditor.VideoBitrateMode.High,
            };
            var a = new AudioTrackAttributes
            {
                sampleRate = new MediaRational(AudioSettings.outputSampleRate), channelCount = (ushort)_audioChannels, language = "en",
            };
            _enc = new MediaEncoder(mp4Path, v, a);
#endif
            AudioRenderer.Start();
        }

        /// <summary>Saves the next encoded frame as a PNG still too.</summary>
        public void Still(string name) => _stillName = name;

        void LateUpdate()
        {
            if (_rt == null) return;
            BindCameras();
            if (_frameNo % 15 == 0) { BindCanvases(); KeepOneListener(); }
            _frameNo++;

            // audio for this game frame (silence if the batch editor mixes none)
            int n = AudioRenderer.GetSampleCountForCaptureFrame();
            if (n > 0)
            {
                var buf = new NativeArray<float>(n * _audioChannels, Allocator.Temp);
                if (AudioRenderer.Render(buf))
                    for (int i = 0; i < buf.Length && !_audioOk; i++) if (buf[i] != 0f) _audioOk = true;
#if UNITY_EDITOR
                _enc?.AddSamples(buf);
#endif
                buf.Dispose();
            }

            if (_frameNo % (GameFps / VideoFps) != 0 || _main == null) return;
            Canvas.ForceUpdateCanvases();
            _main.targetTexture = _rt;
            _main.Render();
            RenderTexture.active = _rt;
            _frame.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            RenderTexture.active = null;
#if UNITY_EDITOR
            _enc?.AddFrame(_frame);
#endif
            _encoded++;
            if (_stillName != null)
            {
                _frame.Apply(false);
                var path = Path.Combine(_stillDir, _stillName + ".png");
                File.WriteAllBytes(path, _frame.EncodeToPNG());
                _stills.Add(path);
                _stillName = null;
            }
        }

        /// <summary>Between scenes there can be no listener (the audio then goes silent and warns every frame): fill the gap.</summary>
        void KeepOneListener()
        {
            if (_listener == null) { _listener = gameObject.AddComponent<AudioListener>(); _listener.enabled = false; }
            bool other = false;
            foreach (var l in FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude)) if (l != _listener && l.enabled) other = true;
            _listener.enabled = !other;
        }

        AudioListener _listener;

        void BindCameras()
        {
            var m = Camera.main;
            if (m == null || m == _main) return;
            _main = m;
            var ppc = m.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
            if (ppc != null) ppc.enabled = false; // as in the layout audit: the render texture sets the size
            m.targetTexture = _rt;
            BindCanvases();
        }

        void BindCanvases()
        {
            if (_main == null) return;
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (!c.isRootCanvas) continue;
                if (c.renderMode == RenderMode.ScreenSpaceOverlay || (c.renderMode == RenderMode.ScreenSpaceCamera && c.worldCamera != _main))
                {
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = _main;
                    c.planeDistance = Mathf.Clamp(5f - c.sortingOrder * 0.004f, 0.5f, 9f);
                }
                _main.cullingMask |= 1 << c.gameObject.layer;
            }
            // in Camera mode the canvases sort with the world sprites: lift every canvas that sets its own order
            // above all of them, once, keeping their order among themselves (the overlay look)
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
                if ((c.isRootCanvas || c.overrideSorting) && _lifted.Add(c)) c.sortingOrder = Mathf.Min(32000, c.sortingOrder + Lift);
        }

        const int Lift = 20000;
        readonly HashSet<Canvas> _lifted = new HashSet<Canvas>();

        public void Finish()
        {
            AudioRenderer.Stop();
#if UNITY_EDITOR
            _enc?.Dispose();
            _enc = null;
#endif
            Time.captureFramerate = 0;
            if (_main != null) _main.targetTexture = null;
            if (_rt != null) { _rt.Release(); _rt = null; }
            Destroy(gameObject);
        }

        void OnDestroy()
        {
#if UNITY_EDITOR
            if (_enc != null) { AudioRenderer.Stop(); _enc.Dispose(); _enc = null; Time.captureFramerate = 0; }
#endif
        }
    }
}
