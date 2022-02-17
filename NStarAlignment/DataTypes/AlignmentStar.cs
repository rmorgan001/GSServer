/* Copyright(C) 2019-2022 Rob Morgan (robert.morgan.e@gmail.com),
    Phil Crompton (phil@unitysoftware.co.uk)

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
namespace NStarAlignment.DataTypes
{
    public class AlignmentStar
    {
        public string CommonName { get; set; }
        public string AlternateName { get; set; }
        public double Ra { get; set; }
        public double Dec { get; set; }

        public double Mag { get; set; }

        public AlignmentStar(string commonName, string alternateName, double ra, double dec, double mag)
        {
            CommonName = commonName;
            AlternateName = alternateName;
            Ra = ra;
            Dec = dec;
            Mag = mag;
        }
    }
}
