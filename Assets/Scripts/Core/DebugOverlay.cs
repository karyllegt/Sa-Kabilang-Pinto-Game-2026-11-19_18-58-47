using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Skp.Core
{
    /// <summary>
    /// Development-build overlay (never added in release builds). Toggle with F1 or gamepad Select.
    /// Built once; the text refreshes 4 times a second, not every frame.
    /// Later milestones add lines through <see cref="ExtraLines"/> (difficulty multipliers,
    /// level and room index, hazard and enemy counts).
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        public const string FontResource = "Fonts/PressStart2P SDF";

        /// <summary>Extra providers, each returns one or more lines.</summary>
        public static event Func<string> ExtraLines;

        public bool Visible { get; private set; }

        Canvas _canvas;
        TextMeshProUGUI _text;
        readonly StringBuilder _sb = new StringBuilder(256);
        float _accum;
        int _frames;
        float _fps;

        void Start()
        {
            var go = new GameObject("DebugOverlay", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32000;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 540);
            scaler.matchWidthOrHeight = 0.5f;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var rt = (RectTransform)textGo.transform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0.6f, 1);
            rt.offsetMin = new Vector2(12, 12);
            rt.offsetMax = new Vector2(0, -12);
            _text = textGo.AddComponent<TextMeshProUGUI>();
            var font = Resources.Load<TMP_FontAsset>(FontResource);
            if (font != null) _text.font = font;
            _text.fontSize = 10;
            _text.color = new Color32(0x41, 0xe0, 0xd0, 0xff);
            _text.raycastTarget = false;
            _text.alignment = TextAlignmentOptions.TopLeft;
            SetVisible(false);
        }

        public void SetVisible(bool on)
        {
            Visible = on;
            if (_canvas != null) _canvas.enabled = on;
        }

        void Update()
        {
            var root = GameRoot.Instance;
            if (root != null && root.Input != null && root.Input.DebugTogglePressed) SetVisible(!Visible);

            _accum += Time.unscaledDeltaTime;
            _frames++;
            if (_accum < 0.25f) return;
            _fps = _frames / _accum;
            _accum = 0;
            _frames = 0;
            if (Visible) Refresh(root);
        }

        void Refresh(GameRoot root)
        {
            _sb.Clear();
            _sb.Append("DEV ").Append(Application.version).Append('\n');
            _sb.Append("FPS ").Append(_fps.ToString("0.0")).Append('\n');
            _sb.Append("SCENE ").Append(SceneManager.GetActiveScene().name).Append('\n');
            if (root != null)
            {
                var s = root.Save.Data;
                _sb.Append("SAVE ").Append(root.Save.LastLoadResult).Append(" unlocked ").Append(s.unlocked)
                   .Append(" diff ").Append(s.diff).Append(" skills ").Append(s.skills.Length).Append('\n');
                _sb.Append("LOADER ").Append(root.Loader.IsBusy ? "busy" : "idle").Append('\n');
            }
            if (ExtraLines != null)
                foreach (Func<string> f in ExtraLines.GetInvocationList())
                    _sb.Append(f()).Append('\n');
            _text.SetText(_sb);
        }
    }
}
