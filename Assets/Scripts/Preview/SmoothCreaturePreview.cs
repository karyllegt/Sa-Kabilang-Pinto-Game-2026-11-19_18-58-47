using System.Linq;
using Skp.Core;
using Skp.Levels;
using Skp.Player;
using Skp.Save;
using Skp.Skills;
using Skp.UI;
using UnityEngine;

namespace Skp.Preview
{
    /// <summary>Opens an existing encounter in the separate creature art review scenes.</summary>
    [DefaultExecutionOrder(-800)]
    public sealed class SmoothCreaturePreview : MonoBehaviour
    {
        public const string LevelScenePath = "Assets/Art/Smooth/Creatures/Creature Encounters.unity";
        public const string MapScenePath = "Assets/Art/Smooth/Creatures/Lola Map Preview.unity";

        public enum Encounter { Anino, Tikbalang, Manananggal, Bellkeeper }

        [SerializeField] LevelRunner level;
        [SerializeField] Encounter encounter;
        [SerializeField] DialogBox dialog;
        [SerializeField] Sprite aninoPortrait;
        [SerializeField] Sprite lolaPortrait;
        [SerializeField] Sprite bossPortrait;

        void Start()
        {
            var save = SmoothActIPreview.UsePreviewSave().Save;
            if (dialog != null) dialog.LineShown += ShowPortrait;
            if (level != null) GameSession.Pending = MakeRequest(level, save, encounter);
        }

        void OnDestroy()
        {
            if (dialog != null) dialog.LineShown -= ShowPortrait;
        }

        void ShowPortrait(int _)
        {
            var line = dialog.CurrentLine;
            if (line.Portrait != null) return;
            var art = line.Who switch
            {
                Speaker.Anino => aninoPortrait,
                Speaker.Lola => lolaPortrait,
                Speaker.Boss => bossPortrait,
                _ => null,
            };
            if (art != null) dialog.portrait.sprite = art;
        }

        public static LevelRequest MakeRequest(LevelRunner runner, SaveService save, Encounter selected)
        {
            int index = selected switch
            {
                Encounter.Tikbalang => 5,
                Encounter.Manananggal => 7,
                Encounter.Bellkeeper => 8,
                _ => 0,
            };
            var spec = runner.levels[index].spec;
            save.Data.unlocked = index + 1;
            save.Data.skills = SkillIds.All.Take(index).ToArray();
            var request = new LevelRequest
            {
                Level = index, Difficulty = "standard",
                Resume = new ResumeState { active = true, lv = index, vit = 99, flame = 100, lives = -1 }
            };
            if (selected == Encounter.Bellkeeper)
            {
                request.Resume.cp = spec.rooms;
                return request;
            }

            // Sample the original level layout and pick an existing lantern near this enemy type.
            var session = new LevelSession(spec, request, runner.tuning, runner.difficulties, runner.curve, save);
            var kind = selected switch
            {
                Encounter.Tikbalang => EnemyKind.Tikbalang,
                Encounter.Manananggal => EnemyKind.Manananggal,
                _ => EnemyKind.Anino,
            };
            float closest = float.PositiveInfinity;
            for (int i = 0; i < session.World.Lanterns.Count; i++)
            {
                var lantern = session.World.Lanterns[i];
                foreach (var enemy in session.World.Enemies)
                {
                    if (enemy.Kind != kind || enemy.Center.y < lantern.y - 2f) continue;
                    float distance = (enemy.Center - lantern).sqrMagnitude;
                    if (distance >= closest) continue;
                    closest = distance;
                    request.Resume.cp = i;
                }
            }
            return request;
        }
    }
}

