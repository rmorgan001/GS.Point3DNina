using NUnit.Framework;
using System.IO;

namespace GS.Point3DNina.Test {
    /// <summary>
    /// NINA only reports the mount every device polling interval, so the viewer has to glide the model across that whole
    /// interval; a shorter glide leaves it standing still between updates.
    /// </summary>
    public class GlideTests {
        [TestCase(2.0, 3000.0)]
        [TestCase(1.0, 1500.0)]
        [TestCase(5.0, 7500.0)]
        public void TheGlideIsHalfAgainAsLongAsThePollingInterval(double interval, double expected)
            => Assert.That(Point3DDockable.GlideMs(interval), Is.EqualTo(expected));

        [TestCase(0.0, 1000.0)]
        [TestCase(0.25, 1000.0)]
        [TestCase(-3.0, 1000.0)]
        [TestCase(60.0, 10000.0)]
        [TestCase(double.NaN, 1000.0)]
        [TestCase(double.PositiveInfinity, 1000.0)]
        public void TheGlideStaysBetweenOneAndTenSecondsAndIgnoresNonsense(double interval, double expected)
            => Assert.That(Point3DDockable.GlideMs(interval), Is.EqualTo(expected));

        [Test]
        public void TheViewerTakesItsGlideLengthFromTheHost() {
            var script = File.ReadAllText(Path.Combine(
                Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!, "Assets", "model3d", "model3d.js"));
            Assert.Multiple(() => {
                Assert.That(script, Does.Contain("options.maxGlideMs"), "read from the create options");
                Assert.That(script, Does.Contain("Math.min(sincePrevious, this.maxGlideMs)"), "used for each glide");
                Assert.That(script, Does.Contain("export function setMaxGlide"), "can change while the viewer runs");
                Assert.That(File.ReadAllText(Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!, "Assets", "viewer.html")),
                    Does.Contain("setMaxGlide:"), "the page passes it on");
                Assert.That(script, Does.Not.Contain("Math.min(sincePrevious, MAX_GLIDE_MS)"), "no fixed cap left");
            });
        }
    }
}
