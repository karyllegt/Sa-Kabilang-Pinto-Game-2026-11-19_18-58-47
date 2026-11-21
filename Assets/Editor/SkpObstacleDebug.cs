// M7 development aid: dumps the geometry of the first few failing obstacles of one type with a few probe moves.
// Batch: -executeMethod SkpObstacleDebug.Dump (writes tests/Results/m7_obstacle_debug.txt)
using System.IO;
using System.Linq;
using System.Text;
using Skp.Core;
using Skp.Dimensions;
using Skp.Levels;
using Skp.Player;
using Skp.Skills;
using UnityEditor;
using UnityEngine;

public static class SkpObstacleDebug
{
    public static void Dump()
    {
        var sb = new StringBuilder();
        var tuning = AssetDatabase.LoadAssetAtPath<PlayerTuning>("Assets/Data/PlayerTuning.asset");
        var table = AssetDatabase.LoadAssetAtPath<DifficultyTable>("Assets/Data/DifficultyTable.asset");
        var d = table.Get("standard");
        int shown = 0;
        foreach (int lv in new[] { 7 })
        {
            var owned = SkillIds.All;
            var L = SkpObstacleReport.Layout(lv, "standard", owned);
            foreach (var o in L.Obstacles.Where(x => x.type == "bolt").ToList())
            {
                var c = ObstacleCheck.Run(Single(L, o), tuning, d, owned).FirstOrDefault();
                if (c.Ok || shown >= 4) continue;
                shown++;
                var w = LevelWorldFactory.Build(L);
                var from = w.Ledges[L.Plats.IndexOf(o.From)];
                var to = w.Ledges[L.Plats.IndexOf(o.To)];
                sb.AppendLine($"L{lv + 1} room {o.room + 1}: from {from.Rect} ({from.Kind}) over {(o.Over != null ? w.Ledges[L.Plats.IndexOf(o.Over)].Rect.ToString() : "-")} to {to.Rect} ({to.Kind}) {c}");
                foreach (var h in w.Hazards.Where(h => h.Rect.yMax > from.Rect.yMin - 10 && h.Rect.yMin < to.Rect.yMax + 10))
                    sb.AppendLine($"   hazard {h.Kind} {h.Rect}");
                foreach (var l in w.Ledges.Where(l => l != from && l != to && l.Rect.yMax > from.Rect.yMax - 1 && l.Rect.yMax < to.Rect.yMax + 4 && (l.Mask & Dim.Ordinary) != 0))
                    sb.AppendLine($"   other ledge {l.Kind} {l.Rect} mask {l.Mask}");
                {
                    var w2 = LevelWorldFactory.Build(L);
                    var f2 = w2.Ledges[L.Plats.IndexOf(o.From)]; var t2 = w2.Ledges[L.Plats.IndexOf(o.To)]; var ov2 = o.Over != null ? w2.Ledges[L.Plats.IndexOf(o.Over)] : f2;
                    var keep = new System.Collections.Generic.HashSet<Ledge> { f2, t2, ov2 };
                    w2.Ledges.RemoveAll(l => !keep.Contains(l));
                    w2.Hazards.RemoveAll(h => !keep.Any(l => h.Rect.xMax > l.Rect.xMin && h.Rect.xMin < l.Rect.xMax && h.Rect.yMax >= l.Rect.yMin - 9f && h.Rect.yMin <= l.Rect.yMax + 9f));
                    w2.Doors.Clear();
                    var bot = new SkillBot(w2, tuning, d, new SkillSet(owned), Dim.Ordinary, true, false, true);
                    var res = bot.Reach(new Vector2(f2.Rect.center.x - 0.3125f, f2.Rect.yMax), l => l == t2);
                    sb.AppendLine("   with skills: " + res + " path: " + string.Join(" | ", bot.LastPath));
                }
                // probe: from the From ledge's center, jump with a double jump at different ticks toward To
                foreach (int at in new[] { 10, 14, 18, 22 })
                {
                    var sim = new PlayerSim(tuning, d, w) { Skills = new SkillSet(owned), SkillTuning = SkillLibrary.Tuning };
                    sim.Position = new Vector2(from.Rect.center.x - 0.3f, from.Rect.yMax);
                    sim.Step(default);
                    int dir = to.Rect.center.x > from.Rect.center.x ? 1 : -1;
                    float top = 0;
                    for (int t = 0; t < 120; t++)
                    {
                        sim.Step(new PlayerInputFrame { MoveX = dir, JumpPressed = t == 0 || t == at, JumpHeld = true });
                        top = Mathf.Max(top, sim.Position.y);
                        if (t > 3 && sim.Grounded) break;
                    }
                    sb.AppendLine($"   probe dj@{at}: peak feet {top:0.00} (to top {to.Rect.yMax:0.00}), ended on {(sim.Standing != null ? sim.Standing.Rect.ToString() : "air")} airjumps {sim.AirJumps} has dj {sim.Has("dj")}");
                }
            }
        }
        File.WriteAllText("tests/Results/m7_obstacle_debug.txt", sb.ToString());
        EditorApplication.Exit(0);
    }

    static LevelLayout Single(LevelLayout L, LObstacle o)
    {
        // reuse the layout with only this obstacle listed
        var keep = L.Obstacles.ToList();
        L.Obstacles.Clear();
        L.Obstacles.Add(o);
        var copy = L;
        return copy;
    }
}
