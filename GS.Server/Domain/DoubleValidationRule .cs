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

using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace GS.Server.Domain
{
    [ComVisible(false)]
    internal class DoubleValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var canConvert = double.TryParse(value as string, NumberStyles.Any, cultureInfo, out _);
            return new ValidationResult(canConvert, Application.Current.Resources["cvtInvNumber"].ToString());
        }
    }
}
