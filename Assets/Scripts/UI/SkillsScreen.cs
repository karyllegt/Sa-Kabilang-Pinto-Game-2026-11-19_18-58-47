using System;
using Skp.Save;
using Skp.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Skp.UI
{
    /// <summary>
    /// The skill list on the level map: the eight skills in two columns, owned ones with their icon, name,
    /// Filipino name and description, locked ones as "?" with the level whose letter teaches them.
    /// </summary>
    public class SkillsScreen : UiScreen
    {
        public const float DescWidth = 300f;

        public PixelButton back;
        public Image[] icons = new Image[8];
        public TextMeshProUGUI[] marks = new TextMeshProUGUI[8];
        public TextMeshProUGUI[] names = new TextMeshProUGUI[8];
        public TextMeshProUGUI[] descriptions = new TextMeshProUGUI[8];

        public Action BackRequested;
        bool _wired;

        public static SkillsScreen Build(Transform parent)
        {
            var th = UiKit.Theme;
            var root = ScreenParts.FullScreen(parent, "SkillsScreen");
            UiKit.Scrim(root).color = th.shadow; // opaque: the map behind would cut the contrast of the long list
            var s = root.gameObject.AddComponent<SkillsScreen>();
            s.back = ScreenParts.BackButton(root);
            ScreenParts.Heading(root, Loc.Get("skills.title"));

            var col = ScreenParts.Column(root, 10, 84);
            var cols = UiKit.HStack(col.transform, "Columns", 28, null, TextAnchor.UpperCenter);
            var a = UiKit.VStack(cols.transform, "A", 10, null, TextAnchor.UpperLeft);
            var b = UiKit.VStack(cols.transform, "B", 10, null, TextAnchor.UpperLeft);
            for (int i = 0; i < SkillIds.All.Length; i++)
            {
                var row = UiKit.HStack((i < 4 ? a : b).transform, "Skill_" + SkillIds.All[i], 10, null, TextAnchor.UpperLeft);
                var frame = UiKit.Box(row.transform, "IconFrame", th.buttonBoxScale);
                frame.raycastTarget = false;
                UiKit.Size(frame, 44, 44);
                s.icons[i] = UiKit.Fill(frame.transform, "Icon", Color.white);
                s.icons[i].preserveAspect = true;
                UiKit.Stretch(s.icons[i].rectTransform, 8, 8, 8, 8);
                s.marks[i] = UiKit.Label(frame.transform, "Locked", "?", th.bodySize, th.textDim, TextAlignmentOptions.Center, false);
                UiKit.Stretch(s.marks[i].rectTransform, 4, 4, 4, 4);
                var texts = UiKit.VStack(row.transform, "Texts", 4, null, TextAnchor.UpperLeft);
                s.names[i] = UiKit.Text(texts.transform, "Name", "", th.smallSize, th.text, TextAlignmentOptions.Left, DescWidth);
                s.descriptions[i] = UiKit.Text(texts.transform, "Description", "", th.tinySize, th.textDim, TextAlignmentOptions.TopLeft, DescWidth);
            }
            s.firstSelected = s.back;
            root.gameObject.SetActive(false);
            return s;
        }

        /// <summary>Fills the list from the save's learned skills.</summary>
        public void Bind(SaveData save)
        {
            if (!_wired)
            {
                _wired = true;
                back.Clicked += () => { if (BackRequested != null) BackRequested(); else Close(); };
            }
            var th = UiKit.Theme;
            var lib = SkillLibrary.Load();
            for (int i = 0; i < SkillIds.All.Length; i++)
            {
                string id = SkillIds.All[i];
                var def = lib != null ? lib.Get(id) : null;
                bool own = save != null && ProgressRules.HasSkill(save, id);
                icons[i].sprite = def != null ? def.icon : null;
                icons[i].enabled = own && icons[i].sprite != null;
                marks[i].enabled = !own;
                if (own && def != null)
                {
                    names[i].text = def.FullName;
                    names[i].color = th.text; // the icon carries the skill color; the name stays at full contrast
                    descriptions[i].text = def.description;
                }
                else
                {
                    names[i].text = Loc.Get("skills.locked");
                    names[i].color = th.textDim;
                    descriptions[i].text = Loc.Format("skills.lockedsub", SkillIds.LevelOf(id) + 1);
                }
            }
        }

        public override bool OnBack() { back.Press(); return true; }
    }
}
