using GS.Point3DNina.Support;
using NUnit.Framework;

namespace GS.Point3DNina.Test {
    /// <summary>The polling time: match NINA's Device polling interval, or follow the mount more slowly than it.</summary>
    public class PollingTimeTests {
        [Test]
        public void ThePollingTimeMatchesNinaByDefault() => Assert.That(new ViewerSettings().PollingSeconds, Is.EqualTo(0));

        [TestCase(0.0, 2.0, 2.0)]    // match NINA
        [TestCase(5.0, 2.0, 5.0)]    // slower than NINA
        [TestCase(1.0, 2.0, 2.0)]    // faster than NINA isn't possible: NINA's interval
        [TestCase(0.0, double.NaN, 0.0)]
        public void TheModelFollowsAtTheSlowerOfThePollingTimeAndNinas(double polling, double nina, double expected)
            => Assert.That(Point3DDockable.EffectiveIntervalSeconds(polling, nina), Is.EqualTo(expected));

        [Test]
        public void EveryReportIsFollowedWhenMatchingNina() {
            Assert.Multiple(() => {
                Assert.That(Point3DDockable.IsDue(0.1, 0, 2), Is.True);
                Assert.That(Point3DDockable.IsDue(2.0, 0, 2), Is.True);
                Assert.That(Point3DDockable.IsDue(0.0, 1, 2), Is.True, "a time shorter than NINA's is NINA's");
            });
        }

        [TestCase(2.0, false)]
        [TestCase(3.9, false)]
        [TestCase(4.0, true)]    // 5 s time, reports 2 s apart: the one 4 s on is due (up to 1 s early counts)
        [TestCase(5.0, true)]
        public void ASlowerTimeSkipsTheReportsInBetween(double since, bool due)
            => Assert.That(Point3DDockable.IsDue(since, 5, 2), Is.EqualTo(due));

        [Test]
        public void TheGlideSpansTheSlowerInterval() {
            // NINA at 2 s: a 5 s polling time glides 7.5 s; a 1 minute one is capped at 10 s.
            Assert.That(Point3DDockable.GlideMs(Point3DDockable.EffectiveIntervalSeconds(5, 2)), Is.EqualTo(7500));
            Assert.That(Point3DDockable.GlideMs(Point3DDockable.EffectiveIntervalSeconds(60, 2)), Is.EqualTo(10000));
        }
    }
}
