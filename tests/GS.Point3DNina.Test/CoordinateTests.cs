using GS.Point3DNina.Shared;
using NUnit.Framework;

namespace GS.Point3DNina.Test {
    public class CoordinateTests {
        private const double Tolerance = 1e-6;

        [Test]
        public void HourAngleIsSiderealTimeMinusRightAscension() {
            Assert.Multiple(() => {
                Assert.That(Coordinate.Ra2Ha12(8, 10), Is.EqualTo(2).Within(Tolerance));
                Assert.That(Coordinate.Ra2Ha12(10, 8), Is.EqualTo(-2).Within(Tolerance));
                Assert.That(Coordinate.Ra2Ha12(23, 1), Is.EqualTo(2).Within(Tolerance), "wraps over midnight");
                Assert.That(Coordinate.Ra2Ha24(10, 8), Is.EqualTo(22).Within(Tolerance));
                Assert.That(Coordinate.Ra2Ha24(8, 8), Is.EqualTo(0).Within(Tolerance));
            });
        }

        [Test]
        public void ObjectOnTheMeridianAtTheLatitudeIsAtTheZenith() {
            var altAz = Coordinate.HaDec2AltAz(0, 52, 52);
            Assert.That(altAz[0], Is.EqualTo(90).Within(Tolerance));
        }

        [Test]
        public void CelestialEquatorSixHoursWestIsOnTheWesternHorizonFromTheEquator() {
            var altAz = Coordinate.HaDec2AltAz(6, 0, 0);
            Assert.Multiple(() => {
                Assert.That(altAz[0], Is.EqualTo(0).Within(Tolerance), "altitude");
                Assert.That(altAz[1], Is.EqualTo(270).Within(Tolerance), "azimuth");
            });
        }

        [Test]
        public void PoleStandsAtAnAltitudeEqualToTheLatitudeDueNorth() {
            var altAz = Coordinate.HaDec2AltAz(3, 90, 40);
            Assert.Multiple(() => {
                Assert.That(altAz[0], Is.EqualTo(40).Within(Tolerance));
                Assert.That(altAz[1] < Tolerance || altAz[1] > 360 - Tolerance, Is.True, $"azimuth {altAz[1]}");
            });
        }

        [Test]
        public void RaDecGoesThroughTheHourAngle() {
            var viaHa = Coordinate.HaDec2AltAz(Coordinate.Ra2Ha12(5, 8), 20, 50);
            var direct = Coordinate.RaDec2AltAz(5, 20, 8, 50);
            Assert.That(direct, Is.EqualTo(viaHa).Within(Tolerance));
        }

        [TestCase(3.0, 20.0, 50.0)]
        [TestCase(-5.5, -35.0, -33.9)]
        [TestCase(1.25, 70.0, 10.0)]
        [TestCase(-9.0, 5.0, 60.0)]
        [TestCase(11.0, -10.0, 0.0)]
        public void HaDecToAltAzAndBackReturnsTheStartingPosition(double hourAngle, double dec, double latitude) {
            var altAz = Coordinate.HaDec2AltAz(hourAngle, dec, latitude);
            var back = Coordinate.AltAz2HaDec(altAz[0], altAz[1], latitude);
            Assert.Multiple(() => {
                Assert.That(back[0], Is.EqualTo(hourAngle).Within(1e-5), "hour angle");
                Assert.That(back[1], Is.EqualTo(dec).Within(1e-5), "declination");
            });
        }

        [Test]
        public void AltAzResultsStayInRange() {
            foreach (var lat in new[] { -60.0, 0.0, 45.0 }) {
                for (var ha = -12.0; ha <= 12.0; ha += 1.5) {
                    for (var dec = -90.0; dec <= 90.0; dec += 15) {
                        var altAz = Coordinate.HaDec2AltAz(ha, dec, lat);
                        Assert.That(altAz[0], Is.InRange(-90.0, 90.0), $"alt ha {ha} dec {dec} lat {lat}");
                        Assert.That(altAz[1], Is.InRange(0.0, 360.0), $"az ha {ha} dec {dec} lat {lat}");
                    }
                }
            }
        }
    }
}
