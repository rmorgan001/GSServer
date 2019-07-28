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
using System.Diagnostics;
using System.IO.Ports;
using System.Reflection;
using System.Threading;
using GS.Shared;

//used only for ascom serial trace logs

namespace GS.SkyWatcher
{
    /// <summary>
    /// Main Skywatcher class for movements, status, and information.
    /// Based on the original skywatcher-pacific project 5/31/2011: release C# basic API 1.0
    /// Contructor takes a connected ASCOM serial object. if ASCOM isn't needed replace with
    /// standard serial obeject and remove ASCOM references. 
    /// </summary>
    public class SkyWatcher
    {
        #region Fields

        private readonly Commands _commands;
        // 128.9 steps per sidereal second * rads per second = steps per rad second
        private const double _lowSpeedMargin = (128 * Constant.Siderealrate);
        private readonly double[] _slewingSpeed = {0, 0}; // store for last used speed in radians
        private readonly double[] _targetPositions = {0, 0}; //  Store for last used target coordinate 
        private readonly double[] _trackingRates = { 0, 0 };
        private readonly long[] _trackingSpeeds = { 0, 0 };
        private double[] _factorRadRateToInt = { 0, 0 };
        private long[] _highSpeedRatio = new long[2];
        private long[] _axisVersion = new long[2];
        private long[] _lowSpeedGotoMargin = new long[2];
        private long[] _breakSteps = new long[2];
        private readonly double[] _stepsPerSecond= new double[2];
        

        #endregion

        #region Properties

        internal bool IsConnected => _commands.IsConnected;

        private bool _ppecOn;
        internal bool IsPpecOn
        {
            get
            {
                ParseCapabilities();
                return _ppecOn;
            }
            private set => _ppecOn = value;
        }

        private bool _ppecTrainning;
        internal bool IsPpecInTrainingOn
        {
            get
            {
                ParseCapabilities();
                return _ppecTrainning;
            }
            private set => _ppecTrainning = value;
        }

        internal bool DecPulseGoTo { get; set; }
        internal bool AlternatingPpec { get; set; }
        internal bool CanAzEq { get; private set; }
        internal bool CanHomeSensors { get; private set; }
        internal bool CanPpec { get; private set; }
        internal bool CanOneStepDec { get; private set; }
        internal bool CanOneStepRa { get; private set; }
        internal bool CanDualEncoders { get; private set; }
        internal bool CanWifi { get; private set; }
        internal bool CanHalfTrack { get; private set; }
        internal bool CanAxisSlewsIndependent { get; private set; }
        internal bool CanPolarLed { get; private set; }
        internal bool MonitorPulse { private get; set; }
        internal string MountType { get; private set; }
        internal string MountVersion { get; private set; }
        internal bool SouthernHemisphere { private get; set; }

        #endregion

        #region Server Methods

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serial">Needs a connected serial object</param>
        internal SkyWatcher(SerialPort serial)
        {
            _commands = new Commands(serial);
           // _commands.TestSerial();
        }

        /// <summary>
        /// Start tracking based degree rate.  Use for tracking and guiding, not gotos
        /// </summary>
        /// <param name="axis">>AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="rate">Rate in degrees per sec</param>
        internal void AxisSlew(AxisId axis, double rate)
        {
            rate = Principles.Units.Deg2Rad1(rate);

            SetTrackingRate(axis, rate);

            var internalSpeed = Math.Abs(rate); // setup a positive speed
            if (internalSpeed > 500)
            {
                internalSpeed = 500;
            }

            var forward = rate > 0.0; // figures out the direction of motion
            var highspeed = false;

            // Stop if speed is too slow
            if (internalSpeed <= Constant.Siderealrate / 1000.0)
            {
                AxisStop(axis);
                return;
            }

            // Calculate and set step period. 
            if (internalSpeed > _lowSpeedMargin)
            {
                internalSpeed = internalSpeed / _highSpeedRatio[(int) axis]; // High speed adjustment
                highspeed = true;
            }

            var axesstatus = _commands.GetAxisStatus(axis); // Get axis status
            if (axesstatus.FullStop || // Already stopped
                (axesstatus.HighSpeed != highspeed) || // Change highspeed
                (axesstatus.SlewingForward && !forward) || // Change direction 
                (!axesstatus.SlewingForward && forward) // Change direction
            )
            {
                if (!axesstatus.FullStop)
                {
                    // stop the motor to change motion
                    AxisStop(axis);

                    // Wait until the axis stops or counter runs out
                    var stopwatch = Stopwatch.StartNew();
                    while (stopwatch.Elapsed.TotalMilliseconds < 2000)
                    {
                        axesstatus = _commands.GetAxisStatus(axis);
                        // Return if the axis has stopped.
                        if (axesstatus.FullStop)
                            break;
                    }
                }

                // Once stopped start a new motion mode
                _commands.SetMotionMode(axis, highspeed ? 3 : 1, forward ? 0 : 1, SouthernHemisphere); // G
            }

            // Calculate mount speed
            var speedInt = CalculateSpeed(axis, internalSpeed);

            //Set the axis step count
            _commands.SetStepSpeed(axis, speedInt);

            // Start motion
            _commands.StartMotion(axis); // J

            // Set the axis status
            _commands.SetSlewing((int) axis, forward, highspeed);

            //store axis rate
            _slewingSpeed[(int) axis] = rate;
        }

        /// <summary>
        /// Directs a pulse guide command to an axis, hemi and tracking rate needs to be set first
        /// </summary>
        /// <param name="axis">Axis 1 or 2</param>
        /// <param name="guiderate">Guiderate degrees, 15.041/3600*.5, negative value denotes direction</param>
        /// <param name="duration">length of pulse in milliseconds, aways positive numbers</param>
        /// <param name="backlashsteps">Positive microsteps added for backlash</param>
        /// <param name="declination"></param>
        internal void AxisPulse(AxisId axis, double guiderate, int duration, int backlashsteps = 0, double declination = 0)
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis},{guiderate},{duration},{backlashsteps},{DecPulseGoTo}" };
            MonitorLog.LogToMonitor(monitorItem);

            if (duration < 1) return;
            backlashsteps = Math.Abs(backlashsteps);
            var arcsecs = duration / 1000.0 * Math.Abs(guiderate) * 3600.0;

            switch (axis)
            {
                //Check for minimum pulse or a pulse less than 1 step
                case AxisId.Axis1 when _stepsPerSecond[0] * arcsecs < 1.0:
                case AxisId.Axis2 when _stepsPerSecond[1] * arcsecs < 1.0:
                    return;
            }

            // setup to log and graph the pulse
            var pulseEntry = new PulseEntry();
            if (MonitorPulse)
            {
                pulseEntry.Axis = (int) axis;
                pulseEntry.Duration = duration;
                pulseEntry.Rate = guiderate;
                pulseEntry.BacklashSteps = backlashsteps;
                pulseEntry.Declination = declination;
            }

            if (axis == AxisId.Axis1)
            {
                var rate = SouthernHemisphere ? -Math.Abs(_trackingRates[0]) : Math.Abs(_trackingRates[0]);

                // Calculate mount speed
                var speedInt = CalculateSpeed(AxisId.Axis1, rate + BasicMath.DegToRad(guiderate));
                
                // Convert lash to extra pulse duration in milliseconds
                if (backlashsteps > 0)
                {
                    var lashduration = backlashsteps * (arcsecs / (Math.Abs(guiderate) * 3600) * 1000);
                    duration += (int) lashduration;
                }

                // implements the alternating PPEC 
                if (_ppecOn && AlternatingPpec) SetPpec(AxisId.Axis1, false);

                if (MonitorPulse) pulseEntry.PositionStart = _commands.GetAxisPositionCounter(AxisId.Axis1);

                // Send pulse to axis
                _commands.SetStepSpeed(AxisId.Axis1, speedInt);

                // get the start time :I ran in the SetStepSpeed and calc difference from now
                pulseEntry.StartTime = _commands.LastI1RunTime;

                var timespan = Principles.HiResDateTime.UtcNow - pulseEntry.StartTime; 
                var msspan = duration - timespan.TotalMilliseconds; // 10 - 5

                // keep checking time difference until duration is met
                if (msspan > 0 && msspan < duration) // 10 - 50
                {
                    var sw1 = Stopwatch.StartNew();
                    while (sw1.Elapsed.TotalMilliseconds < msspan)
                    {
                        //do something while waiting
                    }
                }

                pulseEntry.EndTime = Principles.HiResDateTime.UtcNow;
                if (MonitorPulse) pulseEntry.PositionEnd = _commands.GetAxisPositionCounter(AxisId.Axis1);
                
                // set speed back to current tracking speed
                _commands.SetStepSpeed(AxisId.Axis1, _trackingSpeeds[0]);

                // implements the alternating PPEC
                if (_ppecOn && AlternatingPpec) SetPpec(AxisId.Axis1, true);

                if (MonitorPulse)
                {
                    pulseEntry.AltPPECon = AlternatingPpec;
                    pulseEntry.PPECon = _ppecOn;
                }
            }
            else
            {
                if (DecPulseGoTo)
                {
                    // Turns the pulse into steps and does a goto which is faster than using guiderate
                    var stepsNeeded = (int)(arcsecs * _stepsPerSecond[1]);
                    if (stepsNeeded < 1) return;
                    stepsNeeded += backlashsteps;
                    if (guiderate < double.Epsilon) stepsNeeded = -stepsNeeded;

                    //Firmware for the EQ8 and EQ6 can't move a single step so this conpensates, 2015B3 corrects this 
                    if (!CanOneStepDec) // check if mount is capable of small steps in goto mode
                    {
                        var jumpstep = 0;
                        if (Math.Abs(stepsNeeded) < 20)
                        {
                            if (stepsNeeded < 0) // -5
                            {
                                jumpstep = 20;
                                stepsNeeded -= jumpstep; // -5 - 20
                            }

                            if (stepsNeeded > 0) // 5
                            {
                                jumpstep = -20;
                                stepsNeeded += Math.Abs(jumpstep); // 5 + 20
                            }

                            if (MonitorPulse) pulseEntry.PositionStart = _commands.GetAxisPositionCounter(AxisId.Axis1);

                            AxisMoveSteps(AxisId.Axis2, jumpstep);

                            var axesstatus = _commands.GetAxisStatus(AxisId.Axis2); // Get axis status
                            if (!axesstatus.FullStop)
                            {
                                // Wait until the axis stops or counter runs out
                                var sw2 = Stopwatch.StartNew();
                                while (sw2.Elapsed.TotalMilliseconds < 2000)
                                {
                                    axesstatus = _commands.GetAxisStatus(axis);
                                    // Return if the axis has stopped.
                                    if (axesstatus.FullStop)
                                        break;
                                }
                            }
                        }
                    }

                    if (MonitorPulse) pulseEntry.PositionStart = _commands.GetAxisPositionCounter(AxisId.Axis2);
                    pulseEntry.StartTime = Principles.HiResDateTime.UtcNow;

                    AxisMoveSteps(AxisId.Axis2, stepsNeeded);

                    pulseEntry.EndTime = Principles.HiResDateTime.UtcNow;

                    if (MonitorPulse)
                    {
                        pulseEntry.PositionEnd = _commands.GetAxisPositionCounter(AxisId.Axis2);
                        pulseEntry.AltPPECon = AlternatingPpec;
                        pulseEntry.PPECon = _ppecOn;
                    }
                }
                else
                {
                    // Convert lash to extra pulse duration in milliseconds
                    if (backlashsteps > 0)
                    {
                        // convert backlash
                        var lashduration = backlashsteps * (arcsecs / (Math.Abs(guiderate) * 3600) * 1000);

                        //add the lash time to duration
                        duration += (int) lashduration;
                    }

                    if (MonitorPulse) pulseEntry.PositionStart = _commands.GetAxisPositionCounter(AxisId.Axis2);
                    
                    // Send pulse to axis  
                    AxisSlew(AxisId.Axis2, guiderate);

                    // get last runtime and wait out the rest of the duration
                    pulseEntry.StartTime = _commands.LastJ2RunTime;

                    var timespan = Principles.HiResDateTime.UtcNow - pulseEntry.StartTime;
                    var msspan = duration - timespan.TotalMilliseconds; // 10 - 5

                    // keep checking time difference until duration is met
                    if (msspan > 0 && msspan < duration) // 10 - 50
                    {
                        var sw3 = Stopwatch.StartNew();
                        while (sw3.Elapsed.TotalMilliseconds < msspan)
                        {
                            //do something while waiting
                        }
                    }

                    pulseEntry.EndTime = Principles.HiResDateTime.UtcNow;
                    AxisStop(AxisId.Axis2);
                    
                    if (MonitorPulse)
                    {
                        pulseEntry.PositionEnd = _commands.GetAxisPositionCounter(AxisId.Axis2);
                        pulseEntry.AltPPECon = AlternatingPpec;
                        pulseEntry.PPECon = _ppecOn;
                    }
                }
            }

            if (MonitorPulse)
            {
                //send to monitor
                MonitorLog.LogToMonitor(pulseEntry);
            }
        }

        /// <summary>
        /// Stop the target axis normally
        /// </summary>
        /// <param name="axis">>AxisId.Axis1 or AxisId.Axis2</param>
        internal void AxisStop(AxisId axis)
        {
            _slewingSpeed[(int) axis] = 0;
            _commands.AxisStop(axis);
        }

        /// <summary>
        /// Stop the target axis instantly
        /// </summary>
        /// <param name="axis">>AxisId.Axis1 or AxisId.Axis2</param>
        internal void AxisStopInstant(AxisId axis)
        {
            _slewingSpeed[(int)axis] = 0;
            _commands.AxisStopInstant(axis);
        }

        /// <summary>
        /// Use goto mode to target position
        /// anything less than 20 steps and the mount will force 20 
        /// </summary>
        /// <param name="axis">>AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="movingSteps">Microsteps to move</param>
        internal void AxisMoveSteps(AxisId axis, long movingSteps)
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis},{movingSteps}" };
            MonitorLog.LogToMonitor(monitorItem);

            int direction;

            // If there is no increment, return directly.
            if (movingSteps == 0)
            {
                return;
            }

            // Set moving direction
            if (movingSteps > 0)
            {
                direction = 0;
            }
            else
            {
                direction = 1;
                movingSteps = Math.Abs(movingSteps);
            }

            //Might need to check whether motor has stopped.
            var axesstatus = _commands.GetAxisStatus(axis);
            if (!axesstatus.FullStop)
            {
                AxisStop(axis);
            }

            _commands.SetMotionMode(axis, 2, direction, SouthernHemisphere); // G: '2' low  speed GOTO mode, '0'  +CW  and Nth Hemi
            _commands.SetGotoTargetIncrement(axis, movingSteps); // H:
            _commands.SetBreakPointIncrement(axis,0); // M: send 0 steps
            _commands.StartMotion(axis); // J: Start moving
        }

        /// <summary>
        /// Use goto mode to target position 
        /// </summary>
        /// <param name="axis">>AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="targetPosition">Total radians of target position</param>
        internal void AxisGoToTarget(AxisId axis, double targetPosition)
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis},{targetPosition}" };
            MonitorLog.LogToMonitor(monitorItem);

            int direction;

            // Get current position (radians) of the axis.
            var curPosition = _commands.GetAxisPosition(axis);

            // Calculate slewing distance.
            // Note: For EQ mount, Positions[AXIS1] is offset( -PI/2 ) adjusted in GetAxisPosition().
            var movingAngle = targetPosition - curPosition;

            // Convert distance in radian into steps.
            var movingSteps = _commands.AngleToStep(axis, movingAngle);

            bool forward;
            bool highspeed;

            // If there is no increment, return directly.
            if (movingSteps == 0)
            {
                return;
            }

            // Set moving direction
            if (movingSteps > 0)
            {
                direction = 0;
                forward = true;
            }
            else
            {
                direction = 1;
                movingSteps = Math.Abs(movingSteps);
                forward = false;
            }

            //Might need to check whether motor has stopped.
            var axesstatus = _commands.GetAxisStatus(axis);
            if (!axesstatus.FullStop)
            {
                // stop the motor to change motion
                AxisStop(axis);

                // Wait until the axis stops or counter runs out
                var sw = Stopwatch.StartNew();
                while (sw.Elapsed.TotalMilliseconds < 2000)
                {
                    axesstatus = _commands.GetAxisStatus(axis);
                    // Return if the axis has stopped.
                    if (axesstatus.FullStop)
                        break;
                }
            }

            // Check if the distance is long enough to trigger a high speed GOTO.
            if (movingSteps > _lowSpeedGotoMargin[(int) axis])
            {
                _commands.SetMotionMode(axis, 0, direction, SouthernHemisphere); // high speed GOTO slewing 
                highspeed = true;
            }
            else
            {
                _commands.SetMotionMode(axis, 2, direction, SouthernHemisphere); // low speed GOTO slewing
                highspeed = false;
            }

            _commands.SetGotoTargetIncrement(axis, movingSteps);
            _commands.SetBreakPointIncrement(axis, _breakSteps[(int) axis]);
            _commands.StartMotion(axis);

            _targetPositions[(int) axis] = targetPosition;
            _commands.SetSlewingTo((int) axis, forward, highspeed);
        }

        /// <summary>
        /// Get axis position in degrees
        /// </summary>
        /// <returns>array in degrees, could return array of NaN if no responces returned</returns>
        internal double[] GetPositionsInDegrees()
        {
            var positions = new double[] {0, 0};

            var x = _commands.GetAxisPositionNaN(AxisId.Axis1);
            if (!double.IsNaN(x)) x = Principles.Units.Rad2Deg1(x);
            positions[0] = x;

            var y = _commands.GetAxisPositionNaN(AxisId.Axis2);
            if (!double.IsNaN(y)) y = Principles.Units.Rad2Deg1(y);
            positions[1] = y;

            return positions;
        }

        /// <summary>
        /// Gets axes board versions in a readable format
        /// </summary>
        /// <returns></returns>
        internal string[] GetAxisStringVersions()
        {
            var ret = _commands.GetAxisStringVersions();

            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {ret[0]},{ret[1]}" };
            MonitorLog.LogToMonitor(monitorItem);

            return ret;
        }

        /// <summary>
        /// Gets the long format of the axes board versions
        /// </summary>
        /// <returns></returns>
        internal long[] GetAxisVersions()
        {
            var ret = _commands.GetAxisVersions();

            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" Axis1:{ret[0]}" };
            MonitorLog.LogToMonitor(monitorItem);

            monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" Axis1:{ret[0]}" };
            MonitorLog.LogToMonitor(monitorItem);

            return ret;
        }

        /// <summary>
        /// Gets a struct of status information from an axis
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <returns></returns>
        internal AxisStatus GetAxisStatus(AxisId axis)
        {
            return _commands.GetAxisStatus(axis);
        }

        /// <summary>
        /// Gets last struct of status information but does not run a new mount command
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <returns></returns>
        internal AxisStatus GetCacheAxisStatus(AxisId axis)
        {
            return _commands.GetCacheAxisStatus(axis);
        }

        /// <summary>
        /// Get the sidereal rate in stepcounts
        /// </summary>
        /// <returns></returns>
        internal long GetSiderealRate(AxisId axis)
        {
            return _commands.GetSiderealRate(axis);
        }

        /// <summary>
        /// reset the position of an axis
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="newValue">radians</param>
        internal void SetAxisPosition(AxisId axis, double newValue)
        {
            _commands.SetAxisPosition(axis, newValue);
        }

        /// <summary>
        /// Turn on/off individule axis encoders
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="on">on=true,off=false</param>
        internal void SetEncoder(AxisId axis, bool on)
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" Axis:{axis} Rate:{on}" };
            MonitorLog.LogToMonitor(monitorItem);

            if (!CanDualEncoders) return;
            _commands.SetEncoders(axis, on);
        }

        /// <summary>
        /// Turn on/off PPEC
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="on">on=true,off=false</param>
        internal void SetPpec(AxisId axis, bool on)
        {
            if (!CanPpec) return;
            _commands.SetPpec(axis, on);
        }

        /// <summary>
        /// Turn on/off training PPEC
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="on">on=true,off=false</param>
        internal void SetPpecTrain(AxisId axis, bool on)
        {
            if (!CanPpec) return;
            _commands.SetPpecTrain(axis, on);
        }

        /// <summary>
        /// Enable or Disable Full Current Low speed
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="on">on=true,off=false</param>
        internal void SetFullCurrent(AxisId axis, bool on)
        {
            if (!CanHalfTrack) return;
            _commands.SetLowSpeedCurrent(axis, on);
        }

        /// <summary>
        /// Loads default settings directly from the mount
        /// </summary>
        internal void LoadDefaultMountSettings()
        {
            _commands.LoadMountDefaults();
            _highSpeedRatio = _commands.GetHighSpeedRatio();
            _axisVersion = _commands.GetAxisVersions();
            _lowSpeedGotoMargin = _commands.GetLowSpeedGotoMargin();
            _factorRadRateToInt = _commands.GetFactorRadRateToInt();
            _breakSteps = _commands.GetBreakSteps();
            GetMotorCardVersion(AxisId.Axis1);
            ParseCapabilities();
            //SetDefaultPositions();
            SetStepsPerSecond();
        }

        #endregion

        #region SciptCommands

        /// <summary>
        /// Axis Position
        /// </summary>
        /// <param name="axis"></param>
        /// <returns>Degrees</returns>
        internal double GetAxisPosition(AxisId axis)
        {
            return Principles.Units.Rad2Deg1(_commands.GetAxisPosition(axis));
        }

        /// <summary>
        /// j Gets axis poistion counter
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <returns>Cardinal encoder count</returns>
        internal long GetAxisPositionCounter(AxisId axis)
        {
            return _commands.GetAxisPositionCounter(axis);
        }

        internal double GetRampDownRange(AxisId axis)
        {
            return _commands.GetRampDownRange(axis);
        }

        internal double GetEncoderCount(AxisId axis)
        {
            return _commands.GetEncoderCount(axis);
        }

        internal double GetLastGoToTarget(AxisId axis)
        {
            return _commands.GetLastGoToTarget(axis);
        }

        internal long GetLastSlewSpeed(AxisId axis)
        {
            return _commands.GetLastSlewSpeed(axis);
        }

        internal long GetHomePosition(AxisId axis)
        {
            return !CanHomeSensors ? 0 : _commands.GetHomePosition(axis);
        }

        internal string GetMotorCardVersion(AxisId axis)
        {
            var result = _commands.GetMotorCardVersion(axis);
            switch (result.Substring(5, 2))
            {
                case "00":
                    MountType = "EQ6Pro";
                    break;
                case "01":
                    MountType = "HEQ5";
                    break;
                case "02":
                    MountType = "EQ5";
                    break;
                case "03":
                    MountType = "EQ3";
                    break;
                case "04":
                    MountType = "EQ8";
                    break;
                case "05":
                    MountType = "AZEQ6";
                    break;
                case "06":
                    MountType = "AZEQ5";
                    break;
                case "A5":
                    MountType = "AZGTi";
                    break;
                default:
                    MountType = "Unknown";
                    break;
            }

            MountVersion = result.Substring(1, 2) + "." + result.Substring(3, 2);

            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" Axis:{axis} Mount:{MountType} Version:{MountVersion} " };
            MonitorLog.LogToMonitor(monitorItem);

            return result.Substring(1, 6);
        }

        internal double GetPecPeriod(AxisId axis)
        {
            return _commands.GetPecPeriod(axis);
        }

        internal void InitializeAxes()
        {
            _commands.InitializeAxes();
        }

        /// <summary>
        /// G Set the different motion mode
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="func">'0' high speed GOTO slewing,'1' low speed slewing mode,'2' low speed GOTO mode,'3' High slewing mode</param>
        /// <param name="direction">0=forward/right, 1=backaward/left</param>
        internal void SetMotionMode(AxisId axis, int func, int direction)
        {
            _commands.SetMotionMode(axis, func, direction, SouthernHemisphere);
        }

        internal void StartMotion(AxisId axis)
        {
            _commands.StartMotion(axis);
        }

        internal void SetGotoTargetIncrement(AxisId axis, long stepsCount)
        {
            _commands.SetGotoTargetIncrement(axis, stepsCount);
        }

        internal void SetStepSpeed(AxisId axis, long stepSpeed)
        {
            _commands.SetStepSpeed(axis, stepSpeed);
        }

        internal void SetBreakPointIncrement(AxisId axis, long stepsCount)
        {
            _commands.SetBreakPointIncrement(axis, stepsCount);
        }

        /// <summary>
        /// set ST4 guiderate
        /// </summary>
        /// <param name="rate"> 0..4 (1.0, 0.75, 0.50, 0.25, 0.125)</param>
        internal void SetSt4Guiderate(int rate)
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" Rate:{rate}" };
            MonitorLog.LogToMonitor(monitorItem);

            int cmd;
            switch (rate)
            {
                case 0:
                    cmd = 0;
                    break;
                case 1:
                    cmd = 1;
                    break;
                case 2:
                    cmd = 2;
                    break;
                case 3:
                    cmd = 3;
                    break;
                case 4:
                    cmd = 4;
                    break;
                default:
                    cmd = 2;
                    break;
            }
            _commands.SetSt4Guiderate(cmd);
        }

        /// <summary>
        /// O on/off trigger
        /// </summary>
        /// <param name="on"></param>
        internal void SetSnapPort(bool on)
        {
            _commands.SetSnapPort(on);
        }

        internal void SetTargetPosition(AxisId axis, double position)
        {
            _commands.SetTargetPosition(axis, position);
        }

        /// <summary>
        /// Sets the tracking rate in arcseconds converted to rad 
        /// </summary>
        /// <param name="axis">which axis</param>
        /// <param name="rate">arcseconds in rad</param>
        private void SetTrackingRate(AxisId axis, double rate)
        {
            if (axis == AxisId.Axis1)
            {
                _trackingRates[0] = rate;
                _trackingSpeeds[0] = CalculateSpeed(AxisId.Axis1, rate);
            }
            else
            {
                _trackingRates[1] = rate;
                _trackingSpeeds[1] = CalculateSpeed(AxisId.Axis2, rate);
            }
        }

        internal void SetHomePositionIndex(AxisId axis)
        {
            _commands.SetHomePositionIndex(axis);
        }

        internal long[] GetStepsPerRevolution()
        {
            return _commands.GetStepsPerRevolution();
        }

        internal long[] GetStepTimeFreq()
        {
            return _commands.GetStepTimeFreq();
        }

        internal long[] GetHighSpeedRatio()
        {
            return _commands.GetHighSpeedRatio();
        }

        internal long[] GetLowSpeedGotoMargin()
        {
            return _commands.GetLowSpeedGotoMargin();
        }

        internal double[] GetFactorRadRateToInt()
        {
            return _commands.GetFactorRadRateToInt();
        }

        internal string GetCapabilities()
        {
            return _commands.GetCapabilities(AxisId.Axis1);
        }

        internal long GetAngleToStep(AxisId axis, double angleInRad)
        {
            return _commands.AngleToStep(axis, angleInRad);
        }

        internal double GetStepToAngle(AxisId axis, long steps)
        {
            return _commands.StepToAngle(axis, steps);
        }

        /// <summary>
        /// Tests GoTo mode if mount can move 1 step
        /// Sets properties CanOneStepDec and CanOneStepRa, Some mounts cannot move less than 20 for a GoTo
        /// This is for the 1 step issue for the EQ8 and EQ6 prior to firmware 02.0F 
        /// </summary>
        /// <returns></returns>
        internal bool[] GetOneStepIndicators()
        {
            var indicators = new bool[2];  //initialize 0,1 Booleans
            const int step = 1;  // 1 step to check
            var stopwatch = new Stopwatch(); // new timer to check for stops

            // check axis 1
            AxisStop(AxisId.Axis1); // normal stop
            var startCount1 = GetAxisPositionCounter(AxisId.Axis1); // get position
            AxisMoveSteps(AxisId.Axis1, step); // Move 1 in positive direction
            var axesstatus1 = _commands.GetAxisStatus(AxisId.Axis1); // get status
            if (!axesstatus1.FullStop) // check if stopped
            {
                // Wait until the axis stops or counter runs out
                stopwatch.Start();
                while (stopwatch.Elapsed.TotalMilliseconds < 2000)
                {
                    axesstatus1 = _commands.GetAxisStatus(AxisId.Axis1);
                    // Break loop if the axis has stopped.
                    if (axesstatus1.FullStop)
                        break;
                    Thread.Sleep(1); // no need to hurry
                    AxisStop(AxisId.Axis1); // force another stop
                }
                stopwatch.Reset(); //stop any interval measurement in progress and clear the elapsed time value
            }
            var endCount1 = GetAxisPositionCounter(AxisId.Axis1); // get position again
            var count1 = Math.Abs(startCount1 - endCount1);
            if (count1 == 0)
            {
                CanOneStepRa = indicators[0] == false;
            }
            else
            {
                CanOneStepRa = indicators[0] = count1 <= 4; // check the difference with a variance of 4, populate return array and property
                // all done move back
                AxisMoveSteps(AxisId.Axis1, step); // Move 1 in positive direction
                endCount1 = GetAxisPositionCounter(AxisId.Axis1); // get position again
                count1 = Math.Abs(startCount1 - endCount1);
                AxisMoveSteps(AxisId.Axis1, -count1); 
            }

            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" Axis 1 Count:{count1} CanOneStepDec:{CanOneStepRa}" };
            MonitorLog.LogToMonitor(monitorItem);
            
            // check axis 2
            AxisStop(AxisId.Axis2); // all stop
            var startCount2 = GetAxisPositionCounter(AxisId.Axis2);  // where are we
            AxisMoveSteps(AxisId.Axis2, step); // Move in positive direction
            var axesstatus2 = _commands.GetAxisStatus(AxisId.Axis2);  // get status
            if (!axesstatus2.FullStop)
            {
                // Wait until the axis stops or counter runs out
                stopwatch.Start();
                while (stopwatch.Elapsed.TotalMilliseconds < 2000)
                {
                    axesstatus2 = _commands.GetAxisStatus(AxisId.Axis2);
                    // Break loop if the axis has stopped.
                    if (axesstatus2.FullStop)
                        break;
                    Thread.Sleep(1);
                    AxisStop(AxisId.Axis2); // force another stop
                }
                stopwatch.Reset();
            }
            var endCount2 = GetAxisPositionCounter(AxisId.Axis2);  // where are we
            var count2 = Math.Abs(startCount2 - endCount2);
            if (count2 == 0)
            {
                CanOneStepDec = indicators[1] == false;
            }
            else
            {
                CanOneStepDec = indicators[1] = count2 <= 4; // check the difference with a variance of 4, populate return array and property
                // all done move back
                AxisMoveSteps(AxisId.Axis2, step); // Move 1 in positive direction
                endCount2 = GetAxisPositionCounter(AxisId.Axis2); // get position again
                count2 = Math.Abs(startCount2 - endCount2);
                AxisMoveSteps(AxisId.Axis2, -count2); 
            }

            monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" Axis 2 Count:{count2} CanOneStepDec:{CanOneStepDec}" };
            MonitorLog.LogToMonitor(monitorItem);

            return indicators;
        } 

        #endregion

        #region Helpers

        private void SetStepsPerSecond()
        {
            _stepsPerSecond[0] = Convert.ToDouble(GetStepsPerRevolution()[0] / 360.0 / 3600);
            _stepsPerSecond[1] = Convert.ToDouble(GetStepsPerRevolution()[1] / 360.0 / 3600);
        }

        ///// <summary>
        ///// Set default positions so to be compatable with Syncscan setting 0,90 
        ///// </summary>
        //private void SetDefaultPositions()
        //{
        //    var a1 = _commands.GetAxisPosition(AxisId.Axis1);
        //    var a2 = _commands.GetAxisPosition(AxisId.Axis2);

        //    if (Math.Abs(a1) > 0 || Math.Abs(a2) > 0) return;
        //    SetPositionsHome();
        //}

        //private void SetPositionsHome()
        //{
        //    SetAxisPosition(AxisId.Axis1, BasicMath.DegToRad(0));
        //    SetAxisPosition(AxisId.Axis2, BasicMath.DegToRad(90));
        //}

        /// <summary>
        /// Used as a multiplyer to determine speed for the I command
        /// in general divide 1 by radians then multipley by this number 
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="rateInRad"></param>
        /// <returns></returns>
        private long RadSpeedToInt(AxisId axis, double rateInRad)
        {
            _factorRadRateToInt = _commands.GetFactorRadRateToInt();
            var r = (rateInRad * _factorRadRateToInt[(int)axis]);
            return (long) Math.Round(r,0,MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// parses results from the q command using =010000
        /// </summary>
        private void ParseCapabilities()
        {
            var result = GetCapabilities();

            switch (result.Substring(1, 1))
            {
                case "0":
                    IsPpecOn = false;
                    IsPpecInTrainingOn = false;
                    break;
                case "1":
                    IsPpecOn = false;
                    IsPpecInTrainingOn = true;
                    break;
                case "2":
                    IsPpecOn = true;
                    IsPpecInTrainingOn = false;
                    break;
                case "3":
                    IsPpecOn = true;
                    IsPpecInTrainingOn = true;
                    break;
                default:
                    IsPpecOn = false;
                    IsPpecInTrainingOn = false;
                    break;
            }

            switch (result.Substring(2, 5))
            {
                case "76000":
                    CanAzEq = false;
                    CanHomeSensors = true;
                    CanPpec = true;
                    CanDualEncoders = true;
                    CanWifi = false;
                    CanHalfTrack = true;
                    CanAxisSlewsIndependent = true;
                    CanPolarLed = false;
                    break;
                case "B3000":
                    CanAzEq = true;
                    CanHomeSensors = false;
                    CanPpec = true;
                    CanDualEncoders = true;
                    CanWifi = false;
                    CanHalfTrack = false;
                    CanAxisSlewsIndependent = true;
                    CanPolarLed = true;
                    break;
                case "B6000":
                    CanAzEq = true;
                    CanHomeSensors = false;
                    CanPpec = true;
                    CanDualEncoders = true;
                    CanWifi = false;
                    CanHalfTrack = true;
                    CanAxisSlewsIndependent = true;
                    CanPolarLed = false;
                    break;
                case "98000":
                    CanAzEq = true;
                    CanHomeSensors = false;
                    CanPpec = false;
                    CanDualEncoders = true;
                    CanWifi = true;
                    CanHalfTrack = true;
                    CanAxisSlewsIndependent = true;
                    CanPolarLed = false;
                    break;
                default:
                    CanAzEq = false;
                    CanHomeSensors = false;
                    CanPpec = false;
                    CanDualEncoders = false;
                    CanWifi = false;
                    CanHalfTrack = false;
                    CanPolarLed = false;
                    CanAxisSlewsIndependent = false;
                    break;

            }
        }

        /// <summary>
        /// Converts a rate in radians to mount speed
        /// </summary>
        /// <param name="axis">which axis</param>
        /// <param name="rate">rate in radians</param>
        /// <returns></returns>
        private long CalculateSpeed(AxisId axis, double rate)
        {
            var speed = Math.Abs(rate);

            // For using function RadSpeedToInt(), change to unit Senonds/Rad.
            // ie. sending sidereal as internalspeed will calculate how many rads per second
            speed = 1 / speed;

            //Conversion of radians to int which sets the speed in steps
            var speedInt = RadSpeedToInt(axis, speed);

            //Set maxium speed, The lower the number the faster speed
            if (_axisVersion[0] == 0x010600 || _axisVersion[0] == 0x010601) // For special MC version.
                speedInt -= 3;
            if (speedInt < 6) speedInt = 6;

            return speedInt;
        }



    #endregion
    }
}
