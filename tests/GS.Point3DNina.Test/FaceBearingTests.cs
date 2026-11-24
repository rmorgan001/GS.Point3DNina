using NUnit.Framework;
using System.IO;

namespace GS.Point3DNina.Test {
    /// <summary>Clicking the compass readout turns the view to face the pole: north in the north, south in the south.</summary>
    public class FaceBearingTests {
        [TestCase(51.5, 0.0)]
        [TestCase(0.0, 0.0)]
        [TestCase(-0.01, 180.0)]
        [TestCase(-33.9, 180.0)]
        [TestCase(-90.0, 180.0)]
        public void TheViewFacesTheNearerPole(double latitude, double bearing)
            => Assert.That(Point3DDockable.FaceBearingFor(latitude), Is.EqualTo(bearing));

        [Test]
        public void TheViewerTurnsToTheBearingTheHostSets() {
            var assets = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!, "Assets");
            var script = File.ReadAllText(Path.Combine(assets, "model3d", "model3d.js"));
            var page = File.ReadAllText(Path.Combine(assets, "viewer.html"));
            Assert.Multiple(() => {
                Assert.That(script, Does.Contain("options.faceBearing"), "read from the create options");
                Assert.That(script, Does.Contain("directionOf(this.faceBearing, this.northern)"), "used when the readout is clicked");
                Assert.That(script, Does.Contain("export function setFaceBearing"), "can follow a latitude change");
                Assert.That(page, Does.Contain("setFaceBearing:"), "the page passes it on");
            });
        }
    }
}
