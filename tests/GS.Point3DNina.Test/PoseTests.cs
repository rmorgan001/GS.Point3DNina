using NUnit.Framework;

namespace GS.Point3DNina.Test {
    /// <summary>A mount that reports no position gives NaN, which the viewer can't be sent (JSON has no NaN).</summary>
    public class PoseTests {
        [Test]
        public void AFinitePoseIsUsable() => Assert.That(Point3DDockable.IsUsablePose((1, -2, 3, 0)), Is.True);

        [TestCase(double.NaN, 0, 0, 0)]
        [TestCase(0, double.PositiveInfinity, 0, 0)]
        [TestCase(0, 0, double.NegativeInfinity, 0)]
        [TestCase(0, 0, 0, double.NaN)]
        public void ANonFinitePoseIsNot(double x, double y, double z, double yCentre)
            => Assert.That(Point3DDockable.IsUsablePose((x, y, z, yCentre)), Is.False);
    }
}
