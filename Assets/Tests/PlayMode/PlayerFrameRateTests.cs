using System.Collections;
using NUnit.Framework;
using Skp.Core;
using Skp.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Skp.Tests
{
    /// <summary>
    /// The real PlayerController in play mode, with the frame rate forced through
    /// Time.captureFramerate: the jump must reach the same apex on the same tick at 30, 60 and 144 fps.
    /// </summary>
    public class PlayerFrameRateTests
    {
        GameObject _go;

        [TearDown]
        public void TearDown()
        {
            Time.captureFramerate = 0;
            if (_go != null) Object.Destroy(_go);
        }

        [UnityTest]
        public IEnumerator Jump_IsIdentical_At30_60_144Fps()
        {
            var results = new (float apex, long tick, float x)[3];
            int[] rates = { 30, 60, 144 };
            for (int i = 0; i < rates.Length; i++)
            {
                yield return Measure(rates[i], r => results[i] = r);
            }
            for (int i = 0; i < rates.Length; i++)
            {
                Assert.AreEqual(62.5f / 16f, results[i].apex, 1e-4f, $"{rates[i]} fps apex");
                Assert.AreEqual(results[1].tick, results[i].tick, $"{rates[i]} fps apex tick");
                Assert.AreEqual(results[1].x, results[i].x, 1e-5f, $"{rates[i]} fps x after 60 ticks");
            }
            Assert.AreEqual(16, results[1].tick - 1, "apex on the 17th tick (index 16), as in the prototype");
        }

        IEnumerator Measure(int fps, System.Action<(float, long, float)> done)
        {
            Time.captureFramerate = fps;
            _go = new GameObject("TestPlayer");
            _go.SetActive(false);
            var pc = _go.AddComponent<PlayerController>();
            pc.tuning = ScriptableObject.CreateInstance<PlayerTuning>();
            pc.difficulties = ScriptableObject.CreateInstance<DifficultyTable>();
            pc.difficultyOverride = "standard";
            var world = new PlayerWorld(new Rect(-50, -50, 200, 200));
            world.Ledges.Add(new Rect(-50, -1, 200, 1));
            world.Lanterns.Add(new Vector2(5, 0));
            pc.Initialize(world);

            bool first = true;
            pc.InputOverride = () =>
            {
                var f = new PlayerInputFrame { MoveX = 1, JumpHeld = true, JumpPressed = first };
                first = false;
                return f;
            };
            float y0 = pc.Sim.Position.y, best = 0; long bestTick = 0; float xAt60 = 0;
            pc.Ticked += s =>
            {
                if (s.Position.y - y0 > best + 1e-6f) { best = s.Position.y - y0; bestTick = s.Tick; }
                if (s.Tick == 60) xAt60 = s.Position.x;
            };
            _go.SetActive(true);
            while (pc.Sim.Tick < 60) yield return null;
            done((best, bestTick, xAt60));
            Object.Destroy(_go);
            _go = null;
            yield return null;
        }
    }
}
