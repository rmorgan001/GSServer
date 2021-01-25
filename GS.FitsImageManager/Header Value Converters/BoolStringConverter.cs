﻿/* Copyright(C) 2019-2021  Rob Morgan (robert.morgan.e@gmail.com)

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
namespace GS.FitsImageManager.Header_Value_Converters
{
	public class BoolStringConverter : HeaderValueConverterBase<bool>
	{
		public override string Convert( bool value, int? decimals = null )
		{
			return ( value ) ? "T" : "F";
		}

		public override bool ConvertBack( string value )
		{
			return ( value == "T" );
		}

		public override bool IsConvertible( string value )
		{
			return value == "T" || value == "F";
		}
	}
}
