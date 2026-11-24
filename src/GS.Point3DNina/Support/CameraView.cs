/* Copyright(C) 2026  Rob Morgan (robert.morgan.e@gmail.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

#nullable enable
namespace GS.Point3DNina.Support;

/// <summary>
/// A saved 3D camera position for a server's model view (Babylon ArcRotateCamera).
/// Alpha is the rotation around the model and Beta the tilt from straight down, both in degrees;
/// Radius is the zoom distance; Target is the point the camera orbits (moved by panning).
/// </summary>
public sealed record CameraView(
    double Alpha,
    double Beta,
    double Radius,
    double TargetX,
    double TargetY,
    double TargetZ)
{
    /// <summary>
    /// Whether a camera can be used: every number is finite and the zoom distance is above zero. Anything else (a
    /// hand-edited file, a damaged report) would give an unusable view, so the default view applies instead.
    /// </summary>
    public static bool IsUsable(CameraView? view)
        => view is not null
           && double.IsFinite(view.Alpha) && double.IsFinite(view.Beta)
           && double.IsFinite(view.TargetX) && double.IsFinite(view.TargetY) && double.IsFinite(view.TargetZ)
           && double.IsFinite(view.Radius) && view.Radius > 0;
}
