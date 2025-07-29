﻿/* Copyright(C) 2019-2025 Rob Morgan (robert.morgan.e@gmail.com)

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
using GS.Principles;
using GS.Server.SkyTelescope;
using GS.Shared;
using GS.Simulator;
using System;
using System.Reflection;
using System.Threading;

namespace GS.Server.AutoHome
{
    public class AutoHomeSim
    {
        // private int StartCount { get; set; }
        private int TripPosition { get; set; }
        private bool HasHomeSensor { get; set; }

        /// <summary>
        /// auto home for the simulator
        /// </summary>
        public AutoHomeSim()
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
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
        private void HomeSensorCapabilityCheck()
        {
            HasHomeSensor = false;
            var canHomeCmdA = new GetHomeSensorCapability(MountQueue.NewId);
            bool.TryParse(Convert.ToString(MountQueue.GetCommandResult(canHomeCmdA).Result), out bool hasHome);
            HasHomeSensor = hasHome;
        }

        ///// <summary>
        ///// get current step count
        ///// </summary>
        ///// <param name="axis"></param>
        ///// <returns></returns>
        //private int GetEncoderCount(Axis axis)
        //{
        //    var stepsCmd = new CmdAxisSteps(MountQueue.NewId);
        //    var steps = (int[])MountQueue.GetCommandResult(stepsCmd).Result;
        //    if (!stepsCmd.Successful && stepsCmd.Exception != null) throw stepsCmd.Exception;
        //    switch (axis)
        //    {
        //        case Axis.Axis1:
        //            return steps[0];
        //        case Axis.Axis2:
        //            return steps[1];
        //        default:
        //            throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
        //    }
        //}

        /// <summary>
        /// Gets the direction to home sensor or if null then TripPosition was set
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        private bool? GetHomeSensorStatus(Axis axis)
        {
            var sensorStatusCmd = new CmdHomeSensor(MountQueue.NewId, axis);
            var sensorStatus = (int)MountQueue.GetCommandResult(sensorStatusCmd).Result;
            switch (sensorStatus)
            {
                case 16777215:
                    return false;
                case 0:
                    return true;
                default:
                    TripPosition = sensorStatus;
                    return null;
            }
        }

        /// <summary>
        /// Checks for valid home sensor status after a reset
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        private bool? GetValidStatus(Axis axis)
        {
            for (var i = 0; i < 2; i++)
            {
                ResetHomeSensor(axis);
                var status = GetHomeSensorStatus(axis);
                switch (status)
                {
                    case true:
                        return true;
                    case false:
                        return false;
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
        private void ResetHomeSensor(Axis axis)
        {
            var reset = new CmdHomeSensorReset(MountQueue.NewId, axis);
            // var _ = (int)MountQueue.GetCommandResult(reset).Result;

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
        public int StartAutoHome(Axis axis, int maxMove = 100, int offSetDec = 0)
        {
            HomeSensorCapabilityCheck();
            if (!HasHomeSensor) { return -5; }
            _ = new CmdAxisStop(0, axis);
            if (SkyServer.Tracking) SkyServer.Tracking = false;
            //StartCount = GetEncoderCount(axis);
            var totalMove = 0.0;
            // ReSharper disable once RedundantAssignment
            var clockwise = false;
            var startOvers = 0;
            bool? status;
            bool? loopStatus = null;
            SkyServer.AutoHomeProgressBar += 5;

            // slew away from those that start at home position
            var slew = SlewAxis(3.3, axis);
            totalMove += 3.3;
            if (slew != 0) return slew;

            #region 5 degree loops to look for sensor
            for (var i = 0; i <= (maxMove / 5); i++)
            {
                if (SkyServer.AutoHomeStop) return -3; //stop requested
                if (totalMove >= maxMove) return -2; // home not found
                if (startOvers >= 2) return -4; // too many restarts

                status = GetValidStatus(axis);
                var lastStatus = status;
                // check status last loop vs this loop and see if status changed
                if (status != null && loopStatus != null) // home not found and not first loop
                {
                    if (status != loopStatus) // status changed but no detection of home
                    {
                        slew = SlewAxis(2.7, axis, clockwise); //slew 5 degrees
                        if (slew != 0) return slew;
                        status = GetHomeSensorStatus(axis); // check sensor
                        if (status != null)
                        {
                            //should be far enough from the dead zone to start over.
                            i = 0;
                            //total move = 0.0;
                            startOvers++;
                            continue; //start over
                        }
                        break; //found home
                    }
                }
                switch (status)
                {
                    case null:
                        return SkyServer.AutoHomeStop ? -3 : -1;
                    case true:
                        clockwise = true;
                        break;
                    case false:
                        clockwise = false;
                        break;
                }

                SkyServer.AutoHomeProgressBar += 1;

                slew = SlewAxis(5.0, axis, clockwise); //slew 5 degrees
                if (slew != 0) return slew;
                totalMove += 5.0; // keep track of how far moved
                status = GetHomeSensorStatus(axis); // check sensor
                loopStatus = status;
                if (status != null) // home not found
                {
                    if (status == lastStatus) continue;
                    slew = SlewAxis(5.0 + 2.5, axis, clockwise); //slew 5 degrees
                    if (slew != 0) return slew;
                    status = GetHomeSensorStatus(axis); // check sensor
                    loopStatus = status;
                    if (status != null)
                    {
                        i = 0;
                        //total move = 0.0;
                        startOvers++;
                        continue; //start over
                    }
                }
                break;//found home
            }
            if (SkyServer.AutoHomeStop) return -3; //stop requested
            if (totalMove >= maxMove) return -2; // home not found
            if (startOvers >= 2) return -4; // too many restarts
            #endregion

            #region slew to detected home
            slew = SlewToHome(axis);
            if (slew != 0) return slew;
            #endregion

            SkyServer.AutoHomeProgressBar += 5;

            #region 3.7 degree slew away from home for a validation move
            slew = SlewAxis(3.7, axis); // slew away from home
            if (slew != 0) return slew;
            status = GetValidStatus(axis);
            switch (status)
            {
                case null:
                    return SkyServer.AutoHomeStop ? -3 : -1;
                case true:
                    clockwise = true;
                    break;
                case false:
                    clockwise = false;
                    break;
            }
            #endregion

            SkyServer.AutoHomeProgressBar += 5;

            #region slew back over home to validate home position
            slew = SlewAxis(5, axis, clockwise); // slew over home
            if (slew != 0) return slew;
            status = GetHomeSensorStatus(axis); // check sensor
            switch (status)
            {
                case null:
                    // home found
                    break;
                case true:
                    return -2; // home not found
                case false:
                    return -2; // home not found
            }
            #endregion

            SkyServer.AutoHomeProgressBar += 5;

            #region slew back to remove backlash
            slew = SlewAxis(3, axis, !clockwise); // slew over home
            if (slew != 0) return slew;
            #endregion

            SkyServer.AutoHomeProgressBar += 5;

            //slew to home
            slew = SlewToHome(axis);

            // Dec offset for side saddles
            if (Math.Abs(offSetDec) > 0 && axis == Axis.Axis2)
            {
                slew = SlewAxis(Math.Abs(offSetDec), axis, offSetDec < 0);
                if (slew != 0) return slew;
            }

            return slew != 0 ? slew : 0;
        }

        /// <summary>
        /// Slew to home based on TripPosition already being set
        /// </summary>
        /// <param name="axis"></param>
        private int SlewToHome(Axis axis)
        {
            if (SkyServer.AutoHomeStop) return -3; //stop requested

            var a = TripPosition / 36000;
            var positions = Axes.MountAxis2Mount();
            switch (axis)
            {
                case Axis.Axis1:
                    SkyServer.SlewAxes(a, positions[1], SlewType.SlewMoveAxis, slewAsync: false);
                    break;
                case Axis.Axis2:
                    SkyServer.SlewAxes(positions[0], a, SlewType.SlewMoveAxis, slewAsync: false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }

            while (SkyServer.IsSlewing)
            {
                //Console.WriteLine(@"slewing");
                Thread.Sleep(300);
            }

            _ = new CmdAxisStop(0, axis);
            //Thread.Sleep(1000);
            return 0;
        }

        /// <summary>
        /// Slews degrees from current position using the goto from the server
        /// </summary>
        /// <param name="degrees"></param>
        /// <param name="direction"></param>
        /// <param name="axis"></param>
        private int SlewAxis(double degrees, Axis axis, bool direction = false)
        {
            if (SkyServer.AutoHomeStop) return -3; //stop requested

            if (SkyServer.Tracking)
            {
                SkyServer.TrackingSpeak = false;
                SkyServer.Tracking = false;
            }

            var positions = Axes.MountAxis2Mount();

            switch (axis)
            {
                case Axis.Axis1:
                    degrees = direction ? -Math.Abs(degrees) : Math.Abs(degrees);
                    if (SkyServer.SouthernHemisphere) degrees = direction ? Math.Abs(degrees) : -Math.Abs(degrees);
                    SkyServer.SlewAxes(positions[0] + degrees, positions[1], SlewType.SlewMoveAxis, slewAsync: false);
                    break;
                case Axis.Axis2:
                    degrees = direction ? -Math.Abs(degrees) : Math.Abs(degrees);
                    SkyServer.SlewAxes(positions[0], positions[1] + degrees, SlewType.SlewMoveAxis, slewAsync: false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }

            while (SkyServer.IsSlewing)
            {
                //Console.WriteLine(@"slewing");
                Thread.Sleep(300);
            }

            _ = new CmdAxisStop(0, axis);

            //Thread.Sleep(1000);
            return 0;
        }
    }
}
