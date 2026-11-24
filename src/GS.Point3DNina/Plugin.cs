using NINA.Plugin;
using NINA.Plugin.Interfaces;
using System.ComponentModel.Composition;

namespace GS.Point3DNina {
    [Export(typeof(IPluginManifest))]
    public class Plugin : PluginBase {

        /// <summary>The 3D model's options: the plugin settings page (Options > Plugins) binds to these.</summary>
        public Point3DOptions Options => Point3DOptions.Instance;

        public override Task Initialize() {
            return Task.CompletedTask;
        }

        public override async Task Teardown() {
            Point3DOptions.Instance.Flush();
            await Views.Point3DView.Shutdown();
        }
    }
}
