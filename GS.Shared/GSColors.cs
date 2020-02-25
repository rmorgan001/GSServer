﻿/* Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

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

using System.Windows.Media;

namespace GS.Shared
{
    public class GSColors
    {
        public Brush DrawingColorToBrush(System.Drawing.Color color)
        {
            Brush ret = null;
            var m = new BrushConverter();
            var s = "#" + color.ToArgb().ToString("X8");
            if (m.CanConvertFrom(typeof(string)))
            {
                ret = (Brush)m.ConvertFromString(s);
            }
            return ret;
        }
    }
}
