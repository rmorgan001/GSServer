/* Copyright(C) 2019-2021  Rob Morgan (robert.morgan.e@gmail.com)

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
using ASCOM.Astrometry.Transform;
using ASCOM.DeviceInterface;
using System.Windows;

namespace GS.Server.SkyTelescope
{
    public static class Transforms
    {
        #region Transform

        private static readonly Transform xForm = new Transform();

        ///// <summary>
        ///// Convert RA and DEC to Azimuth and Altitude using Transform
        ///// </summary>
        ///// <param name="rightAscension"></param>
        ///// <param name="declination"></param>
        ///// <param name="latitude"></param>
        ///// <param name="longitude"></param>
        ///// <param name="elevation"></param>
        ///// <returns></returns>
        //private static Vector CalculateAltAz(double rightAscension, double declination, double latitude, double longitude, double elevation)
        //{
        //    xForm.SiteElevation = elevation;
        //    xForm.SiteLatitude = latitude;
        //    xForm.SiteLongitude = longitude;
        //    xForm.Refraction = SkyServer.Refraction;
        //    switch (SkyServer.EquatorialCoordinateType)
        //    {
        //        case EquatorialCoordinateType.equJ2000:
        //            xForm.SetJ2000(rightAscension, declination);
        //            break;
        //        case EquatorialCoordinateType.equLocalTopocentric:
        //            xForm.SetTopocentric(rightAscension, declination);
        //            break;
        //        case EquatorialCoordinateType.equOther:
        //            xForm.SetApparent(rightAscension, declination);
        //            break;
        //        case EquatorialCoordinateType.equJ2050:
        //            xForm.SetJ2000(rightAscension, declination);
        //            break;
        //        case EquatorialCoordinateType.equB1950:
        //            xForm.SetJ2000(rightAscension, declination);
        //            break;
        //        default:
        //            xForm.SetApparent(rightAscension, declination);
        //            break;
        //    }
        //    var r = new Vector
        //    {
        //        X = xForm.AzimuthTopocentric,
        //        Y = xForm.ElevationTopocentric
        //    };
        //    return r;
        //}

        /// <summary>
        /// Converts RA and DEC to the required EquatorialCoordinateType
        /// Used for all RA and DEC coordinates coming into the system 
        /// </summary>
        /// <param name="rightAscension"></param>
        /// <param name="declination"></param>
        /// <returns></returns>
        public static Vector CoordTypeToInternal(double rightAscension, double declination)
        {
            //internal is already topo so return it
            if (SkySettings.EquatorialCoordinateType == EquatorialCoordinateType.equTopocentric) return new Vector(rightAscension, declination);

            xForm.SiteElevation = SkySettings.Elevation;
            xForm.SiteLatitude = SkySettings.Latitude;
            xForm.SiteLongitude = SkySettings.Longitude;
            xForm.Refraction = SkySettings.Refraction;
            xForm.SiteTemperature = SkySettings.Temperature;
            switch (SkySettings.EquatorialCoordinateType)
            {
                case EquatorialCoordinateType.equJ2000:
                    xForm.SetJ2000(rightAscension, declination);
                    break;
                case EquatorialCoordinateType.equTopocentric:
                    return new Vector(rightAscension, declination);
                case EquatorialCoordinateType.equOther:
                    xForm.SetApparent(rightAscension, declination);
                    break;
                case EquatorialCoordinateType.equJ2050:
                    xForm.SetJ2000(rightAscension, declination);
                    break;
                case EquatorialCoordinateType.equB1950:
                    xForm.SetJ2000(rightAscension, declination);
                    break;
                default:
                    xForm.SetTopocentric(rightAscension, declination);
                    break;
            }
            return new Vector(xForm.RATopocentric, xForm.DECTopocentric);
        }

        /// <summary>
        /// Converts internal stored coords to the stored EquatorialCoordinateType
        /// Used for all RA and DEC coordinates going out of the system  
        /// </summary>
        /// <param name="rightAscension"></param>
        /// <param name="declination"></param>
        /// <returns></returns>
        public static Vector InternalToCoordType(double rightAscension, double declination)
        {
            var radec = new Vector();
            //internal is already topo so return it
            if (SkySettings.EquatorialCoordinateType == EquatorialCoordinateType.equTopocentric) return new Vector(rightAscension, declination);

            xForm.SiteElevation = SkySettings.Elevation;
            xForm.SiteLatitude = SkySettings.Latitude;
            xForm.SiteLongitude = SkySettings.Longitude;
            xForm.Refraction = SkySettings.Refraction;
            xForm.SiteTemperature = SkySettings.Temperature;
            xForm.SetTopocentric(rightAscension, declination);
            switch (SkySettings.EquatorialCoordinateType)
            {
                case EquatorialCoordinateType.equJ2000:
                    radec.X = xForm.RAJ2000;
                    radec.Y = xForm.DecJ2000;
                    break;
                case EquatorialCoordinateType.equTopocentric:
                    radec.X = rightAscension;
                    radec.Y = declination;
                    break;
                case EquatorialCoordinateType.equOther:
                    radec.X = xForm.RAApparent;
                    radec.Y = xForm.DECApparent;
                    break;
                case EquatorialCoordinateType.equJ2050:
                    radec.X = xForm.RAJ2000;
                    radec.Y = xForm.DecJ2000;
                    break;
                case EquatorialCoordinateType.equB1950:
                    radec.X = xForm.RAJ2000;
                    radec.Y = xForm.DecJ2000;
                    break;
                default:
                    radec.X = rightAscension;
                    radec.Y = declination;
                    break;
            }
            return radec;
        }

        /// <summary>
        /// Converts internal stored coords to the stored EquatorialCoordinateType
        /// Used for all RA coordinates going out of the system  
        /// </summary>
        /// <param name="rightAscension"></param>
        /// <param name="declination"></param>
        /// <returns></returns>
        public static double RaToCoordType(double rightAscension, double declination)
        {
            double ra;
            //internal is already topo so return it
            if (SkySettings.EquatorialCoordinateType == EquatorialCoordinateType.equTopocentric) return rightAscension;
            xForm.SiteElevation = SkySettings.Elevation;
            xForm.SiteLatitude = SkySettings.Latitude;
            xForm.SiteLongitude = SkySettings.Longitude;
            xForm.Refraction = SkySettings.Refraction;
            xForm.SiteTemperature = SkySettings.Temperature;
            xForm.SetTopocentric(rightAscension, declination);
            switch (SkySettings.EquatorialCoordinateType)
            {
                case EquatorialCoordinateType.equJ2000:
                    ra = xForm.RAJ2000;
                    //radec.Y = declination;
                    break;
                case EquatorialCoordinateType.equTopocentric:
                    ra = rightAscension;
                    //radec.Y = xform.DECTopocentric;
                    break;
                case EquatorialCoordinateType.equOther:
                    ra = xForm.RAApparent;
                    //radec.Y = xform.DECApparent;
                    break;
                case EquatorialCoordinateType.equJ2050:
                    ra = xForm.RAJ2000;
                    //radec.Y = xform.DecJ2000;
                    break;
                case EquatorialCoordinateType.equB1950:
                    ra = xForm.RAJ2000;
                    //radec.Y = xForm.DecJ2000;
                    break;
                default:
                    ra = rightAscension;
                    //xForm.Y = xForm.DECTopocentric;
                    break;
            }
            return ra;
        }

        /// <summary>
        /// Converts internal stored coords to the stored EquatorialCoordinateType
        /// Used for all DEC coordinates going out of the system  
        /// </summary>
        /// <param name="rightAscension"></param>
        /// <param name="declination"></param>
        /// <returns></returns>
        public static double DecToCoordType(double rightAscension, double declination)
        {
            double dec;
            //internal is already topo so return it
            if (SkySettings.EquatorialCoordinateType == EquatorialCoordinateType.equTopocentric) return declination;
            xForm.SiteElevation = SkySettings.Elevation;
            xForm.SiteLatitude = SkySettings.Latitude;
            xForm.SiteLongitude = SkySettings.Longitude;
            xForm.Refraction = SkySettings.Refraction;
            xForm.SiteTemperature = SkySettings.Temperature;
            xForm.SetTopocentric(rightAscension, declination);
            switch (SkySettings.EquatorialCoordinateType)
            {
                case EquatorialCoordinateType.equJ2000:
                    //ra = rightAscension;
                    dec = xForm.DecJ2000;
                    break;
                case EquatorialCoordinateType.equTopocentric:
                    //ra = xForm.RATopocentric;
                    dec = declination;
                    break;
                case EquatorialCoordinateType.equOther:
                    //ra = xform.RAApparent;
                    dec = xForm.DECApparent;
                    break;
                case EquatorialCoordinateType.equJ2050:
                    //ra = xForm.RAJ2000;
                    dec = xForm.DecJ2000;
                    break;
                case EquatorialCoordinateType.equB1950:
                    //ra = xForm.RAJ2000;
                    dec = xForm.DecJ2000;
                    break;
                default:
                    //ra = xForm.RATopocentric;
                    dec = xForm.DECTopocentric;
                    break;
            }
            return dec;
        }

        #endregion

    }
}
