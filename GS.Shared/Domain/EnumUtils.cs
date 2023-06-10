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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GS.Shared.Domain
{
    internal static class EnumUtils
    {
        public static bool TryGetField(object value, out FieldInfo field)
        {

            var type = value.GetType();
            if (!type.IsEnum)
            {
                field = null;
                return false;
            }
            field = type.GetField(value.ToString());
            return field != null;
        }

        public static IEnumerable<TAttribute> GetAttributes<TAttribute>(FieldInfo field)
            where TAttribute : Attribute => field.GetCustomAttributes(typeof(TAttribute), true).Cast<TAttribute>();
    }
}
