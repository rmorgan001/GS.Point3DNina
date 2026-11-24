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

        private long lastPoseTick = long.MinValue;

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            var modeBefore = Mode;
            var latitudeBefore = Latitude;
            info = deviceInfo;
            RaisePropertyChanged(nameof(Connected));
            RaisePropertyChanged(nameof(StatusText));
            // A new mode or latitude means a different pier and tilt: the model loads again.
            if (Mode != modeBefore || LatitudeChanged(latitudeBefore, Latitude)) {
                lastPoseTick = Environment.TickCount64;
                Changed?.Invoke(ViewerChange.Model);
                return;
            }
            // A polling time slower than NINA's: only every so often does the model follow the mount.
            var now = Environment.TickCount64;
            var nina = NinaPollingSeconds;
            if (lastPoseTick != long.MinValue && !IsDue((now - lastPoseTick) / 1000.0, Settings.PollingSeconds, nina)) return;
            lastPoseTick = now;
            Changed?.Invoke(ViewerChange.Pose);
        }

        /// <summary>
        /// Whether the latitude moved enough to need a new model: by a hundredth of a degree or more (the pose rounds it
        /// to two places), so a mount that reports it with tiny differences from one poll to the next is not a change.
        /// </summary>
        public static bool LatitudeChanged(double before, double after)
            => !(Math.Abs(after - before) < 0.01);

        /// <summary>NINA's Device polling interval, in seconds.</summary>
        private double NinaPollingSeconds => profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;

        /// <summary>
        /// How often the model follows the mount, in seconds: NINA's interval when the polling time is 0 ("match NINA")
        /// or shorter than it (NINA reports no faster), else the polling time.
        /// </summary>
        public static double EffectiveIntervalSeconds(double pollingSeconds, double ninaSeconds)
            => Math.Max(double.IsFinite(ninaSeconds) ? ninaSeconds : 0, double.IsFinite(pollingSeconds) ? pollingSeconds : 0);

        /// <summary>
        /// Whether a report from NINA, this long after the one last followed, is due. NINA's reports jitter, so one
        /// arriving up to half its interval early still counts.
        /// </summary>
        public static bool IsDue(double secondsSinceLast, double pollingSeconds, double ninaSeconds) {
            var nina = double.IsFinite(ninaSeconds) ? ninaSeconds : 0;
            if (!(pollingSeconds > nina)) return true; // matching NINA: every report
            return secondsSinceLast >= pollingSeconds - nina / 2;
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

        /// <summary>False when the mount reported no position (NaN or infinite), which can't be sent to the viewer.</summary>
        public static bool IsUsablePose((double X, double Y, double Z, double YCentre) pose)
            => double.IsFinite(pose.X) && double.IsFinite(pose.Y) && double.IsFinite(pose.Z) && double.IsFinite(pose.YCentre);

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

        /// <summary>The bearing a click on the compass readout turns the view to: north (0), or south (180) at a southern latitude.</summary>
        public static double FaceBearingFor(double latitude) => latitude < 0 ? 180.0 : 0.0;

        public double FaceBearing => FaceBearingFor(Latitude);

        public bool UseNorthCompass => Model3D.UseNorthCompass(Mode, Latitude);

        public string Color => Settings.Color;
        public ViewerTheme Theme => Settings.Theme;
        public bool ShowGrid => Settings.ShowGrid;

        /// <summary>
        /// The longest the model may glide to a new position, in milliseconds: NINA only reports the mount every
        /// "device polling interval" (2 s by default), so a glide that ended sooner would leave the model standing still
        /// between updates. Half as long again as the interval covers the jitter in when the updates arrive.
        /// </summary>
        public double MaxGlideMs => GlideMs(EffectiveIntervalSeconds(Settings.PollingSeconds, NinaPollingSeconds));

        /// <summary>The glide length for a polling interval in seconds, kept between one and ten seconds.</summary>
        public static double GlideMs(double pollingIntervalSeconds)
            => double.IsFinite(pollingIntervalSeconds) ? Math.Clamp(pollingIntervalSeconds * 1500.0, 1000.0, 10000.0) : 1000.0;
        public bool ShowCoordinates => Settings.ShowCoordinates;

        /// <summary>The coordinate chips to show, in order, with the mount's values (dashes while it isn't connected).</summary>
        public object CoordinateItems() {
            var items = new List<object>();
            var c = Connected;
            string Dms(double v) => c ? CoordinateFormat.DegreesToDms(v, "° ", ":", "", 2) : "--";
            if (Settings.ShowRa) {
                var ra = !c ? "--" : Settings.RaInDegrees
                    ? CoordinateFormat.DegreesToDms(info.RightAscension * 15.0, "° ", ":", "", 2)
                    : CoordinateFormat.HoursToHms(info.RightAscension, "h ", ":", "", 2);
                items.Add(new { label = "Ra", value = ra, title = "Double-click to switch between hours and degrees", ra = true });
            }
            if (Settings.ShowDec) items.Add(new { label = "Dec", value = Dms(c ? info.Declination : 0), title = "", ra = false });
            if (Settings.ShowAz) items.Add(new { label = "Az", value = Dms(c ? info.Azimuth : 0), title = "", ra = false });
            if (Settings.ShowAlt) items.Add(new { label = "Alt", value = Dms(c ? info.Altitude : 0), title = "", ra = false });
            if (Settings.ShowLha) items.Add(new {
                label = "Lha",
                value = c ? CoordinateFormat.HoursToHms(Coordinate.Ra2Ha12(info.RightAscension, info.SiderealTime)) : "--",
                title = "Local hour angle", ra = false });
            if (Settings.ShowLst) items.Add(new {
                label = "Lst",
                value = c ? CoordinateFormat.HoursToHms(info.SiderealTime, "h ", ":", "", 2) : "--",
                title = "Local sidereal time", ra = false });
            return items;
        }
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
