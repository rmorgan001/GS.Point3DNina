using GS.Point3DNina.Shared;
using GS.Point3DNina.Support;
using NUnit.Framework;
using System.IO;

namespace GS.Point3DNina.Test {
    public class ViewerSettingsTests {
        [Test]
        public void AValueThatNoLongerFitsOnlyLosesThatSetting() {
            var s = ViewerSettings.Parse("{\"Theme\":\"Night\",\"Model\":\"NoSuchModel\",\"ShowGrid\":false,\"OtaSize\":\"big\"}");
            Assert.Multiple(() => {
                Assert.That(s.Theme, Is.EqualTo(ViewerTheme.Night));
                Assert.That(s.ShowGrid, Is.False);
                Assert.That(s.Model, Is.EqualTo(new ViewerSettings().Model));
                Assert.That(s.OtaSize, Is.EqualTo(new ViewerSettings().OtaSize));
            });
        }

        [Test]
        public void ANumberThatNamesNoEnumMemberKeepsTheDefault() {
            var s = ViewerSettings.Parse("{\"Theme\":7,\"ModeOverride\":99,\"ShowGrid\":false}");
            Assert.Multiple(() => {
                Assert.That(s.Theme, Is.EqualTo(ViewerTheme.Dark));
                Assert.That(s.ModeOverride, Is.Null);
                Assert.That(s.ShowGrid, Is.False);
            });
        }

        [Test]
        public void SavingLeavesNoTemporaryFileBehind() {
            var dir = Path.Combine(Path.GetTempPath(), "gs-point3d-test-" + Guid.NewGuid().ToString("N"));
            try {
                var file = Path.Combine(dir, "settings.json");
                new ViewerSettings().Save(file);
                new ViewerSettings { ShowGrid = false }.Save(file);
                Assert.That(Directory.GetFiles(dir), Is.EqualTo(new[] { file }));
                Assert.That(ViewerSettings.Load(file).ShowGrid, Is.False);
            } finally {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        [Test]
        public void EarlierSupportNamesAreStillUnderstood()
            => Assert.That(ViewerSettings.Parse("{\"Support\":\"Pier6\"}").Support, Is.EqualTo(AssemblySupport.SmallPier));

        [TestCase("")]
        [TestCase("not json")]
        [TestCase("[1,2]")]
        [TestCase("null")]
        public void ADamagedFileGivesTheDefaults(string json)
            => Assert.That(ViewerSettings.Parse(json).Theme, Is.EqualTo(ViewerTheme.Dark));

        [Test]
        public void SettingsSurviveSavingAndLoading() {
            var dir = Path.Combine(Path.GetTempPath(), "gs-point3d-test-" + Guid.NewGuid().ToString("N"));
            var file = Path.Combine(dir, "nested", "settings.json");
            try {
                new ViewerSettings {
                    Theme = ViewerTheme.Dusk, PollingSeconds = 10, ShowGrid = false, ShowHeading = true, RaOffset = -12,
                    Support = AssemblySupport.LargePier, ModeOverride = AlignmentModes.algAltAz,
                    Camera = new CameraView(1, 2, 3, 4, 5, 6)
                }.Save(file);
                var s = ViewerSettings.Load(file);
                Assert.Multiple(() => {
                    Assert.That(s.Theme, Is.EqualTo(ViewerTheme.Dusk));
                    Assert.That(s.ShowGrid, Is.False);
                    Assert.That(s.PollingSeconds, Is.EqualTo(10));
                    Assert.That(s.ShowHeading, Is.True);
                    Assert.That(s.RaOffset, Is.EqualTo(-12));
                    Assert.That(s.Support, Is.EqualTo(AssemblySupport.LargePier));
                    Assert.That(s.ModeOverride, Is.EqualTo(AlignmentModes.algAltAz));
                    Assert.That(s.Camera, Is.EqualTo(new CameraView(1, 2, 3, 4, 5, 6)));
                });
            } finally {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        [Test]
        public void ALoadFromAMissingFileGivesTheDefaults()
            => Assert.That(ViewerSettings.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "none.json")).Theme, Is.EqualTo(ViewerTheme.Dark));

        [Test]
        public void ASavedViewIsRead() {
            var s = ViewerSettings.Parse("{\"Camera\":{\"alpha\":1,\"beta\":2,\"radius\":3,\"targetX\":4,\"targetY\":5,\"targetZ\":6}}");
            Assert.That(s.Camera, Is.Not.Null);
        }
    }
}
