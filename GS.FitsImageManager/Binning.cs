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

namespace GS.FitsImageManager
{
	public class Binning : ICloneable
	{
		public static Binning Parse( string binStr )
		{
			if ( binStr == null )
			{
				return null;
			}

            if (binStr.Length != 3 || binStr.Substring(1, 1).ToLower() != "x") throw new ArgumentException();
            var binX = binStr.Substring( 0, 1 );
            var binY = binStr.Substring( 2, 1 );

            var x = int.Parse( binX );
            var y = int.Parse( binY );

            return new Binning( x, y );

        }

		public static Binning Create( int bin )
		{
			return Create( bin, bin );
		}

		public static Binning Create( int binX, int binY )
		{
			return new Binning( binX, binY );
		}

		public Binning()
		{ }

		public Binning( int bin ) : this( bin, bin )
		{ }

		public Binning( int binX, int binY )
		{
			BinX = binX;
			BinY = binY;
		}

		public Binning( Binning other )
		{
			BinX = other.BinX;
			BinY = other.BinY;
		}

		public int BinX { get; set; }
		public int BinY { get; set; }

		public override string ToString()
		{
			return $"{BinX}x{BinY}";
		}

		#region ICloneable Members

		public Binning Clone()
		{
			return new Binning( this );
		}

		object ICloneable.Clone()
		{
			return new Binning( this );
		}

		#endregion ICloneable Members
	}
}
