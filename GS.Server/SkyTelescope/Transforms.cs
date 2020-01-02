/* Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

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

        private static readonly Transform xform = new Transform();

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
        //    xform.SiteElevation = elevation;
        //    xform.SiteLatitude = latitude;
        //    xform.SiteLongitude = longitude;
        //    xform.Refraction = SkyServer.Refraction;
        //    switch (SkyServer.EquatorialCoordinateType)
        //    {
        //        case EquatorialCoordinateType.equJ2000:
        //            xform.SetJ2000(rightAscension, declination);
        //            break;
        //        case EquatorialCoordinateType.equLocalTopocentric:
        //            xform.SetTopocentric(rightAscension, declination);
        //            break;
        //        case EquatorialCoordinateType.equOther:
        //            xform.SetApparent(rightAscension, declination);
        //            break;
        //        case EquatorialCoordinateType.equJ2050:
        //            xform.SetJ2000(rightAscension, declination);
        //            break;
        //        case EquatorialCoordinateType.equB1950:
        //            xform.SetJ2000(rightAscension, declination);
        //            break;
        //        default:
        //            xform.SetApparent(rightAscension, declination);
        //            break;
        //    }
        //    var r = new Vector
        //    {
        //        X = xform.AzimuthTopocentric,
        //        Y = xform.ElevationTopocentric
        //    };
        //    return r;
        //}

        /// <summary>
        /// Converts RA and DEC to the required EquatorialCoordinateType
        /// Used for all RA and DEC corrdinates comming into the system 
        /// </summary>
        /// <param name="rightAscension"></param>
        /// <param name="declination"></param>
        /// <returns></returns>
        public static Vector CoordTypeToInternal(double rightAscension, double declination)
        {
            //internal is already topo so return it
            if (SkySettings.EquatorialCoordinateType == EquatorialCoordinateType.equTopocentric) return new Vector(rightAscension, declination);

            xform.SiteElevation = SkySettings.Elevation;
            xform.SiteLatitude = SkySettings.Latitude;
            xform.SiteLongitude = SkySettings.Longitude;
            xform.Refraction = SkySettings.Refraction;
            xform.SiteTemperature = SkySettings.Temperature;
            switch (SkySettings.EquatorialCoordinateType)
            {
                case EquatorialCoordinateType.equJ2000:
                    xform.SetJ2000(rightAscension, declination);
                    break;
                case EquatorialCoordinateType.equTopocentric:
                    return new Vector(rightAscension, declination);
                case EquatorialCoordinateType.equOther:
                    xform.SetApparent(rightAscension, declination);
                    break;
                case EquatorialCoordinateType.equJ2050:
                    xform.SetJ2000(rightAscension, declination);
                    break;
                case EquatorialCoordinateType.equB1950:
                    xform.SetJ2000(rightAscension, declination);
                    break;
                default:
                    xform.SetTopocentric(rightAscension, declination);
                    break;
            }
            return new Vector(xform.RATopocentric, xform.DECTopocentric);
        }

        /// <summary>
        /// Converts internal stored coords to the stored EquatorialCoordinateType
        /// Used for all RA and DEC corrdinates going out of the system  
        /// </summary>
        /// <param name="rightAscension"></param>
        /// <param name="declination"></param>
        /// <returns></returns>
        public static Vector InternalToCoordType(double rightAscension, double declination)
        {
            var radec = new Vector();
            //internal is already topo so return it
            if (SkySettings.EquatorialCoordinateType == EquatorialCoordinateType.equTopocentric) return new Vector(rightAscension, declination);

            xform.SiteElevation = SkySettings.Elevation;
            xform.SiteLatitude = SkySettings.Latitude;
            xform.SiteLongitude = SkySettings.Longitude;
            xform.Refraction = SkySettings.Refraction;
            xform.SiteTemperature = SkySettings.Temperature;
            xform.SetTopocentric(rightAscension, declination);
            switch (SkySettings.EquatorialCoordinateType)
            {
                case EquatorialCoordinateType.equJ2000:
                    radec.X = xform.RAJ2000;
                    radec.Y = xform.DecJ2000;
                    break;
                case EquatorialCoordinateType.equTopocentric:
                    radec.X = rightAscension;
                    radec.Y = declination;
                    break;
                case EquatorialCoordinateType.equOther:
                    radec.X = xform.RAApparent;
                    radec.Y = xform.DECApparent;
                    break;
                case EquatorialCoordinateType.equJ2050:
                    radec.X = xform.RAJ2000;
                    radec.Y = xform.DecJ2000;
                    break;
                case EquatorialCoordinateType.equB1950:
                    radec.X = xform.RAJ2000;
                    radec.Y = xform.DecJ2000;
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
        /// Used for all RA corrdinates going out of the system  
        /// </summary>
        /// <param name="rightAscension"></param>
        /// <param name="declination"></param>
        /// <returns></returns>
        public static double RaToCoordType(double rightAscension, double declination)
        {
            double ra;
            //internal is already topo so return it
            if (SkySettings.EquatorialCoordinateType == EquatorialCoordinateType.equTopocentric) return rightAscension;
            xform.SiteElevation = SkySettings.Elevation;
            xform.SiteLatitude = SkySettings.Latitude;
            xform.SiteLongitude = SkySettings.Longitude;
            xform.Refraction = SkySettings.Refraction;
            xform.SiteTemperature = SkySettings.Temperature;
            xform.SetTopocentric(rightAscension, declination);
            switch (SkySettings.EquatorialCoordinateType)
            {
                case EquatorialCoordinateType.equJ2000:
                    ra = xform.RAJ2000;
                    //radec.Y = declination;
                    break;
                case EquatorialCoordinateType.equTopocentric:
                    ra = rightAscension;
                    //radec.Y = xform.DECTopocentric;
                    break;
                case EquatorialCoordinateType.equOther:
                    ra = xform.RAApparent;
                    //radec.Y = xform.DECApparent;
                    break;
                case EquatorialCoordinateType.equJ2050:
                    ra = xform.RAJ2000;
                    //radec.Y = xform.DecJ2000;
                    break;
                case EquatorialCoordinateType.equB1950:
                    ra = xform.RAJ2000;
                    //radec.Y = xform.DecJ2000;
                    break;
                default:
                    ra = rightAscension;
                    //radec.Y = xform.DECTopocentric;
                    break;
            }
            return ra;
        }

        /// <summary>
        /// Converts internal stored coords to the stored EquatorialCoordinateType
        /// Used for all DEC corrdinates going out of the system  
        /// </summary>
        /// <param name="rightAscension"></param>
        /// <param name="declination"></param>
        /// <returns></returns>
        public static double DecToCoordType(double rightAscension, double declination)
        {
            double dec;
            //internal is already topo so return it
            if (SkySettings.EquatorialCoordinateType == EquatorialCoordinateType.equTopocentric) return declination;
            xform.SiteElevation = SkySettings.Elevation;
            xform.SiteLatitude = SkySettings.Latitude;
            xform.SiteLongitude = SkySettings.Longitude;
            xform.Refraction = SkySettings.Refraction;
            xform.SiteTemperature = SkySettings.Temperature;
            xform.SetTopocentric(rightAscension, declination);
            switch (SkySettings.EquatorialCoordinateType)
            {
                case EquatorialCoordinateType.equJ2000:
                    //ra = rightAscension;
                    dec = xform.DecJ2000;
                    break;
                case EquatorialCoordinateType.equTopocentric:
                    //ra = xform.RATopocentric;
                    dec = declination;
                    break;
                case EquatorialCoordinateType.equOther:
                    //ra = xform.RAApparent;
                    dec = xform.DECApparent;
                    break;
                case EquatorialCoordinateType.equJ2050:
                    //ra = xform.RAJ2000;
                    dec = xform.DecJ2000;
                    break;
                case EquatorialCoordinateType.equB1950:
                    //ra = xform.RAJ2000;
                    dec = xform.DecJ2000;
                    break;
                default:
                    //ra = xform.RATopocentric;
                    dec = xform.DECTopocentric;
                    break;
            }
            return dec;
        }

        #endregion

    }
}
