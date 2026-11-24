using GS.Point3DNina.Shared;
using NUnit.Framework;
using Rng = GS.Point3DNina.Shared.Range;

namespace GS.Point3DNina.Test {
    public class RangeAndUnitsTests {
        private const double Tolerance = 1e-9;

        [TestCase(0, 0)]
        [TestCase(-1, 359)]
        [TestCase(360, 0)]
        [TestCase(359.9999999, 0)]
        [TestCase(720.5, 0.5)]
        [TestCase(-720.5, 359.5)]
        public void Range360WrapsIntoZeroToThreeSixty(double input, double expected)
            => Assert.That(Rng.Range360(input), Is.EqualTo(expected).Within(1e-6));

        [Test]
        public void Range360HandlesHugeValuesWithoutLooping() {
            var result = Rng.Range360(1e12 + 0.5);
            Assert.That(result, Is.GreaterThanOrEqualTo(0.0).And.LessThan(360.0));
        }

        [TestCase(0, 0)]
        [TestCase(24, 0)]
        [TestCase(-1, 23)]
        [TestCase(25.5, 1.5)]
        [TestCase(-48, 0)]
        public void Range24WrapsIntoZeroToTwentyFour(double input, double expected)
            => Assert.That(Rng.Range24(input), Is.EqualTo(expected).Within(Tolerance));

        [TestCase(0, 0)]
        [TestCase(12, 12)]
        [TestCase(-12, 12)]
        [TestCase(13, -11)]
        [TestCase(-13, 11)]
        [TestCase(36, 12)]
        public void Range12KeepsHourAnglesInHalfOpenInterval(double input, double expected)
            => Assert.That(Rng.Range12(input), Is.EqualTo(expected).Within(Tolerance));

        [TestCase(0, 0)]
        [TestCase(90, 90)]
        [TestCase(-90, -90)]
        [TestCase(100, 80)]
        [TestCase(-100, 80)]
        public void Range90ReflectsDeclinationsBackIntoRange(double input, double expected)
            => Assert.That(Rng.Range90(input), Is.EqualTo(expected).Within(Tolerance));

        [TestCase(180, 180)]
        [TestCase(-180, 180)]
        [TestCase(190, -170)]
        [TestCase(-190, 170)]
        [TestCase(540, 180)]
        public void Range180KeepsAnglesInHalfOpenInterval(double input, double expected)
            => Assert.That(Rng.Range180(input), Is.EqualTo(expected).Within(Tolerance));

        [TestCase(270, -90)]
        [TestCase(-90, -90)]
        [TestCase(-91, 269)]
        [TestCase(0, 0)]
        [TestCase(450, 90)]
        public void Range270KeepsAnglesBetweenMinusNinetyAndTwoSeventy(double input, double expected)
            => Assert.That(Rng.Range270(input), Is.EqualTo(expected).Within(Tolerance));

        [Test]
        public void NonFiniteInputGivesNaNInsteadOfLoopingForever() {
            Assert.Multiple(() => {
                Assert.That(double.IsNaN(Rng.Range360(double.NaN)));
                Assert.That(double.IsNaN(Rng.Range24(double.PositiveInfinity)));
                Assert.That(double.IsNaN(Rng.Range12(double.NegativeInfinity)));
                Assert.That(double.IsNaN(Rng.Range90(double.NaN)));
                Assert.That(double.IsNaN(Rng.Range180(double.NaN)));
                Assert.That(double.IsNaN(Rng.Range270(double.NaN)));
            });
        }

        [Test]
        public void RangeDoubleWrapsIntoZeroToLimitAndRejectsBadLimits() {
            Assert.Multiple(() => {
                Assert.That(Rng.RangeDouble(-1, 10), Is.EqualTo(9).Within(Tolerance));
                Assert.That(Rng.RangeDouble(10, 10), Is.EqualTo(0).Within(Tolerance));
                Assert.That(Rng.RangeDouble(25, 10), Is.EqualTo(5).Within(Tolerance));
                Assert.That(double.IsNaN(Rng.RangeDouble(1, 0)));
                Assert.That(double.IsNaN(Rng.RangeDouble(1, -5)));
                Assert.That(double.IsNaN(Rng.RangeDouble(1, double.NaN)));
            });
        }

        [Test]
        public void ArrayHelpersRangeEachElementWithTheRightRule() {
            Assert.Multiple(() => {
                Assert.That(Rng.RangeAltAz([100, -10]), Is.EqualTo(new[] { 80.0, 350.0 }).Within(Tolerance));
                Assert.That(Rng.RangeAz360Alt90([-10, 100]), Is.EqualTo(new[] { 350.0, 80.0 }).Within(Tolerance));
                Assert.That(Rng.RangeRaDec([25, 100]), Is.EqualTo(new[] { 1.0, 80.0 }).Within(Tolerance));
                Assert.That(Rng.RangeAxesXy([-10, 270]), Is.EqualTo(new[] { 350.0, -90.0 }).Within(Tolerance));
                Assert.That(Rng.RangeAxesYx([-10, 270]), Is.EqualTo(new[] { -90.0, 350.0 }).Within(Tolerance));
                Assert.That(Rng.Range180([190, -190]), Is.EqualTo(new[] { -170.0, 170.0 }).Within(Tolerance));
            });
        }

        [Test]
        public void DegreesAndRadiansConvertBothWays() {
            Assert.Multiple(() => {
                Assert.That(Units.Deg2Rad(180), Is.EqualTo(Math.PI).Within(Tolerance));
                Assert.That(Units.Deg2Rad1(90), Is.EqualTo(Math.PI / 2).Within(Tolerance));
                Assert.That(Units.Rad2Deg(Math.PI), Is.EqualTo(180).Within(Tolerance));
                Assert.That(Units.Rad2Deg1(Math.PI / 2), Is.EqualTo(90).Within(Tolerance));
                Assert.That(Units.Rad2Deg2(Math.PI), Is.EqualTo(180).Within(1e-6));
            });
            foreach (var degrees in new[] { -270.0, -45.5, 0, 0.001, 33.3, 180, 359.9 }) {
                Assert.That(Units.Rad2Deg(Units.Deg2Rad(degrees)), Is.EqualTo(degrees).Within(1e-9), $"{degrees} degrees");
            }
        }

        [Test]
        public void HoursAndRadiansConvertBothWays() {
            Assert.Multiple(() => {
                Assert.That(Units.Hrs2Rad(12), Is.EqualTo(Math.PI).Within(1e-9));
                Assert.That(Units.Hrs2Rad(6), Is.EqualTo(Math.PI / 2).Within(1e-9));
                Assert.That(Units.Rad2Hrs(Math.PI), Is.EqualTo(12).Within(1e-9));
            });
            foreach (var hours in new[] { -11.5, 0, 0.25, 6, 23.99 }) {
                Assert.That(Units.Rad2Hrs(Units.Hrs2Rad(hours)), Is.EqualTo(hours).Within(1e-9), $"{hours} hours");
            }
        }

        [Test]
        public void SexagesimalValuesBecomeDecimals() {
            Assert.Multiple(() => {
                Assert.That(Units.Deg2Dou(10, 30, 0), Is.EqualTo(10.5).Within(Tolerance));
                Assert.That(Units.Deg2Dou(-10, 30, 36), Is.EqualTo(-10.51).Within(Tolerance));
                Assert.That(Units.Deg2Dou(0, 15, 0), Is.EqualTo(0.25).Within(Tolerance));
                Assert.That(Units.Ra2Dou(5, 30, 0), Is.EqualTo(5.5).Within(Tolerance));
                Assert.That(Units.Ra2Dou(1, 1, 36), Is.EqualTo(1 + 1 / 60.0 + 36 / 3600.0).Within(Tolerance));
            });
        }
    }
}
