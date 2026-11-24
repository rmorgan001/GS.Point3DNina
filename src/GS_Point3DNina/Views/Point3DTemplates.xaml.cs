using System.ComponentModel.Composition;
using System.Windows;

namespace GS.Point3DNina.Views {
    /// <summary>Tells NINA which view shows the 3D model panel.</summary>
    [Export(typeof(ResourceDictionary))]
    public partial class Point3DTemplates : ResourceDictionary {
        public Point3DTemplates() {
            InitializeComponent();
        }
    }
}
