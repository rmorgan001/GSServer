using System;
using System.Reflection;
using System.Threading;
using GS.Principles;
using GS.Server.SkyTelescope;
using GS.Shared;
using GS.Simulator;

namespace GS.Server.AutoHome
{
    public class AutohomeSim
    {
       // private int StartCount { get; set; }
        private int TripPosition { get; set;}

        /// <summary>
        /// autohome for the simulator
        /// </summary>
        public AutohomeSim()
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = "Start"
            };
            MonitorLog.LogToMonitor(monitorItem); 

            Initialize();
        }

        /// <summary>
        /// Initialize
        /// </summary>
        private void Initialize()
        {
            var canHomeCmda = new CmdCapabilities(MountQueue.NewId);
            var mountinfo = (MountInfo)MountQueue.GetCommandResult(canHomeCmda).Result;
            if (!canHomeCmda.Successful && canHomeCmda.Exception != null) throw canHomeCmda.Exception;
            if (!mountinfo.CanHomeSensors) throw new Exception("Home sensor not supported");
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
                        SlewAxis(1, axis );
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
            var _ = new CmdHomeSensorReset(0, axis);
        }

        /// <summary>
        /// Start autohome process per axis with max degrees default at 90
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="maxmove"></param>
        /// <returns></returns>
        public int StartAutoHome(Axis axis, int maxmove = 100)
        {
            var _ = new CmdAxisStop(0, axis);
            if (SkyServer.Tracking) SkyServer.Tracking = false;
            //StartCount = GetEncoderCount(axis);
            const double degrees = 5.0;
            var totalmove = 0.0;
            // ReSharper disable once RedundantAssignment
            var clockwise = false;
            bool? status;
            int slew;
            SkyServer.AutoHomeProgressBar += 5;

            #region 5 degree loops to look for sensor
            for (var i = 0; i < 20; i++)
            {
                if (SkyServer.AutoHomeStop) return -3; //stop requested
                
                status = GetValidStatus(axis);
                
                var laststatus = status;
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

                slew = SlewAxis(degrees, axis, clockwise); //slew 5 degrees
                if (slew != 0) return slew;
                totalmove += Math.Abs(degrees); // keep track of how far moved
                status = GetHomeSensorStatus(axis); // check sensor
                if (status != null) // home not found
                {
                    if (status != laststatus) // status changed but no detection of home
                    {
                        slew = SlewAxis(degrees + 2.5, axis, clockwise); //slew 5 degrees
                        if (slew != 0) return slew;
                        status = GetHomeSensorStatus(axis); // check sensor
                        if (status != null)
                        {
                            i = 0;
                            totalmove = 0.0;
                            continue; //start over
                        }
                        break; //found home
                    }
                    if (totalmove >= maxmove) return -2; // home not found
                }
                else
                {
                    break;//found home
                } 
            }
            #endregion

            #region slew to detected home
            slew = SlewToHome(axis);
            if (slew != 0) return slew;
            #endregion

            SkyServer.AutoHomeProgressBar += 5;

            #region 3.7 degree slew away from home for a validation move
            slew = SlewAxis(3.7, axis ); // slew away from home
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
            SlewAxis(5, axis, clockwise); // slew over home
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
                    SkyServer.SlewAxes(a, positions[1], SlewType.SlewMoveAxis);
                    break;
                case Axis.Axis2:
                    SkyServer.SlewAxes(positions[0], a, SlewType.SlewMoveAxis);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }

            while (SkyServer.IsSlewing)
            {
                Console.WriteLine(@"slewing");
                Thread.Sleep(100);
            }

            var _ = new CmdAxisStop(0, axis);

            Thread.Sleep(1000);
            return 0;
        }

        /// <summary>
        /// Slews degrees from current position using the goto from the server
        /// </summary>
        /// <param name="degrees"></param>
        /// <param name="direction"></param>
        /// <param name="axis"></param>
        private int SlewAxis(double degrees,  Axis axis, bool direction = false)
        {
            if (SkyServer.AutoHomeStop) return -3 ; //stop requested

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
                    SkyServer.SlewAxes(positions[0] + degrees, positions[1], SlewType.SlewMoveAxis);
                    break;
                case Axis.Axis2:
                    degrees = direction ? -Math.Abs(degrees) : Math.Abs(degrees);
                    SkyServer.SlewAxes(positions[0], positions[1] + degrees, SlewType.SlewMoveAxis);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }

            while (SkyServer.IsSlewing)
            {
                Console.WriteLine(@"slewing");
                Thread.Sleep(100);
            }

            var _ = new CmdAxisStop(0, axis);
            SkyServer.TrackingSpeak = true;

            Thread.Sleep(1000);
            return 0;
        }
    }
}
