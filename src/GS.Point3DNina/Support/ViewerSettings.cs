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
    public bool OptionsOpen { get; set; } = true;
    /// <summary>The camera the user saved (angle, tilt, zoom, what it looks at); null for the default view.</summary>
    public CameraView? Camera { get; set; }

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NINA", "Plugins", "GS.Point3DNina", "settings.json");

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static ViewerSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<ViewerSettings>(File.ReadAllText(FilePath), Json) ?? new();
        }
        catch { /* a damaged file: start again from the defaults */ }
        return new();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json));
        }
        catch { /* the options are a convenience: never break the panel over them */ }
    }
}
