using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Skp.Core;
using Skp.Dimensions;
using Skp.Levels;
using Skp.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Skp.Tests
{
    /// <summary>
    /// M3 in play mode: a scripted door press switches the real PlayerController into Spirit,
    /// the DimensionDirector restyles the scene (Spirit ledge solid and drawn, Ordinary ledge ghosted,
    /// teal screen tint), the Spirit-only ledge becomes solid, and dying brings everything back.
    /// </summary>
    public class DimensionPlayModeTests
    {
        readonly List<GameObject> _made = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var g in _made) if (g != null) Object.Destroy(g);
            _made.Clear();
        }

        GameObject Make(string name) { var g = new GameObject(name); _made.Add(g); return g; }

        SpriteRenderer Ledge(Transform parent, Rect r, Dim mask)
        {
            var g = new GameObject("ledge " + mask);
            g.transform.SetParent(parent, false);
            g.transform.position = r.position;
            var m = g.AddComponent<WorldMarker>();
            m.kind = WorldMarker.Kind.Ledge; m.size = r.size; m.mask = mask;
            var sr = g.AddComponent<SpriteRenderer>();
            var dm = g.AddComponent<DimensionMember>();
            dm.mask = mask; dm.ghost = DimensionMember.Ghost.Ledge; dm.renderers = new[] { sr };
            return sr;
        }

        [UnityTest]
        public IEnumerator DoorSwitch_RestylesScene_SolidOnlyInDimension_ResetOnDeath()
        {
            var worldGo = Make("World");
            var world = worldGo.AddComponent<LevelWorldAuthoring>();
            world.bounds = new Rect(-20, -5, 60, 40);
            Ledge(worldGo.transform, new Rect(-20, -1, 60, 1), Dim.All);
            var spiritLedge = Ledge(worldGo.transform, new Rect(4, 2, 6, 1), Dim.Spirit);   // top y = 3
            var ordLedge = Ledge(worldGo.transform, new Rect(12, 2, 4, 1), Dim.Ordinary);
            var lantern = new GameObject("lantern").AddComponent<LanternMarker>();
            lantern.transform.SetParent(worldGo.transform, false);
            lantern.transform.position = new Vector3(-15, 0, 0);
            var door = new GameObject("door").AddComponent<DoorMarker>();
            door.transform.SetParent(worldGo.transform, false);
            door.transform.position = new Vector3(-10.5f, 0, 0);
            door.mask = Dim.Ordinary; door.target = Dim.Spirit; door.kind = DoorKind.Enter;

            var playerGo = Make("Player");
            playerGo.SetActive(false);
            var pc = playerGo.AddComponent<PlayerController>();
            pc.tuning = ScriptableObject.CreateInstance<PlayerTuning>();
            pc.difficulties = ScriptableObject.CreateInstance<DifficultyTable>();
            pc.difficultyOverride = "standard";
            pc.worldSource = world;

            var tint = Make("tint").AddComponent<SpriteRenderer>();
            var directorGo = Make("Director");
            directorGo.SetActive(false);
            var director = directorGo.AddComponent<DimensionDirector>();
            director.player = pc;
            director.screenTint = tint;

            var heard = new List<Dim>();
            System.Action<Dim> onChange = d => heard.Add(d);
            DimensionDirector.Changed += onChange;
            try
            {
                // walk from the lantern to the door, press interact once there
                int phase = 0;
                pc.InputOverride = () =>
                {
                    var s = pc.Sim;
                    if (phase == 0 && s.NearDoor >= 0) { phase = 1; return new PlayerInputFrame { InteractPressed = true }; }
                    return new PlayerInputFrame { MoveX = phase == 0 ? 1 : 0 };
                };
                playerGo.SetActive(true);
                directorGo.SetActive(true);

                float t = 0;
                while (pc.Sim == null || (pc.Sim.Dimension != Dim.Spirit && t < 5f)) { t += Time.deltaTime; yield return null; }
                yield return null; // let the director's LateUpdate run
                Assert.AreEqual(Dim.Spirit, pc.Sim.Dimension, "door switched to Spirit");
                Assert.AreEqual(Dim.Spirit, director.Current);
                Assert.AreEqual(1f, spiritLedge.color.a, 1e-4f, "Spirit ledge drawn solid");
                Assert.AreEqual(0.16f, ordLedge.color.a, 1e-4f, "Ordinary ledge ghosted");
                Assert.AreEqual(0.26f, tint.color.a, 1e-3f, "teal screen tint");
                Assert.Contains(Dim.Spirit, heard, "audio/HUD hook raised");

                // solid only in its dimension: drop Tala above the Spirit ledge
                pc.Sim.Invulnerable = 100000;
                pc.Sim.Position = new Vector2(7f - pc.Sim.Size.x / 2f, 6f);
                pc.Sim.Velocity = Vector2.zero;
                pc.Sim.Grounded = false; // teleported into the air
                t = 0;
                while (!(pc.Sim.Grounded && pc.Sim.Position.y < 5.9f) && t < 3f) { t += Time.deltaTime; yield return null; }
                Assert.AreEqual(3f, pc.Sim.Position.y, 1e-4f, "stands on the Spirit-only ledge");

                // death resets the dimension and the scene follows
                pc.Sim.Invulnerable = 0;
                pc.Sim.Die();
                t = 0;
                while (pc.Sim.Life != PlayerLife.Playing && t < 5f) { t += Time.deltaTime; yield return null; }
                yield return null;
                Assert.AreEqual(Dim.Ordinary, pc.Sim.Dimension);
                Assert.AreEqual(Dim.Ordinary, director.Current);
                Assert.AreEqual(0.16f, spiritLedge.color.a, 1e-4f, "Spirit ledge ghosted again");
                Assert.AreEqual(0f, tint.color.a, 1e-4f, "no tint in the Waking World");
            }
            finally
            {
                DimensionDirector.Changed -= onChange;
            }
        }
    }
}
