/* Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System.Windows;
using System.Windows.Input;

namespace GS.Server.Helpers
{
    public class MouseBehaviour
    {
        #region MouseUp

        public static readonly DependencyProperty MouseUpCommandProperty =
            DependencyProperty.RegisterAttached("MouseUpCommand", typeof(ICommand), typeof(MouseBehaviour), new FrameworkPropertyMetadata(MouseUpCommandChanged));

        private static void MouseUpCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)d;

            element.MouseUp += Element_MouseUp;
        }

        private static void Element_MouseUp(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;

            ICommand command = GetMouseUpCommand(element);

            command.Execute(e);
        }

        public static void SetMouseUpCommand(UIElement element, ICommand value)
        {
            element.SetValue(MouseUpCommandProperty, value);
        }

        public static ICommand GetMouseUpCommand(UIElement element)
        {
            return (ICommand)element.GetValue(MouseUpCommandProperty);
        }

        #endregion

        #region MouseDown

        public static readonly DependencyProperty MouseDownCommandProperty =
            DependencyProperty.RegisterAttached("MouseDownCommand", typeof(ICommand), typeof(MouseBehaviour), new FrameworkPropertyMetadata(MouseDownCommandChanged));

        private static void MouseDownCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)d;

            element.MouseDown += Element_MouseDown;
        }

        static void Element_MouseDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;

            ICommand command = GetMouseDownCommand(element);

            command.Execute(e);
        }

        public static void SetMouseDownCommand(UIElement element, ICommand value)
        {
            element.SetValue(MouseDownCommandProperty, value);
        }

        public static ICommand GetMouseDownCommand(UIElement element)
        {
            return (ICommand)element.GetValue(MouseDownCommandProperty);
        }

        #endregion

        #region MouseLeave

        public static readonly DependencyProperty MouseLeaveCommandProperty =
            DependencyProperty.RegisterAttached("MouseLeaveCommand", typeof(ICommand), typeof(MouseBehaviour), new FrameworkPropertyMetadata(MouseLeaveCommandChanged));

        private static void MouseLeaveCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)d;

            element.MouseLeave += Element_MouseLeave;
        }

        static void Element_MouseLeave(object sender, MouseEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;

            ICommand command = GetMouseLeaveCommand(element);

            command.Execute(e);
        }

        public static void SetMouseLeaveCommand(UIElement element, ICommand value)
        {
            element.SetValue(MouseLeaveCommandProperty, value);
        }

        public static ICommand GetMouseLeaveCommand(UIElement element)
        {
            return (ICommand)element.GetValue(MouseLeaveCommandProperty);
        }
        #endregion

        #region MouseLeftButtonDown

        public static readonly DependencyProperty MouseLeftButtonDownCommandProperty =
            DependencyProperty.RegisterAttached("MouseLeftButtonDownCommand", typeof(ICommand), typeof(MouseBehaviour), new FrameworkPropertyMetadata(MouseLeftButtonDownCommandChanged));

        private static void MouseLeftButtonDownCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)d;

            element.PreviewMouseLeftButtonDown += Element_MouseLeftButtonDown;
        }

        static void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;

            ICommand command = GetMouseLeftButtonDownCommand(element);

            command.Execute(e);
        }

        public static void SetMouseLeftButtonDownCommand(UIElement element, ICommand value)
        {
            element.SetValue(MouseLeftButtonDownCommandProperty, value);
        }

        public static ICommand GetMouseLeftButtonDownCommand(UIElement element)
        {
            return (ICommand)element.GetValue(MouseLeftButtonDownCommandProperty);
        }

        #endregion

        #region MouseLeftButtonUp

        public static readonly DependencyProperty MouseLeftButtonUpCommandProperty =
            DependencyProperty.RegisterAttached("MouseLeftButtonUpCommand", typeof(ICommand), typeof(MouseBehaviour), new FrameworkPropertyMetadata(MouseLeftButtonUpCommandChanged));

        private static void MouseLeftButtonUpCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)d;

            element.PreviewMouseLeftButtonUp += Element_MouseLeftButtonUp;
        }

        static void Element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;

            ICommand command = GetMouseLeftButtonUpCommand(element);

            command.Execute(e);
        }

        public static void SetMouseLeftButtonUpCommand(UIElement element, ICommand value)
        {
            element.SetValue(MouseLeftButtonUpCommandProperty, value);
        }

        public static ICommand GetMouseLeftButtonUpCommand(UIElement element)
        {
            return (ICommand)element.GetValue(MouseLeftButtonUpCommandProperty);
        }

        #endregion

        #region MouseMove

        public static readonly DependencyProperty MouseMoveCommandProperty =
            DependencyProperty.RegisterAttached("MouseMoveCommand", typeof(ICommand), typeof(MouseBehaviour), new FrameworkPropertyMetadata(MouseMoveCommandChanged));

        private static void MouseMoveCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)d;

            element.MouseMove += Element_MouseMove;
        }

        static void Element_MouseMove(object sender, MouseEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;

            ICommand command = GetMouseMoveCommand(element);

            command.Execute(e);
        }

        public static void SetMouseMoveCommand(UIElement element, ICommand value)
        {
            element.SetValue(MouseMoveCommandProperty, value);
        }

        public static ICommand GetMouseMoveCommand(UIElement element)
        {
            return (ICommand)element.GetValue(MouseMoveCommandProperty);
        }

        #endregion

        #region MouseRightButtonDown

        public static readonly DependencyProperty MouseRightButtonDownCommandProperty =
            DependencyProperty.RegisterAttached("MouseRightButtonDownCommand", typeof(ICommand), typeof(MouseBehaviour), new FrameworkPropertyMetadata(MouseRightButtonDownCommandChanged));

        private static void MouseRightButtonDownCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)d;

            element.MouseRightButtonDown += Element_MouseRightButtonDown;
        }

        static void Element_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;

            ICommand command = GetMouseRightButtonDownCommand(element);

            command.Execute(e);
        }

        public static void SetMouseRightButtonDownCommand(UIElement element, ICommand value)
        {
            element.SetValue(MouseRightButtonDownCommandProperty, value);
        }

        public static ICommand GetMouseRightButtonDownCommand(UIElement element)
        {
            return (ICommand)element.GetValue(MouseRightButtonDownCommandProperty);
        }

        #endregion

        #region MouseRightButtonUp

        public static readonly DependencyProperty MouseRightButtonUpCommandProperty =
            DependencyProperty.RegisterAttached("MouseRightButtonUpCommand", typeof(ICommand), typeof(MouseBehaviour), new FrameworkPropertyMetadata(MouseRightButtonUpCommandChanged));

        private static void MouseRightButtonUpCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)d;

            element.MouseRightButtonUp += Element_MouseRightButtonUp;
        }

        static void Element_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;

            ICommand command = GetMouseRightButtonUpCommand(element);

            command.Execute(e);
        }

        public static void SetMouseRightButtonUpCommand(UIElement element, ICommand value)
        {
            element.SetValue(MouseRightButtonUpCommandProperty, value);
        }

        public static ICommand GetMouseRightButtonUpCommand(UIElement element)
        {
            return (ICommand)element.GetValue(MouseRightButtonUpCommandProperty);
        }

        #endregion

        #region MouseWheel

        public static readonly DependencyProperty MouseWheelCommandProperty =
            DependencyProperty.RegisterAttached("MouseWheelCommand", typeof(ICommand), typeof(MouseBehaviour), new FrameworkPropertyMetadata(MouseWheelCommandChanged));

        private static void MouseWheelCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)d;

            element.MouseWheel += Element_MouseWheel;
        }

        static void Element_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;

            ICommand command = GetMouseWheelCommand(element);

            command.Execute(e);
        }

        public static void SetMouseWheelCommand(UIElement element, ICommand value)
        {
            element.SetValue(MouseWheelCommandProperty, value);
        }

        public static ICommand GetMouseWheelCommand(UIElement element)
        {
            return (ICommand)element.GetValue(MouseWheelCommandProperty);
        }

        #endregion
    }
}
