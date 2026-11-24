#nullable enable
using GS.Point3DNina.Shared;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GS.Point3DNina.Support;

/// <summary>The 3D view's options, kept between sessions in a small JSON file next to the plugin's other data.</summary>
public sealed class ViewerSettings
{
    public Model3DType Model { get; set; } = Model3DType.Refractor;
    /// <summary>The web app always shows the assembled mount (tripod, mount, counterweights) when the mount type allows it.</summary>
    public bool UseAssembly { get; set; } = true;
    public AssemblyMount Mount { get; set; } = Model3D.DefaultAssemblyMount;
    public AssemblySupport Support { get; set; } = Model3D.DefaultAssemblySupport;
    public AssemblyOta Ota { get; set; } = Model3D.DefaultAssemblyOta;
    public int OtaSize { get; set; } = Model3D.DefaultAssemblyOtaSize;
    /// <summary>Null follows the connected telescope's alignment mode.</summary>
    public AlignmentModes? ModeOverride { get; set; }
    public PolarMode PolarMode { get; set; } = PolarMode.Left;
    public string Color { get; set; } = ModelAppearance.DefaultColor;
    public string Background { get; set; } = ModelAppearance.DefaultBackground;
    public ViewerTheme Theme { get; set; } = ViewerTheme.Dark;
    public bool ShowGrid { get; set; } = true;
    /// <summary>The coordinate chips over the top of the scene (the mount's RA, Dec, Az, Alt, hour angle and sidereal time).</summary>
    public bool ShowCoordinates { get; set; } = true;
    public bool ShowRa { get; set; } = true;
    public bool ShowDec { get; set; } = true;
    public bool ShowAz { get; set; } = true;
    public bool ShowAlt { get; set; } = true;
    public bool ShowLha { get; set; } = true;
    public bool ShowLst { get; set; } = true;
    /// <summary>RA shown in degrees instead of hours (double-click the RA chip to switch).</summary>
    public bool RaInDegrees { get; set; }
    public bool ShowCardinals { get; set; } = true;
    /// <summary>The compass readout over the scene: a needle pointing north and the bearing the camera faces. Click it to face north.</summary>
    public bool ShowHeading { get; set; }
    public CardinalSize CardinalSize { get; set; } = CardinalSize.Medium;
    public int RaOffset { get; set; }
    public int DecOffset { get; set; }
    /// <summary>The camera the user saved (angle, tilt, zoom, what it looks at); null for the default view.</summary>
    public CameraView? Camera { get; set; }

    /// <summary>Where the settings are kept.</summary>
    public static string DefaultFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NINA", "Plugins", "GS.Point3DNina", "settings.json");

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static ViewerSettings Load(string? path = null)
    {
        path ??= DefaultFilePath;
        try
        {
            if (File.Exists(path))
                return Parse(File.ReadAllText(path));
        }
        catch { /* an unreadable file: start again from the defaults */ }
        return new();
    }

    /// <summary>
    /// Reads settings one at a time, so a value that no longer fits (a renamed enum member, a hand edit) loses only
    /// that setting to its default instead of resetting everything. A damaged file gives the defaults.
    /// </summary>
    public static ViewerSettings Parse(string json)
    {
        var settings = new ViewerSettings();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return settings;
            foreach (var property in typeof(ViewerSettings).GetProperties())
            {
                if (!property.CanWrite) continue;
                var match = doc.RootElement.EnumerateObject().FirstOrDefault(p => string.Equals(p.Name, property.Name, StringComparison.OrdinalIgnoreCase));
                if (match.Value.ValueKind == JsonValueKind.Undefined) continue;
                try
                {
                    object? value = property.PropertyType == typeof(AssemblySupport)
                        ? Model3D.ParseAssemblySupport(match.Value.ValueKind == JsonValueKind.String ? match.Value.GetString() : null)
                        : match.Value.Deserialize(property.PropertyType, Json);
                    var enumType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                    if (value != null && enumType.IsEnum && !Enum.IsDefined(enumType, value)) continue; // a number that names no member
                    if (value != null || !property.PropertyType.IsValueType)
                        property.SetValue(settings, value);
                }
                catch { /* this one setting is unusable: keep its default */ }
            }
        }
        catch { /* a damaged file */ }
        return settings;
    }

    public void Save(string? path = null)
    {
        path ??= DefaultFilePath;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            // Write beside the file and swap it in, so a crash part-way never leaves a half-written settings file.
            var temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(this, Json));
            File.Move(temp, path, true);
        }
        catch { /* the options are a convenience: never break the panel over them */ }
    }
}
