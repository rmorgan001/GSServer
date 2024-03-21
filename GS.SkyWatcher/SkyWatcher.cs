/* Copyright(C) 2019-2022 Rob Morgan (robert.morgan.e@gmail.com)

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
using GS.Shared;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace GS.SkyWatcher
{
    /// <summary>
    /// Main SkyWatcher class for movements, status, and information.
    /// Based on the original skyWatcher-pacific project 5/31/2011: release C# basic API 1.0
    /// Constructor takes a connected AsCom serial object. if AsCom isn't needed replace with
    /// standard serial object and remove AsCom references. 
    /// </summary>
    public class SkyWatcher
    {
        #region Fields

        private readonly Commands _commands;
        // 128.9 steps per sidereal second * rads per second = steps per rad second
        private const double _lowSpeedMargin = (128 * Constant.SiderealRate);
        private readonly double[] _slewingSpeed = { 0, 0 }; // store for last used speed in radians
        private readonly double[] _targetPositions = { 0, 0 }; //  Store for last used target coordinate 
        private readonly double[] _trackingRates = { 0, 0 };
        private readonly long[] _trackingSpeeds = { 0, 0 };
        private double[] _factorRadRateToInt = { 0, 0 };
        private long[] _highSpeedRatio = new long[2];
        private long[] _axisVersion = new long[2];
        private long[] _lowSpeedGotoMargin = new long[2];
        private long[] _breakSteps = new long[2];
        private readonly double[] _stepsPerSecond = new double[2];

        #endregion

        #region Properties

        internal bool IsConnected => _commands.IsConnected;

        private bool _pPecOn;
        internal bool IsPPecOn
        {
            get
            {
                ParseCapabilities();
                return _pPecOn;
            }
            private set => _pPecOn = value;
        }

        private bool _pPecTraining;
        internal bool IsPPecInTrainingOn
        {
            get
            {
                ParseCapabilities();
                return _pPecTraining;
            }
            private set => _pPecTraining = value;
        }
        internal bool DecPulseGoTo { get; set; }
        internal bool AlternatingPPec { get; set; }
        internal bool CanAzEq { get; private set; }
        internal bool CanHomeSensors { get; private set; }
        internal bool CanPPec { get; private set; }
        internal bool CanDualEncoders { get; private set; }
        internal bool CanWifi { get; private set; }
        internal bool CanHalfTrack { get; private set; }
        internal bool CanAxisSlewsIndependent { get; private set; }
        internal bool CanPolarLed { get; private set; }
        private string Capabilities { get; set; }
        internal bool MonitorPulse { private get; set; }
        internal string MountType { get; private set; }
        private int MountNum { get; set; }
        internal string MountVersion { get; private set; }
        internal bool SouthernHemisphere { private get; set; }
        internal int MinPulseDurationRa { get; set; }
        internal int MinPulseDurationDec { get; set; }
        #endregion

        #region Server Methods

        /// <summary>
        /// Constructor
        /// </summary>
        internal SkyWatcher()
        {
            _commands = new Commands();
            // _commands.TestSerial();
            MinPulseDurationRa = 20;
            MinPulseDurationDec = 20;
        }

        /// <summary>
        /// Allows the new advanced command set to be used instead of old commands.
        /// </summary>
        /// <param name="on"></param>
        internal void AllowAdvancedCommandSet(bool on)
        {
            _commands.AllowAdvancedCommandSet = on;
        }

        /// <summary>
        /// Start tracking based degree rate.  Use for tracking and guiding, not go tos
        /// </summary>
        /// <param name="axis">>AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="rate">Rate in degrees per arc sec</param>
        internal void AxisSlew(AxisId axis, double rate)
        {
            rate = Principles.Units.Deg2Rad1(rate);

            if (_commands.SupportAdvancedCommandSet && _commands.AllowAdvancedCommandSet)
            {
                var forward = rate > 0.0; // figures out the direction of motion
                const bool highspeed = false;

                SetRates(axis, rate);
                _commands.AxisSlew_Advanced(axis, rate);
                _commands.SetSlewing((int)axis, forward, highspeed); // Set the axis status
            }
            else
            {
                var internalSpeed = Math.Abs(rate); // setup a positive speed
                if (internalSpeed > 500) { internalSpeed = 500; }

                var forward = rate > 0.0; // figures out the direction of motion
                var highspeed = false;

                // Stop if speed is too slow
                if (internalSpeed <= Constant.SiderealRate / 1000.0)
                {
                    AxisStop(axis);

                    // Wait until the axis stops or counter runs out
                    var stopwatch = Stopwatch.StartNew();
                    var counter = 1;
                    while (stopwatch.Elapsed.TotalMilliseconds <= 3500)
                    {
                        var axesstop = _commands.GetAxisStatus(axis);
                        // Return if the axis has stopped.
                        if (axesstop.FullStop) { break; }
                        // issue new stop
                        if (counter % 5 == 0) { AxisStop(axis); }
                        counter++;
                        Thread.Sleep(100);
                    }
                    return;
                }

                // Calculate and set step period. 
                if (internalSpeed > _lowSpeedMargin)
                {
                    internalSpeed /= _highSpeedRatio[(int)axis]; // High speed adjustment
                    highspeed = true;
                }

                var speedInt = CalculateSpeed(axis, internalSpeed); // Calculate mount speed

                SetRates(axis, rate);

                var axesstatus = _commands.GetAxisStatus(axis); // Get axis status

                var rateChangeOnly = axesstatus.Slewing && // In motion slewing
                                     (axesstatus.HighSpeed == highspeed) && // Not in high speed
                                     !highspeed && // High speed not required
                                     (axesstatus.SlewingForward == forward); // No direction change

                if (axesstatus.FullStop || // Already stopped
                    (axesstatus.HighSpeed != highspeed) || // Change high speed
                    highspeed ||
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
                        var counter = 1;
                        while (stopwatch.Elapsed.TotalMilliseconds <= 3500)
                        {
                            axesstatus = _commands.GetAxisStatus(axis);
                            // Return if the axis has stopped.
                            if (axesstatus.FullStop) { break; }
                            // issue new stop
                            if (counter % 5 == 0) { AxisStop(axis); }
                            counter++;
                            Thread.Sleep(100);
                        }
                    }

                    // Once stopped start a new motion mode
                    _commands.SetMotionMode(axis, highspeed ? 3 : 1, forward ? 0 : 1, SouthernHemisphere); // G
                }

                _commands.SetStepSpeed(axis, speedInt); // I Set the axis step count
                if (!rateChangeOnly)
                {
                    _commands.StartMotion(axis); // J Start motion
                    _commands.SetSlewing((int)axis, forward, highspeed); // Set the axis status
                }
            }

            _slewingSpeed[(int)axis] = rate; //store axis rate
            _commands.GetAxisPositionCounter(axis); // read for plotting
        }

        /// <summary>
        /// Directs a pulse guide command to an axis, hemisphere and tracking rate needs to be set first
        /// </summary>
        /// <param name="axis">Axis 1 or 2</param>
        /// <param name="guideRate">Guide rate degrees, 15.041/3600*.5, negative value denotes direction</param>
        /// <param name="duration">length of pulse in milliseconds, always positive numbers</param>
        /// <param name="backlashSteps">Positive micro steps added for backlash</param>
        internal void AxisPulse(AxisId axis, double guideRate, int duration, int backlashSteps = 0)
            {
            var datetime = Principles.HiResDateTime.UtcNow;
            var monitorItem = new MonitorEntry // setup to log the pulse
            { Datetime = datetime, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Debug, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}|{guideRate}|{duration}|{backlashSteps}|{MinPulseDurationRa}|{MinPulseDurationDec}|{DecPulseGoTo}" };
            MonitorLog.LogToMonitor(monitorItem);

            var pulseEntry = new PulseEntry // setup to graph the pulse
            { Axis = (int)axis, Duration = duration, Rate = guideRate, Rejected = false, StartTime = datetime, };

            backlashSteps = Math.Abs(backlashSteps);
            var arcsecs = duration / 1000.0 * Math.Abs(guideRate) * 3600.0;

            switch (axis)
            {
                case AxisId.Axis1:
                    SkyQueue.IsPulseGuidingRa = true;
                    SkyQueue.IsPulseGuidingDec = false;

                    if (backlashSteps > 0) // Convert lash to extra pulse duration in milliseconds
                    {
                        var lashduration = backlashSteps / _stepsPerSecond[0] / 3600 / Math.Abs(guideRate) * 1000;
                        duration += (int)lashduration;
                    }

                    if (duration < MinPulseDurationRa)
                    {
                        SkyQueue.IsPulseGuidingRa = false;

                        if (!MonitorPulse) { return; }
                        pulseEntry.Rejected = true;
                        MonitorLog.LogToMonitor(pulseEntry);
                        return;
                    }

                    var rate = SouthernHemisphere ? -Math.Abs(_trackingRates[0]) : Math.Abs(_trackingRates[0]);

                    var radRate = BasicMath.DegToRad(guideRate);
                    var aplyRate = rate + radRate;
                    if (Math.Abs(aplyRate) < 0.000001)               // When guide rate is 100% check to avoid possible zero                        
                    {                                                // small numbers will mess up the CalculateSpeed.
                        aplyRate = Constant.SiderealRate / 1000.0;   // assign a number so mount looks like it's stopped
                    }

                    if (_pPecOn && AlternatingPPec) { SetPPec(AxisId.Axis1, false); } // implements the alternating pPEC 

                    // Change speed of the R.A. axis
                    if (_commands.SupportAdvancedCommandSet && _commands.AllowAdvancedCommandSet)
                    {
                        _commands.AxisSlew_Advanced(AxisId.Axis1, aplyRate);
                    }
                    else
                    {
                        var speedInt = CalculateSpeed(AxisId.Axis1, aplyRate);  // Calculate mount speed  
                        _commands.SetStepSpeed(AxisId.Axis1, speedInt); // :I Send pulse to axis
                    }

                    pulseEntry.StartTime = _commands.LastI1RunTime; // get the last :I start time

                    var raPulseTime = Principles.HiResDateTime.UtcNow - pulseEntry.StartTime; // possible use for min pulse duration time
                    var raspan = duration - raPulseTime.TotalMilliseconds;

                    if (raspan > 0 && raspan < duration) // checking duration is met
                    {
                        var sw1 = Stopwatch.StartNew();
                        while (sw1.Elapsed.TotalMilliseconds < raspan)
                        {
                            if (sw1.ElapsedMilliseconds % 200 == 0){UpdateSteps();} // Process positions while waiting
                        } 
                    }

                    // Restore rate tracking
                    if (_commands.SupportAdvancedCommandSet && _commands.AllowAdvancedCommandSet)
                    {
                        //_commands.AxisSlew_Advanced(AxisId.Axis1, _trackingSpeeds[0]);
                        _commands.AxisSlew_Advanced(AxisId.Axis1, _trackingRates[0]);
                    }
                    else
                    {
                        _commands.SetStepSpeed(AxisId.Axis1, _trackingSpeeds[0]); // :I set speed back to current tracking speed
                    }

                    if (_pPecOn && AlternatingPPec) { SetPPec(AxisId.Axis1, true); } // implements the alternating pPEC

                    SkyQueue.IsPulseGuidingRa = false;
                    break;
                case AxisId.Axis2:
                    SkyQueue.IsPulseGuidingDec = true;
                    SkyQueue.IsPulseGuidingRa = false;

                    if (DecPulseGoTo)
                    {
                        // Turns the pulse into steps and does a goto which is faster than using guide rate
                        var stepsNeeded = (int)(arcsecs * _stepsPerSecond[1]);
                        stepsNeeded += backlashSteps;

                        if (backlashSteps > 0) // Convert lash to pulse duration in milliseconds
                        {
                            var lashduration = backlashSteps / _stepsPerSecond[1] / 3600 / Math.Abs(guideRate) * 1000;
                            duration += (int)lashduration;
                        }

                        if (stepsNeeded < 1 || duration < MinPulseDurationDec)
                        {
                            SkyQueue.IsPulseGuidingDec = false;
                            if (!MonitorPulse) return;
                            pulseEntry.Rejected = true;
                            MonitorLog.LogToMonitor(pulseEntry);
                            return;
                        }
                        if (guideRate < 0) { stepsNeeded = -stepsNeeded; }

                        pulseEntry.StartTime = Principles.HiResDateTime.UtcNow;

                        AxisMoveSteps(AxisId.Axis2, stepsNeeded);

                        var axesstatus = _commands.GetAxisStatus(AxisId.Axis2);
                        if (!axesstatus.FullStop)
                        {
                            // Wait until the axis stops or counter runs out
                            var stopwatch = Stopwatch.StartNew();
                            while (stopwatch.Elapsed.TotalMilliseconds <= 3500)
                            {
                                axesstatus = _commands.GetAxisStatus(AxisId.Axis2);
                                // Return if the axis has stopped.
                                if (axesstatus.FullStop) { break; }
                                Thread.Sleep(10);
                            }
                        }
                    }
                    else
                    {
                        if (backlashSteps > 0) // Convert lash to extra pulse duration in milliseconds
                        {
                            var lashduration = Convert.ToInt32(backlashSteps / _stepsPerSecond[1] / 3600 / Math.Abs(guideRate) * 1000);
                            // PHD will error if pulse doesn't return within 2 seconds.
                            if (lashduration > 1000){lashduration = 1000;} 
                            duration += lashduration; // add the lash time to duration
                        }

                        if (duration < MinPulseDurationDec)
                        {
                            SkyQueue.IsPulseGuidingDec = false;
                            if (!MonitorPulse) { return; }
                            pulseEntry.Rejected = true;
                            MonitorLog.LogToMonitor(pulseEntry);
                            return;
                        }
                        
                        AxisSlew(AxisId.Axis2, guideRate); // Send pulse to axis 
                        pulseEntry.StartTime = _commands.LastJ2RunTime; // last :J2 start time
                        var decPulseTime = Principles.HiResDateTime.UtcNow - pulseEntry.StartTime; // possible use for min pulse duration time
                        var decspan = duration - decPulseTime.TotalMilliseconds;

                        if (decspan > 0 && decspan < duration) // checking duration is met
                        {
                            var sw3 = Stopwatch.StartNew();
                            while (sw3.Elapsed.TotalMilliseconds < decspan)
                            {
                                if (sw3.ElapsedMilliseconds % 200 == 0){UpdateSteps(); } // Process positions while waiting
                            } 
                        }

                        AxisStop(AxisId.Axis2);
                    }

                    SkyQueue.IsPulseGuidingDec = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }

            if (!MonitorPulse) return;
            pulseEntry.Duration = duration;
            MonitorLog.LogToMonitor(pulseEntry);//send to monitor
        }

        /// <summary>
        /// Directs Ra or Dec pulse guide command to Alt and Az axes at calculated rates, adding tracking rates
        /// </summary>
        /// <param name="axis">Ra or Dec axis</param>
        /// <param name="guideRateAz">Guide rate degrees per second, negative value denotes direction</param>
        /// <param name="guideRateAlt">Guide rate degrees per negative value denotes direction</param>
        /// <param name="duration">length of pulse in milliseconds, always positive numbers</param>
        /// <param name="backlashSteps">Positive micro steps added for backlash</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        internal void AxisPulse(AxisId axis, double guideRateAz, double guideRateAlt, int duration,
            int backlashSteps = 0)
        {
            var datetime = Principles.HiResDateTime.UtcNow;
            var monitorItem = new MonitorEntry // setup to log the pulse
            {
                Datetime = datetime,
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Mount,
                Type = MonitorType.Debug,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message =
                    $"Az|{guideRateAz}|Alt|{guideRateAlt}|{duration}|{backlashSteps}|{MinPulseDurationRa}|{MinPulseDurationDec}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            var guideRate = Math.Sqrt(guideRateAz * guideRateAz + guideRateAlt * guideRateAlt);
            var pulseEntry = new PulseEntry // setup to graph the pulse
            { Axis = (int)axis, Duration = duration, Rate = guideRate, Rejected = false, StartTime = datetime, };

            backlashSteps = Math.Abs(backlashSteps);

            SkyQueue.IsPulseGuidingRa = true;
            SkyQueue.IsPulseGuidingDec = true;

            if (backlashSteps > 0) // Convert lash to extra pulse duration in milliseconds
            {
                var lashduration =
                    Convert.ToInt32(backlashSteps / _stepsPerSecond[1] / 3600 / Math.Abs(guideRateAz) * 1000);
                // PHD will error if pulse doesn't return within 2 seconds.
                if (lashduration > 1000)
                {
                    lashduration = 1000;
                }

                duration += lashduration; // add the lash time to duration
            }

            if (duration < MinPulseDurationRa || duration < MinPulseDurationDec)
            {
                SkyQueue.IsPulseGuidingRa = false;
                SkyQueue.IsPulseGuidingDec = false;

                if (!MonitorPulse)
                {
                    return;
                }

                pulseEntry.Rejected = true;
                MonitorLog.LogToMonitor(pulseEntry);
                return;
            }

            if (_pPecOn && AlternatingPPec) { SetPPec(AxisId.Axis1, false); } // implements the alternating pPEC 

            // Change speed of the Az and Alt axes
            if (_commands.SupportAdvancedCommandSet && _commands.AllowAdvancedCommandSet)
            {
                _commands.AxisSlew_Advanced(AxisId.Axis1, BasicMath.DegToRad(guideRateAz));
                _commands.AxisSlew_Advanced(AxisId.Axis2, BasicMath.DegToRad(guideRateAlt));
            }
            else
            {
                AxisSlew(AxisId.Axis1, guideRateAz);
                AxisSlew(AxisId.Axis2, guideRateAlt);
            }

            pulseEntry.StartTime = _commands.LastI1RunTime; // get the last :I start time

            var pulseTime = Principles.HiResDateTime.UtcNow - pulseEntry.StartTime; // possible use for min pulse duration time
            var span = duration - pulseTime.TotalMilliseconds;

            if (span > 0 && span < duration) // checking duration is met
            {
                var sw1 = Stopwatch.StartNew();
                while (sw1.Elapsed.TotalMilliseconds < span)
                {
                    if (sw1.ElapsedMilliseconds % 200 == 0) { UpdateSteps(); } // Process positions while waiting
                }
            }

            // Restore rate tracking
            if (_commands.SupportAdvancedCommandSet && _commands.AllowAdvancedCommandSet)
            {
                _commands.AxisSlew_Advanced(AxisId.Axis1, _trackingRates[0]);
                _commands.AxisSlew_Advanced(AxisId.Axis2, _trackingRates[1]);
            }
            else
            {
                AxisSlew(AxisId.Axis1, BasicMath.RadToDeg(_trackingRates[0]));
                AxisSlew(AxisId.Axis2, BasicMath.RadToDeg(_trackingRates[1]));
            }

            if (_pPecOn && AlternatingPPec) { SetPPec(AxisId.Axis1, true); } // implements the alternating pPEC

            SkyQueue.IsPulseGuidingDec = false;
            SkyQueue.IsPulseGuidingRa = false;

            if (!MonitorPulse) return;
            pulseEntry.Duration = duration;
            MonitorLog.LogToMonitor(pulseEntry);//send to monitor
        }

        /// <summary>
        /// Stop the target axis normally
        /// </summary>
        /// <param name="axis">>AxisId.Axis1 or AxisId.Axis2</param>
        internal void AxisStop(AxisId axis)
        {
            _slewingSpeed[(int)axis] = 0;
            _commands.AxisStop(axis);
            _commands.GetAxisPositionCounter(axis); // read for plotting
        }

        /// <summary>
        /// Stop the target axis instantly
        /// </summary>
        /// <param name="axis">>AxisId.Axis1 or AxisId.Axis2</param>
        internal void AxisStopInstant(AxisId axis)
        {
            _slewingSpeed[(int)axis] = 0;
            _commands.AxisStopInstant(axis);
            _commands.GetAxisPositionCounter(axis); // read for plotting
        }

        /// <summary>
        /// Use goto mode to target position
        /// </summary>
        /// <param name="axis">>AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="movingSteps">Micro steps to move</param>
        internal void AxisMoveSteps(AxisId axis, long movingSteps)
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Debug, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}|{movingSteps}" };
            MonitorLog.LogToMonitor(monitorItem);

            if (_commands.SupportAdvancedCommandSet && _commands.AllowAdvancedCommandSet)
            {
                long targetPositionInSteps = _commands.GetAxisPositionCounter(axis) + movingSteps;
                _commands.AxisSlewTo_Advanced(axis, _commands.StepToAngle(axis, targetPositionInSteps));
            }
            else
            {
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

                    // Wait until the axis stops or counter runs out
                    var stopwatch = Stopwatch.StartNew();
                    var counter = 1;
                    while (stopwatch.Elapsed.TotalMilliseconds <= 3500)
                    {
                        axesstatus = _commands.GetAxisStatus(axis);
                        // Return if the axis has stopped.
                        if (axesstatus.FullStop) { break; }
                        // issue new stop
                        if (counter % 5 == 0) { AxisStop(axis); }
                        counter++;
                        Thread.Sleep(100);
                    }
                }

                _commands.SetMotionMode(axis, 2, direction, SouthernHemisphere); // G: '2' low  speed GOTO mode, '0'  +CW  and Nth Hemisphere
                // Skywatcher Discovery AzAlt mount seems not to move in either direction if steps is less than 10 - not backlash?
                // Lookup table maps steps in range 0 to 18 to values that will move the mount
                var model = _commands.GetModel();
                if ((movingSteps < 19) && (model[(int)axis] == (int)McModel.StarDiscovery))
                {
                    {
                        int[] lookup = { 0, 10, 10, 10, 10, 10, 11, 12, 13, 14, 14, 15, 15, 16, 16, 17, 17, 18, 18 };
                        movingSteps = lookup[movingSteps];
                    }
                }
                _commands.SetGotoTargetIncrement(axis, movingSteps); // H:
                _commands.SetBreakPointIncrement(axis, 0); // M: send 0 steps
                _commands.StartMotion(axis); // J: Start moving
            }

            _commands.GetAxisPositionCounter(axis); // read for plotting
        }

        /// <summary>
        /// Use goto mode to target position 
        /// </summary>
        /// <param name="axis">>AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="targetPosition">Total radians of target position</param>
        internal void AxisGoToTarget(AxisId axis, double targetPosition) 
        {
            var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Debug, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{axis}|{targetPosition}" };
            MonitorLog.LogToMonitor(monitorItem);

            if (_commands.SupportAdvancedCommandSet && _commands.AllowAdvancedCommandSet)
            {
                var curPosition = _commands.GetAxisPosition(axis);
                monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Mount,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"axis|{axis}|Target|{targetPosition}|curPosition|{curPosition}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                _commands.AxisSlewTo_Advanced(axis, targetPosition);
                _commands.SetSlewingTo((int)axis, false, false);  // Assume we do not need to care about slewing direction and speed when using advanced command set.
            }
            else
            {
                int direction;

                var curPosition = _commands.GetAxisPosition(axis); // :j Get current position (radians) of the axis.

                // Calculate slewing distance.
                // Note: For EQ mount, Positions[AXIS1] is offset( -PI/2 ) adjusted in GetAxisPosition().
                var movingAngle = targetPosition - curPosition;

                var movingSteps = _commands.AngleToStep(axis, movingAngle);// Convert distance in radian into steps.

                monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Mount,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"axis|{axis}|Target|{targetPosition}|curPosition|{curPosition}|movingSteps|{movingSteps}"
                };
                MonitorLog.LogToMonitor(monitorItem);

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
                    var counter = 1;
                    while (sw.Elapsed.TotalMilliseconds <= 3500)
                    {
                        axesstatus = _commands.GetAxisStatus(axis);
                        // Return if the axis has stopped.
                        if (axesstatus.FullStop) { break; }
                        if (counter % 5 == 0) { AxisStop(axis); }
                        counter++;
                        Thread.Sleep(100);
                    }
                }

                // Check if the distance is long enough to trigger a high speed GOTO.
                if (movingSteps > _lowSpeedGotoMargin[(int)axis])
                {
                    _commands.SetMotionMode(axis, 0, direction, SouthernHemisphere); // :G high speed GOTO slewing 
                    highspeed = true;
                }
                else
                {
                    _commands.SetMotionMode(axis, 2, direction, SouthernHemisphere); // :G low speed GOTO slewing
                    highspeed = false;
                }
                // Skywatcher Discovery AzAlt mount seems not to move in either direction if steps is less than 10 - not backlash?
                // Lookup table maps steps in range 0 to 18 to values that will move the mount
                var model =_commands.GetModel();
                if ((movingSteps < 19) && (model[(int)axis] == (int)McModel.StarDiscovery))
                {
                    {
                        int[] lookup = { 0, 10, 10, 10, 10, 10, 11, 12, 13, 14, 14, 15, 15, 16, 16, 17, 17, 18, 18 };
                        movingSteps = lookup[movingSteps];
                    }
                }
                _commands.SetGotoTargetIncrement(axis, movingSteps); // :H
                _commands.SetBreakPointIncrement(axis, _breakSteps[(int)axis]); // :M
                _commands.StartMotion(axis); // :J

                _commands.SetSlewingTo((int)axis, forward, highspeed);
            }
            _commands.GetAxisPositionCounter(axis); // read for plotting

            _targetPositions[(int)axis] = targetPosition;
        }

        /// <summary>
        /// Bypass for mount commands
        /// </summary>
        /// <param name="axis">1 or 2</param>
        /// <param name="cmd">The command char set</param>
        /// <param name="cmdData">The data need to send</param>
        /// <param name="ignoreWarnings">ignore serial response issues?</param>
        /// <returns>mount data, null for IsNullOrEmpty</returns>
        /// <example>CmdToMount(1,"X","0003","true")</example>
        internal string CmdToMount(int axis, string cmd, string cmdData, string ignoreWarnings)
        {
            return  _commands.CmdToMount(axis, cmd, cmdData, ignoreWarnings);
        }

        /// <summary>
        /// Supports the new advanced command set
        /// </summary>
        /// <returns></returns>
        internal bool GetAdvancedCmdSupport()
        {
            return _commands.SupportAdvancedCommandSet;
        }

        /// <summary>
        /// Get axis position in degrees
        /// </summary>
        /// <returns>array in degrees, could return array of NaN if no responses returned</returns>
        internal double[] GetPositionsInDegrees()
        {
            var positions = new double[] { 0, 0 };

            var x = _commands.GetAxisPositionNaN(AxisId.Axis1);
            if (!double.IsNaN(x)) { x = Principles.Units.Rad2Deg1(x); }
            positions[0] = x;

            var y = _commands.GetAxisPositionNaN(AxisId.Axis2);
            if (!double.IsNaN(y)) { y = Principles.Units.Rad2Deg1(y); }
            positions[1] = y;

            return positions;
        }

        /// <summary>
        /// Get axis position in steps
        /// </summary>
        /// <returns>array in steps, could return array of NaN if no responses returned</returns>
        internal double[] GetSteps()
        {
            var positions = new double[] { 0, 0 };

            var x = _commands.GetAxisStepsNaN(AxisId.Axis1);
            positions[0] = x;

            var y = _commands.GetAxisStepsNaN(AxisId.Axis2);
            positions[1] = y;

            return positions;
        }

        /// <summary>
        /// Get axis positions in steps and update the property
        /// </summary>
        /// <returns>array in steps</returns>
        internal void UpdateSteps()
        {
            try
            {
                var positions = new double[] { 0, 0 };

                var x = _commands.GetAxisStepsNaN(AxisId.Axis1);
                positions[0] = x;

                var y = _commands.GetAxisStepsNaN(AxisId.Axis2);
                positions[1] = y;

                if (double.IsNaN(positions[0]) || double.IsNaN(positions[1])){return;}

                var a = new[] { positions[0], positions[1] };
                SkyQueue.Steps = a;
            }
            catch (Exception e)
            {
                var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{e.Message}" };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        /// <summary>
        /// Gets axes board versions in a readable format
        /// </summary>
        /// <returns></returns>
        internal string[] GetAxisStringVersions()
        {
            var ret = _commands.GetAxisStringVersions();

            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ret[0]}|{ret[1]}" };
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
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ret[0]}|{ret[1]}" };
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
        /// Get the sidereal rate in step counts
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
        /// reset the position of an axis
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="newValue">steps</param>
        internal void SetAxisPositionCounter(AxisId axis, int newValue)
        {
            _commands.SetAxisPositionCounter(axis, newValue);
        }

        /// <summary>
        /// Turn on/off individual axis encoders
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="on">on=true,off=false</param>
        internal void SetEncoder(AxisId axis, bool on)
        {
            if (!CanDualEncoders) { return; }
            _commands.SetEncoders(axis, on);
        }

        /// <summary>
        /// Turn on/off pPEC
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="on">on=true,off=false</param>
        internal string SetPPec(AxisId axis, bool on)
        {
            return !CanPPec ? null : _commands.SetPPec(axis, on);
        }

        /// <summary>
        /// Turn on/off training pPEC
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="on">on=true,off=false</param>
        internal void SetPPecTrain(AxisId axis, bool on)
        {
            if (!CanPPec) { return; }
            _commands.SetPPecTrain(axis, on);
        }

        /// <summary>
        /// Enable or Disable Full Current Low speed
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="on">on=true,off=false</param>
        internal void SetFullCurrent(AxisId axis, bool on)
        {
            if (!CanHalfTrack) { return; }
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
            ParseCapabilities();
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
        /// j Gets axis position counter
        /// </summary>
        /// <param name="axis">AxisId.Axis1 or AxisId.Axis2</param>
        /// <param name="raw">false to subtract 0x00800000</param>
        /// <returns>Cardinal encoder count</returns>
        internal long GetAxisPositionCounter(AxisId axis, bool raw = false)
        {
            return _commands.GetAxisPositionCounter(axis, raw);
        }

        /// <summary>
        /// Gets the position and the timestamp
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        internal Tuple<double?, DateTime> GetAxisPositionDate(AxisId axis)
        {
            switch (axis)
            {
                case AxisId.Axis1:
                    return new Tuple<double?, DateTime>(_commands.GetAxisPositionCounter(axis),
                        _commands.Last_j1RunTime);
                case AxisId.Axis2:
                    return new Tuple<double?, DateTime>(_commands.GetAxisPositionCounter(axis),
                        _commands.Last_j2RunTime);
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
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

        internal long? GetHomePosition(AxisId axis)
        {
            return !CanHomeSensors ? ((long?)null).GetValueOrDefault() : _commands.GetHomePosition(axis);
        }

        internal string GetMotorCardVersion(AxisId axis)
        {
            _commands.GetAxisVersion(axis);
            var models = _commands.GetModel();
            var versions = _commands.GetAxisStringVersions();

            try
            {
                switch (axis)
                {
                    case AxisId.Axis1:
                        MountVersion = versions[0];
                        MountType = GetEnumDescription((McModel)models[0]);
                        MountNum = models[0];
                        break;
                    case AxisId.Axis2:
                        MountVersion = versions[1];
                        MountType = GetEnumDescription((McModel)models[1]);
                        MountNum = models[1];
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
                }
            }
            catch (Exception)
            {
                MountVersion = "99";
            }

            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{MountType}|{MountVersion}|{MountNum}" };
            MonitorLog.LogToMonitor(monitorItem);

            return MountVersion;
        }

        internal double GetPecPeriod(AxisId axis)
        {
            return _commands.GetPecPeriod(axis);
        }

        internal string GetPositionsAndTime(bool raw, string pp = "00000000")
        {
            return _commands.GetPositionsAndTime(raw, pp);
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
        /// <param name="direction">0=forward/right, 1=backward/left</param>
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
        /// set ST4 guide rate
        /// </summary>
        /// <param name="rate"> 0..4 (1.0, 0.75, 0.50, 0.25, 0.125)</param>
        internal void SetSt4GuideRate(int rate)
        {
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
            _commands.SetSt4GuideRate(cmd);
        }

        /// <summary>
        /// O on/off trigger
        /// </summary>
        /// <param name="port"></param>
        /// <param name="on"></param>
        internal bool SetSnapPort(int port, bool on)
        {
            AxisId axis;
            switch (port)
            {
                case 1:
                    axis = AxisId.Axis1;
                    break;
                case 2:
                    axis = AxisId.Axis2;
                    break;
                default:
                    return false;
            }
            var a = _commands.SetSnapPort(axis, on).Trim();
            return a == "=";
        } 

        internal void SetTargetPosition(AxisId axis, double position)
        {
            _commands.SetTargetPosition(axis, position);
        }

        internal void SetPolarLedLevel(AxisId axis, int level)
        {
            if (CanPolarLed) { _commands.SetPolarLedLevel(axis, level); }
        }

        /// <summary>
        /// Store the rates to use later 
        /// </summary>
        /// <param name="axis">which axis</param>
        /// <param name="rate">arc seconds in rad</param>
        private void SetRates(AxisId axis, double rate)
        {
            if (axis == AxisId.Axis1)
            {
                _trackingRates[0] = rate;
                //_trackingSpeeds[0] = CalculateSpeed(AxisId.Axis1, rate);
            }
            else
            {
                _trackingRates[1] = rate;
                //_trackingSpeeds[1] = CalculateSpeed(AxisId.Axis2, rate);
            }

            if (_commands.SupportAdvancedCommandSet && _commands.AllowAdvancedCommandSet) return;

            if (axis == AxisId.Axis1)
            {
                // _trackingRates[0] = rate;
                _trackingSpeeds[0] = CalculateSpeed(AxisId.Axis1, rate);
            }
            else
            {
                //  _trackingRates[1] = rate;
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

        internal double Get_j(AxisId axis, bool raw = false)
        {
            return _commands.Get_j(axis, raw);
        }

        internal long[] GetLowSpeedGotoMargin()
        {
            return _commands.GetLowSpeedGotoMargin();
        }

        internal double[] GetFactorRadRateToInt()
        {
            return _commands.GetFactorRadRateToInt();
        }

        internal double[] GetFactorStepToRad()
        {
            return _commands.GetFactorStepToRad();
        }

        internal string GetCapabilities()
        {
            return Capabilities;
        }

        internal int GetAngleToStep(AxisId axis, double angleInRad)
        {
            return _commands.AngleToStep(axis, angleInRad);
        }

        internal double GetStepToAngle(AxisId axis, long steps)
        {
            return _commands.StepToAngle(axis, steps);
        }

        #endregion

        #region Methods

        private void SetStepsPerSecond()
        {
            _stepsPerSecond[0] = Convert.ToDouble(GetStepsPerRevolution()[0] / 360.0 / 3600);
            _stepsPerSecond[1] = Convert.ToDouble(GetStepsPerRevolution()[1] / 360.0 / 3600);
        }

        /// <summary>
        /// Used as a multiplier to determine speed for the I command
        /// in general divide 1 by radians then multiply by this number 
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="rateInRad"></param>
        /// <returns></returns>
        private long RadSpeedToInt(AxisId axis, double rateInRad)
        {
            try
            {
                _factorRadRateToInt = _commands.GetFactorRadRateToInt();
                var r = (rateInRad * _factorRadRateToInt[(int)axis]);
                var a = (long)Math.Round(r, 0, MidpointRounding.AwayFromZero);
                return a;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// parses status from the q command using =010000
        /// </summary>
        private void ParseCapabilities()
        {
            var result = _commands.GetCapabilities(AxisId.Axis1);
            Capabilities = result;
            if (result == null || result.Contains("!") || result.Length < 3 || !result.Contains("="))
            {
                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Warning, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{result}" };
                MonitorLog.LogToMonitor(monitorItem);
                return;
            }
            if (result.Contains("=")) { result = result.Replace("=", ""); }

            var code = result.ToCharArray();
            for (var i = 0; i < code.Length; i++)
            {
                var b = Enums.GetFlags((StatusBit)uint.Parse(code[i].ToString(), NumberStyles.HexNumber));

                switch (i)
                {
                    case 0:
                        foreach (var item in b)
                        {
                            switch (item)
                            {
                                case StatusBit.None: // 0
                                    IsPPecOn = false;
                                    IsPPecInTrainingOn = false;
                                    break;
                                case StatusBit.A: // 1
                                    IsPPecInTrainingOn = true;
                                    break;
                                case StatusBit.B: // 2
                                    IsPPecOn = true;
                                    break;
                                case StatusBit.C: // 4
                                case StatusBit.D: // 8
                                    //not defined
                                    break;
                            }
                        }
                        break;
                    case 1:
                        foreach (var item in b)
                        {
                            switch (item)
                            {
                                case StatusBit.None: // 0
                                    CanAzEq = false;
                                    CanHomeSensors = false;
                                    CanPPec = false;
                                    CanDualEncoders = false;
                                    break;
                                case StatusBit.A: // 1
                                    CanDualEncoders = true;
                                    break;
                                case StatusBit.B: // 2
                                    CanPPec = true;
                                    break;
                                case StatusBit.C: // 4
                                    CanHomeSensors = true;
                                    break;
                                case StatusBit.D: // 8
                                    CanAzEq = true;
                                    break;
                            }
                        }
                        break;
                    case 2:
                        foreach (var item in b)
                        {
                            switch (item)
                            {
                                case StatusBit.None: // 0
                                    CanWifi = false;
                                    CanHalfTrack = false;
                                    CanPolarLed = false;
                                    CanAxisSlewsIndependent = false;
                                    break;
                                case StatusBit.A: // 1
                                    CanPolarLed = true;
                                    break;
                                case StatusBit.B: // 2
                                    CanAxisSlewsIndependent = true;
                                    break;
                                case StatusBit.C: // 4
                                    CanHalfTrack = true;
                                    break;
                                case StatusBit.D: // 8
                                    CanWifi = true;
                                    break;
                            }
                        }
                        break;
                    case 3:
                    case 4:
                    case 5:
                        //not defined all zeros
                        break;
                }
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

            // For using function RadSpeedToInt(), change to unit Seconds/Rad.
            // ie. sending sidereal as internal speed will calculate how many rads per second
            speed = 1 / speed;

            //Conversion of radians to int which sets the speed in steps
            var speedInt = RadSpeedToInt(axis, speed);

            //Set maximum speed, The lower the number the faster speed
            if (_axisVersion[0] == 0x010600 || _axisVersion[0] == 0x010601)
            {
                // For special MC version.
                speedInt -= 3;
            }

            if (speedInt < 3)
            {
                speedInt = 3;
            }

            return speedInt;
        }

        private static string GetEnumDescription(Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());

            if (fi.GetCustomAttributes(typeof(DescriptionAttribute), false) is DescriptionAttribute[] attributes && attributes.Any())
            {
                return attributes.First().Description;
            }

            return value.ToString();
        }

        #endregion
    }

    [Flags]
    public enum StatusBit
    {
        None = 0,
        A = 1,
        B = 1 << 1,
        C = 1 << 2,
        D = 1 << 3
    }
}
