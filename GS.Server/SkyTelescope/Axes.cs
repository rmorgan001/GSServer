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
using System;
using System.Reflection;
using System.Threading;
using ASCOM.DeviceInterface;
using GS.Principles;
using GS.Shared;

namespace GS.Server.SkyTelescope
{
    public static class Axes
    {
        /// <summary>
        /// Convert internal mountaxis degrees to mount with corrrect hemi
        /// </summary>
        /// <returns></returns>
        public static double[] MountAxis2Mount()
        {
            var a = new[] { 0.0, 0.0 };
            if (SkyServer.SouthernHemisphere)
            {
                a[0] = SkyServer.MountAxisX + 180;
                a[1] = 180 - SkyServer.MountAxisY;
            }
            else
            {
                a[0] = SkyServer.MountAxisX;
                a[1] = SkyServer.MountAxisY;
            }

            return a;
        }

        /// <summary>
        /// Converts axes positions from Local to Mount 
        /// </summary>
        /// <param name="axes"></param>
        /// <returns></returns>
        internal static double[] AxesAppToMount(double[] axes)
        {
            var a = new[] { axes[0], axes[1] };
            switch (SkySystem.Mount)
            {
                case MountType.Simulator:
                    if (SkyServer.SouthernHemisphere)
                    {
                        a[0] = 180 - a[0];
                        a[1] = a[1];
                    }
                    else
                    {
                        a[0] = a[0];
                        a[1] = a[1];
                    }
                    break;
                case MountType.SkyWatcher:
                    if (SkyServer.SouthernHemisphere)
                    {
                        a[0] = 180 - a[0];
                        a[1] = a[1];
                    }
                    else
                    {
                        a[0] = a[0];
                        a[1] = 180 - a[1];
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axes[0]},{axes[1]},{a[0]},{a[1]}" };
            MonitorLog.LogToMonitor(monitorItem);
            return a;
        }

        /// <summary>
        /// Converts axes positions from Mount to Local 
        /// </summary>
        /// <param name="axes"></param>
        /// <returns></returns>
        internal static double[] AxesMountToApp(double[] axes)
        {
            var a = new[] { axes[0], axes[1] };
            switch (SkySystem.Mount)
            {
                case MountType.Simulator:
                    if (SkyServer.SouthernHemisphere)
                    {
                        a[0] = a[0] * -1.0;
                        a[1] = 180 - a[1];
                    }
                    else
                    {
                        a[0] = a[0];
                        a[1] = a[1];
                    }

                    break;
                case MountType.SkyWatcher:
                    if (SkyServer.SouthernHemisphere)
                    {
                        a[0] = a[0] * -1.0;
                        a[1] = 180 - a[1];
                    }
                    else
                    {
                        a[0] = a[0];
                        a[1] = 180 - a[1];
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return a;
        }

        /// <summary>
        /// GEMs have two possible axes positions, given an axis position this returns the other 
        /// </summary>
        /// <param name="alt">position</param>
        /// <returns>other axis position</returns>
        internal static double[] GetAltAxisPosition(double[] alt)
        {
            var d = new[] { 0.0, 0.0 };
            if (alt[0] > 90)
            {
                d[0] = alt[0] - 180;
                d[1] = 180 - alt[1];
            }
            else
            {
                d[0] = alt[0] + 180;
                d[1] = 180 - alt[1];
            }
            return d;
        }

        /// <summary>
        /// convert a decimal Alt/Az positions to an axes positions.
        /// </summary>
        /// <param name="altAz"></param>
        /// <returns></returns>
        internal static double[] AltAzToAxesYX(double[] altAz)
        { 
            var axes = new []{altAz[0],altAz[1]};
            double lst;
            switch (SkySettings.AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    break;
                case AlignmentModes.algGermanPolar:
                    lst = SkyServer.SiderealTime;
                    axes = Coordinate.AltAz2RaDec(axes[0], axes[1], SkySettings.Latitude, lst);

                    axes[0] = Coordinate.Ra2Ha(axes[0], lst) * 15.0; // ha in degrees

                    if (SkyServer.SouthernHemisphere) axes[1] = -axes[1];

                    axes = Range.RangeAzAlt(axes);

                    if (axes[0] > 180.0 || axes[0] < 0)
                    {
                        // adjust the targets to be through the pole
                        axes[0] += 180;
                        axes[1] = 180 - axes[1];
                    }
                    break;
                case AlignmentModes.algPolar:
                    lst = SkyServer.SiderealTime;
                    axes = Coordinate.AltAz2RaDec(axes[0], axes[1], SkySettings.Latitude, lst);

                    axes[0] = Coordinate.Ra2Ha(axes[0], lst) * 15.0; // ha in degrees

                    if (SkyServer.SouthernHemisphere) axes[1] = -axes[1];

                    axes = Range.RangeAzAlt(axes);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            axes = Range.RangeAxesXY(axes);
            
            var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Range:{axes[0]},{axes[1]}" };
            MonitorLog.LogToMonitor(monitorItem);

            return new[] { axes[1], axes[0] };
        }

        /// <summary>
        /// Conversion of mount axis positions in degrees to Alt and Az
        /// </summary>
        /// <param name="axes"></param>
        /// <returns>AzAlt</returns>
        internal static double[] AxesXYToAzAlt(double[] axes)
        {
            var a = AxesYXToAltAz(new[] { axes[1], axes[0] });
            var b = new[] { a[1], a[0] };
            return b;
        }

        /// <summary>
        /// Conversion of mount axis positions in degrees to Alt and Az
        /// </summary>
        /// <param name="axes"></param>
        /// <returns>AltAz</returns>
        private static double[] AxesYXToAltAz(double[] axes)
        {
            var altAz = new[] { axes[0], axes[1] };
            switch (SkySettings.AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    break;
                case AlignmentModes.algGermanPolar:
                    if (altAz[0] > 90)
                    {
                        altAz[1] += 180.0;
                        altAz[0] = 180 - altAz[0];
                        altAz = Range.RangeAltAz(altAz);
                    }

                    //southern hemi
                    if (SkyServer.SouthernHemisphere) altAz[0] = -altAz[0];

                    //axis degrees to ha
                    var ha = altAz[1] / 15.0;
                    altAz = Coordinate.HaDec2AltAz(ha, altAz[0], SkySettings.Latitude);
                    break;
                case AlignmentModes.algPolar:
                    //axis degrees to ha
                    ha = altAz[1] / 15.0;
                    if (SkyServer.SouthernHemisphere) altAz[0] = -altAz[0];
                    altAz = Coordinate.HaDec2AltAz(ha, altAz[0], SkySettings.Latitude);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            altAz = Range.RangeAltAz(altAz);
            return altAz;
        }

        /// <summary>
        /// Conversion of mount axis positions in degrees to Ra and Dec
        /// </summary>
        /// <param name="axes"></param>
        /// <returns></returns>
        internal static double[] AxesXYToRaDec(double[] axes)
        {
            var raDec = new[]{ axes[0], axes[1] };
            switch (SkySettings.AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    var radec = Coordinate.AltAz2RaDec(SkyServer.Altitude, SkyServer.Azimuth, SkySettings.Latitude, SkyServer.SiderealTime);
                    raDec[0] = Coordinate.Ra2Ha(radec[0], SkyServer.SiderealTime) * 15.0; // ha in degrees
                    raDec[1] = radec[1];
                    break;
                case AlignmentModes.algGermanPolar:
                case AlignmentModes.algPolar:
                    if (raDec[1] > 90)
                    {
                        raDec[0] += 180.0;
                        raDec[1] = 180 - raDec[1];
                        raDec = Range.RangeAzAlt(raDec);
                    }

                    raDec[0] = SkyServer.SiderealTime - raDec[0] / 15.0;
                    //southern hemi
                    if (SkyServer.SouthernHemisphere) raDec[1] = -raDec[1];
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            raDec = Range.RangeRaDec(raDec);
            return raDec;
        }

        /// <summary>
        /// convert a RaDec position to an axes positions. 
        /// </summary>
        /// <param name="raDec"></param>
        /// <returns></returns>
        internal static double[] RaDecToAxesXY(double[] raDec)
        {
            var axes = new[] { raDec[0], raDec[1]};
            switch (SkySettings.AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    axes = Range.RangeAzAlt(axes);
                    axes = Coordinate.RaDec2AltAz(axes[0], axes[1], SkyServer.SiderealTime, SkySettings.Latitude);
                    return axes;
                case AlignmentModes.algGermanPolar:
                    axes[0] = (SkyServer.SiderealTime - axes[0]) * 15.0;
                    if (SkyServer.SouthernHemisphere) axes[1] = -axes[1];
                    axes[0] = Range.Range360(axes[0]);

                    if (axes[0] > 180.0 || axes[0] < 0)
                    {
                        // adjust the targets to be through the pole
                        axes[0] += 180;
                        axes[1] = 180 - axes[1];
                    }
                    axes = Range.RangeAxesXY(axes);

                    //check for alternative position within meridian limits
                    var b = AxesAppToMount(axes);
                    var alt = SkyServer.CheckAlternatePosition(b);
                    if (alt != null) axes = alt;
                    
                    return axes;
                case AlignmentModes.algPolar:
                    axes[0] = (SkyServer.SiderealTime - axes[0]) * 15.0;
                    axes[1] = (SkyServer.SouthernHemisphere) ? -axes[1] : axes[1];
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            axes = Range.RangeAxesXY(axes);
            return axes;
        }
    }
}
