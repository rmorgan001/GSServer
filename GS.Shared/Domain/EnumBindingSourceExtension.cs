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
using System.Windows;
using System.Windows.Markup;

namespace GS.Shared.Domain
{
    public class EnumBindingSourceExtension : MarkupExtension
    {
        private Type _enumType;
        public Type EnumType
        {
            get => _enumType;
            set
            {
                if (value == _enumType) {return;}
                if (null != value)
                {
                    var enumType = Nullable.GetUnderlyingType(value) ?? value;
                    if (!enumType.IsEnum)
                    { throw new ArgumentException(Application.Current.Resources["cvtEnumErr1"].ToString());}
                }

                _enumType = value;
            }
        }

        public EnumBindingSourceExtension() { }

        public EnumBindingSourceExtension(Type enumType)
        {
            EnumType = enumType;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (null == _enumType)
            {
                throw new InvalidOperationException(Application.Current.Resources["cvtEnumErr2"].ToString());
            }

            var actualEnumType = Nullable.GetUnderlyingType(_enumType) ?? _enumType;
            //var enumValues = Enum.GetValues(actualEnumType);
            var enumValues = Enum.GetNames(actualEnumType);

            if (actualEnumType == _enumType)
            {  return enumValues;}

            var tempArray = Array.CreateInstance(actualEnumType, enumValues.Length + 1);
            enumValues.CopyTo(tempArray, 1);
            return tempArray;
        }
    }
}
