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
using System.Linq;

namespace GS.FitsImageManager.Header_Value_Converters
{
	public class StringStringConverter : HeaderValueConverterBase<string>
	{
		public override string Convert( string value, int? decimals = null )
		{
			if ( value.StartsWith( "\'" ) && value.EndsWith( "\'" ) )
			{
				return value;
			}

			return $"'{value}'";
		}

		public override string ConvertBack( string value )
		{
			var retval = value;

            if (string.IsNullOrEmpty(value)) return retval;
            if ( value.First() == '\'' && value.Last() == '\'' )
            {
                retval = value.Substring( 1, value.Length - 2 );
            }

            return retval;
		}

		public override bool IsConvertible( string value )
		{
			return true;
		}
	}
}
