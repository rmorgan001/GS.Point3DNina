using GS.Point3DNina.Shared;
using GS.Point3DNina.Support;
using NUnit.Framework;
using System.IO;
using System.Text.RegularExpressions;

namespace GS.Point3DNina.Test {
    /// <summary>
    /// The plugin names its models, backgrounds and viewer parts in C# and the viewer looks them up in model3d.js.
    /// A typo or a missing file shows as a blank or half-built panel in NINA, so these tests check every name
    /// resolves to a file or a viewer entry.
    /// </summary>
    public class AssetIntegrityTests {
        private static string AssetsDirectory => Path.Combine(
            Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!, "Assets");
        private static string ModelDirectory => Path.Combine(AssetsDirectory, "model3d");
        private static string ViewerScript => File.ReadAllText(Path.Combine(ModelDirectory, "model3d.js"));

        [Test]
        public void ViewerFilesAreDeployedWithThePlugin() {
            Assert.Multiple(() => {
                Assert.That(File.Exists(Path.Combine(AssetsDirectory, "viewer.html")), "viewer.html");
                Assert.That(File.Exists(Path.Combine(ModelDirectory, "model3d.js")), "model3d.js");
                Assert.That(File.Exists(Path.Combine(ModelDirectory, "babylon.custom.min.js")), "babylon.custom.min.js");
                Assert.That(File.Exists(Path.Combine(ModelDirectory, "compassN.png")), "compassN.png");
                Assert.That(File.Exists(Path.Combine(ModelDirectory, "compassS.png")), "compassS.png");
            });
        }

        [Test]
        public void ViewerPageLoadsTheScriptsItShips() {
            var html = File.ReadAllText(Path.Combine(AssetsDirectory, "viewer.html"));
            // The page loads its script as an ES module import, or with a script tag's src.
            var scripts = Regex.Matches(html, "<script[^>]*\\ssrc=[\"']([^\"']+)[\"']").Select(m => m.Groups[1].Value)
                .Concat(Regex.Matches(html, "import\\s[^;]*?from\\s*[\"']([^\"']+)[\"']").Select(m => m.Groups[1].Value))
                .Where(s => !s.StartsWith("http", StringComparison.OrdinalIgnoreCase)).ToList();
            Assert.That(scripts, Is.Not.Empty, "viewer.html loads no local scripts");
            foreach (var script in scripts) {
                Assert.That(File.Exists(Path.Combine(AssetsDirectory, script.Replace('/', Path.DirectorySeparatorChar))),
                    Is.True, $"viewer.html loads {script}");
            }
        }

        [Test]
        public void ViewerPageHasTheCompassHeadingReadoutAndCommand() {
            var html = File.ReadAllText(Path.Combine(AssetsDirectory, "viewer.html"));
            Assert.Multiple(() => {
                Assert.That(html, Does.Contain("id=\"heading\""), "the readout button");
                Assert.That(html, Does.Contain("heading-needle"), "the needle model3d.js turns");
                Assert.That(html, Does.Contain("heading-value"), "the bearing model3d.js writes");
                Assert.That(html, Does.Contain("setHeading:"), "the command the host sends");
                Assert.That(ViewerScript, Does.Contain("export function setHeadingElement"));
            });
        }

        [Test]
        public void EveryThemeHasItsColoursInTheViewerPageAndTheSceneKnowsIt() {
            var html = File.ReadAllText(Path.Combine(AssetsDirectory, "viewer.html"));
            Assert.Multiple(() => {
                Assert.That(html, Does.Contain("setTheme:"), "the command the host sends");
                foreach (var theme in Enum.GetValues<ViewerTheme>().Where(t => t != ViewerTheme.Dark)) {
                    var name = theme.ToString().ToLowerInvariant();
                    Assert.That(html, Does.Contain($":root[data-theme=\"{name}\"]"), $"colours for {theme}");
                    Assert.That(html, Does.Contain($"'{name}'"), $"{theme} is accepted by setTheme");
                }
                // Dusk and Night also tint the whole picture to one colour in the viewer.
                Assert.That(KeysOf(ViewerScript, "LOOK_FILTERS"), Is.EquivalentTo(new[] { "night", "dusk" }));
            });
        }

        [Test]
        public void ViewerPageHasTheCoordinateChipsAndCommand() {
            var html = File.ReadAllText(Path.Combine(AssetsDirectory, "viewer.html"));
            Assert.Multiple(() => {
                Assert.That(html, Does.Contain("id=\"coords\""), "the chips container");
                Assert.That(html, Does.Contain("setCoordinates:"), "the command the host sends");
                Assert.That(html, Does.Contain("toggleRa"), "the RA double-click event the host handles");
            });
        }

        [Test]
        public void CoordinatesAreOnByDefaultWithEveryItemShown() {
            var settings = new ViewerSettings();
            Assert.Multiple(() => {
                Assert.That(settings.ShowCoordinates, Is.True);
                Assert.That(new[] { settings.ShowRa, settings.ShowDec, settings.ShowAz, settings.ShowAlt, settings.ShowLha, settings.ShowLst },
                    Is.All.True);
                Assert.That(settings.RaInDegrees, Is.False);
            });
        }

        [Test]
        public void ThemeDefaultsToDark() => Assert.That(new ViewerSettings().Theme, Is.EqualTo(ViewerTheme.Dark));

        [Test]
        public void CompassHeadingIsOffByDefault() => Assert.That(new ViewerSettings().ShowHeading, Is.False);

        public static IEnumerable<TestCaseData> ModelCombinations() {
            foreach (var type in Enum.GetValues<Model3DType>())
            foreach (var mode in Enum.GetValues<AlignmentModes>())
            foreach (var polar in Enum.GetValues<PolarMode>())
                yield return new TestCaseData(type, mode, polar).SetName($"ModelFileExists({type}, {mode}, {polar})");
        }

        [TestCaseSource(nameof(ModelCombinations))]
        public void EveryModelChoiceResolvesToAnObjFileOnDisk(Model3DType type, AlignmentModes mode, PolarMode polar) {
            var url = Model3D.GetModelUrl(type, mode, polar);
            Assert.That(url, Does.StartWith(Model3D.ModelFolder).And.EndsWith(".obj"));
            Assert.That(File.Exists(Path.Combine(AssetsDirectory, url.Replace('/', Path.DirectorySeparatorChar))),
                Is.True, $"{url} is missing");
        }

        [Test]
        public void EveryObjFileOnDiskCanBeChosen() {
            var chosen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var type in Enum.GetValues<Model3DType>())
            foreach (var mode in Enum.GetValues<AlignmentModes>())
            foreach (var polar in Enum.GetValues<PolarMode>())
                chosen.Add(Model3D.GetModelFile(type, mode, polar));
            var onDisk = Directory.GetFiles(Path.Combine(ModelDirectory, "models"), "*.obj").Select(Path.GetFileName);
            Assert.That(onDisk.Except(chosen, StringComparer.OrdinalIgnoreCase), Is.Empty, "shipped but never selectable");
        }

        [Test]
        public void EveryBackgroundImageExistsAndEveryKeyIsUnique() {
            Assert.Multiple(() => {
                foreach (var background in ModelAppearance.Backgrounds) {
                    if (background.File is null) {
                        Assert.That(background.Url, Is.Null, background.Key);
                        continue;
                    }
                    Assert.That(File.Exists(Path.Combine(AssetsDirectory, background.Url!.Replace('/', Path.DirectorySeparatorChar))),
                        Is.True, $"{background.Key}: {background.Url} is missing");
                }
                Assert.That(ModelAppearance.Backgrounds.Select(b => b.Key), Is.Unique.IgnoreCase);
                Assert.That(ModelAppearance.Backgrounds.Select(b => b.Name), Is.Unique);
            });
        }

        [Test]
        public void EveryBackgroundImageOnDiskIsOffered() {
            var offered = ModelAppearance.Backgrounds.Where(b => b.File is not null).Select(b => b.File!);
            var onDisk = Directory.GetFiles(Path.Combine(ModelDirectory, "backgrounds")).Select(Path.GetFileName);
            Assert.That(onDisk.Except(offered, StringComparer.OrdinalIgnoreCase), Is.Empty, "shipped but never offered");
        }

        [Test]
        public void TheDefaultBackgroundAndColorAreOffered() {
            Assert.Multiple(() => {
                Assert.That(ModelAppearance.Backgrounds.Select(b => b.Key), Does.Contain(ModelAppearance.DefaultBackground));
                Assert.That(ModelAppearance.ColorPresets.Select(c => c.Hex), Does.Contain(ModelAppearance.DefaultColor));
                Assert.That(ModelAppearance.NormalizeColor(ModelAppearance.DefaultColor), Is.EqualTo(ModelAppearance.DefaultColor));
            });
        }

        [Test]
        public void EveryAssemblyPartHasAMatchingEntryInTheViewer() {
            var script = ViewerScript;
            var supports = KeysOf(script, "ASSEMBLY_SUPPORTS");
            var mounts = KeysOf(script, "ASSEMBLY_MOUNTS");
            var builtOtas = KeysOf(script, "ASSEMBLY_BUILT_OTAS");
            var objOtas = ListOf(script, "ASSEMBLY_OTAS");
            var cardinalSizes = KeysOf(script, "CARDINAL_SIZES");
            Assert.Multiple(() => {
                foreach (var support in Enum.GetValues<AssemblySupport>())
                    Assert.That(supports, Does.Contain(support.ToString().ToLowerInvariant()), $"support {support}");
                foreach (var mount in Enum.GetValues<AssemblyMount>())
                    Assert.That(mounts, Does.Contain(mount.ToString().ToLowerInvariant()), $"mount {mount}");
                foreach (var size in Enum.GetValues<CardinalSize>())
                    Assert.That(cardinalSizes, Does.Contain(size.ToString().ToLowerInvariant()), $"cardinal size {size}");
                foreach (var ota in Enum.GetValues<AssemblyOta>()) {
                    var known = Model3D.GetObjModel(ota) is not null ? objOtas : builtOtas;
                    Assert.That(known, Does.Contain(ota.ToString()), $"OTA {ota}");
                }
            });
        }

        [Test]
        public void EveryViewerEntryHasAnEnumValue() {
            var script = ViewerScript;
            Assert.Multiple(() => {
                Assert.That(KeysOf(script, "ASSEMBLY_SUPPORTS"),
                    Is.EquivalentTo(Enum.GetNames<AssemblySupport>().Select(n => n.ToLowerInvariant())));
                Assert.That(KeysOf(script, "ASSEMBLY_MOUNTS"),
                    Is.EquivalentTo(Enum.GetNames<AssemblyMount>().Select(n => n.ToLowerInvariant())));
                Assert.That(KeysOf(script, "CARDINAL_SIZES"),
                    Is.EquivalentTo(Enum.GetNames<CardinalSize>().Select(n => n.ToLowerInvariant())));
                Assert.That(KeysOf(script, "ASSEMBLY_BUILT_OTAS").Concat(ListOf(script, "ASSEMBLY_OTAS")),
                    Is.EquivalentTo(Enum.GetNames<AssemblyOta>()));
            });
        }

        [Test]
        public void ObjOtasComeFromModelsThatExistOnDisk() {
            foreach (var ota in Model3D.AssemblyOtas.Where(o => Model3D.GetObjModel(o) is not null)) {
                var url = Model3D.GetModelUrl(Model3D.GetObjModel(ota)!.Value, AlignmentModes.algGermanPolar, PolarMode.Left);
                Assert.That(File.Exists(Path.Combine(AssetsDirectory, url.Replace('/', Path.DirectorySeparatorChar))),
                    Is.True, $"{ota}: {url}");
            }
        }

        [Test]
        public void EveryAssemblyChoiceHasADisplayNameAndSuggestedPartsThatExist() {
            Assert.Multiple(() => {
                foreach (var mount in Enum.GetValues<AssemblyMount>()) {
                    Assert.That(Model3D.GetDisplayName(mount), Is.Not.Empty, $"mount {mount}");
                    var (support, ota) = Model3D.GetSuggestedParts(mount);
                    Assert.That(Enum.IsDefined(support), $"{mount} suggests support {support}");
                    Assert.That(Enum.IsDefined(ota), $"{mount} suggests OTA {ota}");
                }
                foreach (var support in Enum.GetValues<AssemblySupport>())
                    Assert.That(Model3D.GetDisplayName(support), Is.Not.Empty, $"support {support}");
                foreach (var ota in Enum.GetValues<AssemblyOta>())
                    Assert.That(Model3D.GetDisplayName(ota), Is.Not.Empty, $"OTA {ota}");
                Assert.That(Model3D.AssemblyOtaSizes, Does.Contain(Model3D.DefaultAssemblyOtaSize));
            });
        }

        /// <summary>The property names of <c>const NAME = { ... };</c> in the viewer script, lower-cased as written.</summary>
        private static List<string> KeysOf(string script, string name) {
            var body = Block(script, name, '{', '}');
            // Keys sit at the start of a line (multi-line tables) or after a brace or comma (one-line tables).
            // Entries are single-line values, so a nested key such as "tripod:" is told apart by its indentation.
            var multiLine = Regex.Matches(body, @"^[ \t]{4}([A-Za-z0-9_]+)\s*:", RegexOptions.Multiline)
                .Select(m => m.Groups[1].Value).ToList();
            if (multiLine.Count > 0) return multiLine;
            return Regex.Matches(body, @"[{,]\s*([A-Za-z0-9_]+)\s*:").Select(m => m.Groups[1].Value).ToList();
        }

        /// <summary>The string items of <c>const NAME = [ 'a', 'b' ];</c>.</summary>
        private static List<string> ListOf(string script, string name)
            => Regex.Matches(Block(script, name, '[', ']'), "['\"]([^'\"]+)['\"]").Select(m => m.Groups[1].Value).ToList();

        private static string Block(string script, string name, char open, char close) {
            var start = script.IndexOf($"const {name} = {open}", StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), $"model3d.js has no 'const {name}'");
            var begin = script.IndexOf(open, start);
            var depth = 0;
            for (var i = begin; i < script.Length; i++) {
                if (script[i] == open) depth++;
                else if (script[i] == close && --depth == 0) return script[begin..(i + 1)];
            }
            throw new AssertionException($"model3d.js: unbalanced '{name}'");
        }
    }
}
