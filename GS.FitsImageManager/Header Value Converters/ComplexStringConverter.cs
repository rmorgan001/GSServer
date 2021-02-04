/* Copyright(C) 2019-2021-2021  Rob Morgan (robert.morgan.e@gmail.com)

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
using System.Linq;
using System.Numerics;

namespace GS.FitsImageManager.Header_Value_Converters
{
	public class ComplexStringConverter : HeaderValueConverterBase<Complex>
	{
        readonly CultureInfo _culture;

		public ComplexStringConverter()
		{
			_culture = new CultureInfo( "en-US" );
		}

		public override string Convert( Complex value, int? decimals = null )
		{
			var format = "";

            if (!decimals.HasValue) return value.ToString(format, _culture);
            var precision = Math.Abs( decimals.Value );
            var specifier = ( decimals.Value < 0 ) ? "F":"E";
            format = $"{specifier}{precision}";

            return value.ToString( format, _culture );
		}

		public override Complex ConvertBack( string value )
		{
			if ( value.First() == '(' && value.Last() == ')' )
			{
				value = value.Substring( 1, value.Length - 2 );
			}

			var parts = value.Split( ',' );

			var realPart = Double.Parse( parts[0], NumberStyles.Any, _culture );
			var imagPart = Double.Parse( parts[1], NumberStyles.Any, _culture );

			return new Complex( realPart, imagPart );
		}

		public override bool IsConvertible( string value )
		{
			var retval = false;

            if (value.First() != '(' || value.Last() != ')') return false;
            // Remove the surrounding parentheses.

            value = value.Substring( 1, value.Length-2 );

            // Split the string on the comma. We should end up with 2 non-null parts.

            var parts = value.Split( ',' );

            if (parts.Length != 2 || String.IsNullOrEmpty(parts[0]) || String.IsNullOrEmpty(parts[1])) return false;
            var fCvt = new FloatStringConverter();
            var dCvt = new DoubleStringConverter();

            if ( ( fCvt.IsConvertible( parts[0] ) || dCvt.IsConvertible( parts[0] ) )
                 && ( fCvt.IsConvertible( parts[1] ) || dCvt.IsConvertible( parts[1] ) ) )
            {
                retval = true;
            }

            return retval;
		}
	}
}
