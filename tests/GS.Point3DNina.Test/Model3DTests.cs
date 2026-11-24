using GS.Point3DNina.Shared;
using GS.Point3DNina.Support;
using NUnit.Framework;

namespace GS.Point3DNina.Test {
    public class Model3DTests {
        private const AlignmentModes German = AlignmentModes.algGermanPolar;
        private const AlignmentModes AltAz = AlignmentModes.algAltAz;
        private const AlignmentModes Polar = AlignmentModes.algPolar;
        private const AlignmentModes Unknown = AlignmentModes.algUnknown;

        // ===== Model files =====

        [TestCase(AlignmentModes.algGermanPolar, PolarMode.Left, "")]
        [TestCase(AlignmentModes.algUnknown, PolarMode.Left, "")]
        [TestCase(AlignmentModes.algAltAz, PolarMode.Left, "AltAz")]
        [TestCase(AlignmentModes.algPolar, PolarMode.Left, "PolarLeft")]
        [TestCase(AlignmentModes.algPolar, PolarMode.Right, "PolarRight")]
        public void FileSuffixFollowsTheAlignmentMode(AlignmentModes mode, PolarMode side, string expected)
            => Assert.That(Model3D.GetSuffix(mode, side), Is.EqualTo(expected));

        [TestCase(Model3DType.Refractor, AlignmentModes.algGermanPolar, PolarMode.Left, "Refractor.obj")]
        [TestCase(Model3DType.Reflector, AlignmentModes.algAltAz, PolarMode.Left, "ReflectorAltAz.obj")]
        [TestCase(Model3DType.Default, AlignmentModes.algGermanPolar, PolarMode.Left, "Default.obj")]
        [TestCase(Model3DType.Default, AlignmentModes.algUnknown, PolarMode.Left, "Default.obj")]
        [TestCase(Model3DType.DualTelescope, AlignmentModes.algAltAz, PolarMode.Left, "DualTelescopeAltAz.obj")]
        public void ModelFileIsTheTypePlusTheSuffixWhenThatFileExists(Model3DType type, AlignmentModes mode, PolarMode side, string expected)
            => Assert.That(Model3D.GetModelFile(type, mode, side), Is.EqualTo(expected));

        [TestCase(Model3DType.DualTelescope, AlignmentModes.algGermanPolar, PolarMode.Left, "Default.obj", TestName = "DualTelescopeHasNoGermanPolarFile")]
        [TestCase(Model3DType.Refractor, AlignmentModes.algPolar, PolarMode.Left, "DualTelescopePolarLeft.obj", TestName = "PolarMountsOnlyHaveDualTelescopeLeft")]
        [TestCase(Model3DType.SchmidtCassegrain, AlignmentModes.algPolar, PolarMode.Right, "DualTelescopePolarRight.obj", TestName = "PolarMountsOnlyHaveDualTelescopeRight")]
        public void AMissingCombinationFallsBackToTheNearestModelForThatMount(Model3DType type, AlignmentModes mode, PolarMode side, string expected)
            => Assert.That(Model3D.GetModelFile(type, mode, side), Is.EqualTo(expected));

        [Test]
        public void ModelUrlIsTheFileInTheModelFolder()
            => Assert.That(Model3D.GetModelUrl(Model3DType.Refractor, AltAz, PolarMode.Left), Is.EqualTo("model3d/models/RefractorAltAz.obj"));

        // ===== Offsets and the pose =====

        [Test]
        public void AxisModelOffsetsAreOnlyDifferentForAltAz() {
            Assert.Multiple(() => {
                Assert.That(Model3D.GetAxisModelOffsets(AltAz), Is.EqualTo((0.0, 90.0, 0.0)));
                Assert.That(Model3D.GetAxisModelOffsets(German), Is.EqualTo((90.0, -90.0, 0.0)));
                Assert.That(Model3D.GetAxisModelOffsets(Polar), Is.EqualTo((90.0, -90.0, 0.0)));
                Assert.That(Model3D.GetAxisModelOffsets(Unknown), Is.EqualTo((90.0, -90.0, 0.0)));
            });
        }

        [Test]
        public void ZAxisIsNinetyForAltAzOtherwiseTheLatitudeRoundedToTwoPlaces() {
            Assert.Multiple(() => {
                Assert.That(Model3D.GetZAxis(AltAz, 51.5), Is.EqualTo(90.0));
                Assert.That(Model3D.GetZAxis(German, 51.5074), Is.EqualTo(51.51));
                Assert.That(Model3D.GetZAxis(German, -33.8688), Is.EqualTo(33.87), "the southern latitude is used as a positive tilt");
                Assert.That(Model3D.GetZAxis(Polar, 0), Is.EqualTo(0.0));
            });
        }

        [Test]
        public void OnlyAGermanMountPivotsAboutTheYAxisCentre() {
            Assert.Multiple(() => {
                Assert.That(Model3D.GetYAxisCentre(German), Is.EqualTo(-102.0));
                Assert.That(Model3D.GetYAxisCentre(AltAz), Is.EqualTo(0.0));
                Assert.That(Model3D.GetYAxisCentre(Polar), Is.EqualTo(0.0));
            });
        }

        [Test]
        public void RotateModelRoundsToThreePlacesAndNegatesTheSecondAxis() {
            Assert.That(Model3D.RotateModel(30.12345, 45.6789, false, German), Is.EqualTo((30.123, -45.679)));
        }

        [Test]
        public void RotateModelOffsetsTheSecondAxisByHalfATurnOnlyForASouthernGermanMount() {
            Assert.Multiple(() => {
                Assert.That(Model3D.RotateModel(30, 45, true, German), Is.EqualTo((30.0, -135.0)));
                Assert.That(Model3D.RotateModel(10, 20, true, AltAz), Is.EqualTo((10.0, -20.0)));
                Assert.That(Model3D.RotateModel(10, 20, true, Polar), Is.EqualTo((10.0, -20.0)));
            });
        }

        [Test]
        public void PoseForANorthernGermanMountAddsTheOffsetsAndAdjustments() {
            var pose = Model3D.GetPose(German, 50, 30, 45, raAdjust: 2, decAdjust: 3);
            Assert.That(pose, Is.EqualTo((X: 48.0, Y: -58.0, Z: -50.0, YCentre: -102.0)));
        }

        [Test]
        public void PoseForASouthernGermanMountTurnsTheRaAxisTheOtherWay() {
            var pose = Model3D.GetPose(German, -40, 30, 45, raAdjust: 0, decAdjust: 0);
            Assert.That(pose, Is.EqualTo((X: -45.0, Y: 60.0, Z: -40.0, YCentre: -102.0)));
        }

        [Test]
        public void PoseForAnAltAzMountUsesItsOwnOffsetsAndAVerticalAxis() {
            var pose = Model3D.GetPose(AltAz, 50, 100, 20, raAdjust: 2, decAdjust: 3);
            Assert.That(pose, Is.EqualTo((X: -17.0, Y: 192.0, Z: -90.0, YCentre: 0.0)));
        }

        [Test]
        public void PoseForAForkMountHasNoYAxisCentre() {
            var pose = Model3D.GetPose(Polar, 50, 0, 0, 0, 0);
            Assert.That(pose, Is.EqualTo((X: 90.0, Y: -90.0, Z: -50.0, YCentre: 0.0)));
        }

        [Test]
        public void HomePositionWhenDisconnectedIsTheMountAtZero() {
            var pose = Model3D.GetPose(German, 50, 0, 0, 0, 0);
            Assert.That(pose, Is.EqualTo((X: 90.0, Y: -90.0, Z: -50.0, YCentre: -102.0)));
        }

        [TestCase(AlignmentModes.algGermanPolar, 50.0, true)]
        [TestCase(AlignmentModes.algGermanPolar, 0.0, true)]
        [TestCase(AlignmentModes.algGermanPolar, -33.0, false)]
        [TestCase(AlignmentModes.algPolar, -33.0, false)]
        [TestCase(AlignmentModes.algAltAz, -33.0, true)]
        [TestCase(AlignmentModes.algAltAz, 50.0, true)]
        public void TheSouthernCompassIsOnlyUsedForEquatorialMountsInTheSouth(AlignmentModes mode, double latitude, bool north)
            => Assert.That(Model3D.UseNorthCompass(mode, latitude), Is.EqualTo(north));

        // ===== User offsets =====

        [Test]
        public void OffsetChoicesAreEveryWholeDegreeFromMinusNinetyToNinety() {
            Assert.Multiple(() => {
                Assert.That(Model3D.OffsetValues, Has.Count.EqualTo(181));
                Assert.That(Model3D.OffsetValues[0], Is.EqualTo(-90));
                Assert.That(Model3D.OffsetValues[^1], Is.EqualTo(90));
                Assert.That(Model3D.OffsetValues, Does.Contain(0));
            });
        }

        [TestCase(95.0, 90.0)]
        [TestCase(-95.0, -90.0)]
        [TestCase(12.4, 12.0)]
        [TestCase(-12.6, -13.0)]
        [TestCase(90.0, 90.0)]
        [TestCase(-90.0, -90.0)]
        public void ClampOffsetRoundsToWholeDegreesWithinTheLimits(double input, double expected)
            => Assert.That(Model3D.ClampOffset(input), Is.EqualTo(expected));

        [Test]
        public void ClampOffsetNeverReturnsNegativeZero() {
            var result = Model3D.ClampOffset(-0.4);
            Assert.Multiple(() => {
                Assert.That(result, Is.EqualTo(0.0));
                Assert.That(double.IsNegative(result), Is.False, "-0 would be written as \"-0\"");
            });
        }

        // ===== The assembled model =====

        [TestCase(AlignmentModes.algAltAz, AssemblyMount.Am5, true)]
        [TestCase(AlignmentModes.algAltAz, AssemblyMount.Wave150iPro, true)]
        [TestCase(AlignmentModes.algAltAz, AssemblyMount.Fork, true)]
        [TestCase(AlignmentModes.algAltAz, AssemblyMount.Eq6rPro, false)]
        [TestCase(AlignmentModes.algPolar, AssemblyMount.Fork, true)]
        [TestCase(AlignmentModes.algPolar, AssemblyMount.Eq6rPro, false)]
        [TestCase(AlignmentModes.algGermanPolar, AssemblyMount.Eq6rPro, true)]
        [TestCase(AlignmentModes.algGermanPolar, AssemblyMount.Fork, false)]
        [TestCase(AlignmentModes.algUnknown, AssemblyMount.Cem70, true)]
        [TestCase(AlignmentModes.algUnknown, AssemblyMount.Fork, false)]
        public void TheAssembledModelShowsOnlyWhenTheMountWorksTheWayTheRealOneDoes(AlignmentModes mode, AssemblyMount mount, bool shows)
            => Assert.That(Model3D.ShowsAssembly(mode, mount), Is.EqualTo(shows));

        [Test]
        public void SuggestedPartsSuitTheMount() {
            Assert.Multiple(() => {
                Assert.That(Model3D.GetSuggestedParts(AssemblyMount.Heq5Pro), Is.EqualTo((AssemblySupport.SmallTripod, AssemblyOta.Newtonian)));
                Assert.That(Model3D.GetSuggestedParts(AssemblyMount.Eq8rPro), Is.EqualTo((AssemblySupport.LargePier, AssemblyOta.RitcheyChretienTruss)));
                Assert.That(Model3D.GetSuggestedParts(AssemblyMount.Cem70), Is.EqualTo((AssemblySupport.MediumPier, AssemblyOta.SchmidtCassegrain)));
                Assert.That(Model3D.GetSuggestedParts(AssemblyMount.Fork), Is.EqualTo((AssemblySupport.LargeTripod, AssemblyOta.SchmidtCassegrain)));
                Assert.That(Model3D.GetSuggestedParts(AssemblyMount.Eq6rPro), Is.EqualTo((Model3D.DefaultAssemblySupport, Model3D.DefaultAssemblyOta)),
                    "the default mount starts with the default parts");
            });
        }

        [TestCase(100, 100)]
        [TestCase(50, 50)]
        [TestCase(200, 200)]
        [TestCase(80, 100)]
        [TestCase(0, 100)]
        [TestCase(-50, 100)]
        [TestCase(999, 100)]
        public void OtaSizeIsOneOfTheOfferedSizesElseTheMountsOwn(int saved, int expected)
            => Assert.That(Model3D.GetAssemblyOtaSize(saved), Is.EqualTo(expected));

        [Test]
        public void ObjOtasUseTheirOwnTubeAndBuiltOnesHaveNone() {
            Assert.Multiple(() => {
                Assert.That(Model3D.GetObjModel(AssemblyOta.Refractor), Is.EqualTo(Model3DType.Refractor));
                Assert.That(Model3D.GetObjModel(AssemblyOta.RitcheyChretienTruss), Is.EqualTo(Model3DType.RitcheyChretienTruss));
                Assert.That(Model3D.GetObjModel(AssemblyOta.Newtonian), Is.Null);
                Assert.That(Model3D.GetObjModel(AssemblyOta.MaksutovCassegrain), Is.Null);
            });
        }

        [Test]
        public void ABuiltOtaShowsAsTheNearestUsualModel() {
            Assert.Multiple(() => {
                Assert.That(Model3D.GetUsualModel(AssemblyOta.Newtonian), Is.EqualTo(Model3DType.Reflector));
                Assert.That(Model3D.GetUsualModel(AssemblyOta.MaksutovCassegrain), Is.EqualTo(Model3DType.SchmidtCassegrain));
                Assert.That(Model3D.GetUsualModel(AssemblyOta.Refractor), Is.EqualTo(Model3DType.Refractor));
            });
        }

        [Test]
        public void AnOldModelTypeMapsToItsOtaAndTheModelsWithoutOneToTheRefractor() {
            Assert.Multiple(() => {
                Assert.That(Model3D.GetOtaFor(Model3DType.Reflector), Is.EqualTo(AssemblyOta.Reflector));
                Assert.That(Model3D.GetOtaFor(Model3DType.SchmidtCassegrain), Is.EqualTo(AssemblyOta.SchmidtCassegrain));
                Assert.That(Model3D.GetOtaFor(Model3DType.Default), Is.EqualTo(AssemblyOta.Refractor));
                Assert.That(Model3D.GetOtaFor(Model3DType.DualTelescope), Is.EqualTo(AssemblyOta.Refractor));
            });
        }

        [TestCase("Tripod", AssemblySupport.MediumTripod)]
        [TestCase("Pier", AssemblySupport.MediumPier)]
        [TestCase("Pier10", AssemblySupport.MediumPier)]
        [TestCase("Pier6", AssemblySupport.SmallPier)]
        [TestCase("Pier12", AssemblySupport.LargePier)]
        [TestCase("LargeTripod", AssemblySupport.LargeTripod)]
        [TestCase("SmallPier", AssemblySupport.SmallPier)]
        [TestCase(null, AssemblySupport.MediumTripod)]
        [TestCase("", AssemblySupport.MediumTripod)]
        [TestCase("Gazebo", AssemblySupport.MediumTripod)]
        [TestCase("99", AssemblySupport.MediumTripod)]
        public void ASavedSupportKeepsItsMeaningAcrossRenames(string saved, AssemblySupport expected)
            => Assert.That(Model3D.ParseAssemblySupport(saved), Is.EqualTo(expected));

        // ===== Names =====

        [Test]
        public void DisplayNamesAreReadable() {
            Assert.Multiple(() => {
                Assert.That(Model3D.GetDisplayName(AssemblyMount.Heq5Pro), Is.EqualTo("Sky-Watcher HEQ5 Pro"));
                Assert.That(Model3D.GetDisplayName(AssemblyMount.Am5), Is.EqualTo("ZWO AM5"));
                Assert.That(Model3D.GetDisplayName(AssemblyMount.Cem40), Is.EqualTo("iOptron CEM40"));
                Assert.That(Model3D.GetDisplayName(AssemblyMount.Eq6rPro), Is.EqualTo("Sky-Watcher EQ6-R Pro"));
                Assert.That(Model3D.GetDisplayName(AssemblyMount.Fork), Is.EqualTo("Fork mount"));
                Assert.That(Model3D.GetDisplayName(AssemblySupport.SmallTripod), Is.EqualTo("Small tripod"));
                Assert.That(Model3D.GetDisplayName(AssemblySupport.LargePier), Is.EqualTo("Large pier"));
                Assert.That(Model3D.GetDisplayName(AssemblyOta.Newtonian), Is.EqualTo("Newtonian"));
                Assert.That(Model3D.GetDisplayName(AssemblyOta.MaksutovCassegrain), Is.EqualTo("Maksutov-Cassegrain"));
                Assert.That(Model3D.GetDisplayName(AssemblyOta.SchmidtCassegrain), Is.EqualTo("Schmidt-Cassegrain"));
                Assert.That(Model3D.GetDisplayName(AssemblyOta.RitcheyChretien), Is.EqualTo("Ritchey-Chrétien"));
                Assert.That(Model3D.GetDisplayName(Model3DType.RitcheyChretienTruss), Is.EqualTo("Ritchey-Chrétien Truss"));
                Assert.That(Model3D.GetDisplayName(Model3DType.DualTelescope), Is.EqualTo("Dual Telescope"));
                Assert.That(Model3D.GetDisplayName(Model3DType.Default), Is.EqualTo("Default"));
                Assert.That(Model3D.GetDisplayName(AltAz), Is.EqualTo("Alt-Az"));
                Assert.That(Model3D.GetDisplayName(Polar), Is.EqualTo("Polar (fork)"));
                Assert.That(Model3D.GetDisplayName(German), Is.EqualTo("German polar"));
                Assert.That(Model3D.GetDisplayName(Unknown), Is.EqualTo("Unknown"));
            });
        }

        // ===== Colour and background choices =====

        [Test]
        public void AnUnknownBackgroundFallsBackToTheFirstAndKeysIgnoreCase() {
            Assert.Multiple(() => {
                Assert.That(ModelAppearance.GetBackground(null).Key, Is.EqualTo(ModelAppearance.DefaultBackground));
                Assert.That(ModelAppearance.GetBackground("nonsense").Key, Is.EqualTo(ModelAppearance.DefaultBackground));
                Assert.That(ModelAppearance.GetBackground("heart").Key, Is.EqualTo("Heart"));
                Assert.That(ModelAppearance.GetBackground("STARS").Key, Is.EqualTo(ModelAppearance.StarFieldBackground));
            });
        }

        [Test]
        public void NormalizeBackgroundKeepsTheCustomPictureAndFixesEverythingElse() {
            Assert.Multiple(() => {
                Assert.That(ModelAppearance.NormalizeBackground("custom"), Is.EqualTo(ModelAppearance.CustomBackground));
                Assert.That(ModelAppearance.NormalizeBackground("m51"), Is.EqualTo("M51"));
                Assert.That(ModelAppearance.NormalizeBackground(null), Is.EqualTo(ModelAppearance.DefaultBackground));
                Assert.That(ModelAppearance.NormalizeBackground("bogus"), Is.EqualTo(ModelAppearance.DefaultBackground));
            });
        }

        [Test]
        public void BackgroundUrlsPointIntoTheBackgroundFolderOrAreNull() {
            Assert.Multiple(() => {
                Assert.That(ModelAppearance.GetBackground("NorthAmerica").Url, Is.EqualTo("model3d/backgrounds/NorthAmerica.jpg"));
                Assert.That(ModelAppearance.GetBackground("None").Url, Is.Null);
                Assert.That(ModelAppearance.GetBackground(ModelAppearance.StarFieldBackground).Url, Is.Null);
                Assert.That(ModelAppearance.GetBackground(ModelAppearance.StarFieldBackground).Generated, Is.True);
            });
        }

        [TestCase("#ABCDEF", "#abcdef")]
        [TestCase("#52b788", "#52b788")]
        [TestCase(null, "#52b788")]
        [TestCase("", "#52b788")]
        [TestCase("#abc", "#52b788")]
        [TestCase("#12345g", "#52b788")]
        [TestCase("red", "#52b788")]
        [TestCase("#1234567", "#52b788")]
        [TestCase("#123456\n", "#52b788")]
        public void ColoursAreSixDigitHexLowerCasedElseTheDefaultGreen(string input, string expected)
            => Assert.That(ModelAppearance.NormalizeColor(input), Is.EqualTo(expected));

        [Test]
        public void EveryColourPresetIsAValidColourAndNamed() {
            Assert.Multiple(() => {
                foreach (var preset in ModelAppearance.ColorPresets) {
                    Assert.That(ModelAppearance.NormalizeColor(preset.Hex), Is.EqualTo(preset.Hex), preset.Name);
                    Assert.That(preset.Name, Is.Not.Empty);
                }
                Assert.That(ModelAppearance.ColorPresets.Select(p => p.Hex), Is.Unique.IgnoreCase);
            });
        }
    }
}
