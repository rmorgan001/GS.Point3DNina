using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace GS.Point3DNina.Views {
    /// <summary>
    /// Hosts GS.Point3DWeb's Babylon.js viewer (Assets/viewer.html with model3d.js) in a WebView2 and keeps it in step
    /// with the panel's view model: the options, and the model's pose as the telescope moves.
    /// </summary>
    public partial class Point3DView : UserControl {
        private const string Host = "gs-point3d.invalid";
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private Point3DDockable vm;
        private bool created;    // the scene exists, so commands can follow
        private string loadedModelKey;

        public Point3DView() {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e) {
            vm = DataContext as Point3DDockable;
            if (vm == null) return;
            vm.Changed -= OnChanged;
            vm.Changed += OnChanged;
            if (Web.CoreWebView2 != null) return; // already running: the panel was only moved
            try {
                var data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "NINA", "Plugins", "GS.Point3DNina", "WebView2");
                Web.CreationProperties = new CoreWebView2CreationProperties { UserDataFolder = data };
                await Web.EnsureCoreWebView2Async();

                // Serve the plugin's Assets folder as a site, so the viewer's ES modules and models load normally.
                var assets = Path.Combine(Path.GetDirectoryName(typeof(Point3DDockable).Assembly.Location)!, "Assets");
                Web.CoreWebView2.SetVirtualHostNameToFolderMapping(Host, assets, CoreWebView2HostResourceAccessKind.Allow);
                Web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                Web.CoreWebView2.Settings.AreDevToolsEnabled = false;
                Web.CoreWebView2.WebMessageReceived += OnMessage;
                Web.CoreWebView2.Navigate($"https://{Host}/viewer.html");
            } catch (Exception ex) {
                NINA.Core.Utility.Logger.Error($"GS.Point3DNina: the 3D viewer could not start: {ex.Message}");
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            if (vm != null) vm.Changed -= OnChanged;
        }

        private void Send(object message) {
            if (Web.CoreWebView2 == null) return;
            Web.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(message, Json));
        }

        private void OnMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e) {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            switch (doc.RootElement.GetProperty("event").GetString()) {
                case "ready":
                    created = false;
                    loadedModelKey = null;
                    Send(new {
                        cmd = "create",
                        options = new {
                            primaryColor = vm.Color,
                            background = vm.Background,
                            grid = vm.ShowGrid,
                            cardinals = vm.ShowCardinals,
                            cardinalSize = vm.CardinalSize.ToString().ToLowerInvariant(),
                            heading = vm.ShowHeading,
                            northern = vm.UseNorthCompass,
                            secondaryColor = "#e8e6df"
                        }
                    });
                    break;
                case "created":
                    created = true;
                    LoadModel();
                    break;
                case "modelLoaded":
                    SendPose();
                    break;
                case "error":
                    NINA.Core.Utility.Logger.Error($"GS.Point3DNina viewer: {doc.RootElement.GetProperty("message").GetString()}");
                    break;
            }
        }

        private void LoadModel() {
            if (!created) return;
            var key = $"{vm.ModelUrl}|{JsonSerializer.Serialize(vm.MountInfo, Json)}";
            if (key == loadedModelKey) { SendPose(); return; }
            loadedModelKey = key;
            Send(new { cmd = "setHemisphere", northern = vm.UseNorthCompass });
            Send(new { cmd = "loadModel", url = vm.ModelUrl, mount = vm.MountInfo });
        }

        private void SendPose() {
            if (!created) return;
            var (x, y, z, yCentre) = vm.CurrentPose();
            Send(new { cmd = "setPose", x, y, z, yCentre });
        }

        // The telescope reports from a background thread: go through the dispatcher for the WebView.
        private void OnChanged(ViewerChange change) {
            Dispatcher.BeginInvoke(() => {
                if (!created) return;
                switch (change) {
                    case ViewerChange.Pose: SendPose(); break;
                    case ViewerChange.Model: LoadModel(); break;
                    case ViewerChange.Color: Send(new { cmd = "setModelColor", hex = vm.Color }); break;
                    case ViewerChange.Background: Send(new { cmd = "setBackground", background = vm.Background }); break;
                    case ViewerChange.Grid: Send(new { cmd = "setGrid", visible = vm.ShowGrid }); break;
                    case ViewerChange.Heading: Send(new { cmd = "setHeading", visible = vm.ShowHeading }); break;
                    case ViewerChange.Cardinals:
                        Send(new { cmd = "setCardinals", visible = vm.ShowCardinals });
                        Send(new { cmd = "setCardinalSize", size = vm.CardinalSize.ToString().ToLowerInvariant() });
                        break;
                }
            });
        }
    }
}
