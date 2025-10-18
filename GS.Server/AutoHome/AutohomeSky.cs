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
using GS.Server.SkyTelescope;
using GS.Shared;
using GS.SkyWatcher;
using System;
using System.Reflection;
using System.Threading;

namespace GS.Server.AutoHome
{
    public class AutoHomeSky
    {
        private int TripPosition { get; set; }
        private static bool HasHomeSensor { get; set; }

        /// <summary>
        /// auto home for skywatcher mounts
        /// </summary>
        public AutoHomeSky()
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Ui,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = "Start"
            };
            MonitorLog.LogToMonitor(monitorItem);
        }

        /// <summary>
        /// Check for home sensor capability
        /// </summary>
        private static void HomeSensorCapabilityCheck()
        {
            HasHomeSensor = false;
            var canHomeSky = new SkyCanHomeSensors(SkyQueue.NewId);
            bool.TryParse(Convert.ToString(SkyQueue.GetCommandResult(canHomeSky).Result), out bool hasHome);
            HasHomeSensor = hasHome;
        }

        /// <summary>
        /// Gets the direction to home sensor or if null then TripPosition was set
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        private bool? GetHomeSensorStatus(AxisId axis)
        {
            var sensorStatusSky = new SkyGetHomePosition(SkyQueue.NewId, axis);
            var sensorStatus = (long)SkyQueue.GetCommandResult(sensorStatusSky).Result;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{sensorStatus}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            switch (sensorStatus)
            {
                case 100000000000:      //-2147483648 =80000000 
                case 300000000000:      //0 =000000 
                    return true;
                // ReSharper disable once CommentTypo
                case 200000000000:      //2147483647 =7FFFFFFF
                                        //// ReSharper disable once CommentTypo
                case 400000000000:      //16777215 =FFFFFF 
                    return false;
                default:
                    if (sensorStatus < 100000000000)
                    {
                        TripPosition = Convert.ToInt32(sensorStatus);
                    }
                    return null;
            }
        }

        /// <summary>
        /// Checks for valid home sensor status after a reset
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        private bool? GetValidStatus(AxisId axis)
        {
            for (var i = 0; i < 2; i++)
            {
                ResetHomeSensor(axis);
                var status = GetHomeSensorStatus(axis);
                switch (status)
                {
                    case true:
                    case false:
                        return (bool)status;
                    case null:
                        SlewAxis(1, axis);
                        break;
                }
            }
            return null;
        }

        /// <summary>
        /// Reset home sensor :Wx080000[0D]
        /// </summary>
        /// <param name="axis"></param>
        private void ResetHomeSensor(AxisId axis)
        {
            var reset = new SkySetHomePositionIndex(SkyQueue.NewId, axis);
            //var _ = (long)SkyQueue.GetCommandResult(reset).Result;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{reset.Successful}|{axis}"
            };
            MonitorLog.LogToMonitor(monitorItem);
        }

        /// <summary>
        /// Start auto home process per axis with max degrees default at 90
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="offSetDec"></param>
        /// <param name="maxMove"></param>
        /// <returns></returns>
        public AutoHomeResult StartAutoHome(AxisId axis, int maxMove = 100, int offSetDec = 0)
        {
            HomeSensorCapabilityCheck();
            if (!HasHomeSensor) { return AutoHomeResult.HomeCapabilityCheckFailed; }
            _ = new SkyAxisStop(0, axis);
            if (SkyServer.Tracking) SkyServer.Tracking = false;
            var totalMove = 0.0;
            // ReSharper disable once RedundantAssignment
            var clockwise = false;
            var startOvers = 0;
            bool? status;
            bool? loopStatus = null;
            SkyServer.AutoHomeProgressBar += 5;

            // slew away from those that start at home position
            var slewResult = SlewAxis(3.3, axis);
            totalMove += 3.3;
            if (slewResult != AutoHomeResult.Success) return slewResult;

            // 5 degree loops to look for sensor
            for (var i = 0; i <= (maxMove / 5); i++)
            {
                if (SkyServer.AutoHomeStop) return AutoHomeResult.StopRequested;
                if (totalMove >= maxMove) return AutoHomeResult.HomeSensorNotFound;
                if (startOvers >= 2) return AutoHomeResult.TooManyRestarts;

                status = GetValidStatus(axis);
                var lastStatus = status;
                // check status last loop vs this loop and see if status changed
                if (status != null && loopStatus != null) // home not found and not first loop
                {
                    if (status != loopStatus) // status changed but no detection of home
                    {
                        slewResult = SlewAxis(2.7, axis, clockwise); //slew 2.7 degrees
                        if (slewResult != 0) return slewResult;
                        status = GetHomeSensorStatus(axis); // check sensor
                        if (status != null)
                        {
                            i = 0;
                            //total move = 0.0;
                            startOvers++;
                            continue; //start over
                        }
                        break; //found home
                    }
                    if (totalMove >= maxMove) return AutoHomeResult.HomeSensorNotFound;
                }
                switch (status)
                {
                    case null:
                        return SkyServer.AutoHomeStop ? AutoHomeResult.StopRequested : AutoHomeResult.FailedHomeSensorReset;
                    case true:
                    case false:
                        clockwise = (bool)status;
                        break;
                }

                SkyServer.AutoHomeProgressBar += 1;

                slewResult = SlewAxis(5.0, axis, clockwise); //slew 5 degrees
                if (slewResult != AutoHomeResult.Success) return slewResult;
                totalMove += 5.0; // keep track of how far moved
                status = GetHomeSensorStatus(axis); // check sensor
                loopStatus = status;
                if (status != null) // home not found
                {
                    if (status == lastStatus) continue;
                    slewResult = SlewAxis(2.5, axis, clockwise);
                    if (slewResult != AutoHomeResult.Success) return slewResult;
                    status = GetHomeSensorStatus(axis);
                    loopStatus = status;
                    if (status != null)
                    {
                        i = 0;
                        //total move = 0.0;
                        startOvers++;
                        continue; //start over
                    }
                }
                break;
            }
            if (SkyServer.AutoHomeStop) return AutoHomeResult.StopRequested;
            if (totalMove >= maxMove) return AutoHomeResult.HomeSensorNotFound;
            if (startOvers >= 2) return AutoHomeResult.TooManyRestarts;

            // slew to detected home
            slewResult = SlewToHome(axis);
            if (slewResult != AutoHomeResult.Success) return slewResult;

            SkyServer.AutoHomeProgressBar += 5;

            // 3.7 degree slew away from home for a validation move
            slewResult = SlewAxis(3.7, axis); // slew away from home
            if (slewResult != AutoHomeResult.Success) return slewResult;
            status = GetValidStatus(axis);
            switch (status)
            {
                case null:
                    return SkyServer.AutoHomeStop ? AutoHomeResult.StopRequested : AutoHomeResult.FailedHomeSensorReset;
                case true:
                case false:
                    clockwise = (bool)status;
                    break;
            }

            SkyServer.AutoHomeProgressBar += 5;

            // slew back over home to validate home position
            slewResult = SlewAxis(5, axis, clockwise); // slew over home
            if (slewResult != AutoHomeResult.Success) return slewResult;
            status = GetHomeSensorStatus(axis); // check sensor
            switch (status)
            {
                case null:
                    // home found
                    break;
                case true:
                case false:
                    return AutoHomeResult.HomeSensorNotFound; // home not found
            }

            SkyServer.AutoHomeProgressBar += 5;

            // slew back to remove backlash
            slewResult = SlewAxis(3, axis, !clockwise); // slew over home
            if (slewResult != AutoHomeResult.Success) return slewResult;

            SkyServer.AutoHomeProgressBar += 5;

            // slew to home
            slewResult = SlewToHome(axis);
            if (slewResult != AutoHomeResult.Success) return slewResult;

            // Dec offset for side saddles
            if (Math.Abs(offSetDec) > 0 && axis == AxisId.Axis2)
            {
                slewResult = SlewAxis(Math.Abs(offSetDec), axis, offSetDec < 0);
                if (slewResult != AutoHomeResult.Success) return slewResult;
            }

            return AutoHomeResult.Success;
        }

        /// <summary>
        /// Slew to home based on TripPosition already being set
        /// </summary>
        /// <param name="axis"></param>
        private AutoHomeResult SlewToHome(AxisId axis)
        {
            if (SkyServer.AutoHomeStop) return AutoHomeResult.StopRequested;

            //convert position to mount degrees 
            var a = TripPosition; // -= 0x00800000;
            var skyCmd = new SkyGetStepToAngle(SkyQueue.NewId, axis, a);
            var b = (double)SkyQueue.GetCommandResult(skyCmd).Result;
            var c = Units.Rad2Deg1(b);

            var positions = Axes.MountAxis2Mount();
            switch (axis)
            {
                case AxisId.Axis1:
                    var d = Axes.AxesMountToApp(new[] { c, 0 }); // Convert to local
                    if ((SkySettings.AlignmentMode != AlignmentModes.algAltAz) && (SkyServer.SouthernHemisphere)) d[0] = d[0] + 180;

                    SkyServer.SlewAxes(d[0], positions[1], SlewType.SlewMoveAxis, slewAsync: false);
                    break;
                case AxisId.Axis2:
                    var e = Axes.AxesMountToApp(new[] { 0, c }); // Convert to local
                    if ((SkySettings.AlignmentMode != AlignmentModes.algAltAz) && (SkyServer.SouthernHemisphere)) e[1] = 180 - e[1];

                    SkyServer.SlewAxes(positions[0], e[1], SlewType.SlewMoveAxis, slewAsync: false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }

            while (SkyServer.IsSlewing)
            {
                Thread.Sleep(300);
            }

            _ = new SkyAxisStop(0, axis);

            return AutoHomeResult.Success;
        }

        /// <summary>
        /// Slews degrees from current position using the goto from the server
        /// </summary>
        /// <param name="degrees"></param>
        /// <param name="direction"></param>
        /// <param name="axis"></param>
        private AutoHomeResult SlewAxis(double degrees, AxisId axis, bool direction = false)
        {
            if (SkyServer.AutoHomeStop) return AutoHomeResult.StopRequested;

            if (SkyServer.Tracking)
            {
                SkyServer.TrackingSpeak = false;
                SkyServer.Tracking = false;
            }

            var positions = Axes.MountAxis2Mount();

            switch (axis)
            {
                case AxisId.Axis1:
                    degrees = direction ? Math.Abs(degrees) : -Math.Abs(degrees);
                    if ((SkySettings.AlignmentMode != AlignmentModes.algAltAz) && (SkyServer.SouthernHemisphere))
                        degrees = direction ? -Math.Abs(degrees) : Math.Abs(degrees);
                    SkyServer.SlewAxes(positions[0] + degrees, positions[1], SlewType.SlewMoveAxis, slewAsync: false);
                    break;
                case AxisId.Axis2:
                    degrees = direction ? -Math.Abs(degrees) : Math.Abs(degrees);
                    if ((SkySettings.AlignmentMode != AlignmentModes.algAltAz) && (SkyServer.SouthernHemisphere))
                        degrees = direction ? Math.Abs(degrees) : -Math.Abs(degrees);
                    SkyServer.SlewAxes(positions[0], positions[1] + degrees, SlewType.SlewMoveAxis, slewAsync: false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{positions[0]}|{positions[1]}|{degrees}|{axis}|{direction}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            while (SkyServer.IsSlewing)
            {
                Thread.Sleep(300);
            }

            _ = new SkyAxisStop(0, axis);

            return AutoHomeResult.Success;
        }
    }
}
