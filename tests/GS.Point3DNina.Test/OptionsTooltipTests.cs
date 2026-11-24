#nullable enable
using NUnit.Framework;
using System.IO;
using System.Xml.Linq;

namespace GS.Point3DNina.Test {
    /// <summary>
    /// Every option on the plugin's settings page has a tooltip, and it is on its own row. Tooltips are written by hand
    /// beside each label, so one pasted onto the next row (it has happened) would describe the wrong option.
    /// </summary>
    public class OptionsTooltipTests {
        private static readonly XNamespace Wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        // Each option's label, and a word its own tooltip has to contain.
        private static readonly (string Label, string Word)[] Options = [
            ("Assembled model", "separate parts"), ("Ground grid", "grid on the ground"), ("N E S W", "North, East, South"),
            ("Coordinates", "position as chips"),
            ("RA", "Right Ascension."), ("Dec", "Declination"), ("Az", "Azimuth"), ("Alt", "Altitude"),
            ("LHA", "Hour Angle"), ("LST", "Sidereal"), ("RA in degrees", "instead of hours"),
            ("Compass heading", "compass"),
            ("Mount", "assembled model"), ("Support", "pier or tripod"), ("OTA", "optical tube"),
            ("OTA size %", "percentage"), ("Telescope", "ready-made"), ("Mount type", "Follow telescope"),
            ("Fork side", "fork"), ("Theme", "Night"), ("Background", "behind the model"),
            ("Colour", "swatch"), ("N E S W size", "letters"), ("Polling time", "follows the mount"),
            ("View", "camera view"), ("RA offset °", "RA axis"), ("Dec offset °", "Dec axis")
        ];

        private static XDocument Load() {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent) {
                var file = Path.Combine(dir.FullName, "src", "GS.Point3DNina", "Views", "Point3DOptionsView.xaml");
                if (File.Exists(file)) return XDocument.Load(file);
            }
            throw new FileNotFoundException("Point3DOptionsView.xaml was not found above the test folder.");
        }

        /// <summary>Each row's label and the tooltip on the row (a grid of label and control, or a switch with its text).</summary>
        private static Dictionary<string, string> RowTooltips() {
            var rows = new Dictionary<string, string>();
            foreach (var row in Load().Descendants().Where(e => e.Name == Wpf + "Grid" || e.Name == Wpf + "StackPanel")) {
                var label = row.Elements(Wpf + "TextBlock").Select(t => (string?)t.Attribute("Text")).FirstOrDefault(t => t != null);
                if (label == null) continue;
                // A switch is a CheckBox beside its text; a grid row has a control beside its label.
                rows[label] = (string?)row.Attribute("ToolTip") ?? "";
            }
            return rows;
        }

        [Test]
        public void EveryOptionHasATooltipOnItsOwnRow() {
            var rows = RowTooltips();
            Assert.Multiple(() => {
                foreach (var (label, word) in Options) {
                    Assert.That(rows.ContainsKey(label), $"'{label}' has no row on the options page");
                    if (!rows.TryGetValue(label, out var tip)) continue;
                    Assert.That(tip, Is.Not.Empty, $"'{label}' has no tooltip");
                    Assert.That(tip, Does.Contain(word).IgnoreCase, $"the tooltip on '{label}' describes another option: {tip}");
                }
            });
        }

        [Test]
        public void NoRowOnTheOptionsPageIsLeftWithoutATooltip() {
            var known = Options.Select(o => o.Label).ToHashSet();
            var rows = RowTooltips();
            Assert.Multiple(() => {
                foreach (var (label, tip) in rows) {
                    Assert.That(known, Does.Contain(label), $"'{label}' is a new row: add it to this test");
                    Assert.That(tip, Is.Not.Empty, $"'{label}' has no tooltip");
                }
            });
        }
    }
}
