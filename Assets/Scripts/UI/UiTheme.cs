using TMPro;
using UnityEngine;
using Skp.Skills;

namespace Skp.UI
{
    /// <summary>
    /// The one visual language of every screen: the pixel font, the shared PixelBox 9-slice, hearts,
    /// portraits and the palette (docs/04_ASSET_GUIDE.md). Created by the setup at
    /// Assets/Resources/UI/UiTheme.asset; screens never pick their own colors or sprites.
    /// </summary>
    [CreateAssetMenu(menuName = "Sa Kabilang Pinto/UI Theme", fileName = "UiTheme")]
    public class UiTheme : ScriptableObject
    {
        public const string ResourcePath = "UI/UiTheme";

        public TMP_FontAsset font;
        [Tooltip("The pixel font with a black outline, for world-space labels (map node numbers).")]
        public Material fontOutline;
        [Tooltip("ui_pixelbox_9slice_border6: the only panel and button frame.")]
        public Sprite pixelBox;
        public Sprite heartFull, heartEmpty;
        public Sprite white;
        public Sprite portraitLola, portraitBoy, portraitGirl, portraitClue, portraitAnino, portraitBoss;
        public Sprite titleArt, cardBoy, cardGirl, menuBackdrop;
        [Tooltip("Optional display definitions for this theme. Gameplay tuning is still read from the gameplay library.")]
        public SkillLibrary skillArt;

        [Header("Palette")]
        public Color panel = new Color32(0x14, 0x10, 0x1c, 0xff);
        public Color gold = new Color32(0xe8, 0xb8, 0x5a, 0xff);
        public Color goldBright = new Color32(0xff, 0xd3, 0x6a, 0xff);
        public Color teal = new Color32(0x41, 0xe0, 0xd0, 0xff);
        public Color violet = new Color32(0xc0, 0x7d, 0xff, 0xff);
        public Color danger = new Color32(0xff, 0x5a, 0x4a, 0xff);
        public Color text = new Color32(0xf6, 0xe7, 0xc1, 0xff);
        [Tooltip("Secondary text; still at least 7:1 against the panel.")]
        public Color textDim = new Color32(0xc9, 0xbe, 0xd8, 0xff);
        public Color textDisabled = new Color32(0x8a, 0x80, 0x96, 0xff);
        public Color shadow = new Color32(0x0a, 0x07, 0x10, 0xff);
        [Tooltip("Fill of the highlighted (selected) row or button.")]
        public Color highlight = new Color32(0x4a, 0x36, 0x1c, 0xff);
        [Tooltip("Screen dimmer behind modal boxes.")]
        public Color scrim = new Color(0.02f, 0.01f, 0.04f, 0.72f);

        [Header("Type sizes at the 960x540 reference")]
        public float titleSize = 28, headingSize = 20, bodySize = 14, smallSize = 12, tinySize = 10;
        [Tooltip("PixelBox border scale: 0.5 draws the 6 px border at 12 px (2x the 480x270 grid).")]
        public float boxScale = 0.5f;
        [Tooltip("Buttons use a thinner frame (6 px) so labels keep room.")]
        public float buttonBoxScale = 1f;
        [Tooltip("Minimum touch target in density-independent pixels.")]
        public float minTouchDp = 48;

        static UiTheme _cached;
        internal static UiTheme Override { get; set; }

        /// <summary>Forget the cached theme (the setup calls this after creating or editing the asset).</summary>
        public static void ClearCache() => _cached = null;

        /// <summary>The theme asset (Resources), or built-in defaults when it is missing (tests).</summary>
        public static UiTheme Load() => Override != null ? Override : LoadDefault();

        internal static UiTheme LoadDefault()
        {
            if (_cached != null) return _cached;
            _cached = Resources.Load<UiTheme>(ResourcePath);
            if (_cached == null)
            {
                _cached = CreateInstance<UiTheme>();
                _cached.font = Resources.Load<TMP_FontAsset>("Fonts/PressStart2P SDF");
            }
            return _cached;
        }

        /// <summary>WCAG relative contrast ratio between two opaque colors (1..21).</summary>
        public static float Contrast(Color a, Color b)
        {
            float la = Luminance(a), lb = Luminance(b);
            if (la < lb) (la, lb) = (lb, la);
            return (la + 0.05f) / (lb + 0.05f);
        }

        public static float Luminance(Color c)
        {
            float L(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * L(c.r) + 0.7152f * L(c.g) + 0.0722f * L(c.b);
        }
    }
}
