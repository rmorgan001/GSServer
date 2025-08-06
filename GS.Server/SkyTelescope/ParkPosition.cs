/* Copyright(C) 2019-2025 Rob Morgan (robert.morgan.e@gmail.com)

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
using ASCOM.DeviceInterface;
using System;

namespace GS.Server.SkyTelescope
{
    public class ParkPosition
    {
        public ParkPosition()
        {
            Name = "Blank";
            switch (SkySettings.AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    X = 0; Y = 0;
                    break;
                case AlignmentModes.algPolar:
                    X = 0; Y = 0;
                    break;
                case AlignmentModes.algGermanPolar:
                    X = 90; Y = 90;
                    break;
            }
        }

        public ParkPosition(string name, double x, double y)
        {
            Name = name;
            X = x;
            Y = y;
        }

        public string Name { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public override bool Equals(object obj) => this.Equals(obj as ParkPosition);

        private bool Equals(ParkPosition p)
        {
            if (p is null) return false;
            // Optimization for a common success case.
            if (Object.ReferenceEquals(this, p)) return true;

            // If run-time types are not exactly the same, return false.
            if (this.GetType() != p.GetType()) return false;

            // Return true if the fields match.
            // Note that the base class is not invoked because it is
            // System.Object, which defines Equals as reference equality.
            return (Name == p.Name) && (X == p.X) && (Y == p.Y);
        }

        public override int GetHashCode() => (Name, X, Y).GetHashCode();

        public static bool operator ==(ParkPosition lhs, ParkPosition rhs)
        {
            if (lhs is null)
            {
                if (rhs is null) return true;
                // Only the left side is null.
                return false;
            }
            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ParkPosition lhs, ParkPosition rhs) => !(lhs == rhs);
    }
}