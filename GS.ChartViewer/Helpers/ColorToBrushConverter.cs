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
using System.Windows.Data;
using System.Windows.Media;


namespace GS.ChartViewer.Helpers
{
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

            if (value is Color)
                return new SolidColorBrush((Color)value);

            if (value is string)
            {
                return value.Equals(string.Empty) ? new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)) : new SolidColorBrush(ParseString((string)value));
            }


            throw new NotSupportedException("ColorToBurshConverter only supports converting from Color and String");
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

        //private static Color Parse(string color)
        //{
        //    var offset = color.StartsWith("#") ? 1 : 0;

        //    var a = Byte.Parse(color.Substring(0 + offset, 2), NumberStyles.HexNumber);
        //    var r = Byte.Parse(color.Substring(2 + offset, 2), NumberStyles.HexNumber);
        //    var g = Byte.Parse(color.Substring(4 + offset, 2), NumberStyles.HexNumber);
        //    var b = Byte.Parse(color.Substring(6 + offset, 2), NumberStyles.HexNumber);

        //    return Color.FromArgb(a, r, g, b);
        //}
    }
}
