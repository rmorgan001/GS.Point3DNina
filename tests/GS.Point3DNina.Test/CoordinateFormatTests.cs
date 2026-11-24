using GS.Point3DNina.Support;
using NUnit.Framework;

namespace GS.Point3DNina.Test {
    public class CoordinateFormatTests {
        [Test]
        public void HoursFormatAsHoursMinutesSeconds() {
            Assert.Multiple(() => {
                Assert.That(CoordinateFormat.HoursToHms(5.5, "h ", ":", "", 2), Is.EqualTo("05h 30:00.00"));
                Assert.That(CoordinateFormat.HoursToHms(0), Is.EqualTo("00:00:00"));
                Assert.That(CoordinateFormat.HoursToHms(13.25), Is.EqualTo("13:15:00"));
                Assert.That(CoordinateFormat.HoursToHms(-2.5), Is.EqualTo("-02:30:00"));
            });
        }

        [Test]
        public void DegreesFormatAsDegreesMinutesSeconds() {
            Assert.Multiple(() => {
                Assert.That(CoordinateFormat.DegreesToDms(-10.5, "° ", ":", "", 2), Is.EqualTo("-10° 30:00.00"));
                Assert.That(CoordinateFormat.DegreesToDms(45.25, "° ", "' ", "\""), Is.EqualTo("45° 15' 00\""));
            });
        }

        [Test]
        public void RoundingNeverPrintsSixtySeconds() {
            Assert.Multiple(() => {
                Assert.That(CoordinateFormat.HoursToHms(1.99999999, ":", ":", "", 2), Is.EqualTo("02:00:00.00"));
                Assert.That(CoordinateFormat.DegreesToDms(29.9999999, "° ", ":", "", 2), Is.EqualTo("30° 00:00.00"));
            });
        }

        [Test]
        public void ValuesThatAreNotNumbersShowDashes() {
            Assert.Multiple(() => {
                Assert.That(CoordinateFormat.HoursToHms(double.NaN), Is.EqualTo("--"));
                Assert.That(CoordinateFormat.DegreesToDms(double.PositiveInfinity), Is.EqualTo("--"));
            });
        }
    }
}
