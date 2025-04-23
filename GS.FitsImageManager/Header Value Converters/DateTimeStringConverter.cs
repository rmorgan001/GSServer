/* Copyright(C) 2019-2025  Rob Morgan (robert.morgan.e@gmail.com)

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
	public class DateTimeStringConverter : HeaderValueConverterBase<DateTime>
	{
		private const string _timeFormat = @"yyyy-MM-ddTHH:mm:ss";

		private readonly CultureInfo _culture;

		public DateTimeStringConverter()
		{
			_culture = CultureInfo.InvariantCulture;
		}

		public override string Convert( DateTime value, int? decimals = null )
		{
			var target = ( value.Kind == DateTimeKind.Local ) ? value.ToUniversalTime() : value;

			return target.ToString( _timeFormat, _culture );
		}

		//private string AddDelimiters( string source )
		//{
		//	if ( string.IsNullOrEmpty( source ) )
		//	{ 
		//		return source;
		//	}

		//	return "'" + source + "'";
		//}

		public override DateTime ConvertBack( string value )
		{
			// Trim apostrophes from both ends of the value.

			value = value.Trim('\'');

			var retval = DateTime.Parse( value, null, DateTimeStyles.RoundtripKind );

			return retval.ToLocalTime();
		}

		public override bool IsConvertible( string value )
		{
            var retval = DateTime.TryParse( value, null, DateTimeStyles.RoundtripKind, out _ );

			return retval;
		}
	}
}


