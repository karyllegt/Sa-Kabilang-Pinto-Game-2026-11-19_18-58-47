using System.Linq;
using NUnit.Framework;
using Skp.Core;
using Skp.Player;
using UnityEditor;
using UnityEngine;

namespace Skp.Tests
{
    /// <summary>R3: every simulation state maps to its animation, the sets hold every animation for both characters,
    /// frames keep the 16x18 canvas with the bottom-centre pivot, and the player prefab uses them.</summary>
    public class R3TalaAnimTests
    {
        PlayerTuning _t;
        DifficultyTable _d;

        [SetUp] public void SetUp() { _t = ScriptableObject.CreateInstance<PlayerTuning>(); _d = ScriptableObject.CreateInstance<DifficultyTable>(); }

        PlayerSim Sim()
        {
            var w = new PlayerWorld(new Rect(0, 0, 32, 32));
            var s = new PlayerSim(_t, _d.Get("standard"), w) { Grounded = true, Velocity = Vector2.zero };
            return s;
        }

        static float PxPerTickToVel(float px) => px / (PlayerSim.Dt * PlayerTuning.PxPerUnit);

        [Test]
        public void EverySimState_MapsToItsAnimation()
        {
            var cases = new (string what, System.Action<PlayerSim> set, TalaAnim? shot, TalaAnim want)[]
            {
                ("standing", s => { }, null, TalaAnim.Idle),
                ("running", s => s.Velocity = new Vector2(PxPerTickToVel(1.5f), 0), null, TalaAnim.Run),
                ("rising", s => { s.Grounded = false; s.Velocity = new Vector2(0, PxPerTickToVel(3f)); }, null, TalaAnim.JumpRise),
                ("apex", s => { s.Grounded = false; s.Velocity = new Vector2(0, PxPerTickToVel(0.2f)); }, null, TalaAnim.Apex),
                ("falling", s => { s.Grounded = false; s.Velocity = new Vector2(0, -PxPerTickToVel(3f)); }, null, TalaAnim.Fall),
                ("falling up in Inverted", s => { s.SetDimension(Skp.Dimensions.Dim.Inverted); s.Grounded = false; s.Velocity = new Vector2(0, PxPerTickToVel(3f)); }, null, TalaAnim.Fall),
                ("just landed", s => { }, TalaAnim.Land, TalaAnim.Land),
                ("landing while running", s => s.Velocity = new Vector2(PxPerTickToVel(1.5f), 0), TalaAnim.Land, TalaAnim.Land),
                ("dashing", s => s.DashTicks = 5, null, TalaAnim.Dash),
                ("warding", s => s.WardTicks = 5, null, TalaAnim.Ward),
                ("gliding", s => { s.Grounded = false; s.Gliding = true; s.Velocity = new Vector2(0, -PxPerTickToVel(1f)); }, null, TalaAnim.Glide),
                ("hurt", s => s.DashTicks = 5, TalaAnim.Hurt, TalaAnim.Hurt),
                ("through a door", s => { }, TalaAnim.Door, TalaAnim.Door),
                ("casting a bolt in the air", s => { s.Grounded = false; s.Velocity = new Vector2(0, PxPerTickToVel(3f)); }, TalaAnim.Bolt, TalaAnim.Bolt),
                ("dead", s => { s.Life = PlayerLife.Dead; s.DashTicks = 5; }, TalaAnim.Hurt, TalaAnim.Death),
            };
            foreach (var (what, set, shot, want) in cases)
            {
                var s = Sim();
                set(s);
                Assert.AreEqual(want, PlayerAnimator.Pick(s, shot), what);
            }
        }

        [Test]
        public void OneShots_StartFromSimEvents_AndEnd()
        {
            var s = Sim();
            var a = new PlayerAnimator();
            a.Bind(s);
            a.Tick(s);
            Assert.AreEqual(TalaAnim.Idle, a.State);
            s.TryHurt(1, null);
            s.Invulnerable = 100;
            a.Tick(s);
            Assert.AreEqual(TalaAnim.Hurt, a.State, "Hurt event starts the hurt animation");
            for (int i = 0; i < PlayerAnimator.HurtTicks + 2; i++) { s.Velocity = Vector2.zero; s.Grounded = true; a.Tick(s); }
            Assert.AreEqual(TalaAnim.Idle, a.State, "and it ends");
            a.Unbind();
        }

        [Test]
        public void AnimSets_HaveEveryAnimation_ForBothCharacters_On16x18WithBottomPivot()
        {
            foreach (var ch in new[] { "girl", "boy" })
            {
                var set = AssetDatabase.LoadAssetAtPath<PlayerAnimSet>(SkpTalaAnimSetup.SetPath(ch));
                Assert.IsNotNull(set, ch + " set");
                foreach (TalaAnim anim in System.Enum.GetValues(typeof(TalaAnim)))
                {
                    var clip = set.Get(anim);
                    Assert.IsNotNull(clip, $"{ch} {anim}");
                    foreach (var f in clip.frames)
                    {
                        Assert.AreEqual(new Vector2(16, 18), f.rect.size, $"{ch} {anim} {f.name} canvas");
                        Assert.AreEqual(new Vector2(8, 0), f.pivot, $"{ch} {anim} {f.name} pivot bottom centre");
                    }
                }
                Assert.AreEqual(4, set.Get(TalaAnim.Idle).frames.Length, "idle 4");
                Assert.AreEqual(6, set.Get(TalaAnim.Run).frames.Length, "run 6");
                Assert.AreEqual(5, set.Get(TalaAnim.Death).frames.Length, "death 5");
                Assert.AreEqual(10, set.Get(TalaAnim.Idle).ticksPerFrame, "idle 6 fps");
                Assert.AreEqual(6, set.Get(TalaAnim.Run).ticksPerFrame, "run 10 fps");
            }
        }

        [Test]
        public void PlayerPrefab_UsesBothSets_AndKeepsThePrototypeFallback()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkpTestRoomBuilder.PlayerPrefabPath);
            var view = prefab.GetComponentInChildren<PlayerView>(true);
            Assert.IsNotNull(view.girlAnims);
            Assert.IsNotNull(view.boyAnims);
            Assert.IsTrue(view.girl.All(s => s != null) && view.boy.All(s => s != null), "the 4 prototype frames stay as the fallback");
        }

        [Test]
        public void OneShotLengths_MatchTheirClips()
        {
            var set = AssetDatabase.LoadAssetAtPath<PlayerAnimSet>(SkpTalaAnimSetup.SetPath("girl"));
            int Len(TalaAnim a) => set.Get(a).frames.Length * set.Get(a).ticksPerFrame;
            Assert.AreEqual(PlayerAnimator.LandTicks, Len(TalaAnim.Land));
            Assert.AreEqual(PlayerAnimator.HurtTicks, Len(TalaAnim.Hurt));
            Assert.AreEqual(PlayerAnimator.DoorTicks, Len(TalaAnim.Door));
            Assert.AreEqual(PlayerAnimator.BoltTicks, Len(TalaAnim.Bolt));
        }
    }
}
