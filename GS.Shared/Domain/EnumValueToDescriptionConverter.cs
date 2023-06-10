/* Copyright(C) 2019-2022 Rob Morgan (robert.morgan.e@gmail.com)

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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace GS.Shared.Domain
{
    public sealed class EnumValueToDescriptionConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            if (!EnumUtils.TryGetField(value, out var field))
            {
                return null;
            }

            var attr = EnumUtils.GetAttributes<DescriptionAttribute>(field).FirstOrDefault();
            if (attr != null)
            {
                return attr.Description;
            }
            else
            {
                var name = field.Name;
                if (char.IsLower(name[0]))
                {
                    return CamelCaseConverter.Replace(
                        LowerCaseAtBeginning.Replace(name, "", 1), 
                        " $1"
                    ).Trim();
                }
                else
                {
                    return name;
                }
            }
        }

        static readonly Regex LowerCaseAtBeginning = new Regex("(^[a-z]+)");
        static readonly Regex CamelCaseConverter = new Regex("([A-Z0-9]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
