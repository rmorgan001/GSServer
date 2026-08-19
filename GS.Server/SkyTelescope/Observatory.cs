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
    /// Represents an observatory site with a name, geographic coordinates (latitude and longitude), elevation, and temperature.
    /// </summary>
    /// <remarks>Observatory instances can be compared for equality based on their properties. 
    /// This class is useful for managing observatory data in telescope control applications.</remarks>
    public class Observatory
    {
        public Observatory()
        {
            Name = "Default";
            Latitude = 0;
            Longitude = 0;
            Elevation = 0;
            Temperature = 0;
        }

        public Observatory(string name, double latitude, double longitude, double elevation, double temperature)
        {
            Name = name;
            Latitude = latitude;
            Longitude = longitude;
            Elevation = elevation;
            Temperature = temperature;
        }

        public string Name { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double Elevation { get; set; }

        public double Temperature { get; set; }

        public override bool Equals(object obj) => Equals(obj as Observatory);

        private bool Equals(Observatory o)
        {
            if (o is null) return false;
            // Optimization for a common success case.
            if (ReferenceEquals(this, o)) return true;

            // If run-time types are not exactly the same, return false.
            if (GetType() != o.GetType()) return false;

            // Return true if the fields match.
            // Note that the base class is not invoked because it is
            // System.Object, which defines Equals as reference equality.
            return Name == o.Name && Latitude == o.Latitude && Longitude == o.Longitude && Elevation == o.Elevation && Temperature == o.Temperature;
        }

        public override int GetHashCode() => (Name, Latitude, Longitude, Elevation, Temperature).GetHashCode();

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
