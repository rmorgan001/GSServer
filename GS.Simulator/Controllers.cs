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
using GS.Principles;
using GS.Shared;

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
        private const bool CanAxisSlewsIndependent = false;
        private const bool CanAzEq = false;
        private const bool CanDualEncoders = false;
        private const bool CanHalfTrack = false;
        private const bool CanHomeSensors = false;
        private const bool CanPolarLed = false;
        private const bool CanPpec = false;
        private const bool CanWifi = false;
        private const string MountName = "SimScope";
        private const string MountVersion = "1.0";
        private bool _running;
        private DateTime _lastUpdateTime;
        private double _gotoX;
        private double _gotoY;
        private double _pulseX;
        private double _pulseY;
        private double _rateX;
        private double _rateY;
        private double _rateAxisX;
        private double _rateAxisY;
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

        private const int _maxrate = 4;
        private const double SlewSpeedOne = .01 * _maxrate;
        //private const double SlewSpeedTwo = .02 * _maxrate;
        //private const double SlewSpeedThree = 1.0 * _maxrate;
        private const double SlewSpeedFour = 1.5 * _maxrate;
        //private const double SlewSpeedFive = 2.0 * _maxrate;
        private const double SlewSpeedSix = 2.5* _maxrate;
        //private const double SlewSpeedSeven = 3.0 * _maxrate;
        private const double SlewSpeedEight = 3.4 * _maxrate;

        #endregion

        #region Properties

        private double DegreesX { get; set; }
        private double DegreesY { get; set; }
        private int StepsX => (int) (DegreesX * 36000);
        private int StepsY => (int) (DegreesY * 36000);
        private double HcX { get; set; }
        private double HcY { get; set; }

        #endregion

        internal Controllers()
        {
            DegreesX = 0;
            DegreesY = 0;
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
        /// Process incomming commands
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        internal string Command(string command)
        {
            var cmd = command.Split(',');
            double a;
            switch (cmd[0].ToLower())
            {
                case "capabilities":
                    return
                        $"{CanAxisSlewsIndependent},{CanAzEq},{CanDualEncoders},{CanHalfTrack},{CanHomeSensors},{CanPolarLed},{CanPpec},{CanWifi},{MountName},{MountVersion}";
                case "initialize":
                    return Start().ToString();
                case "shutdown":
                    return Stop().ToString();
                case "rate":
                    a = Convert.ToDouble(cmd[2]);
                    switch (ParseAxis(cmd[1]))
                    {
                        case Axis.Axis1:
                            _isRateTrackingX = true;
                            _rateX = a;
                            break;
                        case Axis.Axis2:
                            _isRateTrackingY = true;
                            _rateY = a;
                            break;
                    }
                    break;
                case "rateaxis":
                    a = Convert.ToDouble(cmd[2]);
                    switch (ParseAxis(cmd[1]))
                    {
                        case Axis.Axis1:
                            _isRateAxisSlewingX = true;
                            _rateAxisX = a;
                            break;
                        case Axis.Axis2:
                            _isRateAxisSlewingY = true;
                            _rateAxisY = a;
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
                            return $"{_isSlewingX},{_isStoppedX},{_isTrackingX},{_isRateTrackingY}";
                        case Axis.Axis2:
                            return $"{_isSlewingY},{_isStoppedY},{_isTrackingY},{_isRateTrackingY}";
                    }

                    break;
                case "mountname":
                    return $"{MountName}";
                case "mountversion":
                    return $"{MountVersion}";
                case "spr":
                    return $"{RevolutionSteps}";
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
            else if (delta < 1 )
            {
                change = SlewSpeedSix * sign;
            }
            else
            {
                change = SlewSpeedEight * sign;
            }

            return change * interval ;
        }

        /// <summary>
        /// Rate tracking Movement
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        private double Rate(Axis axis)
        {
            switch (axis)
            {
                case Axis.Axis1:
                    _isRateTrackingX = Math.Abs(_rateX) > 0;
                    return _isRateTrackingX ? _rateX : 0;
                case Axis.Axis2:
                    _isRateTrackingY = Math.Abs(_rateY) > 0;
                    return _isRateTrackingY ? _rateY : 0;
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
        /// <returns></returns>
        private double RateAxis(Axis axis)
        {
            switch (axis)
            {
                case Axis.Axis1:
                    _isRateAxisSlewingX = Math.Abs(_rateAxisX) > 0;
                    return _isRateAxisSlewingX ? _rateAxisX : 0;
                case Axis.Axis2:
                    _isRateAxisSlewingY = Math.Abs(_rateAxisY) > 0;
                    return _isRateAxisSlewingY ? _rateAxisY : 0;
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
                    var monitorItem = new MonitorEntry
                        { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Telescope, Category = MonitorCategory.Mount, Type = MonitorType.Data, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"tracking,{_trackingX},{StepsX}" };
                    MonitorLog.LogToMonitor(monitorItem);
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

            // RateAxis
            changeX += RateAxis(Axis.Axis1);
            changeY += RateAxis(Axis.Axis2);

            // Rate
            changeX += Rate(Axis.Axis1);
            changeY += Rate(Axis.Axis2);

            // Slew
            changeX += Slew(Axis.Axis1, seconds);
            changeY += Slew(Axis.Axis2, seconds);

            // Tracking
            changeX += Tracking(Axis.Axis1, seconds);
            changeY += Tracking(Axis.Axis2, seconds);

            // Hand controls
            changeX +=  HandControl(Axis.Axis1, seconds);
            changeY += HandControl(Axis.Axis2, seconds);

            // Slewing
            changeX += GoTo(Axis.Axis1, seconds);
            changeY += GoTo(Axis.Axis2, seconds);

            CheckStopped(changeX, changeY);
            CheckSlewing();
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
                    _rateX = 0;
                    _gotoX = double.NaN;
                    _rateAxisX = 0;
                    _slewX = 0;
                    _trackingX = 0;
                    _pulseX = 0;
                    HcX = 0;
                    break;
                case Axis.Axis2:
                    _rateY = 0;
                    _gotoY = double.NaN;
                    _rateAxisY = 0;
                    _slewY = 0;
                    _trackingY = 0;
                    _pulseY = 0;
                    HcY = 0;
                    break;
            }
        }
    } 
}
