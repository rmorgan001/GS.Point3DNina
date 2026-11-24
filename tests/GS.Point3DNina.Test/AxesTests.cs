using GS.Point3DNina.Shared;
using NUnit.Framework;

namespace GS.Point3DNina.Test {
    public class AxesTests {
        private const double Tolerance = 1e-9;
        private const int East = 0, West = 1, Unknown = -1;

        [Test]
        public void GermanPolarEastSideUsesHourAngleInDegreesAndDeclination() {
            var axes = Axes.RaDecToAxesXy(AlignmentModes.algGermanPolar, [6, 45], 8, false, East);
            Assert.That(axes, Is.EqualTo(new[] { 30.0, 45.0 }).Within(Tolerance));
        }

        [Test]
        public void GermanPolarUnknownPierSideIsTheSameAsEast() {
            var east = Axes.RaDecToAxesXy(AlignmentModes.algGermanPolar, [6, 45], 8, false, East);
            var unknown = Axes.RaDecToAxesXy(AlignmentModes.algGermanPolar, [6, 45], 8, false, Unknown);
            Assert.That(unknown, Is.EqualTo(east).Within(Tolerance));
        }

        [Test]
        public void GermanPolarWestSideFlipsToTheOtherAxisSolution() {
            var axes = Axes.RaDecToAxesXy(AlignmentModes.algGermanPolar, [6, 45], 8, false, West);
            Assert.That(axes, Is.EqualTo(new[] { 210.0, 135.0 }).Within(Tolerance));
        }

        [Test]
        public void GermanPolarWestSideBeyondNinetyDegreesFlipsTheOtherWay() {
            // RA axis = (8 - 0) * 15 = 120 degrees, past the 90 degree split.
            var axes = Axes.RaDecToAxesXy(AlignmentModes.algGermanPolar, [0, 30], 8, false, West);
            Assert.That(axes, Is.EqualTo(new[] { -60.0, 150.0 }).Within(Tolerance));
        }

        [Test]
        public void GermanPolarSouthernHemisphereNegatesTheDeclinationAxis() {
            var axes = Axes.RaDecToAxesXy(AlignmentModes.algGermanPolar, [6, -45], 8, true, East);
            Assert.That(axes, Is.EqualTo(new[] { 30.0, 45.0 }).Within(Tolerance));
        }

        [Test]
        public void AltAzPassesTheCoordinatesThrough() {
            var axes = Axes.RaDecToAxesXy(AlignmentModes.algAltAz, [123.4, 56.7], 8, false, East);
            Assert.That(axes, Is.EqualTo(new[] { 123.4, 56.7 }).Within(Tolerance));
        }

        [Test]
        public void PolarModeRangesTheAxes() {
            var axes = Axes.RaDecToAxesXy(AlignmentModes.algPolar, [-10, 280], 8, false, East);
            Assert.That(axes, Is.EqualTo(new[] { 350.0, -80.0 }).Within(Tolerance));
        }

        [Test]
        public void TheInputPositionIsNotModified() {
            double[] raDec = [6, 45];
            Axes.RaDecToAxesXy(AlignmentModes.algGermanPolar, raDec, 8, false, West);
            Assert.That(raDec, Is.EqualTo(new[] { 6.0, 45.0 }));
        }

        [Test]
        public void UnknownAlignmentModeAndBadPierSideAreRejected() {
            Assert.Multiple(() => {
                Assert.Throws<ArgumentOutOfRangeException>(() => Axes.RaDecToAxesXy(AlignmentModes.algUnknown, [1, 1], 8, false, East));
                Assert.Throws<ArgumentOutOfRangeException>(() => Axes.RaDecToAxesXy(AlignmentModes.algGermanPolar, [1, 1], 8, false, 5));
            });
        }
    }
}
