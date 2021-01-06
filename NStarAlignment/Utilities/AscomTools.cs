/* Copyright(C) 2021  Phil Crompton (phil@unitysoftware.co.uk)

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
using ASCOM.Astrometry.Transform;
using ASCOM.Utilities;

namespace NStarAlignment.Utilities
{
    public class AscomTools : IDisposable
    {
        public Transform Transform { get; private set; }

        public Util Util { get; private set; }

        public AscomTools()
        {
            Util = new Util();
            Transform = new Transform();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (Util != null)
            {
                Util.Dispose();
                Util = null;
            }

            if (Transform == null) return;
            Transform.Dispose();
            Transform = null;
        }

        public double[] GetAltAz(double rightAscension, double declination, DateTime localTime)
        {
            Transform.JulianDateUTC = Util.DateUTCToJulian(localTime);
            Transform.SetTopocentric(rightAscension, declination);
            return new[] { Transform.ElevationTopocentric, Range.Range360(Transform.AzimuthTopocentric) };

        }
        public double[] GetRaDec(double[] altAz, DateTime localTime)
        {
            return GetRaDec(altAz[0], altAz[1], localTime);
        }

        public double[] GetRaDec(double altitude, double azimuth, DateTime localUtcTime)
        {
            Transform.JulianDateUTC = Util.DateUTCToJulian(localUtcTime);
            Transform.SetAzimuthElevation(azimuth, altitude);
            return new[] { Transform.RATopocentric, Transform.DECTopocentric };

        }
    }
}
