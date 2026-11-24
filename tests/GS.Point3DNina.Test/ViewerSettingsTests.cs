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

        [TestCase("{\"RaOffset\":500,\"DecOffset\":-500}", 90, -90)]
        [TestCase("{\"RaOffset\":-12,\"DecOffset\":45}", -12, 45)]
        public void OffsetsAreKeptWithinTheLists(string json, int ra, int dec) {
            var s = ViewerSettings.Parse(json);
            Assert.Multiple(() => {
                Assert.That(s.RaOffset, Is.EqualTo(ra));
                Assert.That(s.DecOffset, Is.EqualTo(dec));
            });
        }

        [TestCase("0", 0.0)]
        [TestCase("3", 3.0)]
        [TestCase("10", 10.0)]
        [TestCase("-1", 0.0)]
        [TestCase("1", 0.0)]
        [TestCase("4.5", 0.0)]
        [TestCase("11", 0.0)]
        [TestCase("1000000000", 0.0)]
        public void APollingTimeThatIsNotOfferedReturnsToMatchingNina(string value, double expected)
            => Assert.That(ViewerSettings.Parse("{\"PollingSeconds\":" + value + "}").PollingSeconds, Is.EqualTo(expected));

        [Test]
        public void AnUnusableColourReturnsToTheDefault() {
            Assert.Multiple(() => {
                Assert.That(ViewerSettings.Parse("{\"Color\":\"red\"}").Color, Is.EqualTo(ModelAppearance.DefaultColor));
                Assert.That(ViewerSettings.Parse("{\"Color\":\"#F2C84B\"}").Color, Is.EqualTo("#f2c84b"));
            });
        }

        [TestCase(1.0, 1.0, 5.0, true)]
        [TestCase(1.0, 1.0, 0.0, false)]            // no zoom distance
        [TestCase(1.0, 1.0, -3.0, false)]
        [TestCase(double.NaN, 1.0, 5.0, false)]
        [TestCase(1.0, double.PositiveInfinity, 5.0, false)]
        public void OnlyACameraWithFiniteNumbersAndAZoomCanBeUsed(double alpha, double beta, double radius, bool usable)
            => Assert.That(CameraView.IsUsable(new CameraView(alpha, beta, radius, 0, 0, 0)), Is.EqualTo(usable));

        [Test]
        public void ASavedCameraThatCannotBeUsedGivesTheDefaultView() {
            Assert.Multiple(() => {
                Assert.That(CameraView.IsUsable(null), Is.False);
                Assert.That(ViewerSettings.Parse("{\"Camera\":{\"Alpha\":1,\"Beta\":2}}").Camera, Is.Null, "no zoom distance");
                Assert.That(ViewerSettings.Parse("{\"Camera\":{\"Alpha\":1,\"Beta\":2,\"Radius\":-4}}").Camera, Is.Null);
            });
        }

        [Test]
        public void SavesFromSeveralThreadsNeverLoseOrCorruptTheFile() {
            var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"concurrent-{Guid.NewGuid():N}", "settings.json");
            try {
                var settings = new ViewerSettings { RaOffset = 12 };
                Parallel.For(0, 64, _ => settings.Save(path));
                Assert.Multiple(() => {
                    Assert.That(ViewerSettings.Parse(File.ReadAllText(path)).RaOffset, Is.EqualTo(12));
                    Assert.That(File.Exists(path + ".tmp"), Is.False);
                });
            } finally {
                Directory.Delete(Path.GetDirectoryName(path)!, true);
            }
        }

        [Test]
        public void AnOldForkMountKeepsTheDefaultMountsSuggestionsForLaterChanges() {
            var fork = Model3D.GetSuggestedParts(AssemblyMount.Fork);
            var defaults = Model3D.GetSuggestedParts(Model3D.DefaultAssemblyMount);
            var s = new ViewerSettings { Mount = AssemblyMount.Fork, Support = fork.Support, Ota = fork.Ota };
            Point3DOptions.MigrateFork(s);
            Assert.Multiple(() => {
                Assert.That(s.Mount, Is.EqualTo(Model3D.DefaultAssemblyMount));
                Assert.That(s.Support, Is.EqualTo(defaults.Support));
                Assert.That(s.Ota, Is.EqualTo(defaults.Ota));
            });
        }

        [Test]
        public void AnOldForkMountKeepsASupportOrOtaThatWasChanged() {
            var s = new ViewerSettings { Mount = AssemblyMount.Fork, Support = AssemblySupport.SmallPier, Ota = AssemblyOta.Newtonian };
            Point3DOptions.MigrateFork(s);
            Assert.Multiple(() => {
                Assert.That(s.Support, Is.EqualTo(AssemblySupport.SmallPier));
                Assert.That(s.Ota, Is.EqualTo(AssemblyOta.Newtonian));
            });
        }

        [Test]
        public void AMountThatIsNotAForkIsLeftAlone() {
            var s = new ViewerSettings { Mount = AssemblyMount.Heq5Pro, Support = AssemblySupport.LargePier, Ota = AssemblyOta.Refractor };
            Point3DOptions.MigrateFork(s);
            Assert.Multiple(() => {
                Assert.That(s.Mount, Is.EqualTo(AssemblyMount.Heq5Pro));
                Assert.That(s.Support, Is.EqualTo(AssemblySupport.LargePier));
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
            var s = ViewerSettings.Parse("{\"Camera\":{\"Alpha\":1,\"Beta\":2,\"Radius\":3,\"TargetX\":4,\"TargetY\":5,\"TargetZ\":6}}");
            Assert.That(s.Camera, Is.EqualTo(new CameraView(1, 2, 3, 4, 5, 6)));
        }
    }
}
