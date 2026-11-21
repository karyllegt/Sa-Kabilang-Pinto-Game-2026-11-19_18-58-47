using System;
using Skp.Save;
using TMPro;
using UnityEngine;

namespace Skp.UI
{
    /// <summary>
    /// Settings: music, sound effects, vibration, screen shake, reduced flash, high contrast, left-handed
    /// touch layout and touch controls (auto/on/off), in two columns so every row stays a full 48 dp
    /// target even on the shortest screens. Changes save immediately.
    /// </summary>
    public class SettingsScreen : UiScreen
    {
        public PixelButton back;
        public PixelButton music, sfx, vibration, shake, flash, contrast, lefty, touch;

        SettingsStore _store;
        Action _onBack;
        bool _wired;

        public static SettingsScreen Build(Transform parent)
        {
            var th = UiKit.Theme;
            var root = ScreenParts.FullScreen(parent, "SettingsScreen");
            UiKit.Scrim(root).color = new Color(th.shadow.r, th.shadow.g, th.shadow.b, 0.94f); // reads cleanly over gameplay when opened from pause
            var s = root.gameObject.AddComponent<SettingsScreen>();
            s.back = ScreenParts.BackButton(root);
            ScreenParts.Heading(root, Loc.Get("settings.title"));

            var col = ScreenParts.Column(root, 14, 84);
            var cols = UiKit.HStack(col.transform, "Columns", 36, null, TextAnchor.UpperCenter);
            var a = UiKit.VStack(cols.transform, "A", 10);
            var b = UiKit.VStack(cols.transform, "B", 10);
            PixelButton Row(Transform p, string key)
            {
                var r = UiKit.HStack(p, key, 12, null, TextAnchor.MiddleLeft);
                UiKit.Text(r.transform, "Label", Loc.Get(key), th.bodySize, th.text, TextAlignmentOptions.Left, 258);
                return UiKit.Button(r.transform, "Toggle", Loc.Get("common.on"), new Vector2(104, UiKit.MinTarget), th.smallSize);
            }
            s.music = Row(a.transform, "settings.music");
            s.sfx = Row(a.transform, "settings.sfx");
            s.vibration = Row(a.transform, "settings.vibration");
            s.shake = Row(a.transform, "settings.shake");
            s.flash = Row(b.transform, "settings.flash");
            s.contrast = Row(b.transform, "settings.contrast");
            s.lefty = Row(b.transform, "settings.lefty");
            s.touch = Row(b.transform, "settings.touch");
            UiKit.Text(col.transform, "Note", Loc.Get("settings.note"), th.tinySize, th.textDim, TextAlignmentOptions.Center, 720);
            s.firstSelected = s.music;
            root.gameObject.SetActive(false);
            return s;
        }

        public void Bind(SettingsStore store, Action onBack)
        {
            _store = store;
            _onBack = onBack;
            if (!_wired)
            {
                _wired = true;
                back.Clicked += () => _onBack?.Invoke();
                music.Clicked += () => Flip(d => d.music = !d.music);
                sfx.Clicked += () => Flip(d => d.sfx = !d.sfx);
                vibration.Clicked += () => Flip(d => d.vibration = !d.vibration);
                shake.Clicked += () => Flip(d => d.shake = !d.shake);
                flash.Clicked += () => Flip(d => d.reducedFlash = !d.reducedFlash);
                contrast.Clicked += () => Flip(d => d.highContrast = !d.highContrast);
                lefty.Clicked += () => Flip(d => d.leftHanded = !d.leftHanded);
                touch.Clicked += () => Flip(d => d.touch = (TouchMode)(((int)d.touch + 1) % 3));
            }
            Refresh();
        }

        void Flip(Action<GameSettings> change)
        {
            if (_store == null) return;
            change(_store.Data);
            _store.Save();
            Refresh();
        }

        public void Refresh()
        {
            var d = _store != null ? _store.Data : new GameSettings();
            string On(bool v) => Loc.Get(v ? "common.on" : "common.off");
            music.Text = On(d.music);
            sfx.Text = On(d.sfx);
            vibration.Text = On(d.vibration);
            shake.Text = On(d.shake);
            flash.Text = On(d.reducedFlash);
            contrast.Text = On(d.highContrast);
            lefty.Text = On(d.leftHanded);
            touch.Text = Loc.Get("settings.touch." + d.touch.ToString().ToLowerInvariant());
        }

        public override bool OnBack() { back.Press(); return true; }
    }
}
