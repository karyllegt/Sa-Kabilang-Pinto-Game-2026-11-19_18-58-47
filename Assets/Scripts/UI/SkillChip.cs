using Skp.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Skp.UI
{
    /// <summary>
    /// One HUD skill chip. Active skills (veil, bolt) show the key letter, a short label and a dark cooldown
    /// fill that drains upward as the skill recharges; the outline turns gold when ready and bright while the
    /// skill is active. Passive skills and upgrades are a small badge with the skill's icon.
    /// </summary>
    public class SkillChip : MonoBehaviour
    {
        public string skillId;
        public bool active;
        public Image border, ready, inner, cooldown, icon;
        public TextMeshProUGUI key, label;
        public LayoutElement layout;

        public const float ActiveWidth = 64, ActiveTall = 56, ActiveShort = 36;
        public const float BadgeTall = 36, BadgeShort = 28;

        /// <summary>0 = ready, 1 = just used.</summary>
        public float CooldownFraction { get; private set; }
        public bool IsReady { get; private set; }
        public bool IsOn { get; private set; }

        Color _color;

        public static SkillChip Build(Transform parent, SkillDefinition def, string id, bool isActive)
        {
            var th = UiKit.Theme;
            Color col = def != null ? def.color : th.gold;
            var rt = UiKit.Rect(parent, "Chip_" + id);
            var c = rt.gameObject.AddComponent<SkillChip>();
            c.skillId = id;
            c.active = isActive;
            c._color = col;
            c.layout = rt.gameObject.AddComponent<LayoutElement>();

            c.ready = UiKit.Fill(rt, "Ready", th.goldBright);
            UiKit.Stretch(c.ready.rectTransform);
            c.border = UiKit.Fill(rt, "Border", col);
            UiKit.Stretch(c.border.rectTransform, 2, 2, 2, 2);
            c.inner = UiKit.Fill(rt, "Inner", th.panel);
            UiKit.Stretch(c.inner.rectTransform, 4, 4, 4, 4);
            if (isActive)
            {
                c.cooldown = UiKit.Fill(c.inner.transform, "Cooldown", new Color(0, 0, 0, 0.72f));
                UiKit.Stretch(c.cooldown.rectTransform);
                c.cooldown.enabled = false;
                c.key = UiKit.Label(rt, "Key", def != null && !string.IsNullOrEmpty(def.keyHint) ? def.keyHint : "?", th.smallSize, th.text, TextAlignmentOptions.Top, false);
                UiKit.Stretch(c.key.rectTransform, 4, 4, 4, 6);
                c.label = UiKit.Label(rt, "Label", def != null ? def.shortLabel : id.ToUpperInvariant(), th.tinySize, th.textDim, TextAlignmentOptions.Bottom, false);
                UiKit.Stretch(c.label.rectTransform, 4, 6, 4, 4);
                foreach (var t in new[] { c.key, c.label }) { t.outlineWidth = 0.25f; t.outlineColor = new Color32(0, 0, 0, 255); }
            }
            else
            {
                c.ready.enabled = false;
                c.icon = UiKit.Fill(rt, "Icon", Color.white);
                c.icon.sprite = def != null ? def.icon : null;
                c.icon.preserveAspect = true;
                UiKit.Stretch(c.icon.rectTransform, 6, 6, 6, 6);
            }
            c.SetTall(true);
            return c;
        }

        /// <summary>Touch layout: chips at least 48 dp tall (56 reference units); keyboard layout: compact.</summary>
        public void SetTall(bool tall)
        {
            float w = active ? ActiveWidth : (tall ? BadgeTall : BadgeShort);
            float h = active ? (tall ? ActiveTall : ActiveShort) : (tall ? BadgeTall : BadgeShort);
            layout.preferredWidth = layout.minWidth = w;
            layout.preferredHeight = layout.minHeight = h;
            ((RectTransform)transform).sizeDelta = new Vector2(w, h);
        }

        /// <summary>Cooldown state of an active chip.</summary>
        public void Set(float cooldownFraction, bool isReady, bool on)
        {
            if (!active) return;
            cooldownFraction = Mathf.Clamp01(cooldownFraction);
            if (Mathf.Approximately(cooldownFraction, CooldownFraction) && isReady == IsReady && on == IsOn) return;
            CooldownFraction = cooldownFraction;
            IsReady = isReady;
            IsOn = on;
            // the dark fill is anchored to the top and shrinks upward as the skill recharges
            var cr = cooldown.rectTransform;
            cr.anchorMin = new Vector2(0, 1f - cooldownFraction);
            cooldown.enabled = cooldownFraction > 0.001f;
            var th = UiKit.Theme;
            ready.enabled = isReady || on;
            ready.color = on ? th.text : th.goldBright;
            border.color = on ? th.text : _color;
        }
    }
}
