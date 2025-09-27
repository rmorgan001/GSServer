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
using System;
using System.Threading;

namespace GS.Simulator
{
    /// <summary>
    /// Simulates a mount controller card that drives dual stepper motors
    /// </summary>
    internal class Controllers
    {
        #region Fields

        private static CancellationTokenSource _ctsMount = new CancellationTokenSource();
        private const long RevolutionSteps = 12960000;
        private const long WormRevolutionSteps = 64800;
        private const int MaxSteps = Int32.MaxValue;
        private const int MinSteps = Int32.MinValue;
        private const bool CanAxisSlewsIndependent = false;
        private const bool CanAzEq = false;
        private const bool CanDualEncoders = false;
        private const bool CanHalfTrack = false;
        private const bool CanHomeSensors = true;
        private const bool CanPolarLed = false;
        private const bool CanPPec = false;
        private const bool CanWifi = false;
        private const string MountName = "SimScope";
        private const string MountVersion = "0100";
        private bool _running;
        private DateTime _lastUpdateTime;
        private double _gotoX;
        private double _gotoY;
        private double _pulseX;
        private double _pulseY;
        private double _raDecRateX;
        private double _raDecRateY;
        private double _moveAxisRateX;
        private double _moveAxisRateY;
        private double _slewX;
        private double _slewY;
        private double _trackingX;
        private double _trackingY;
        private bool _isStoppedX;
        private bool _isStoppedY;

        private bool _isSlewingX;
        private bool _isSlewingY;
        private bool _isTrackingX;
        private bool _isTrackingY;
        private bool _isRateTrackingX;
        private bool _isRateTrackingY;
        private bool _isRateAxisSlewingX;
        private bool _isRateAxisSlewingY;
        private bool _isSlewSlewingX;
        private bool _isSlewSlewingY;
        private bool _isGotoSlewingX;
        private bool _isGotoSlewingY;
        private bool _homeSensorX;
        private bool _homeSensorY;

        private const int FactorSteps = 36000;
        private const int MaxRate = 4;
        private const double SlewSpeedOne = .01 * MaxRate;
        //private const double SlewSpeedTwo = .02 * _maxRate;
        //private const double SlewSpeedThree = 1.0 * _maxRate;
        private const double SlewSpeedFour = 1.5 * MaxRate;
        //private const double SlewSpeedFive = 2.0 * _maxRate;
        private const double SlewSpeedSix = 2.5 * MaxRate;
        //private const double SlewSpeedSeven = 3.0 * _maxRate;
        //private const double SlewSpeedEight = 3.4 * _maxRate;

        #endregion

        #region Properties

        private double DegreesX { get; set; }
        private double DegreesY { get; set; }
        private int StepsX => (int)(DegreesX * FactorSteps);
        private int StepsY => (int)(DegreesY * FactorSteps);
        private double HcX { get; set; }
        private double HcY { get; set; }
        private int HomeSensorX { get; set; }
        private int HomeSensorY { get; set; }
        private int SlewSpeedEight { get; set; }
        private bool SnapPort1 { get; set; }
        private bool SnapPort2 { get; set; }

        #endregion

        internal Controllers()
        {
            DegreesX = 0;
            DegreesY = 0;
            SlewSpeedEight = 13;
            SnapPort1 = false;
            SnapPort2 = false;
        }

        /// <summary>
        /// 
        /// </summary>
        private void CheckSlewing()
        {
            if (_isGotoSlewingX || _isRateAxisSlewingX || _isSlewSlewingX)
            {
                _isSlewingX = true;
            }
            else
            {
                _isSlewingX = false;
            }

            if (_isGotoSlewingY || _isRateAxisSlewingY || _isSlewSlewingY)
            {
                _isSlewingY = true;
            }
            else
            {
                _isSlewingY = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="changeX"></param>
        /// <param name="changeY"></param>
        private void CheckStopped(double changeX, double changeY)
        {
            if (Math.Abs(changeX) > 0)
            {
                _isStoppedX = false;
                DegreesX += changeX;
            }
            else
            {
                _isStoppedX = true;
            }

            if (Math.Abs(changeY) > 0)
            {
                _isStoppedY = false;
                DegreesY += changeY;
            }
            else
            {
                _isStoppedY = true;
            }
        }

        /// <summary>
        /// Process incoming commands
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        internal string Command(string command)
        {
            var cmd = command.Split('|');
            double a;
            switch (cmd[0].ToLowerInvariant())
            {
                case "capabilities":
                    return
                        $"{CanAxisSlewsIndependent}|{CanAzEq}|{CanDualEncoders}|{CanHalfTrack}|{CanHomeSensors}|{CanPolarLed}|{CanPPec}|{CanWifi}|{MountName}|{MountVersion}";
                case "initialize":
                    return Start().ToString();
                case "shutdown":
                    return Stop().ToString();
                case "radecrate":
                    a = Convert.ToDouble(cmd[2]);
                    switch (ParseAxis(cmd[1]))
                    {
                        case Axis.Axis1:
                            _isRateTrackingX = true;
                            _raDecRateX = a;
                            break;
                        case Axis.Axis2:
                            _isRateTrackingY = true;
                            _raDecRateY = a;
                            break;
                    }
                    break;
                case "moveaxisrate":
                    a = Convert.ToDouble(cmd[2]);
                    switch (ParseAxis(cmd[1]))
                    {
                        case Axis.Axis1:
                            _isRateAxisSlewingX = true;
                            _moveAxisRateX = a;
                            break;
                        case Axis.Axis2:
                            _isRateAxisSlewingY = true;
                            _moveAxisRateY = a;
                            break;
                    }
                    break;
                case "slew":
                    a = Convert.ToDouble(cmd[2]);
                    switch (ParseAxis(cmd[1]))
                    {
                        case Axis.Axis1:
                            _isSlewSlewingX = true;
                            _slewX = a;
                            break;
                        case Axis.Axis2:
                            _isSlewSlewingY = true;
                            _slewY = a;
                            break;
                    }

                    break;
                case "pulse":
                    a = Convert.ToDouble(cmd[2]);
                    switch (ParseAxis(cmd[1]))
                    {
                        case Axis.Axis1:
                            _isSlewSlewingX = true;
                            _slewX = a;
                            break;
                        case Axis.Axis2:
                            _isSlewSlewingY = true;
                            _slewY = a;
                            break;
                    }

                    break;
                case "degrees":
                    switch (ParseAxis(cmd[1]))
                    {
                        case Axis.Axis1:
                            return $"{DegreesX}";
                        case Axis.Axis2:
                            return $"{DegreesY}";
                    }

                    break;
                case "steps":
                    switch (ParseAxis(cmd[1]))
                    {
                        case Axis.Axis1:
                            return $"{StepsX}";
                        case Axis.Axis2:
                            return $"{StepsY}";
                    }

                    break;
                case "stop":
                    StopAxis(ParseAxis(cmd[1]));
                    break;
                case "hc":
                    a = Convert.ToDouble(cmd[2]);
                    switch (ParseAxis(cmd[1]))
                    {
                        case Axis.Axis1:
                            HcX = a;
                            break;
                        case Axis.Axis2:
                            HcY = a;
                            break;
                    }

                    break;
                case "tracking":
                    a = Convert.ToDouble(cmd[2]);
                    switch (ParseAxis(cmd[1]))
                    {
                        case Axis.Axis1:
                            _trackingX = a;
                            break;
                        case Axis.Axis2:
                            _trackingY = a;
                            break;
                    }

                    break;
                case "getrevsteps":
                    return $"{RevolutionSteps}";
                case "gototarget":
                    a = Convert.ToDouble(cmd[2]);
                    switch (ParseAxis(cmd[1]))
                    {
                        case Axis.Axis1:
                            _isGotoSlewingX = true;
                            _gotoX = a;
                            break;
                        case Axis.Axis2:
                            _isGotoSlewingY = true;
                            _gotoY = a;
                            break;
                    }

                    break;
                case "homesensor":
                    switch (ParseAxis(cmd[1]))
                    {
                        case Axis.Axis1:
                            return $"{HomeSensorX}";
                        case Axis.Axis2:
                            return $"{HomeSensorY}";
                    }
                    break;
                case "homesensorreset":
                    HomeSensorReset(ParseAxis(cmd[1]));
                    break;
                case "setdegrees":
                    a = Convert.ToDouble(cmd[2]);
                    switch (ParseAxis(cmd[1]))
                    {
                        case Axis.Axis1:
                            DegreesX = a;
                            break;
                        case Axis.Axis2:
                            DegreesY = a;
                            break;
                    }

                    break;
                case "axisstatus":
                    CheckSlewing();
                    switch (ParseAxis(cmd[1]))
                    {
                        case Axis.Axis1:
                            return $"{_isSlewingX}|{_isStoppedX}|{_isTrackingX}|{_isRateTrackingY}";
                        case Axis.Axis2:
                            return $"{_isSlewingY}|{_isStoppedY}|{_isTrackingY}|{_isRateTrackingY}";
                    }

                    break;
                case "mountname":
                    return $"{MountName}";
                case "mountversion":
                    return $"{MountVersion}";
                case "spr":
                    return $"{RevolutionSteps}";
                case "spw":
                    return $"{WormRevolutionSteps}";
                case "factorsteps":
                    return $"{FactorSteps}";
                case "gotorate":
                    var x = Convert.ToInt32(cmd[1]);
                    if (x >= 1 && x <= 20)
                    {
                        SlewSpeedEight = x;
                    }
                    break;
                case "snapport":
                    var s = Convert.ToBoolean(cmd[2]);
                    switch (Convert.ToInt32(cmd[1]))
                    {
                        case 1:
                            if (SnapPort1 != s){SnapPort1 = s;}
                            break;
                        case 2:
                            if (SnapPort2 != s) { SnapPort2 = s; }
                            break;
                    }
                    break;
                default:
                    return "!";

            }

            return "ok";
        }

        /// <summary>
        ///  GoTo Movement
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        private double GoTo(Axis axis, double interval)
        {
            var change = 0.0;
            double delta;
            int sign;
            switch (axis)
            {
                case Axis.Axis1:
                    if (!_isGotoSlewingX || double.IsNaN(_gotoX))
                    {
                        _isGotoSlewingX = false;
                        return change;
                    }
                    delta = _gotoX - DegreesX;
                    sign = delta < 0 ? -1 : 1;
                    delta = Math.Abs(delta);
                    break;
                case Axis.Axis2:
                    if (!_isGotoSlewingY || double.IsNaN(_gotoY))
                    {
                        _isGotoSlewingY = false;
                        return change;
                    }
                    delta = _gotoY - DegreesY;
                    sign = delta < 0 ? -1 : 1;
                    delta = Math.Abs(delta);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }

            if (delta <= 0) return change;

            if (delta < .01)
            {
                change = delta * sign;
                switch (axis)
                {
                    case Axis.Axis1:
                        _isGotoSlewingX = false;
                        break;
                    case Axis.Axis2:
                        _isGotoSlewingY = false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
                }

                return change;
            }
            else if (delta < .2)
            {
                change = SlewSpeedOne * sign;
            }
            else if (delta < .6)
            {
                change = SlewSpeedFour * sign;
            }
            else if (delta < 1)
            {
                change = SlewSpeedSix * sign;
            }
            else
            {
                change = SlewSpeedEight * sign;
            }

            return change * interval;
        }

        /// <summary>
        /// Rate tracking Movement
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        private double RaDecRate(Axis axis, double interval)
        {
            switch (axis)
            {
                case Axis.Axis1:
                    _isRateTrackingX = Math.Abs(_raDecRateX) > 0;
                    return _isRateTrackingX ? _raDecRateX * interval : 0;
                case Axis.Axis2:
                    _isRateTrackingY = Math.Abs(_raDecRateY) > 0;
                    return _isRateTrackingY ? _raDecRateY * interval : 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }

        /// <summary>
        /// Pulse Movement
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        private double Pulse(Axis axis)
        {
            switch (axis)
            {
                case Axis.Axis1:
                    return _pulseX;
                case Axis.Axis2:
                    return _pulseY;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }

        /// <summary>
        /// RateAxis Movement
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        private double MoveAxisRate(Axis axis, double interval)
        {
            switch (axis)
            {
                case Axis.Axis1:
                    _isRateAxisSlewingX = Math.Abs(_moveAxisRateX) > 0;
                    return _isRateAxisSlewingX ? _moveAxisRateX * interval : 0;
                case Axis.Axis2:
                    _isRateAxisSlewingY = Math.Abs(_moveAxisRateY) > 0;
                    return _isRateAxisSlewingY ? _moveAxisRateY * interval : 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }

        /// <summary>
        /// Slew Movement
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        private double Slew(Axis axis, double interval)
        {
            switch (axis)
            {
                case Axis.Axis1:
                    _isSlewSlewingX = Math.Abs(_slewX) > 0;
                    return _isSlewSlewingX ? _slewX * interval : 0;
                case Axis.Axis2:
                    _isSlewSlewingY = Math.Abs(_slewY) > 0;
                    return _isSlewSlewingY ? _slewY * interval : 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }

        /// <summary>
        /// Tracking Movement
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        private double Tracking(Axis axis, double interval)
        {

            switch (axis)
            {
                case Axis.Axis1:
                    _isTrackingX = Math.Abs(_trackingX) > 0;
                    return _isTrackingX ? _trackingX * interval : 0;
                case Axis.Axis2:
                    _isTrackingY = Math.Abs(_trackingY) > 0;
                    return _isTrackingY ? _trackingY * interval : 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }

        /// <summary>
        /// Hand Controls
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        private double HandControl(Axis axis, double interval)
        {
            switch (axis)
            {
                case Axis.Axis1:
                    var hcx = Math.Abs(HcX) > 0;
                    return hcx ? HcX * interval : 0;
                case Axis.Axis2:
                    var hcy = Math.Abs(HcY) > 0;
                    return hcy ? HcY * interval : 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }

        /// <summary>
        /// Mount Thread
        /// </summary>
        private async void MountLoopAsync()
        {
            try
            {
                if (_ctsMount == null) _ctsMount = new CancellationTokenSource();
                var ct = _ctsMount.Token;
                _running = true;
                _lastUpdateTime = HiResDateTime.UtcNow;
                var task = System.Threading.Tasks.Task.Run(() =>
                {
                    while (!ct.IsCancellationRequested)
                    {
                        MoveAxes();
                    }
                }, ct);
                await task;
                task.Wait(ct);
                _running = false;
            }
            catch (OperationCanceledException)
            {
                _running = false;
            }
            catch (Exception)
            {
                _running = false;
            }
        }

        /// <summary>
        /// Main loop
        /// </summary>
        private void MoveAxes()
        {
            Thread.Sleep(20);
            var now = HiResDateTime.UtcNow;
            var seconds = (now - _lastUpdateTime).TotalSeconds;
            _lastUpdateTime = now;
            var changeX = 0.0;
            var changeY = 0.0;

            // Pulse
            changeX += Pulse(Axis.Axis1);
            changeY += Pulse(Axis.Axis2);

            // Tracking
            var trkX = Tracking(Axis.Axis1, seconds);
            var trkY = Tracking(Axis.Axis2, seconds);

            //Move Axis Rates
            var marX = MoveAxisRate(Axis.Axis1, seconds);
            var marY = MoveAxisRate(Axis.Axis2, seconds);

            // Ra & Dec Rates 
            var rdrX = RaDecRate(Axis.Axis1, seconds);
            var rdrY = RaDecRate(Axis.Axis2, seconds);

            //MoveAxis is absolute
            if (Math.Abs(marX) > 0)
            {
                changeX += marX;
            }
            else
            {
                changeX += rdrX;
                changeX += trkX;
            }

            //MoveAxis is absolute
            if (Math.Abs(marY) > 0)
            {
                changeY += marY;
            }
            else
            {
                changeY += rdrY;
                changeY += trkY;
            }

            // Slew
            changeX += Slew(Axis.Axis1, seconds);
            changeY += Slew(Axis.Axis2, seconds);

            // Hand controls
            changeX += HandControl(Axis.Axis1, seconds);
            changeY += HandControl(Axis.Axis2, seconds);

            // Slewing
            changeX += GoTo(Axis.Axis1, seconds);
            changeY += GoTo(Axis.Axis2, seconds);

            // Update Home Sensor
            HomeSensorTripCheck(Axis.Axis1, changeX);
            HomeSensorTripCheck(Axis.Axis2, changeY);

            // Updates position
            CheckStopped(changeX, changeY);

            // Updates slewing info
            CheckSlewing();
        }

        /// <summary>
        /// Checks and updates the state of the home sensor for the specified axis based on the provided change value.
        /// </summary>
        /// <remarks>This method adjusts the home sensor state for the specified axis if the axis position
        /// crosses a predefined threshold. The thresholds are determined by the <see cref="Settings.AutoHomeAxisX"/>
        /// and <see cref="Settings.AutoHomeAxisY"/> values. If the change is negligible (less than 1e-10), the method
        /// exits without making any updates.</remarks>
        /// <param name="axis">The axis to check, either <see cref="Axis.Axis1"/> or <see cref="Axis.Axis2"/>.</param>
        /// <param name="change">The change in position to evaluate. Must be a non-negligible value.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the <paramref name="axis"/> parameter is not a valid <see cref="Axis"/> value.</exception>
        private void HomeSensorTripCheck(Axis axis, double change)
        {
            if (Math.Abs(change) < .0000000001) return;
            switch (axis)
            {
                case Axis.Axis1:
                    // if (DegreesX > 110 || DegreesX < 70) return;
                    if (DegreesX > Settings.AutoHomeAxisX && _homeSensorX)
                    {
                        HomeSensorX = Settings.AutoHomeAxisX * 36000;
                        _homeSensorX = false;
                    }
                    if (DegreesX < Settings.AutoHomeAxisX && !_homeSensorX)
                    {
                        HomeSensorX = Settings.AutoHomeAxisX * 36000;
                        _homeSensorX = true;
                    }
                    break;
                case Axis.Axis2:
                    // if (DegreesY > 110 || DegreesY < 70) return;
                    if (DegreesY > Settings.AutoHomeAxisY && _homeSensorY)
                    {
                        HomeSensorY = Settings.AutoHomeAxisY * 36000;
                        _homeSensorY = false;
                    }

                    if (DegreesY < Settings.AutoHomeAxisY && !_homeSensorY)
                    {
                        HomeSensorY = Settings.AutoHomeAxisY * 36000;
                        _homeSensorY = true;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }

        /// <summary>
        /// Resets the home sensor value for the specified axis based on its current position using Advanced Command values.
        /// </summary>
        /// <remarks>This method adjusts the home sensor value for the specified axis depending on whether
        /// the current position exceeds or falls below the predefined auto-home threshold for that axis. The thresholds
        /// are defined in <see cref="Settings.AutoHomeAxisX"/> for <see cref="Axis.Axis1"/> and <see
        /// cref="Settings.AutoHomeAxisY"/> for <see cref="Axis.Axis2"/>.
        /// 0x80000000 (-2147483648 / Int32.MinValue) if axis is CW from home (ie -ve) just after home sensor trip has been reset
        /// 0x7FFFFFFF (+2147483647 / Int32.MaxValue) if axis CCW from home(ie +ve) just after home sensor trip has been reset
        /// </remarks>
        /// <param name="axis">The axis for which the home sensor should be reset. Must be either <see cref="Axis.Axis1"/> or <see
        /// cref="Axis.Axis2"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the <paramref name="axis"/> parameter is not a valid value of the <see cref="Axis"/> enumeration.</exception>
        private void HomeSensorReset(Axis axis)
        {
            switch (axis)
            {
                case Axis.Axis1:
                    if (DegreesX > Settings.AutoHomeAxisX) HomeSensorX = MinSteps;
                    if (DegreesX < Settings.AutoHomeAxisX) HomeSensorX = MaxSteps;
                    break;
                case Axis.Axis2:
                    if (DegreesY > Settings.AutoHomeAxisY) HomeSensorY = MinSteps;
                    if (DegreesY < Settings.AutoHomeAxisY) HomeSensorY = MaxSteps;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }

        /// <summary>
        /// Convert axis string to axis id
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        private static Axis ParseAxis(string axis)
        {
            switch (axis)
            {
                case "axis1":
                    return Axis.Axis1;
                case "axis2":
                    return Axis.Axis2;
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// Initialize Mount
        /// </summary>
        private bool Start()
        {
            if (!_running) MountLoopAsync();
            return _running;
        }

        /// <summary>
        /// Shutdown Mount
        /// </summary>
        private static bool Stop()
        {
            _ctsMount?.Cancel();
            _ctsMount?.Dispose();
            _ctsMount = null;
            return true;
        }

        /// <summary>
        /// Complete Stop
        /// </summary>
        /// <param name="axis"></param>
        private void StopAxis(Axis axis)
        {
            switch (axis)
            {
                case Axis.Axis1:
                    _raDecRateX = 0;
                    _gotoX = double.NaN;
                    _moveAxisRateX = 0;
                    _slewX = 0;
                    _trackingX = 0;
                    _pulseX = 0;
                    HcX = 0;
                    break;
                case Axis.Axis2:
                    _raDecRateY = 0;
                    _gotoY = double.NaN;
                    _moveAxisRateY = 0;
                    _slewY = 0;
                    _trackingY = 0;
                    _pulseY = 0;
                    HcY = 0;
                    break;
            }
        }
    }
}
