using Skp.Dimensions;
using Skp.Levels;
using Skp.Player;
using Skp.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Skp.Preview
{
    [CreateAssetMenu(menuName = "Sa Kabilang Pinto/Smooth UI Art")]
    public sealed class SmoothUiArt : ScriptableObject
    {
        const string HudShadeName = "HUD contrast gradient";
        [SerializeField] UiTheme theme;
        [SerializeField] Sprite mapBackdrop, hudFade;
        [SerializeField] LevelPrefabCatalog mapCatalog;
        [SerializeField] PlayerAnimSet girl, boy;
        [SerializeField] Sprite[] lolaFrames;
        [SerializeField] LevelArt environment;
        [SerializeField] LevelPrefabCatalog[] levels;
        [SerializeField] Sprite ward, veil, spiritScenery, invertedScenery;

        public void Activate() => UiTheme.Override = theme;
        public void Deactivate()
        {
            if (UiTheme.Override == theme) UiTheme.Override = null;
        }

        public Sprite Scenery(Dim dimension) => dimension == Dim.Spirit ? spiritScenery :
            dimension == Dim.Inverted ? invertedScenery : null;

        public void Apply(Scene scene, int levelIndex, float aspect, bool highContrast = false, bool configureLevelArt = true)
        {
            var original = UiTheme.LoadDefault();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true)) StyleText(text, original);
                foreach (var image in root.GetComponentsInChildren<Image>(true)) StyleImage(image, original);
                foreach (var chip in root.GetComponentsInChildren<SkillChip>(true))
                {
                    if (chip.icon != null) chip.icon.sprite = theme.skillArt.Get(chip.skillId)?.icon;
                }
                foreach (var button in root.GetComponentsInChildren<PixelButton>(true))
                {
                    // Repaint the existing enabled/disabled color without resetting selection.
                    button.Interactable = button.Interactable;
                    if (button.highlight != null) button.highlight.color = theme.highlight;
                    if (button.arrow != null) button.arrow.color = theme.goldBright;
                }
                foreach (var flow in root.GetComponentsInChildren<TitleFlow>(true)) StyleTitle(flow);
                foreach (var map in root.GetComponentsInChildren<MapController>(true)) StyleMap(map);
                foreach (var runner in root.GetComponentsInChildren<LevelRunner>(true))
                {
                    if (configureLevelArt)
                    {
                        int act = runner.levels[Mathf.Clamp(levelIndex, 0, runner.levels.Length - 1)].spec.act;
                        ApplyLevelArt(runner, act);
                    }
                    AddHudShade(runner, highContrast);
                }
                foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                {
                    var pixelCamera = camera.GetComponent<PixelPerfectCamera>();
                    if (pixelCamera != null) pixelCamera.enabled = false;
                }
            }
            FitTitle(scene, aspect);
        }

        public void FitTitle(Scene scene, float aspect)
        {
            var camera = Camera.main;
            if (camera == null) return;
            foreach (var root in scene.GetRootGameObjects())
            {
                var backdrop = root.GetComponent<SpriteRenderer>();
                if (backdrop == null || backdrop.sprite != theme.menuBackdrop) continue;
                float height = camera.orthographicSize * 2f;
                backdrop.size = new Vector2(height * aspect, height);
            }
        }

        void StyleTitle(TitleFlow flow)
        {
            // The new painting already includes the doorway. Hide the duplicate overlay.
            flow.title.art.enabled = false;
            var heading = flow.title.transform.Find("Column/Logo");
            if (heading != null && heading.TryGetComponent(out TextMeshProUGUI logo))
            {
                logo.fontSize = 34f;
                logo.fontStyle |= FontStyles.Bold;
                logo.color = theme.text;
            }
            if (flow.title.transform.Find("Footer") is RectTransform footer)
            {
                var shade = UiKit.Fill(footer.parent, "Footer contrast", new Color(theme.panel.r, theme.panel.g, theme.panel.b, 0.88f));
                var rect = shade.rectTransform;
                rect.anchorMin = footer.anchorMin;
                rect.anchorMax = footer.anchorMax;
                rect.pivot = footer.pivot;
                rect.sizeDelta = footer.sizeDelta + new Vector2(12f, 2f);
                rect.anchoredPosition = footer.anchoredPosition;
                rect.SetSiblingIndex(footer.GetSiblingIndex());
            }
            foreach (var root in flow.gameObject.scene.GetRootGameObjects())
            {
                if (root.name != "Background" || !root.TryGetComponent(out SpriteRenderer backdrop)) continue;
                backdrop.sprite = theme.menuBackdrop;
                backdrop.drawMode = SpriteDrawMode.Sliced;
                backdrop.color = new Color(0.86f, 0.84f, 0.92f, 1f);
            }
        }

        void StyleMap(MapController map)
        {
            map.background.sprite = mapBackdrop;
            map.background.drawMode = SpriteDrawMode.Sliced;
            map.background.size = new Vector2(MapModel.MapWidth / 16f, MapModel.MapHeight / 16f);
            map.catalog = mapCatalog;
            map.font = theme.font;
            map.girlFrames = MapFrames(girl);
            map.boyFrames = MapFrames(boy);
            map.lolaFrames = lolaFrames;
        }

        static Sprite[] MapFrames(PlayerAnimSet set)
        {
            var run = set.Get(TalaAnim.Run).frames;
            return new[] { set.Frame(TalaAnim.Idle, 0), run[0], run[run.Length / 2], set.Frame(TalaAnim.JumpRise, 0) };
        }

        public void ApplyLevelArt(LevelRunner runner, int act)
        {
            runner.view.art = environment;
            runner.view.catalog = levels[Mathf.Clamp(act - 1, 0, levels.Length - 1)];
            var player = runner.player.view;
            player.girlAnims = girl;
            player.boyAnims = boy;
            player.wardRing.sprite = ward;
            player.veilRing.sprite = veil;
            player.boltPrefab = runner.view.catalog.bolt;
        }

        void AddHudShade(LevelRunner runner, bool highContrast)
        {
            var hud = runner.ui.hud;
            var shade = UiKit.Rect(hud.transform, HudShadeName);
            shade.SetAsFirstSibling();
            shade.anchorMin = new Vector2(0f, 1f);
            shade.anchorMax = Vector2.one;
            shade.offsetMin = new Vector2(0f, -118f);
            shade.offsetMax = Vector2.zero;
            var image = shade.gameObject.AddComponent<Image>();
            image.sprite = hudFade;
            image.raycastTarget = false;
            SetHudContrast(runner, highContrast);
        }

        public void SetHudContrast(LevelRunner runner, bool highContrast)
        {
            if (runner == null) return;
            var shade = runner.ui.hud.transform.Find(HudShadeName);
            if (shade != null && shade.TryGetComponent(out Image image))
                image.color = new Color(1f, 1f, 1f, highContrast ? 1f : 0.85f);
        }

        void StyleText(TextMeshProUGUI text, UiTheme original)
        {
            if (text.font != original.font) return;
            bool outlined = text.fontSharedMaterial == original.fontOutline || text.outlineWidth > 0.01f;
            text.font = theme.font;
            text.fontSharedMaterial = outlined ? theme.fontOutline : theme.font.material;
            text.fontSize *= 1.25f;
            if (text.enableAutoSizing)
            {
                text.fontSizeMin *= 1.25f;
                text.fontSizeMax *= 1.25f;
            }
            text.lineSpacing = 3f;
            text.color = ColorFor(text.color, original);
        }

        void StyleImage(Image image, UiTheme original)
        {
            Sprite existing = image.sprite;
            if (existing == original.pixelBox) image.sprite = theme.pixelBox;
            else if (existing == original.heartFull) image.sprite = theme.heartFull;
            else if (existing == original.heartEmpty) image.sprite = theme.heartEmpty;
            else if (existing == original.cardGirl) image.sprite = theme.cardGirl;
            else if (existing == original.cardBoy) image.sprite = theme.cardBoy;
            else if (existing == original.portraitLola) image.sprite = theme.portraitLola;
            else if (existing == original.portraitGirl) image.sprite = theme.portraitGirl;
            else if (existing == original.portraitBoy) image.sprite = theme.portraitBoy;
            else if (existing == original.portraitClue) image.sprite = theme.portraitClue;
            else if (existing == original.portraitAnino) image.sprite = theme.portraitAnino;
            else if (existing == original.portraitBoss) image.sprite = theme.portraitBoss;
            else if (existing == original.titleArt) image.sprite = theme.titleArt;
            if (image.name == "BrassInlay") image.enabled = false;
            image.color = ColorFor(image.color, original);
        }

        Color ColorFor(Color value, UiTheme original)
        {
            Color replacement;
            if (Matches(value, original.text)) replacement = theme.text;
            else if (Matches(value, original.textDim)) replacement = theme.textDim;
            else if (Matches(value, original.textDisabled)) replacement = theme.textDisabled;
            else if (Matches(value, original.gold)) replacement = theme.gold;
            else if (Matches(value, original.goldBright)) replacement = theme.goldBright;
            else if (Matches(value, original.panel)) replacement = theme.panel;
            else if (Matches(value, original.highlight)) replacement = theme.highlight;
            else if (Matches(value, original.shadow)) replacement = theme.shadow;
            else if (Matches(value, original.scrim)) replacement = theme.scrim;
            else return value;
            replacement.a = Matches(value, original.scrim) ? theme.scrim.a : value.a;
            return replacement;
        }

        static bool Matches(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.003f && Mathf.Abs(a.g - b.g) < 0.003f && Mathf.Abs(a.b - b.b) < 0.003f;
    }
}
