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
using GS.Principles;
using GS.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace GS.Server.SkyTelescope
{
    public static class Axes
    {
        /// <summary>
        /// Convert internal mount axis degrees to mount with correct hemisphere
        /// </summary>
        /// <returns></returns>
        public static double[] MountAxis2Mount()
        {
            var a = new[] { SkyServer.MountAxisX, SkyServer.MountAxisY };
            if (SkySettings.AlignmentMode == AlignmentModes.algGermanPolar)
            {
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
            switch (SkySettings.AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    break; 
                case AlignmentModes.algGermanPolar:
                    switch (SkySettings.Mount)
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

                    break;
                case AlignmentModes.algPolar:
                    switch (SkySettings.Mount)
                    {
                        case MountType.Simulator:
                            if (SkyServer.SouthernHemisphere)
                            {
                                a[0] = -a[0];
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
                                a[0] = -a[0];
                                a[1] = a[1];
                            }
                            else
                            {
                                a[0] = a[0];
                                a[1] = a[1];
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server,
                Type = MonitorType.Debug, Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axes[0]}|{axes[1]}|{a[0]}|{a[1]}"
            };
            MonitorLog.LogToMonitor(monitorItem);
            return a;
        }

        /// <summary>
        /// Converts axes positions from Mount to App ha/dec or alt/az) 
        /// </summary>
        /// <param name="axes"></param>
        /// <returns></returns>
        internal static double[] AxesMountToApp(double[] axes)
        {
            var a = new[] { axes[0], axes[1] };
            switch (SkySettings.AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    break;
                case AlignmentModes.algGermanPolar:
                    switch (SkySettings.Mount)
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

                    break;
                case AlignmentModes.algPolar:
                    switch (SkySettings.Mount)
                    {
                        case MountType.Simulator:
                            if (SkyServer.SouthernHemisphere)
                            {
                                a[0] = a[0] * -1.0;
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
                                a[0] = a[0] * -1.0;
                                a[1] = a[1];
                            }
                            else
                            {
                                a[0] = a[0];
                                a[1] = a[1];
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                default:
                    break;
            }

            return a;
        }

        /// <summary>
        /// German and polar equatorial mounts have two possible axes positions, given an axis position this returns the other 
        /// Alternate position is 180 degrees from the current position
        /// Alt Az have two possible axes positions, given an axis position this returns the other
        /// Alternate position plus / minus 360 degrees from the current position
        /// </summary>
        /// <param name="alt">position</param>
        /// <returns>other axis position</returns>
        internal static double[] GetAltAxisPosition(double[] alt)
        {
            var d = new[] { 0.0, 0.0 };
            switch (SkySettings.AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    if (alt[0] > 0)
                    {
                        d[0] = alt[0] - 360;
                    }
                    else
                    {
                        d[0] = alt[0] + 360;
                    }
                    d[1] = alt[1];
                    break;
                case AlignmentModes.algPolar:
                case AlignmentModes.algGermanPolar:
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
                    break;
            }
            return d;
        }

        /// <summary>
        /// convert a decimal Az/Alt positions to an axes positions.
        /// </summary>
        /// <param name="azAlt"></param>
        /// <returns></returns>
        internal static double[] AzAltToAxesXy(double[] azAlt, double lst = double.NaN)
        {
            var axes = new[] { 0.0, 0.0 };
            var b = new[] { 0.0, 0.0 };
            var alt = new[] { 0.0, 0.0 };
            if (double.IsNaN(lst)) lst = SkyServer.SiderealTime;
            switch (SkySettings.AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    axes[0] = Range.Range180(azAlt[0]); // Azimuth range is -180 to 180
                    axes[1] = azAlt[1];
                    //check for alternative position within hardware limits
                    b = AxesAppToMount(axes);
                    alt = SkyServer.GetAlternatePosition(b);
                    if (alt != null) axes = alt;
                    break;
                case AlignmentModes.algPolar:
                case AlignmentModes.algGermanPolar:
//                    axes = Coordinate.AltAz2RaDec(azAlt[1], azAlt[0], SkySettings.Latitude, lst);
                    axes = Coordinate.AltAz2HaDec(azAlt[1], azAlt[0], SkySettings.Latitude);
                    axes = RaDecToAxesXy(axes, haDec: true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server,
                Type = MonitorType.Debug, Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Range:{axes[0]}|{axes[1]}"
            };
            MonitorLog.LogToMonitor(monitorItem);
            return new[] { axes[0], axes[1] };
        }

        /// <summary>
        /// Conversion of mount axis positions in degrees to Az and Alt
        /// </summary>
        /// <param name="axes"></param>
        /// <returns>AzAlt</returns>
        internal static double[] AxesXyToAzAlt(double[] axes)
        {
            var altAz = new[] { axes[1], axes[0] };
            var ha = 0.0;
            switch (SkySettings.AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    break;
                case AlignmentModes.algGermanPolar:
                case AlignmentModes.algPolar:
                    if (altAz[0] > 90)
                    {
                        altAz[1] += 180.0;
                        altAz[0] = 180 - altAz[0];
                        altAz = Range.RangeAltAz(altAz);
                    }

                    //southern hemisphere
                    if (SkyServer.SouthernHemisphere) altAz[0] = -altAz[0];

                    //axis degrees to ha
                    ha = altAz[1] / 15.0;
                    altAz = Coordinate.HaDec2AltAz(ha, altAz[0], SkySettings.Latitude);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            altAz = Range.RangeAltAz(altAz);
            return new[] { altAz[1], altAz[0] };
        }

        /// <summary>
        /// Conversion of mount axis positions in degrees to Ra and Dec
        /// </summary>
        /// <param name="axes"></param>
        /// <param name="lst"></param>
        /// <returns></returns>
        internal static double[] AxesXyToRaDec(IReadOnlyList<double> axes, double lst = double.NaN)
        {
            var raDec = new[] { axes[0], axes[1] };
            if (double.IsNaN(lst)) lst = SkyServer.SiderealTime;
            switch (SkySettings.AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    raDec = Coordinate.AltAz2RaDec(axes[1], axes[0], SkySettings.Latitude, lst);
                    break;
                case AlignmentModes.algGermanPolar:
                case AlignmentModes.algPolar:
                    if (raDec[1] > 90)
                    {
                        raDec[0] += 180.0;
                        raDec[1] = 180 - raDec[1];
                        raDec = Range.RangeAz360Alt90(raDec);
                    }

                    raDec[0] = lst - raDec[0] / 15.0;
                    if (SkyServer.SouthernHemisphere)
                        raDec[1] = -raDec[1];
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            raDec = Range.RangeRaDec(raDec);
            return raDec;
        }

        /// <summary>
        /// Convert a RaDec position to an axes positions. 
        /// </summary>
        /// <param name="raDec">Ra is in range [0..24] and Dec is in the range [-90..90] as defined by ASCOM</param>
        /// <param name="lst">Local Sidereal Time</param>
        /// <returns></returns>
        internal static double[] RaDecToAxesXy(IReadOnlyList<double> raDec, double lst = double.NaN, bool haDec = false)
        {
            double[] axes = { raDec[0], raDec[1] };
            double[] b;
            double[] alt;

            // if (double.IsNaN(lst) && !haDec) lst = SkyServer.SiderealTime;
            lst = double.IsNaN(lst) && !haDec ? SkyServer.SiderealTime : 0.0;
            switch (SkySettings.AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    axes = Coordinate.RaDec2AltAz(axes[0], axes[1], lst, SkySettings.Latitude);
                    Array.Reverse(axes);
                    axes[0] = Range.Range180(axes[0]); // Azimuth range is -180 to 180
                    //check for alternative position within hardware limits
                    b = AxesAppToMount(axes);
                    alt = SkyServer.GetAlternatePosition(b);
                    // alt = SkyServer.GetAlternatePosition(axes);
                    if (alt != null) axes = alt;
                    return Axes.AxesAppToMount(axes);
                case AlignmentModes.algPolar:
                case AlignmentModes.algGermanPolar:
                    // Convert to axes and set Axis[0] in range [0..360), no lst offset if HA
                    axes[0] = haDec ? 15.0 * axes[0] : 15.0 * (lst - axes[0]);
                    axes[0] = Range.Range360(axes[0]);
                    if (SkyServer.SouthernHemisphere) axes[1] = -axes[1];

                    if (axes[0] > 180.0) // adjust axes to be through the pole
                    {
                        axes[0] += 180;
                        axes[1] = 180 - axes[1];
                    }
                    axes = Range.RangeAxesXy(axes);  // Axes[0] is in range [0..180), Axes[1] is in range [-90..90] or [-180..-90] U [90..180]
                    //check for alternative position within Flip Angle and hardware limits
                    axes = AxesAppToMount(axes);
                    alt = SkyServer.GetAlternatePosition(axes);
                    return (alt is null) ? axes : alt;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Determine if a flip is needed to reach the RA/Dec coordinates
        /// </summary>
        /// <remarks>Uses a SideOfPier test at the converted coordinates and compares to current SideOfPier</remarks>
        /// <param name="raDec"></param>
        /// <param name="lst"></param>
        /// <returns></returns>
        internal static bool IsFlipRequired(IReadOnlyList<double> raDec, double lst = double.NaN)
        {
            var axes = new[] { raDec[0], raDec[1] };
            if (double.IsNaN(lst)) lst = SkyServer.SiderealTime;
            switch (SkySettings.AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    return false;
                case AlignmentModes.algGermanPolar:
                case AlignmentModes.algPolar:
                    axes[0] = (lst - axes[0]) * 15.0;
                    if (SkyServer.SouthernHemisphere) axes[1] = -axes[1];
                    axes[0] = Range.Range360(axes[0]);

                    if (axes[0] > 180.0 || axes[0] < 0)
                    {
                        // adjust the targets to be through the pole
                        axes[0] += 180;
                        axes[1] = 180 - axes[1];
                    }

                    axes = Range.RangeAxesXy(axes);

                    //check if within Flip Angle
                    var b = AxesAppToMount(axes);
                    if (SkyServer.IsWithinFlipLimits(b))
                    {
                        return false;
                    }

                    //similar to SideOfPier property code but this passes conform for both hemispheres
                    PierSide e = PierSide.pierUnknown;
                    if (SkyServer.SideOfPier == PierSide.pierUnknown) { return false; }

                    switch (SkySettings.AlignmentMode)
                    {
                        case AlignmentModes.algAltAz:
                            e = b[0] >= 0.0 ? PierSide.pierEast : PierSide.pierWest;
                            break;
                        case AlignmentModes.algPolar:
                            e = (b[1] < 90.0000000001 && b[1] > -90.0000000001) ? PierSide.pierEast : PierSide.pierWest;
                            break;
                        case AlignmentModes.algGermanPolar:
                            switch (SkySettings.Mount)
                            {
                                case MountType.Simulator:
                                    if (b[1] < 90.0000000001 && b[1] > -90.0000000001) { e = PierSide.pierEast; }
                                    else { e = PierSide.pierWest; }
                                    break;
                                case MountType.SkyWatcher:
                                    if (SkyServer.SouthernHemisphere)
                                    {
                                        if (b[1] < 90.0 && b[1] > -90.0) { e = PierSide.pierEast; }
                                        else { e = PierSide.pierWest; }
                                    }
                                    else
                                    {
                                        if (b[1] < 90.0 && b[1] > -90.0) { e = PierSide.pierWest; }
                                        else { e = PierSide.pierEast; }
                                    }

                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            break;
                    }
                    return e != SkyServer.SideOfPier;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    }
}
