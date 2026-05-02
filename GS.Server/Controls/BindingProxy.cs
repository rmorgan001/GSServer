/* Copyright(C) 2019-2026 Rob Morgan (robert.morgan.e@gmail.com)

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
using System.Windows;

namespace GS.Server.Controls
{
    /// <summary>
    /// A Freezable resource that relays DataContext into Freezable objects inside a 3D scene.
    ///
    /// The core WPF limitation: Freezable objects nested inside a Visual3D transform chain
    /// (e.g. RotateTransform3D, AxisAngleRotation3D) have no governing FrameworkElement, so a
    /// plain {Binding Path=Foo} on them fails with WPF binding Error 2.
    ///
    /// Placing this proxy in UserControl.Resources works because the UserControl itself IS a
    /// FrameworkElement and properly inherits DataContext. Freezable-target bindings in the 3D
    /// scene use:
    ///   {Binding Source={StaticResource ViewModelProxy}, Path=Data.PropertyName}
    /// which supplies an explicit Source, bypassing the broken inheritance-context lookup.
    /// </summary>
    public sealed class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore() => new BindingProxy();

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(object),
                typeof(BindingProxy),
                new UIPropertyMetadata(null));

        /// <summary>Gets or sets the proxied DataContext object.</summary>
        public object Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }
    }
}
