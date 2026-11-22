using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Skp.Core;
using Skp.Dimensions;
using Skp.Enemies;
using Skp.Levels;
using Skp.Player;
using UnityEditor;
using UnityEngine;

namespace Skp.Tests
{
    /// <summary>R1: the prefab catalog is complete, every prefab carries what its view drives, and the views no longer build objects in code.</summary>
    public class R1PrefabTests
    {
        static LevelPrefabCatalog Catalog => AssetDatabase.LoadAssetAtPath<LevelPrefabCatalog>("Assets/Resources/Level/LevelPrefabCatalog.asset");

        [Test]
        public void Catalog_IsInResources_AndEveryEntryIsAPrefab()
        {
            var c = Catalog;
            Assert.IsNotNull(c, "Run M1 Setup builds the catalog");
            Assert.AreSame(c, LevelPrefabCatalog.Load(), "the views find it through Resources");
            foreach (var (name, prefab) in c.Entries())
            {
                Assert.IsNotNull(prefab, name + " is set");
                Assert.IsTrue(PrefabUtility.IsPartOfPrefabAsset(prefab), name + " is a prefab asset");
                StringAssert.StartsWith("Assets/Prefabs/", AssetDatabase.GetAssetPath(prefab), name);
            }
            Assert.IsNull(c.decorByAct[0], "Act I has no decoration");
        }

        [Test]
        public void EveryPiece_HasTheRenderersAndStatesItsViewDrives()
        {
            var c = Catalog;
            foreach (var (name, o) in c.Entries())
            {
                if (!(o is LevelPiece p)) continue;
                if (p == c.mapNumber) { Assert.IsNotNull(p.label, name); continue; }
                if (p == c.tollRing) { Assert.IsNotNull(p.line, name); Assert.IsTrue(p.line.loop, name); continue; }
                Assert.IsNotNull(p.main, name + " main renderer");
                Assert.IsNotNull(p.main.sprite, name + " has a sprite");
            }
            Assert.AreEqual(2, c.exit.StateCount, "exit: locked, open");
            Assert.AreEqual(3, c.root.StateCount, "root: dormant, warning, strike");
            Assert.AreEqual(2, c.thorns.StateCount, "thorns: whole, burnt");
            Assert.AreEqual(3, c.bossBody.StateCount, "Bellkeeper: normal, stunned, tolling");
            Assert.AreEqual(2, c.mapNodeIcon.StateCount, "map node: star, bell");
            foreach (var cr in new[] { c.anino, c.tikbalang, c.manananggal }) Assert.GreaterOrEqual(cr.StateCount, 2, cr.name + " frames");
            Assert.AreEqual(2, c.lantern.extra.Length + 1, "lantern: post and flame");
            Assert.IsNotNull(c.mapLolaMark.label, "Lola's !");
            Assert.AreEqual(1, c.mapCorner.extra.Length, "corner: two bars");
        }

        [Test]
        public void LevelPieces_FollowTheDimensionRules_BossAndMapPiecesDoNot()
        {
            var c = Catalog;
            var level = new[] { c.ledgeByAct[0], c.ledgeByAct[1], c.ledgeByAct[2], c.ledgeSpirit, c.ledgeInverted, c.ledgeCrumble, c.ledgeMoving, c.groundBlock,
                                c.doorSpirit, c.doorInverted, c.doorHome, c.exit, c.spike, c.spikeGauntlet, c.root, c.thorns, c.curtain,
                                c.flame, c.fragment, c.letter, c.lantern, c.anino, c.tikbalang, c.manananggal, c.fallingBell, c.bellMark,
                                c.waterBody, c.waterSurface, c.decorByAct[1], c.decorByAct[2] };
            foreach (var p in level)
            {
                Assert.IsNotNull(p.dimension, p.name + " has a DimensionMember");
                Assert.IsTrue(p.dimension.renderers.Length > 0 && p.dimension.renderers.All(r => r != null), p.name + " lists its renderers");
            }
            Assert.AreEqual(DimensionMember.Ghost.AlwaysVisible, c.exit.dimension.ghost);
            Assert.AreEqual(DimensionMember.Ghost.Door, c.doorSpirit.dimension.ghost);
            Assert.AreEqual(DimensionMember.Ghost.Hazard, c.thorns.dimension.ghost);
            Assert.AreEqual(DimensionMember.Ghost.Hidden, c.anino.dimension.ghost);
            Assert.AreEqual(DimensionMember.Ghost.Ledge, c.ledgeByAct[0].dimension.ghost);
            foreach (var p in new[] { c.bossBody, c.shockwave, c.bossMark, c.bossBell, c.bolt, c.mapDot, c.mapToken, c.mapLola })
                Assert.IsNull(p.dimension, p.name + " is drawn in every dimension");
        }

        [Test]
        public void Prefabs_CarryTheLookTheViewsUsedToSetInCode()
        {
            var c = Catalog;
            Assert.AreEqual(SpriteDrawMode.Tiled, c.ledgeByAct[0].main.drawMode, "ledges tile their middle");
            Assert.AreEqual(SpriteDrawMode.Tiled, c.spike.main.drawMode);
            Assert.AreEqual(SpriteDrawMode.Tiled, c.curtain.main.drawMode);
            Assert.AreEqual(-1, c.groundBlock.main.sortingOrder);
            Assert.AreEqual(0, c.ledgeSpirit.main.sortingOrder);
            Assert.AreEqual(3, c.thorns.main.sortingOrder);
            Assert.AreEqual(6, c.anino.main.sortingOrder);
            Assert.AreEqual(20, c.waterBody.main.sortingOrder);
            Assert.AreEqual(8, c.bossBody.main.sortingOrder);
            Assert.AreEqual(12, c.tollRing.line.sortingOrder);
            Assert.AreEqual((Color)new Color32(0x3a, 0x24, 0x10, 0xff), c.lantern.extra[0].color, "the lantern post is dark wood");
            Assert.AreEqual(0.82f, c.waterBody.main.color.a, 1e-3f, "the water is translucent");
            Assert.AreEqual(0.85f, c.decorByAct[1].main.color.a, 1e-3f);
            var fx = c.fx;
            foreach (LevelFx.Kind k in System.Enum.GetValues(typeof(LevelFx.Kind)))
            {
                var child = fx.transform.Find(LevelFx.ChildName(k));
                Assert.IsNotNull(child, "fx " + k);
                Assert.IsNotNull(child.GetComponent<ParticleSystem>(), "fx " + k + " particle system");
                Assert.AreEqual(30, child.GetComponent<ParticleSystemRenderer>().sortingOrder);
            }
        }

        /// <summary>The views no longer create renderers in code; only the two empty folders (level objects, map world) remain.</summary>
        [Test]
        public void Views_DoNotBuildDrawnObjectsInCode()
        {
            var files = new[] { "Assets/Scripts/Levels/LevelView.cs", "Assets/Scripts/Enemies/BossView.cs", "Assets/Scripts/Player/PlayerView.cs",
                                "Assets/Scripts/UI/MapController.cs", "Assets/Scripts/Levels/LevelFx.cs", "Assets/Scripts/Levels/LevelRunner.cs" };
            var add = new Regex(@"AddComponent<(SpriteRenderer|LineRenderer|TextMeshPro|ParticleSystem|BossView|LevelFx)>");
            var newGo = new Regex(@"new GameObject\(");
            int folders = 0;
            foreach (var f in files)
            {
                var src = File.ReadAllText(f);
                Assert.IsFalse(add.IsMatch(src), f + " adds a renderer in code");
                foreach (Match m in newGo.Matches(src))
                {
                    var line = src.Substring(m.Index, src.IndexOf('\n', m.Index) - m.Index);
                    Assert.IsTrue(line.Contains("\"Level Objects\"") || line.Contains("\"MapWorld\""), f + ": " + line);
                    folders++;
                }
            }
            Assert.AreEqual(2, folders, "only the level and map folders are made in code");
        }

        [Test]
        public void Bolts_ComeFromAPrewarmedPool_FiringMakesNoNewObjects()
        {
            var parent = new GameObject("r1 bolts");
            try
            {
                var player = new GameObject("player");
                player.transform.SetParent(parent.transform, false);
                var view = player.AddComponent<PlayerView>();
                view.body = player.AddComponent<SpriteRenderer>();
                view.boltPrefab = Catalog.bolt;
                view.PrewarmBoltPool();
                int made = parent.transform.childCount - 1;
                Assert.AreEqual(PlayerView.PrewarmBolts, made, "bolts are made before play");

                var table = AssetDatabase.LoadAssetAtPath<DifficultyTable>("Assets/Data/DifficultyTable.asset");
                var sim = new PlayerSim(AssetDatabase.LoadAssetAtPath<PlayerTuning>("Assets/Data/PlayerTuning.asset"), table.Get(DifficultyId.Standard), new PlayerWorld(new Rect(0, 0, 32, 32)));
                for (int i = 0; i < 3; i++) sim.Bolts.Add(new Bolt { Position = new Vector2(i, 1), Velocity = new Vector2(10, 0) });
                view.Render(sim, sim.Position, 1f);
                Assert.AreEqual(3, view.VisibleBolts, "a triple bolt is drawn");
                Assert.AreEqual(made, parent.transform.childCount - 1, "firing reuses the pool");
            }
            finally { Object.DestroyImmediate(parent); }
        }
    }
}
