/* Copyright(C) 2019-2026 Rob Morgan (robert.morgan.e@gmail.com)

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

namespace GS.Server.SkyTelescope
{
    /// <summary>
    /// Represents a named observatory site with geographic coordinates.
    /// </summary>
    /// <remarks>Instances are considered equal if their name and coordinates match.</remarks>
    public class Observatory
    {
        public Observatory()
        {
            Name = "Default";
            Latitude = 0;
            Longitude = 0;
            Elevation = 0;
        }

        public Observatory(string name, double latitude, double longitude, double elevation)
        {
            Name = name;
            Latitude = latitude;
            Longitude = longitude;
            Elevation = elevation;
        }

        public string Name { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double Elevation { get; set; }

        public override bool Equals(object obj) => Equals(obj as Observatory);

        private bool Equals(Observatory o)
        {
            if (o is null) return false;
            if (ReferenceEquals(this, o)) return true;
            if (GetType() != o.GetType()) return false;
            return Name == o.Name && Latitude == o.Latitude && Longitude == o.Longitude && Elevation == o.Elevation;
        }

        public override int GetHashCode() => (Name, Latitude, Longitude, Elevation).GetHashCode();

        public static bool operator ==(Observatory lhs, Observatory rhs)
        {
            if (lhs is null)
            {
                if (rhs is null) return true;
                return false;
            }
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Observatory lhs, Observatory rhs) => !(lhs == rhs);
    }
}
