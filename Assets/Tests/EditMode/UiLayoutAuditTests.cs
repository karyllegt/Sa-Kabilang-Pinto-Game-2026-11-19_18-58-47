using NUnit.Framework;

namespace Skp.Tests
{
    /// <summary>
    /// Every screen at 1280x720, 2400x1080, 2340x1080 with a notch, 1024x768 and 800x480: no overlapping targets,
    /// 48 dp minimum, safe area, no clipped or overflowing text, text at least 10 px, contrast, dialog never over
    /// the pause button. Writes the screenshots and contact sheets to tests/Screenshots/m5 (see layout_report.md).
    /// </summary>
    public class UiLayoutAuditTests
    {
        [Test]
        public void EveryScreen_AtEveryResolution_PassesTheLayoutAudit()
        {
            var r = SkpUiAudit.Run(saveShots: true);
            Assert.Greater(r.Checks, 1000, "the audit ran");
            Assert.AreEqual(18 * 5 + 5, r.Shots.Count, "18 screens x 5 resolutions + 5 contact sheets");
            Assert.IsEmpty(r.Problems, string.Join("\n", r.Problems));
        }
    }
}
