using UnityEngine;
using UnityEngine.InputSystem;

namespace Skp.Core
{
    /// <summary>
    /// Vibration on the phone and rumble on a gamepad, both off when the Vibration setting is off.
    /// Tests read <see cref="Pulses"/> and <see cref="LastMs"/>; on Android the system Vibrator is called directly
    /// (cached once, so a pulse allocates nothing on the managed heap).
    /// </summary>
    public static class Haptics
    {
        public static int Pulses { get; private set; }
        public static int Suppressed { get; private set; }
        public static int LastMs { get; private set; }

        /// <summary>Tests force the setting without a GameRoot.</summary>
        public static bool? EnabledOverride;

        static float _rumbleUntil;
        static bool _rumbling;

        public static bool Enabled
        {
            get
            {
                if (EnabledOverride.HasValue) return EnabledOverride.Value;
                var r = GameRoot.Instance;
                return r == null || r.Settings == null || r.Settings.Data.vibration;
            }
        }

        public static void Pulse(int ms, float rumble)
        {
            if (ms <= 0 && rumble <= 0) return;
            if (!Enabled) { Suppressed++; return; }
            Pulses++;
            LastMs = ms;
            if (ms > 0) Vibrate(ms);
            var pad = Gamepad.current;
            if (pad != null && rumble > 0)
            {
                pad.SetMotorSpeeds(rumble * 0.6f, rumble);
                _rumbling = true;
                _rumbleUntil = Time.unscaledTime + Mathf.Max(0.05f, ms / 1000f);
            }
        }

        /// <summary>Called every frame by the GameRoot's feel service to end rumble.</summary>
        public static void Tick()
        {
            if (_rumbling && Time.unscaledTime >= _rumbleUntil) Stop();
        }

        public static void Stop()
        {
            _rumbling = false;
            Gamepad.current?.SetMotorSpeeds(0f, 0f);
#if UNITY_ANDROID && !UNITY_EDITOR
            try { _vibrator?.Call("cancel"); } catch { }
#endif
        }

        public static void ResetCounts() { Pulses = 0; Suppressed = 0; LastMs = 0; }

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject _vibrator;
        static bool _looked;
        static void Vibrate(int ms)
        {
            try
            {
                if (!_looked)
                {
                    _looked = true;
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                if (_vibrator != null) _vibrator.Call("vibrate", (long)ms);
                else Handheld.Vibrate();
            }
            catch { Handheld.Vibrate(); }
        }
#else
        static void Vibrate(int ms) { }
#endif
    }
}
