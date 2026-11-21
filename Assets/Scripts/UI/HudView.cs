using System.Collections.Generic;
using Skp.Dimensions;
using Skp.Levels;
using Skp.Player;
using Skp.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Skp.UI
{
    /// <summary>
    /// Gameplay HUD in the top band of the safe area: level name and dimension (left) with the skill chips
    /// under them, hearts and flame bar (center), level and difficulty, lives and score (right), the pause
    /// button top-right, the objective line under it (its own lane, the toast has the next one) and the boss bar
    /// (boss level only). While chips show, the objective and toast lanes narrow so they never reach the chip
    /// column. Values are written only when they change.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        public const int MaxHearts = 8;
        public const float ObjectiveTop = 84f;
        public const float SkillsTop = 48f, SideMargin = 14f, LaneGap = 12f, ObjectivePad = 14f;

        public TextMeshProUGUI levelName, dimension, levelLabel, livesScore, flameText, objective, bossName;
        public Image[] hearts = new Image[MaxHearts];
        public Image flameFill;
        public RectTransform flameBar, objectiveBox, bossBar;
        public Image bossFill;
        public PixelButton pause;
        public RectTransform skillsColumn, chipRow, badgeGrid;
        [Tooltip("One chip per skill, in SkillIds.All order.")]
        public SkillChip[] chips = new SkillChip[0];

        string _skillKey;
        bool _tall = true;
        float _laneFor = -1;
        /// <summary>Distance from the top of the HUD to the bottom of the objective line.</summary>
        public float LaneBottom { get; private set; } = ObjectiveTop + 26f;
        /// <summary>Widest the centered objective and toast may be.</summary>
        public float LaneWidth { get; private set; } = 9999f;
        public bool TallChips => _tall;

        int _vit = -1, _maxVit = -1, _flame = -1, _maxFlame = -1, _score = -1, _lives = -2;
        Dim _dim = (Dim)(-1);
        string _objective;

        public static HudView Build(Transform safe)
        {
            var th = UiKit.Theme;
            var root = UiKit.Rect(safe, "Hud");
            UiKit.Stretch(root);
            var h = root.gameObject.AddComponent<HudView>();

            var left = UiKit.VStack(root, "Left", 4, null, TextAnchor.UpperLeft);
            UiKit.Place((RectTransform)left.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -10), new Vector2(270, 60));
            UiKit.Fit(left, false, true);
            h.levelName = UiKit.Text(left.transform, "LevelName", "Fiesta Night", th.smallSize, th.text, TextAlignmentOptions.Left);
            h.levelName.textWrappingMode = TextWrappingModes.NoWrap;
            h.dimension = UiKit.Text(left.transform, "Dimension", "WAKING WORLD", th.tinySize, th.gold, TextAlignmentOptions.Left);

            // skill chips: active skills on the first row, passive badges in a grid of three under them
            var sk = UiKit.VStack(root, "Skills", 4, null, TextAnchor.UpperLeft);
            h.skillsColumn = (RectTransform)sk.transform;
            UiKit.Place(h.skillsColumn, new Vector2(0, 1), new Vector2(0, 1), new Vector2(SideMargin, -SkillsTop), new Vector2(140, 10));
            UiKit.Fit(sk);
            var chipRow = UiKit.HStack(sk.transform, "Active", 6, null, TextAnchor.UpperLeft);
            h.chipRow = (RectTransform)chipRow.transform;
            var grid = UiKit.Rect(sk.transform, "Badges");
            h.badgeGrid = grid;
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(SkillChip.BadgeTall, SkillChip.BadgeTall);
            gl.spacing = new Vector2(4, 4);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 3;
            gl.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gl.childAlignment = TextAnchor.UpperLeft;
            UiKit.Fit(gl);
            var lib = SkillLibrary.Load();
            var list = new List<SkillChip>();
            foreach (var id in SkillIds.All)
            {
                bool act = SkillIds.IsActive(id);
                var chip = SkillChip.Build(act ? chipRow.transform : grid, lib != null ? lib.Get(id) : null, id, act);
                chip.gameObject.SetActive(false);
                list.Add(chip);
            }
            h.chips = list.ToArray();
            sk.gameObject.SetActive(false);

            var mid = UiKit.VStack(root, "Center", 6, null, TextAnchor.UpperCenter);
            UiKit.Place((RectTransform)mid.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(232, 60));
            var row = UiKit.HStack(mid.transform, "Hearts", 2);
            for (int i = 0; i < MaxHearts; i++)
            {
                h.hearts[i] = UiKit.Picture(row.transform, "Heart" + i, th.heartFull, new Vector2(24, 24));
                h.hearts[i].gameObject.SetActive(i < 4);
            }
            var bar = UiKit.Fill(mid.transform, "FlameBar", th.shadow);
            h.flameBar = bar.rectTransform;
            UiKit.Size(bar, 224, 18);
            var outline = UiKit.Fill(bar.transform, "Edge", th.gold);
            UiKit.Stretch(outline.rectTransform);
            var back = UiKit.Fill(bar.transform, "Back", th.panel);
            UiKit.Stretch(back.rectTransform, 2, 2, 2, 2);
            h.flameFill = UiKit.Fill(bar.transform, "Fill", new Color32(0xe8, 0x9a, 0x3a, 0xff));
            var frt = h.flameFill.rectTransform;
            frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(1, 1);
            frt.offsetMin = new Vector2(2, 2); frt.offsetMax = new Vector2(-2, -2);
            h.flameText = UiKit.Label(bar.transform, "Text", "FLAME 100", th.tinySize, th.text, TextAlignmentOptions.Center, false);
            UiKit.Stretch(h.flameText.rectTransform);
            h.flameText.outlineWidth = 0.2f;
            h.flameText.outlineColor = new Color32(0, 0, 0, 255);

            h.pause = UiKit.Button(root, "Pause", "II", new Vector2(UiKit.MinTarget, UiKit.MinTarget), th.bodySize);
            UiKit.Place((RectTransform)h.pause.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -8), new Vector2(UiKit.MinTarget, UiKit.MinTarget));

            var right = UiKit.VStack(root, "Right", 6, null, TextAnchor.UpperRight);
            UiKit.Place((RectTransform)right.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12 - UiKit.MinTarget - 12, -12), new Vector2(204, 40));
            UiKit.Fit(right, false, true);
            h.levelLabel = UiKit.Text(right.transform, "Level", "1 / 9 - Standard", th.tinySize, th.text, TextAlignmentOptions.Right);
            h.livesScore = UiKit.Text(right.transform, "LivesScore", "Lives 5  Score 0", th.tinySize, th.text, TextAlignmentOptions.Right);

            var obj = UiKit.Fill(root, "Objective", new Color(0.03f, 0.02f, 0.05f, 0.9f));
            h.objectiveBox = obj.rectTransform;
            UiKit.Place(h.objectiveBox, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -ObjectiveTop), new Vector2(560, 26));
            var oh = obj.gameObject.AddComponent<HorizontalLayoutGroup>();
            oh.padding = new RectOffset(14, 14, 6, 6);
            oh.childControlWidth = oh.childControlHeight = true;
            oh.childForceExpandWidth = false;
            UiKit.Fit(oh);
            h.objective = UiKit.Text(obj.transform, "Text", "", th.tinySize, th.text);
            h.objective.textWrappingMode = TextWrappingModes.NoWrap;

            var boss = UiKit.Box(root, "BossBar", th.buttonBoxScale);
            h.bossBar = boss.rectTransform;
            UiKit.Place(h.bossBar, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(300, 44));
            h.bossName = UiKit.Label(boss.transform, "Name", Loc.Get("boss.name"), th.tinySize, th.danger, TextAlignmentOptions.Top, false);
            UiKit.Stretch(h.bossName.rectTransform, 8, 20, 8, 6);
            h.bossFill = UiKit.Fill(boss.transform, "Fill", th.danger);
            var brt = h.bossFill.rectTransform;
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0);
            brt.offsetMin = new Vector2(10, 8); brt.offsetMax = new Vector2(-10, 16);
            boss.gameObject.SetActive(false);
            // HUD text floats over the moving world: a black outline keeps it readable on any ledge or backdrop
            foreach (var t in new[] { h.levelName, h.dimension, h.levelLabel, h.livesScore, h.objective })
            {
                t.outlineWidth = 0.25f;
                t.outlineColor = new Color32(0, 0, 0, 255);
            }
            return h;
        }

        public void SetLevel(LevelSession s)
        {
            levelName.text = s.Spec.name;
            string diff = Loc.Get("diff." + (s.DifficultyKey == "endless" ? "endless" : s.DifficultyKey));
            diff = diff.Length > 1 ? diff.Substring(0, 1) + diff.Substring(1).ToLowerInvariant() : diff;
            levelLabel.text = s.Level >= 0 ? Loc.Format("hud.level", s.Level + 1, diff) : diff;
            _vit = _maxVit = _flame = _maxFlame = _score = -1; _lives = -2; _dim = (Dim)(-1); _objective = null; _skillKey = null; _laneFor = -1;
        }

        /// <summary>Writes the HUD from the simulation; cheap when nothing changed.</summary>
        public void Show(LevelSession s)
        {
            var sim = s.Sim;
            var th = UiKit.Theme;
            if (sim.MaxVitality != _maxVit || sim.Vitality != _vit)
            {
                _maxVit = sim.MaxVitality; _vit = sim.Vitality;
                for (int i = 0; i < hearts.Length; i++)
                {
                    hearts[i].gameObject.SetActive(i < _maxVit);
                    hearts[i].sprite = i < _vit ? th.heartFull : th.heartEmpty;
                }
            }
            if (sim.Flame != _flame || sim.MaxFlame != _maxFlame)
            {
                _flame = sim.Flame; _maxFlame = sim.MaxFlame;
                flameFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(_flame / (float)Mathf.Max(1, _maxFlame)), 1);
                flameFill.color = _flame < 25 ? th.danger : (Color)new Color32(0xe8, 0x9a, 0x3a, 0xff);
                flameText.text = Loc.Format("hud.flame", _flame);
            }
            int lives = sim.InfiniteLives ? -1 : sim.Lives;
            if (lives != _lives || sim.Score != _score)
            {
                _lives = lives; _score = sim.Score;
                livesScore.text = lives < 0 ? Loc.Format("hud.lives.inf", _score) : Loc.Format("hud.lives", lives, _score);
            }
            if (sim.Dimension != _dim)
            {
                _dim = sim.Dimension;
                dimension.text = Loc.Get("hud.dim." + (int)_dim);
                dimension.color = _dim == Dim.Spirit ? th.teal : _dim == Dim.Inverted ? th.violet : th.gold;
            }
            ShowSkills(sim);
            var o = Objective(s);
            if (o != _objective) { _objective = o; objective.text = o; _laneFor = -1; }
            FitLanes();
        }

        /// <summary>Keyboard and gamepad: compact chips. Touch layout: chips at least 48 dp tall.</summary>
        public void SetTallChips(bool tall)
        {
            if (_tall == tall && _skillKey != null) return;
            _tall = tall;
            foreach (var c in chips) if (c != null) c.SetTall(tall);
            if (badgeGrid != null)
            {
                float b = tall ? SkillChip.BadgeTall : SkillChip.BadgeShort;
                badgeGrid.GetComponent<GridLayoutGroup>().cellSize = new Vector2(b, b);
            }
            _laneFor = -1;
        }

        void ShowSkills(PlayerSim sim)
        {
            var key = new System.Text.StringBuilder(10);
            foreach (var id in SkillIds.All) key.Append(sim.Has(id) ? '1' : '0');
            string k = key.ToString();
            if (k != _skillKey)
            {
                _skillKey = k;
                bool any = false, anyActive = false, anyBadge = false;
                for (int i = 0; i < chips.Length && i < SkillIds.All.Length; i++)
                {
                    bool own = sim.Has(SkillIds.All[i]);
                    if (chips[i] != null && chips[i].gameObject.activeSelf != own) chips[i].gameObject.SetActive(own);
                    any |= own;
                    if (own && SkillIds.IsActive(SkillIds.All[i])) anyActive = true; else if (own) anyBadge = true;
                }
                if (skillsColumn != null && skillsColumn.gameObject.activeSelf != any) skillsColumn.gameObject.SetActive(any);
                if (chipRow != null) chipRow.gameObject.SetActive(anyActive);
                if (badgeGrid != null) badgeGrid.gameObject.SetActive(anyBadge);
                _laneFor = -1;
            }
            var st = sim.SkillTuning ?? SkillTuning.Default;
            foreach (var c in chips)
            {
                if (c == null || !c.active || !c.gameObject.activeSelf) continue;
                if (c.skillId == SkillIds.Veil)
                {
                    float max = PlayerTuning.Ticks(sim.Has(SkillIds.GreaterVeil) ? st.greaterVeilCooldown : st.veilCooldown);
                    c.Set(sim.VeilCooldown / Mathf.Max(1f, max), sim.VeilCooldown == 0, sim.VeilTicks > 0);
                }
                else
                {
                    bool spread = sim.Has(SkillIds.TripleBolt);
                    float max = PlayerTuning.Ticks(spread ? st.tripleBoltCooldown : st.boltCooldown);
                    int cost = spread ? st.tripleBoltFlameCost : st.boltFlameCost;
                    c.Set(sim.BoltCooldown / Mathf.Max(1f, max), sim.BoltCooldown == 0 && sim.Flame >= cost, false);
                }
            }
        }

        /// <summary>Width the skill column takes from the left edge (0 when no chip shows).</summary>
        public float SkillsWidth
        {
            get
            {
                if (skillsColumn == null || !skillsColumn.gameObject.activeSelf) return 0;
                int act = 0, badges = 0;
                for (int i = 0; i < chips.Length; i++)
                    if (chips[i] != null && chips[i].gameObject.activeSelf) { if (chips[i].active) act++; else badges++; }
                float b = _tall ? SkillChip.BadgeTall : SkillChip.BadgeShort;
                float wa = act > 0 ? act * SkillChip.ActiveWidth + (act - 1) * 6 : 0;
                int cols = Mathf.Min(3, badges);
                float wb = cols > 0 ? cols * b + (cols - 1) * 4 : 0;
                return Mathf.Max(wa, wb);
            }
        }

        /// <summary>
        /// Narrows the objective line (and, through <see cref="LaneWidth"/>, the toast) so neither reaches the chip
        /// column; a long objective wraps instead. Recomputed only when the text, the chips or the width change.
        /// </summary>
        public void FitLanes(bool force = false)
        {
            float width = ((RectTransform)transform).rect.width;
            if (width <= 0) return;
            if (!force && Mathf.Approximately(width, _laneFor)) return;
            _laneFor = width;
            float side = SkillsWidth;
            float gutter = side > 0 ? SideMargin + side + LaneGap : 0;
            LaneWidth = side > 0 ? width - 2 * gutter : 9999f;
            var le = objective.GetComponent<LayoutElement>();
            float maxText = LaneWidth - 2 * ObjectivePad;
            string text = objective.text ?? "";
            float natural = objective.GetPreferredValues(text).x;
            float h;
            if (natural > maxText)
            {
                objective.textWrappingMode = TextWrappingModes.Normal;
                le.preferredWidth = maxText;
                h = objective.GetPreferredValues(text, maxText, 0).y;
            }
            else
            {
                objective.textWrappingMode = TextWrappingModes.NoWrap;
                le.preferredWidth = -1;
                h = objective.GetPreferredValues(text).y;
            }
            LaneBottom = ObjectiveTop + h + 12f;
        }

        /// <summary>The objective line (prototype objective()); the boss lines arrive with the Bellkeeper in M7.</summary>
        public static string Objective(LevelSession s)
        {
            var sim = s.Sim;
            var L = s.Layout;
            if (sim.Dimension == Dim.Spirit) return Loc.Get("obj.spirit");
            if (sim.Dimension == Dim.Inverted) return Loc.Get("obj.inverted");
            int cp = Mathf.Max(0, sim.Checkpoint);
            bool lastRoom = cp >= L.Rooms;
            if (s.Spec.boss && cp >= L.Rooms - 1) return Loc.Get("obj.arena");
            if (lastRoom && !s.Spec.boss) return L.NeedClue && !sim.HasClue ? Loc.Get("obj.findfinal") : Loc.Get("obj.final");
            if (L.NeedClue && !sim.HasClue && cp == L.ClueRoom) return Loc.Get("obj.findroom");
            return Loc.Format("obj.climb", Mathf.Min(cp + 1, L.Rooms), L.Rooms);
        }

        public void SetBoss(bool visible, float fraction)
        {
            if (bossBar.gameObject.activeSelf != visible) bossBar.gameObject.SetActive(visible);
            if (visible) bossFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), 0);
        }
    }
}
