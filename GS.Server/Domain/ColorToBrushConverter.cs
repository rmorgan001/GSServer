/* Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

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
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;


namespace GS.Server.Domain
{
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case null:
                    return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                case Color color:
                    return new SolidColorBrush(color);
                case string s:
                    return value.Equals(string.Empty) ? new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)) : new SolidColorBrush(ParseString(s));
                default:
                    throw new NotSupportedException(Application.Current.Resources["cvtInvNumber"].ToString());
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static Color ParseString(string color)
        {
            var c = ColorConverter.ConvertFromString(color);
            if (c == null) return Color.FromArgb(0, 0, 0, 0);
            return (Color)c;
        }

    }
}
