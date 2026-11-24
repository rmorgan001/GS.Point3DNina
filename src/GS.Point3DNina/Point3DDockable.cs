using GS.Point3DNina.Shared;
using GS.Point3DNina.Support;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace GS.Point3DNina {

    /// <summary>
    /// A NINA panel showing one 3D model of the connected telescope (the viewer is GS.Point3DWeb's Babylon.js scene).
    /// The options are on the plugin's settings page (Options > Plugins); the model moves as NINA reports the mount's
    /// position.
    /// </summary>
    [Export(typeof(IDockableVM))]
    public class Point3DDockable : DockableVM, ITelescopeConsumer, IDisposable {
        private readonly ITelescopeMediator telescopeMediator;
        private TelescopeInfo info;

        [ImportingConstructor]
        public Point3DDockable(IProfileService profileService, ITelescopeMediator telescopeMediator) : base(profileService) {
            this.telescopeMediator = telescopeMediator;
            Title = "3D Model";
            ImageGeometry = MountIcon();
            Options.Changed += OnOptionsChanged;
            telescopeMediator.RegisterConsumer(this);
        }

        /// <summary>The panel's icon: a telescope on a tripod with its counterweight (the same drawing as assets/icon.png).</summary>
        private static GeometryGroup MountIcon() {
            var group = new GeometryGroup { FillRule = FillRule.Nonzero };
            group.Children.Add(Geometry.Parse(
                "M55.05,49.1 L82.6,25.96 L164.9,124 L137.4,147.2 Z " +                 // tube
                "M45.3,46.8 L82.1,16 L89.8,25.2 L53,56 Z " +                         // dew shield
                "M106,126 a22,22 0 1 0 44,0 a22,22 0 1 0 -44,0 Z " +                  // mount head
                "M124,122 L132,130 L92,180 L84,172 Z " +                             // counterweight shaft
                "M72,185 a17,17 0 1 0 34,0 a17,17 0 1 0 -34,0 Z " +                  // counterweight
                "M120,140 L136,140 L136,180 L120,180 Z " +                           // column
                "M123,166 L133,174 L83,236 L73,228 Z M120,170 L136,170 L136,236 L120,236 Z M123,174 L133,166 L183,228 L173,236 Z")); // legs
            group.Freeze();
            return group;
        }

        public Point3DOptions Options => Point3DOptions.Instance;
        private ViewerSettings Settings => Options.Settings;

        /// <summary>Raised on any change that needs the scene updated.</summary>
        public event Action<ViewerChange> Changed;

        private void OnOptionsChanged(ViewerChange change) {
            RaisePropertyChanged(nameof(StatusText));
            Changed?.Invoke(change);
        }

        // ===== Mount state =====

        public bool Connected => info?.Connected == true;

        /// <summary>The assembled model is German equatorial only, whatever the telescope reports.</summary>
        public AlignmentModes Mode => Settings.UseAssembly ? AlignmentModes.algGermanPolar : Settings.ModeOverride ?? MountMode();

        private AlignmentModes MountMode() => info?.AlignmentMode switch {
            NINA.Core.Enum.AlignmentMode.AltAz => AlignmentModes.algAltAz,
            NINA.Core.Enum.AlignmentMode.Polar => AlignmentModes.algPolar,
            _ => AlignmentModes.algGermanPolar
        };

        /// <summary>The mount's own site latitude when connected (the NINA profile may not match it), else the profile's.</summary>
        public double Latitude => Connected && !double.IsNaN(info.SiteLatitude)
            ? info.SiteLatitude
            : profileService.ActiveProfile.AstrometrySettings.Latitude;

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            var modeBefore = Mode;
            var latitudeBefore = Latitude;
            info = deviceInfo;
            RaisePropertyChanged(nameof(Connected));
            RaisePropertyChanged(nameof(StatusText));
            // A new mode or latitude means a different pier and tilt: the model loads again.
            Changed?.Invoke(Mode != modeBefore || Latitude != latitudeBefore ? ViewerChange.Model : ViewerChange.Pose);
        }

        public string StatusText => Connected
            ? $"{info.Name} · {Model3D.GetDisplayName(Mode)}"
            : "Telescope not connected";

        /// <summary>The model's transform for where the mount points (the home position when disconnected).</summary>
        public (double X, double Y, double Z, double YCentre) CurrentPose() {
            var mode = Mode;
            var latitude = Latitude;
            double ax = 0, ay = 0;
            if (Connected) {
                var southern = latitude < 0;
                switch (mode) {
                    case AlignmentModes.algAltAz:
                        ax = info.Azimuth; ay = info.Altitude;
                        break;
                    case AlignmentModes.algPolar:
                        ax = (info.SiderealTime - info.RightAscension) * 15.0;
                        ay = southern ? -info.Declination : info.Declination;
                        break;
                    default:
                        var side = info.SideOfPier switch {
                            NINA.Core.Enum.PierSide.pierEast => 0,
                            NINA.Core.Enum.PierSide.pierWest => 1,
                            _ => -1
                        };
                        var axes = Axes.RaDecToAxesXy(AlignmentModes.algGermanPolar,
                            [info.RightAscension, info.Declination], info.SiderealTime, southern, side);
                        ax = axes[0]; ay = axes[1];
                        break;
                }
            }
            return Model3D.GetPose(mode, latitude, ax, ay, Settings.RaOffset, Settings.DecOffset);
        }

        // ===== What the viewer needs for the chosen options =====

        public bool ShowsAssembly => Settings.UseAssembly && Model3D.ShowsAssembly(Mode, Settings.Mount);

        public string ModelUrl => Model3D.GetModelUrl(
            Settings.UseAssembly ? Model3D.GetUsualModel(Settings.Ota) : Settings.Model, Mode, Settings.PolarMode);

        public object MountInfo => new {
            mode = (int)Mode,
            latitude = Latitude,
            assembly = ShowsAssembly
                ? new {
                    support = Settings.Support.ToString().ToLowerInvariant(),
                    mount = Settings.Mount.ToString().ToLowerInvariant(),
                    ota = Settings.Ota.ToString(),
                    otaSize = Model3D.GetAssemblyOtaSize(Settings.OtaSize)
                }
                : null
        };

        public bool UseNorthCompass => Model3D.UseNorthCompass(Mode, Latitude);

        public string Color => Settings.Color;
        public bool ShowGrid => Settings.ShowGrid;
        public bool ShowCardinals => Settings.ShowCardinals;
        public bool ShowHeading => Settings.ShowHeading;
        public CardinalSize CardinalSize => Settings.CardinalSize;

        /// <summary>The viewer's background: an image path, { generated } for the star field, or null for a plain sky.</summary>
        public object Background {
            get {
                var bg = ModelAppearance.GetBackground(Settings.Background);
                if (bg.Generated) return new { generated = ModelAppearance.StarFieldGenerated };
                return bg.Url;
            }
        }

        public void Dispose() {
            Options.Changed -= OnOptionsChanged;
            telescopeMediator.RemoveConsumer(this);
        }
    }
}
