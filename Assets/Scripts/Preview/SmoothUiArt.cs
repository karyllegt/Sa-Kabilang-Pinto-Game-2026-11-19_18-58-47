using Skp.Dimensions;
using Skp.Levels;
using Skp.Player;
using Skp.Skills;
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
        [SerializeField] Material sceneryGrade;
        [SerializeField] SpriteRenderer talaRim, mapLabelBacking;
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
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true)) StyleText(text, original);
                foreach (var image in root.GetComponentsInChildren<Image>(true)) StyleImage(image, original);
                foreach (var chip in root.GetComponentsInChildren<SkillChip>(true))
                {
                    StyleSkill(chip);
                }
                foreach (var button in root.GetComponentsInChildren<PixelButton>(true))
                {
                    if (button.arrow != null)
                    {
                        button.arrow.text = "";
                        button.selectionMark = Mark(button.arrow.transform, LanternMark.Capiz, "Focus capiz",
                            new Vector2(0.5f, 0.5f), Vector2.zero, 23);
                    }
                    button.UseLanternFace(theme);
                }
            }
            // Finish the shared theme pass before styling named widgets. The level
            // runner and the level canvas can live on different scene roots.
            foreach (var root in roots)
            {
                foreach (var dialog in root.GetComponentsInChildren<DialogBox>(true)) StyleDialog(dialog);
                foreach (var flow in root.GetComponentsInChildren<TitleFlow>(true)) StyleTitle(flow);
                foreach (var map in root.GetComponentsInChildren<MapController>(true)) StyleMap(map);
                foreach (var runner in root.GetComponentsInChildren<LevelRunner>(true))
                {
                    if (configureLevelArt)
                    {
                        int act = runner.levels[Mathf.Clamp(levelIndex, 0, runner.levels.Length - 1)].spec.act;
                        ApplyLevelArt(runner, act);
                    }
                    StyleHud(runner.ui.hud);
                    StyleTouch(runner.ui.touch);
                    StyleModal(runner.ui.pause);
                    StyleModal(runner.ui.clear);
                    runner.ui.UseLanternHud();
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
                Display(logo, 44);
                logo.color = theme.goldBright;
                logo.lineSpacing = 7;
                var glow = logo.fontMaterial;
                glow.EnableKeyword("UNDERLAY_ON");
                glow.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0.68f, 0.31f, 0.13f, 0.85f));
                glow.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.18f);
                glow.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.48f);
                logo.UpdateMeshPadding();
                var lines = logo.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 2)
                    logo.text = $"<size=20>{lines[0]}</size>\n{lines[1]}";
                var column = (RectTransform)heading.parent;
                column.anchoredPosition = new Vector2(24, -12);
                column.GetComponent<VerticalLayoutGroup>().spacing = 5;
                var menu = column.Find("MenuBox");
                menu.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(14, 14, 10, 10);
                Mark(menu, LanternMark.Weave, "Woven menu trim", new Vector2(1, 1), new Vector2(-26, -17), 24);
                Mark(column, LanternMark.Parol, "Wordmark parol", new Vector2(0, 1), new Vector2(301, -23), 40);
                var plate = UiKit.Box(column, "Wordmark plaque", 1.3f);
                plate.raycastTarget = false;
                plate.transform.Find("BrassInlay").gameObject.SetActive(false);
                plate.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                plate.rectTransform.anchorMin = new Vector2(0, 1);
                plate.rectTransform.anchorMax = Vector2.one;
                plate.rectTransform.offsetMin = new Vector2(-11, -109);
                plate.rectTransform.offsetMax = new Vector2(0, 7);
                plate.transform.SetAsFirstSibling();
            }
            if (flow.title.transform.Find("Footer") is RectTransform footer)
            {
                var label = footer.GetComponent<TextMeshProUGUI>();
                label.fontSize = 17;
                label.color = theme.text;
                UiKit.Place(footer, new Vector2(1, 0), new Vector2(1, 0),
                    new Vector2(-14, 12), new Vector2(415, 29));
                var shade = UiKit.Box(footer.parent, "Footer contrast", 2.4f);
                shade.raycastTarget = false;
                shade.transform.Find("BrassInlay").gameObject.SetActive(false);
                UiKit.Place(shade.rectTransform, footer.anchorMin, footer.pivot, footer.anchoredPosition,
                    footer.sizeDelta);
                shade.transform.SetSiblingIndex(footer.GetSiblingIndex());
            }
            foreach (var root in flow.gameObject.scene.GetRootGameObjects())
            {
                if (root.name != "Background" || !root.TryGetComponent(out SpriteRenderer backdrop)) continue;
                backdrop.sprite = theme.menuBackdrop;
                backdrop.drawMode = SpriteDrawMode.Sliced;
                backdrop.color = Color.white;
                Grade(backdrop, 1);
            }
        }

        void StyleMap(MapController map)
        {
            map.background.sprite = mapBackdrop;
            Grade(map.background, 0);
            map.background.drawMode = SpriteDrawMode.Sliced;
            map.background.size = new Vector2(MapModel.MapWidth / 16f, MapModel.MapHeight / 16f);
            map.catalog = mapCatalog;
            map.font = theme.font;
            map.lanternTheme = theme;
            map.lanternLabelBacking = mapLabelBacking;
            map.dialog.coveredCard = map.screen.infoPanel.gameObject;
            map.screen.skills.Text = Loc.Get("skills.title");
            var header = UiKit.Box(map.screen.transform, "Map header frame", 1.15f);
            header.raycastTarget = false;
            header.transform.Find("BrassInlay").gameObject.SetActive(false);
            UiKit.Stretch(header.rectTransform, 6, 0, 6, 4);
            header.rectTransform.anchorMin = new Vector2(0, 1);
            header.rectTransform.offsetMin = new Vector2(6, -76);
            header.transform.SetSiblingIndex(map.screen.topBar.GetSiblingIndex());
            Display(map.screen.levelName, 22);
            Mark(map.screen.infoPanel, LanternMark.Parol, "Map card parol", new Vector2(0, 0.5f),
                new Vector2(36, 0), 34);
            var infoText = (RectTransform)map.screen.levelName.transform.parent;
            infoText.offsetMin = new Vector2(62, infoText.offsetMin.y);

            map.girlFrames = MapFrames(girl);
            map.boyFrames = MapFrames(boy);
            map.lolaFrames = lolaFrames;
        }

        void Grade(SpriteRenderer backdrop, float warmth)
        {
            if (sceneryGrade == null) return;
            backdrop.sharedMaterial = sceneryGrade;
            var properties = new MaterialPropertyBlock();
            backdrop.GetPropertyBlock(properties);
            properties.SetFloat("_Title", warmth);
            backdrop.SetPropertyBlock(properties);
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
            player.UseLanternRim(talaRim);
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
            shade.offsetMin = new Vector2(0f, -180f);
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

        void StyleHud(HudView hud)
        {
            TextBacking(hud.levelName.transform.parent, "Level label ink");
            TextBacking(hud.levelLabel.transform.parent, "Score label ink");
            var skillsPosition = hud.skillsColumn.anchoredPosition;
            hud.skillsColumn.anchoredPosition = new Vector2(skillsPosition.x, -67);
            UiKit.Size(hud.levelName, 268);
            hud.levelName.fontSize = 22;
            hud.levelName.color = theme.goldBright;
            hud.levelName.fontStyle |= FontStyles.Bold;
            hud.dimension.fontSize = 16;
            hud.flameText.fontSize = 16;
            hud.objective.fontSize = 16;
            hud.flameFill.sprite = theme.white;
            UiKit.Size(hud.flameBar, 224, 23);
            var readout = (RectTransform)hud.flameBar.Find("NumberBack");
            readout.sizeDelta = new Vector2(138, 21);
            readout.GetComponent<Image>().color = theme.panel;
            Mark(hud.flameBar, LanternMark.Flame, "Flame seal", new Vector2(0, 0.5f), new Vector2(-12, 0), 23);
            foreach (var heart in hud.hearts) UiKit.Size(heart, 27, 27);
            var objective = hud.objectiveBox.GetComponent<Image>();
            objective.sprite = theme.pixelBox;
            objective.type = Image.Type.Sliced;
            objective.pixelsPerUnitMultiplier = 2.3f;
            objective.color = Color.white;
        }

        void TextBacking(Transform parent, string name)
        {
            var ink = UiKit.Fill(parent, name, theme.panel);
            ink.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UiKit.Stretch(ink.rectTransform, -5, -2, -5, -2);
            ink.transform.SetAsFirstSibling();
        }

        void StyleSkill(SkillChip chip)
        {
            var def = theme.skillArt.Get(chip.skillId);
            if (def == null) return;
            if (!chip.active)
            {
                if (chip.icon != null)
                {
                    chip.icon.sprite = def.icon;
                    UiKit.Stretch(chip.icon.rectTransform, 4, 4, 4, 4);
                }
                return;
            }
            var icon = UiKit.Picture(chip.transform, "Active skill icon", def.icon, new Vector2(27, 27));
            UiKit.Place(icon.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f),
                new Vector2(18, -2), new Vector2(27, 27));
            UiKit.Stretch(chip.key.rectTransform, 32, 4, 4, 5);
            chip.key.fontSize = 16;
            chip.label.fontSize = 15;
        }

        void StyleTouch(TouchControls touch)
        {
            if (touch == null) return;
            foreach (var slot in touch.slots)
            {
                if (slot.rect == null || !System.Enum.TryParse(slot.rect.name, out LanternMark mark) ||
                    (int)mark > (int)LanternMark.Door) continue;
                var face = slot.rect.GetComponent<Image>();
                face.color = Color.white;
                face.pixelsPerUnitMultiplier = 1.5f;
                var label = slot.rect.GetComponentInChildren<TextMeshProUGUI>(true);
                bool movement = mark == LanternMark.Left || mark == LanternMark.Right;
                if (label != null)
                {
                    label.enabled = !movement;
                    label.fontSize = 16;
                    label.fontStyle |= FontStyles.Bold;
                    UiKit.Stretch(label.rectTransform, 0, 4, 0, 0);
                    label.alignment = TextAlignmentOptions.Bottom;
                }
                var icon = Mark(slot.rect, mark, "Touch symbol", new Vector2(0.5f, 0.5f),
                    new Vector2(0, movement ? 0 : 9), movement ? 54 : 43);
                var accent = mark == LanternMark.Ward ? theme.teal : mark == LanternMark.Veil ? theme.violet : theme.goldBright;
                var style = slot.rect.gameObject.AddComponent<LanternTouchFace>();
                style.Initialize(face, icon, label, theme, accent);
            }
            touch.CacheLanternFaces();
        }

        void StyleDialog(DialogBox dialog)
        {
            dialog.box.anchorMin = new Vector2(0.5f, 0);
            dialog.box.anchorMax = new Vector2(0.5f, 0);
            dialog.box.sizeDelta = new Vector2(700, 172);
            dialog.box.anchoredPosition = new Vector2(0, 12);
            var row = dialog.box.Find("Row").GetComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(17, 17, 12, 12);
            row.spacing = 14;
            var portraitFrame = dialog.portrait.transform.parent.GetComponent<Image>();
            UiKit.Size(portraitFrame, 112, 83);
            Mark(portraitFrame.transform, LanternMark.Capiz, "Portrait capiz", new Vector2(1, 1),
                new Vector2(-10, -11), 22);
            dialog.sub.fontSize = 15;
            dialog.sub.color = theme.teal;
            Display(dialog.speaker, 20);
            dialog.body.fontSize = 18;
        }

        void StyleModal(UiScreen screen)
        {
            if (screen.transform.Find("Scrim") is RectTransform scrim)
            {
                // The content remains inside the safe area. Only the dimmer reaches the screen edges.
                scrim.anchorMin = new Vector2(-1, -1);
                scrim.anchorMax = new Vector2(2, 2);
            }
            var box = screen.transform.Find("Box");
            if (box == null) return;
            Display(box.Find("Title")?.GetComponent<TextMeshProUGUI>(), 26);
            Mark(box, LanternMark.Trim, "Carved header trim", new Vector2(0, 1), new Vector2(36, -35), 35);
            Mark(box, LanternMark.Trim, "Carved header trim", new Vector2(1, 1), new Vector2(-36, -35), 35);
        }

        void Display(TextMeshProUGUI label, float size)
        {
            if (label == null) return;
            if (theme.displayFont != null)
            {
                label.font = theme.displayFont;
                label.fontSharedMaterial = theme.displayFont.material;
            }
            label.fontSize = size;
        }

        Image Mark(Transform parent, LanternMark mark, string name, Vector2 anchor, Vector2 offset, float size)
        {
            var extent = Vector2.one * size;
            var image = UiKit.Picture(parent, name, theme.Mark(mark), extent);
            image.GetComponent<LayoutElement>().ignoreLayout = true;
            UiKit.Place(image.rectTransform, anchor, new Vector2(0.5f, 0.5f), offset, extent);
            return image;
        }

        void StyleText(TextMeshProUGUI text, UiTheme original)
        {
            if (text.font != original.font) return;
            bool outlined = text.fontSharedMaterial == original.fontOutline || text.outlineWidth > 0.01f;
            text.font = theme.font;
            text.fontSharedMaterial = outlined ? theme.fontOutline : theme.font.material;
            text.fontSize = Mathf.Max(theme.tinySize, text.fontSize * 1.25f);
            if (text.enableAutoSizing)
            {
                text.fontSizeMin = Mathf.Max(theme.tinySize, text.fontSizeMin * 1.25f);
                text.fontSizeMax = Mathf.Max(text.fontSizeMin, text.fontSizeMax * 1.25f);
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
