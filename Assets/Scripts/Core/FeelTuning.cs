using System;
using UnityEngine;

namespace Skp.Core
{
    /// <summary>One game-feel response: screen shake (trauma 0 to 1), hit-stop and a vibration pulse.</summary>
    [Serializable]
    public struct FeelKick
    {
        [Range(0, 1)] public float shake;
        [Tooltip("Seconds the simulation holds still.")]
        public float hitStop;
        [Tooltip("Vibration length in milliseconds (0 = none).")]
        public int vibrateMs;
        [Range(0, 1)] public float rumble;

        public FeelKick(float shake, float hitStop, int vibrateMs, float rumble)
        { this.shake = shake; this.hitStop = hitStop; this.vibrateMs = vibrateMs; this.rumble = rumble; }
    }

    /// <summary>A particle burst: count, colour, speed, life and sprite.</summary>
    [Serializable]
    public struct FxBurst
    {
        public int count;
        public Color color;
        public float speed;
        public float life;
        [Tooltip("Upward drift (units/s^2, negative falls).")]
        public float rise;
        public Sprite sprite;

        public FxBurst(int count, Color color, float speed, float life, float rise)
        { this.count = count; this.color = color; this.speed = speed; this.life = life; this.rise = rise; sprite = null; }
    }

    /// <summary>
    /// Game feel numbers (Resources/Feel/FeelTuning): shake, hit-stop, vibration per event and the particle bursts.
    /// Built by SkpAudioSetup; edit in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Sa Kabilang Pinto/Feel Tuning", fileName = "FeelTuning")]
    public class FeelTuning : ScriptableObject
    {
        public const string ResourcePath = "Feel/FeelTuning";

        [Header("Screen shake")]
        [Tooltip("Largest camera offset at full trauma (units; 1 = 16 px).")]
        public float maxShake = 0.375f;
        [Tooltip("Trauma lost per second.")]
        public float shakeDecay = 2.2f;
        [Tooltip("Shake jitter frequency (Hz).")]
        public float shakeFrequency = 24f;

        [Header("Events")]
        public FeelKick hurt = new FeelKick(0.45f, 0.07f, 60, 0.5f);
        public FeelKick die = new FeelKick(0.7f, 0.12f, 180, 0.9f);
        public FeelKick kill = new FeelKick(0.2f, 0.05f, 25, 0.25f);
        public FeelKick ward = new FeelKick(0.08f, 0f, 15, 0.15f);
        public FeelKick boltHit = new FeelKick(0.1f, 0.03f, 15, 0.15f);
        public FeelKick burn = new FeelKick(0.15f, 0.04f, 20, 0.2f);
        public FeelKick parry = new FeelKick(0.65f, 0.12f, 120, 0.8f);
        public FeelKick toll = new FeelKick(0.5f, 0.06f, 90, 0.6f);
        public FeelKick bossStruck = new FeelKick(0.3f, 0.06f, 40, 0.4f);
        public FeelKick bossDefeated = new FeelKick(0.8f, 0.2f, 250, 1f);
        public FeelKick bell = new FeelKick(0.18f, 0f, 0, 0.2f);
        public FeelKick crumble = new FeelKick(0.06f, 0f, 0, 0f);
        public FeelKick letter = new FeelKick(0f, 0f, 40, 0.2f);
        [Tooltip("Bells farther than this from Tala (units) do not shake the screen.")]
        public float bellShakeRange = 8f;

        [Header("Particles")]
        public FxBurst flame = new FxBurst(10, new Color(1f, 0.83f, 0.42f), 3.5f, 0.5f, 3f);
        public FxBurst fragment = new FxBurst(14, new Color(0.25f, 0.88f, 0.82f), 4f, 0.7f, 1.5f);
        public FxBurst veil = new FxBurst(16, new Color(0.75f, 0.49f, 1f), 2.5f, 0.6f, 0.5f);
        [Tooltip("Veil motes while it lasts: one every N ticks.")]
        public int veilTrickleTicks = 5;
        public FxBurst bolt = new FxBurst(8, new Color(1f, 0.93f, 0.6f), 5f, 0.3f, 0f);
        public FxBurst burnt = new FxBurst(18, new Color(0.45f, 0.4f, 0.42f), 2.5f, 1.0f, 2f);
        public FxBurst ember = new FxBurst(10, new Color(1f, 0.35f, 0.29f), 3f, 0.6f, 2.5f);
        public FxBurst letterGlow = new FxBurst(16, new Color(0.96f, 0.9f, 0.76f), 3f, 0.8f, 1f);
        [Tooltip("Sprite-unlit material for the pixel particles.")]
        public Material particleMaterial;
        [Tooltip("Particles alive at once per system (fixed pools, no allocation in play).")]
        public int maxParticles = 128;
    }
}
