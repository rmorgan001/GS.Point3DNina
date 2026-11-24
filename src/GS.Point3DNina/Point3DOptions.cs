using GS.Point3DNina.Shared;
using GS.Point3DNina.Support;
using NINA.Core.Utility;
using System.Windows.Input;

namespace GS.Point3DNina {

    /// <summary>What changed, so the view updates only that part of the scene.</summary>
    public enum ViewerChange { Pose, Model, Color, Background, Grid, Cardinals, Heading, Theme, Coordinates, View, Timing }

    /// <summary>One entry of the alignment mode list.</summary>
    public record ModeChoice(AlignmentModes? Mode, string Name);

    /// <summary>One entry of the mount list: the mount and its maker and product name.</summary>
    public record MountChoice(AssemblyMount Value, string Name);

    /// <summary>One entry of a list of values shown by readable name (the support and OTA lists).</summary>
    public record Choice<T>(T Value, string Name);

    /// <summary>
    /// The 3D model's options. One instance is shared by NINA's plugin settings page (where they are changed) and the
    /// 3D Model panel (which listens for <see cref="Changed"/> and updates the scene).
    /// </summary>
    public class Point3DOptions : BaseINPC {
        public static Point3DOptions Instance { get; } = new();

        private Point3DOptions() {
            Settings = ViewerSettings.Load();
            saveTimer = new System.Threading.Timer(_ => Flush(), null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            // The assembled model is a German equatorial mount only: a saved fork mount (an old list entry) isn't one.
            if (Settings.Mount == AssemblyMount.Fork) Settings.Mount = Model3D.DefaultAssemblyMount;
            PickColorCommand = new CommunityToolkit.Mvvm.Input.RelayCommand<object>(o => Color = (string)o);
            SaveViewCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => SaveViewRequested?.Invoke());
            ResetViewCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => Changed?.Invoke(ViewerChange.View));
            ForgetViewCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => StoreView(null));
        }

        public ViewerSettings Settings { get; }

        // Changes are written a moment after the last one, so stepping through a list doesn't write the file each time.
        private readonly System.Threading.Timer saveTimer;
        private int savePending;
        private const int SaveDelayMs = 300;

        private void ScheduleSave() {
            System.Threading.Volatile.Write(ref savePending, 1);
            saveTimer.Change(SaveDelayMs, System.Threading.Timeout.Infinite);
        }

        /// <summary>Writes any change not yet saved (called when the plugin shuts down).</summary>
        public void Flush() {
            if (System.Threading.Interlocked.Exchange(ref savePending, 0) == 1) Settings.Save();
        }

        /// <summary>Raised on any change that needs the scene updated.</summary>
        public event Action<ViewerChange> Changed;

        // ===== The lists the controls offer =====

        public IReadOnlyList<Model3DType> Models { get; } = [.. Enum.GetValues<Model3DType>()];
        public IReadOnlyList<MountChoice> Mounts { get; } =
            [.. Enum.GetValues<AssemblyMount>().Where(m => m != AssemblyMount.Fork).Select(m => new MountChoice(m, Model3D.GetDisplayName(m)))];
        public IReadOnlyList<Choice<AssemblySupport>> Supports { get; } =
            [.. Enum.GetValues<AssemblySupport>().Select(s => new Choice<AssemblySupport>(s, Model3D.GetDisplayName(s)))];
        public IReadOnlyList<Choice<AssemblyOta>> Otas { get; } =
            [.. Model3D.AssemblyOtas.Select(o => new Choice<AssemblyOta>(o, Model3D.GetDisplayName(o)))];
        public IReadOnlyList<int> OtaSizes { get; } = Model3D.AssemblyOtaSizes;
        public IReadOnlyList<int> Offsets { get; } = Model3D.OffsetValues;
        public IReadOnlyList<SceneBackground> Backgrounds { get; } = ModelAppearance.Backgrounds;
        public IReadOnlyList<ModelColorPreset> Colors { get; } = ModelAppearance.ColorPresets;
        /// <summary>The polling times offered: 0 matches NINA, the others are slower than it.</summary>
        public IReadOnlyList<Choice<double>> PollingTimes { get; } =
            [new(0, "Match NINA"), .. Enumerable.Range(3, 8).Select(s => new Choice<double>(s, $"{s} seconds"))];
        public IReadOnlyList<ViewerTheme> Themes { get; } = [.. Enum.GetValues<ViewerTheme>()];
        public IReadOnlyList<CardinalSize> CardinalSizes { get; } = [.. Enum.GetValues<CardinalSize>()];
        public IReadOnlyList<PolarMode> PolarModes { get; } = [.. Enum.GetValues<PolarMode>()];

        /// <summary>The mount type choices: only German polar when the assembled model shows.</summary>
        public IReadOnlyList<ModeChoice> ModeChoices => Settings.UseAssembly ? GermanOnly : AllModes;

        private static readonly ModeChoice[] GermanOnly = [new(AlignmentModes.algGermanPolar, "German polar")];
        private static readonly ModeChoice[] AllModes = [
            new(null, "Follow telescope"),
            new(AlignmentModes.algGermanPolar, "German polar"),
            new(AlignmentModes.algPolar, "Polar (fork)"),
            new(AlignmentModes.algAltAz, "Alt-Az")
        ];

        // ===== The options =====

        public ModeChoice SelectedMode {
            get => Settings.UseAssembly ? GermanOnly[0] : AllModes.FirstOrDefault(c => c.Mode == Settings.ModeOverride) ?? AllModes[0];
            set {
                if (value == null || Settings.UseAssembly) return; // cleared while the list is rebuilt; or fixed to German polar
                Settings.ModeOverride = value.Mode;
                Apply(ViewerChange.Model, nameof(SelectedMode));
            }
        }
        public Model3DType Model {
            get => Settings.Model;
            set { Settings.Model = value; Apply(ViewerChange.Model, nameof(Model)); }
        }
        public bool UseAssembly {
            get => Settings.UseAssembly;
            set {
                Settings.UseAssembly = value;
                RaisePropertyChanged(nameof(ModeChoices));
                RaisePropertyChanged(nameof(SelectedMode));
                Apply(ViewerChange.Model, nameof(UseAssembly));
            }
        }
        public AssemblyMount Mount {
            get => Settings.Mount;
            set {
                // The new mount's usual support and OTA, unless they were changed from the previous mount's.
                var previous = Model3D.GetSuggestedParts(Settings.Mount);
                var suggested = Model3D.GetSuggestedParts(value);
                var keepSupport = Settings.Support != previous.Support;
                var keepOta = Settings.Ota != previous.Ota;
                Settings.Mount = value;
                if (!keepSupport) Settings.Support = suggested.Support;
                if (!keepOta) Settings.Ota = suggested.Ota;
                RaisePropertyChanged(nameof(Support));
                RaisePropertyChanged(nameof(Ota));
                Apply(ViewerChange.Model, nameof(Mount));
            }
        }
        public AssemblySupport Support {
            get => Settings.Support;
            set { Settings.Support = value; Apply(ViewerChange.Model, nameof(Support)); }
        }
        public AssemblyOta Ota {
            get => Settings.Ota;
            set { Settings.Ota = value; Apply(ViewerChange.Model, nameof(Ota)); }
        }
        public int OtaSize {
            get => Settings.OtaSize;
            set { Settings.OtaSize = value; Apply(ViewerChange.Model, nameof(OtaSize)); }
        }
        public PolarMode PolarMode {
            get => Settings.PolarMode;
            set { Settings.PolarMode = value; Apply(ViewerChange.Model, nameof(PolarMode)); }
        }
        public string Color {
            get => Settings.Color;
            set { Settings.Color = ModelAppearance.NormalizeColor(value); Apply(ViewerChange.Color, nameof(Color)); }
        }
        public string BackgroundKey {
            get => ModelAppearance.GetBackground(Settings.Background).Key;
            set { Settings.Background = value; Apply(ViewerChange.Background, nameof(BackgroundKey)); }
        }
        public bool ShowCoordinates {
            get => Settings.ShowCoordinates;
            set { Settings.ShowCoordinates = value; Apply(ViewerChange.Coordinates, nameof(ShowCoordinates)); }
        }
        public bool ShowRa {
            get => Settings.ShowRa;
            set { Settings.ShowRa = value; Apply(ViewerChange.Coordinates, nameof(ShowRa)); }
        }
        public bool ShowDec {
            get => Settings.ShowDec;
            set { Settings.ShowDec = value; Apply(ViewerChange.Coordinates, nameof(ShowDec)); }
        }
        public bool ShowAz {
            get => Settings.ShowAz;
            set { Settings.ShowAz = value; Apply(ViewerChange.Coordinates, nameof(ShowAz)); }
        }
        public bool ShowAlt {
            get => Settings.ShowAlt;
            set { Settings.ShowAlt = value; Apply(ViewerChange.Coordinates, nameof(ShowAlt)); }
        }
        public bool ShowLha {
            get => Settings.ShowLha;
            set { Settings.ShowLha = value; Apply(ViewerChange.Coordinates, nameof(ShowLha)); }
        }
        public bool ShowLst {
            get => Settings.ShowLst;
            set { Settings.ShowLst = value; Apply(ViewerChange.Coordinates, nameof(ShowLst)); }
        }
        public bool RaInDegrees {
            get => Settings.RaInDegrees;
            set { Settings.RaInDegrees = value; Apply(ViewerChange.Coordinates, nameof(RaInDegrees)); }
        }
        public ViewerTheme Theme {
            get => Settings.Theme;
            set { Settings.Theme = value; Apply(ViewerChange.Theme, nameof(Theme)); }
        }
        public double PollingSeconds {
            get => Settings.PollingSeconds;
            set { Settings.PollingSeconds = value; Apply(ViewerChange.Timing, nameof(PollingSeconds)); }
        }
        public bool ShowGrid {
            get => Settings.ShowGrid;
            set { Settings.ShowGrid = value; Apply(ViewerChange.Grid, nameof(ShowGrid)); }
        }
        public bool ShowCardinals {
            get => Settings.ShowCardinals;
            set { Settings.ShowCardinals = value; Apply(ViewerChange.Cardinals, nameof(ShowCardinals)); }
        }
        public bool ShowHeading {
            get => Settings.ShowHeading;
            set { Settings.ShowHeading = value; Apply(ViewerChange.Heading, nameof(ShowHeading)); }
        }
        public CardinalSize CardinalSize {
            get => Settings.CardinalSize;
            set { Settings.CardinalSize = value; Apply(ViewerChange.Cardinals, nameof(CardinalSize)); }
        }
        public int RaOffset {
            get => Settings.RaOffset;
            set { Settings.RaOffset = value; Apply(ViewerChange.Pose, nameof(RaOffset)); }
        }
        public int DecOffset {
            get => Settings.DecOffset;
            set { Settings.DecOffset = value; Apply(ViewerChange.Pose, nameof(DecOffset)); }
        }

        public ICommand PickColorCommand { get; }

        // ===== The view =====

        /// <summary>Raised when the user asks to keep the camera as it is; the 3D Model panel answers with <see cref="StoreView"/>.</summary>
        public event Action SaveViewRequested;

        /// <summary>Keep the camera as it is now.</summary>
        public ICommand SaveViewCommand { get; }

        /// <summary>Move the camera back to the saved view, or the default one if none is saved.</summary>
        public ICommand ResetViewCommand { get; }

        /// <summary>Drop the saved view and return to the default one.</summary>
        public ICommand ForgetViewCommand { get; }

        public bool HasSavedView => Settings.Camera != null;

        /// <summary>Keeps the camera (null forgets it, so the default view applies) and, for a forget, shows the default now.</summary>
        public void StoreView(CameraView view) {
            if (view != null && !CameraView.IsUsable(view)) {
                Logger.Warning("GS.Point3DNina: the 3D viewer reported a camera that can't be used, so the view was not saved.");
                return;
            }
            Settings.Camera = view;
            Settings.Save();
            RaisePropertyChanged(nameof(HasSavedView));
            if (view == null) Changed?.Invoke(ViewerChange.View);
        }

        private void Apply(ViewerChange change, string property) {
            ScheduleSave();
            RaisePropertyChanged(property);
            Changed?.Invoke(change);
        }
    }
}
