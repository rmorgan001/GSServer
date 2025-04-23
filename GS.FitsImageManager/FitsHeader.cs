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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GS.FitsImageManager
{
	public class FitsHeader : List<FitsHeaderItem>
	{
		public override string ToString()
		{
			var sb = new StringBuilder();

			for ( var n = 0; n < Count; ++n )
			{
				var item = this.ElementAt( n );

				sb.AppendLine( item.ToString() );
			}

			return sb.ToString();
		}

		public FitsHeaderItem GetItemByKeyName( string keyName )
		{
			return this.FirstOrDefault(i => i.KeyName == keyName);
		}
	}
}
