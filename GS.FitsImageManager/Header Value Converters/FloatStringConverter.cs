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

namespace GS.FitsImageManager.Header_Value_Converters
{
	public class FloatStringConverter : HeaderValueConverterBase<float>
	{
        readonly CultureInfo _culture;

		public FloatStringConverter()
		{
			_culture = new CultureInfo( "en-US" );
		}

		public override string Convert( float value, int? decimals = null )
		{
			string format;

            if (!decimals.HasValue) return value.ToString(null, _culture);
            int precision;

            if ( decimals < 0 )
            {
                precision = Math.Abs( decimals.Value );
                format = $"F{precision}";
            }
            else if ( decimals == 0 )
            {
                format = @"0\.";
            }
            else
            {
                precision = decimals.Value;
                format = $"E{precision}";
            }

            return value.ToString( format, _culture );
		}

		public override float ConvertBack( string value )
		{
            return float.TryParse( value, NumberStyles.Any, _culture, out var result ) ? result : float.NaN;
		}

		public override bool IsConvertible( string value )
		{
            return float.TryParse( value, NumberStyles.Any, _culture, out _ );
		}
	}
}
