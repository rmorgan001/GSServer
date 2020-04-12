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
using ASCOM.DeviceInterface;
using GS.Principles;
using GS.Server.Helpers;
using GS.Server.Main;
using GS.Server.Settings;
using GS.Server.SkyTelescope;
using GS.Shared;
using MaterialDesignThemes.Wpf;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GS.Server.Controls.Dialogs;

namespace GS.Server.Gamepad
{
    public sealed class GamepadVM : ObservableObject, IPageVM, IDisposable
    {
        public string TopName => "";
        public string BottomName => "Gamepad";
        public int Uid => 2;

        private SkyTelescopeVM _skyTelescopeVM;
        private SettingsVM _settingsVM;
        private CancellationTokenSource ctsGamepad;
        private string _focusTextbox;
        private Gamepad _gamepad;

        private int _trackingCount;
        private int _homeCount;
        private int _parkCount;
        private int _stopCount;
        private int _speedupCount;
        private int _speeddownCount;
        private int _volumeupCount;
        private int _volumedownCount;
        private int _ratesiderealCount;
        private int _ratelunarCount;
        private int _ratesolarCount;
        private int _ratekingCount;


        public GamepadVM()
        {
            try
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = " Loading GamepadVM" };
                MonitorLog.LogToMonitor(monitorItem);

                GamepadSettings.Load();
                SkyTelescope();
                Settings();
                IsGamepadRunning = GamepadSettings.Startup;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message);
            }


        }

        private bool _isGamepadRunning;
        public bool IsGamepadRunning
        {
            get => GamepadSettings.Startup;
            set
            {
                if (_isGamepadRunning == value) return;
                if (value)
                {
                    GamepadLoopAsync();

                }
                else
                {
                    EnableTextBoxes = false;
                    ctsGamepad?.Cancel();
                    ctsGamepad?.Dispose();
                    ctsGamepad = null;
                }
                _isGamepadRunning = value;
                GamepadSettings.Startup = _isGamepadRunning;
                OnPropertyChanged();
            }
        }

        private bool _enableTextBoxes;
        public bool EnableTextBoxes
        {
            get => _enableTextBoxes;
            set
            {
                if (_enableTextBoxes == value) return;
                _enableTextBoxes = value;
                OnPropertyChanged();
            }
        }

        private string _tracking;
        public string Tracking
        {
            get => _tracking;
            set
            {
                _tracking = value;
                OnPropertyChanged();
            }
        }

        private string _stop;
        public string Stop
        {
            get => _stop;
            set
            {
                _stop = value;
                OnPropertyChanged();
            }
        }

        private string _home;
        public string Home
        {
            get => _home;
            set
            {
                _home = value;
                OnPropertyChanged();
            }
        }

        private string _park;
        public string Park
        {
            get => _park;
            set
            {
                _park = value;
                OnPropertyChanged();
            }
        }

        private string _up;
        public string Up
        {
            get => _up;
            set
            {
                _up = value;
                OnPropertyChanged();
            }
        }

        private string _down;
        public string Down
        {
            get => _down;
            set
            {
                _down = value;
                OnPropertyChanged();
            }
        }

        private string _left;
        public string Left
        {
            get => _left;
            set
            {
                _left = value;
                OnPropertyChanged();
            }
        }

        private string _right;
        public string Right
        {
            get => _right;
            set
            {
                _right = value;
                OnPropertyChanged();
            }
        }

        private string _speedup;
        public string Speedup
        {
            get => _speedup;
            set
            {
                _speedup = value;
                OnPropertyChanged();
            }
        }

        private string _speeddown;
        public string Speeddown
        {
            get => _speeddown;
            set
            {
                _speeddown = value;
                OnPropertyChanged();
            }
        }

        private string _volumeup;
        public string Volumeup
        {
            get => _volumeup;
            set
            {
                _volumeup = value;
                OnPropertyChanged();
            }
        }

        private string _volumedown;
        public string Volumedown
        {
            get => _volumedown;
            set
            {
                _volumedown = value;
                OnPropertyChanged();
            }
        }

        private string _rateSidereal;
        public string RateSidereal
        {
            get => _rateSidereal;
            set
            {
                _rateSidereal = value;
                OnPropertyChanged();
            }
        }

        private string _rateLunar;
        public string RateLunar
        {
            get => _rateLunar;
            set
            {
                _rateLunar = value;
                OnPropertyChanged();
            }
        }

        private string _rateSolar;
        public string RateSolar
        {
            get => _rateSolar;
            set
            {
                _rateSolar = value;
                OnPropertyChanged();
            }
        }

        private string _rateKing;
        public string RateKing
        {
            get => _rateKing;
            set
            {
                _rateKing = value;
                OnPropertyChanged();
            }
        }

        private int _delay;
        public int Delay
        {
            get => GamepadSettings.Delay;
            set
            {
                _delay = value;
                GamepadSettings.Delay = _delay;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Sets up a reference back to run any SkyTelesope commands
        /// </summary>
        /// <returns></returns>
        private bool SkyTelescope()
        {
            if (_skyTelescopeVM == null) _skyTelescopeVM = SkyTelescopeVM._skyTelescopeVM;
            return _skyTelescopeVM != null;
        }

        /// <summary>
        /// Sets up a reference back to run any Settings commands
        /// </summary>
        /// <returns></returns>
        private bool Settings()
        {
            if (_settingsVM == null) _settingsVM = SettingsVM._settingsVM;
            return _settingsVM != null;
        }

        /// <summary>
        /// Async to find or poll gamepads
        /// </summary>
        private async void GamepadLoopAsync()
        {
            try
            {
                if (ctsGamepad == null) ctsGamepad = new CancellationTokenSource();
                var ct = ctsGamepad.Token;
                var task = Task.Run(() =>
                {
                    var windowHandle = new IntPtr();
                    ThreadContext.InvokeOnUiThread(delegate { windowHandle = Process.GetCurrentProcess().MainWindowHandle; }, ct);
                    _gamepad = new Gamepad(windowHandle);
                    LoadTextboxes();
                    ResetCounts();
                    EnableTextBoxes = true;
                    var buttontocheck = -1;
                    var povtocheck = new PovPair(-1, 0);
                    var xaxistocheck = new AxisPair(-1, string.Empty);
                    var yaxistocheck = new AxisPair(-1, string.Empty);
                    var zaxistocheck = new AxisPair(-1, string.Empty);
                    var KeepRunning = true;
                    while (KeepRunning)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            KeepRunning = false;
                            continue;
                        }

                        if (!_gamepad.IsAvailable)
                        {
                            Thread.Sleep(2000);
                            _gamepad.Find();
                            continue;
                        }

                        _gamepad.Poll();
                        var key = _focusTextbox;

                        // Check buttons
                        var gamepadButtons = _gamepad.Buttons;
                        if (gamepadButtons != null && gamepadButtons.Length > 0)
                        {
                            if (buttontocheck > -1)
                            {
                                if (string.IsNullOrEmpty(key))
                                {
                                    buttontocheck = DoGamepadCommand(buttontocheck, gamepadButtons[buttontocheck],
                                        _gamepad.Get_KeyByValue("button" + " " + buttontocheck));
                                }
                            }
                            else
                            {
                                for (var i = 0; i < gamepadButtons.Length; i++)
                                {
                                    if (!gamepadButtons[i]) continue;
                                    var cmd = "button" + " " + i;
                                    if (key != null)
                                    {
                                        DoGamepadSetKey(key, cmd);
                                        break;
                                    }
                                    buttontocheck = DoGamepadCommand(i, gamepadButtons[i],
                                        _gamepad.Get_KeyByValue(cmd));
                                    break;
                                }
                            }
                        }

                        // Check PoVs
                        var gamepadPovs = _gamepad.Povs;
                        if (gamepadPovs != null && gamepadPovs.Length > 0)
                        {
                            if (povtocheck.Key > -1)
                            {
                                if (string.IsNullOrEmpty(key))
                                {
                                    var pushed = povtocheck.Value == gamepadPovs[povtocheck.Key];
                                    var dir = Gamepad.PovDirection(povtocheck.Value);
                                    var cmd = _gamepad.Get_KeyByValue("pov" + " " + dir + " " + povtocheck.Key);
                                    var id = DoGamepadCommand(povtocheck.Key, pushed, cmd);
                                    if (id == -1) povtocheck = new PovPair(-1, 0);
                                }
                            }
                            else
                            {
                                for (var i = 0; i < gamepadPovs.Length; i++)
                                {
                                    if (gamepadPovs[i] == -1) continue;
                                    var newhit = new PovPair(i, gamepadPovs[i]);
                                    var dir = Gamepad.PovDirection(newhit.Value);
                                    var val = "pov" + " " + dir + " " + newhit.Key;
                                    var cmd = _gamepad.Get_KeyByValue(val);

                                    if (key != null)
                                    {
                                        DoGamepadSetKey(key, val);
                                        break;
                                    }

                                    var id = DoGamepadCommand(newhit.Key, true, cmd);
                                    povtocheck = id == -1 ? new PovPair(-1, 0) : newhit;
                                    break;
                                }
                            }
                        }

                        // Check X Axis
                        if (xaxistocheck.Key > -1)
                        {
                            var xDirection = Gamepad.AxisDirection(_gamepad.Xaxis);
                            var pushed = xaxistocheck.Value == xDirection;
                            if (!pushed)
                            {
                                if (string.IsNullOrEmpty(key))
                                {
                                    var cmd = _gamepad.Get_KeyByValue("xaxis" + " " + xaxistocheck.Value);
                                    var id = DoGamepadCommand(xaxistocheck.Key, false, cmd);
                                    if (id == -1) xaxistocheck = new AxisPair(-1, string.Empty);
                                }
                            }
                        }
                        else
                        {
                            var xDirection = Gamepad.AxisDirection(_gamepad.Xaxis);
                            if (xDirection != "normal")
                            {
                                var newaxis = new AxisPair(1, xDirection);
                                var val = "xaxis" + " " + xDirection;
                                var cmd = _gamepad.Get_KeyByValue(val);

                                if (key != null)
                                {
                                    DoGamepadSetKey(key, val);
                                }
                                else
                                {
                                    var id = DoGamepadCommand(1, true, cmd);
                                    xaxistocheck = id == -1 ? new AxisPair(-1, string.Empty) : newaxis;
                                }
                            }
                        }

                        // Check Y Axis
                        if (yaxistocheck.Key > -1)
                        {
                            var yDirection = Gamepad.AxisDirection(_gamepad.Yaxis);
                            var pushed = yaxistocheck.Value == yDirection;
                            if (!pushed)
                            {
                                if (string.IsNullOrEmpty(key))
                                {
                                    var cmd = _gamepad.Get_KeyByValue("yaxis" + " " + yaxistocheck.Value);
                                    var id = DoGamepadCommand(yaxistocheck.Key, false, cmd);
                                    if (id == -1) yaxistocheck = new AxisPair(-1, string.Empty);
                                }
                            }
                        }
                        else
                        {
                            var yDirection = Gamepad.AxisDirection(_gamepad.Yaxis);
                            if (yDirection != "normal")
                            {
                                var newaxis = new AxisPair(1, yDirection);
                                var val = "yaxis" + " " + yDirection;
                                var cmd = _gamepad.Get_KeyByValue(val);

                                if (key != null)
                                {
                                    DoGamepadSetKey(key, val);
                                }
                                else
                                {
                                    var id = DoGamepadCommand(1, true, cmd);
                                    yaxistocheck = id == -1 ? new AxisPair(-1, string.Empty) : newaxis;
                                }
                            }
                        }

                        // Check Z Axis
                        if (zaxistocheck.Key > -1)
                        {
                            var zDirection = Gamepad.AxisDirection(_gamepad.Zaxis);
                            var pushed = zaxistocheck.Value == zDirection;
                            if (!pushed)
                            {
                                if (string.IsNullOrEmpty(key))
                                {
                                    var cmd = _gamepad.Get_KeyByValue("zaxis" + " " + zaxistocheck.Value);
                                    var id = DoGamepadCommand(zaxistocheck.Key, false, cmd);
                                    if (id == -1) zaxistocheck = new AxisPair(-1, string.Empty);
                                }
                            }
                        }
                        else
                        {
                            var zDirection = Gamepad.AxisDirection(_gamepad.Zaxis);
                            if (zDirection != "normal")
                            {
                                var newaxis = new AxisPair(1, zDirection);
                                var val = "zaxis" + " " + zDirection;
                                var cmd = _gamepad.Get_KeyByValue(val);

                                if (key != null)
                                {
                                    DoGamepadSetKey(key, val);
                                }
                                else
                                {
                                    var id = DoGamepadCommand(1, true, cmd);
                                    zaxistocheck = id == -1 ? new AxisPair(-1, string.Empty) : newaxis;
                                }
                            }
                        }

                        Thread.Sleep(Delay);
                    }
                }, ct);
                await task;
                task.Wait(ct);
                IsGamepadRunning = false;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                IsGamepadRunning = false;
                OpenDialog(ex.Message);

            }
        }

        /// <summary>
        /// Execute relay commands from a gamepad
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        private int DoGamepadCommand(int id, bool value, string command)
        {
            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{id},{value},{command}" };
            MonitorLog.LogToMonitor(monitorItem);

            var returnId = -1;
            if (!SkyServer.IsMountRunning) return returnId;
            if (command == null) return returnId;
            if (ctsGamepad.IsCancellationRequested) return returnId;
            ThreadContext.InvokeOnUiThread(delegate
            {
                switch (command)
                {
                    case "home":
                        if (value)
                        {
                            if (!SkyTelescope()) return;
                            if (_homeCount == 0)
                            {
                                if (_skyTelescopeVM.ClickHomeCommand.CanExecute(null))
                                    _skyTelescopeVM.ClickHomeCommand.Execute(null);
                            }
                            _homeCount++;
                            returnId = id;
                            break;
                        }
                        else
                        {
                            ResetCounts();
                            break;
                        }
                    case "park":
                        if (value)
                        {
                            if (!SkyTelescope()) return;
                            if (_parkCount == 0)
                            {
                                if (_skyTelescopeVM.ClickParkCommand.CanExecute(null))
                                    _skyTelescopeVM.ClickParkCommand.Execute(null);
                            }
                            _parkCount++;
                            returnId = id;
                            break;
                        }
                        else
                        {
                            ResetCounts();
                            break;
                        }
                    case "stop":
                        if (value)
                        {
                            if (!SkyTelescope()) return;
                            if (_stopCount == 0)
                            {
                                if (_skyTelescopeVM.ClickStopCommand.CanExecute(null))
                                    _skyTelescopeVM.ClickStopCommand.Execute(null);
                            }
                            _stopCount++;
                            returnId = id;
                            break;
                        }
                        else
                        {
                            ResetCounts();
                            break;
                        }
                    case "up":
                        if (value)
                        {
                            if (!SkyTelescope()) return;
                            if (_skyTelescopeVM.HcMouseDownUpCommand.CanExecute(null))
                                _skyTelescopeVM.HcMouseDownUpCommand.Execute(null);
                            returnId = id;
                            break;
                        }
                        else
                        {
                            if (!SkyTelescope()) return;
                            if (_skyTelescopeVM.HcMouseUpUpCommand.CanExecute(null))
                                _skyTelescopeVM.HcMouseUpUpCommand.Execute(null);
                            break;
                        }
                    case "down":
                        if (value)
                        {
                            if (!SkyTelescope()) return;
                            if (_skyTelescopeVM.HcMouseDownDownCommand.CanExecute(null))
                                _skyTelescopeVM.HcMouseDownDownCommand.Execute(null);
                            returnId = id;
                            break;
                        }
                        else
                        {
                            if (!SkyTelescope()) return;
                            if (_skyTelescopeVM.HcMouseUpDownCommand.CanExecute(null))
                                _skyTelescopeVM.HcMouseUpDownCommand.Execute(null);
                            break;
                        }
                    case "left":
                        if (value)
                        {
                            if (!SkyTelescope()) return;
                            if (_skyTelescopeVM.HcMouseDownLeftCommand.CanExecute(null))
                                _skyTelescopeVM.HcMouseDownLeftCommand.Execute(null);
                            returnId = id;
                            break;
                        }
                        else
                        {
                            if (!SkyTelescope()) return;
                            if (_skyTelescopeVM.HcMouseUpLeftCommand.CanExecute(null))
                                _skyTelescopeVM.HcMouseUpLeftCommand.Execute(null);
                            break;
                        }
                    case "right":
                        if (value)
                        {
                            if (!SkyTelescope()) return;
                            if (_skyTelescopeVM.HcMouseDownRightCommand.CanExecute(null))
                                _skyTelescopeVM.HcMouseDownRightCommand.Execute(null);
                            returnId = id;
                            break;
                        }
                        else
                        {
                            if (!SkyTelescope()) return;
                            if (_skyTelescopeVM.HcMouseUpRightCommand.CanExecute(null))
                                _skyTelescopeVM.HcMouseUpRightCommand.Execute(null);
                            break;
                        }
                    case "speedup":
                        if (value)
                        {
                            if (!SkyTelescope()) return;
                            if (_speedupCount == 0)
                            {
                                if (_skyTelescopeVM.HcSpeedupCommand.CanExecute(null))
                                    _skyTelescopeVM.HcSpeedupCommand.Execute(null);
                            }
                            _speedupCount++;
                            returnId = id;
                            break;
                        }
                        else
                        {
                            ResetCounts();
                            break;
                        }
                    case "speeddown":
                        if (value)
                        {
                            if (!SkyTelescope()) return;
                            if (_speeddownCount == 0)
                            {
                                if (_skyTelescopeVM.HcSpeeddownCommand.CanExecute(null))
                                    _skyTelescopeVM.HcSpeeddownCommand.Execute(null);
                            }
                            _speeddownCount++;
                            returnId = id;
                            break;
                        }
                        else
                        {
                            ResetCounts();
                            break;
                        }
                    case "volumeup":
                        if (value)
                        {
                            if (!Settings()) return;
                            if (_volumeupCount == 0)
                            {
                                if (_settingsVM.VolumeupCommand.CanExecute(null))
                                    _settingsVM.VolumeupCommand.Execute(null);
                            }
                            _volumeupCount++;
                            returnId = id;
                            break;
                        }
                        else
                        {
                            ResetCounts();
                            // returnId = id;
                            break;
                        }
                    case "volumedown":
                        if (value)
                        {
                            if (!Settings()) return;
                            if (_volumedownCount == 0)
                            {
                                if (_settingsVM.VolumedownCommand.CanExecute(null))
                                    _settingsVM.VolumedownCommand.Execute(null);
                            }
                            _volumedownCount++;
                            returnId = id;
                            break;
                        }
                        else
                        {
                            ResetCounts();
                            //returnId = id;
                            break;
                        }
                    case "tracking":
                        if (value)
                        {
                            if (!SkyTelescope()) return;
                            if (_trackingCount == 0)
                            {
                                if (_skyTelescopeVM.ClickTrackingCommand.CanExecute(null))
                                    _skyTelescopeVM.ClickTrackingCommand.Execute(null);
                            }
                            _trackingCount++;
                            returnId = id;
                            break;
                        }
                        else
                        {
                            ResetCounts();
                            break;
                        }
                    case "ratesidereal":
                        if (value)
                        {
                            if (_ratesiderealCount == 0)
                            {
                                SkySettings.TrackingRate = DriveRates.driveSidereal;
                                if (SkyServer.Tracking)
                                {
                                    SkyServer.TrackingSpeak = false;
                                    SkyServer.Tracking = false;
                                    SkyServer.Tracking = true;
                                    SkyServer.TrackingSpeak = true;
                                }
                                Synthesizer.Speak(Application.Current.Resources["vceSidereal"].ToString());
                            }
                            _ratesiderealCount++;
                            returnId = id;
                            break;
                        }
                        else
                        {
                            ResetCounts();
                            break;
                        }
                    case "ratelunar":
                        if (value)
                        {
                            if (_ratelunarCount == 0)
                            {
                                SkySettings.TrackingRate = DriveRates.driveLunar;
                                if (SkyServer.Tracking)
                                {
                                    SkyServer.TrackingSpeak = false;
                                    SkyServer.Tracking = false;
                                    SkyServer.Tracking = true;
                                    SkyServer.TrackingSpeak = true;
                                }

                                Synthesizer.Speak(Application.Current.Resources["vceLunar"].ToString());
                            }
                            _ratelunarCount++;
                            returnId = id;
                            break;
                        }
                        else
                        {
                            ResetCounts();
                            break;
                        }
                    case "ratesolar":
                        if (value)
                        {
                            if (_ratesolarCount == 0)
                            {
                                SkySettings.TrackingRate = DriveRates.driveSolar;
                                if (SkyServer.Tracking)
                                {
                                    SkyServer.TrackingSpeak = false;
                                    SkyServer.Tracking = false;
                                    SkyServer.Tracking = true;
                                    SkyServer.TrackingSpeak = true;
                                }

                                Synthesizer.Speak(Application.Current.Resources["vceSolar"].ToString());
                            }
                            _ratesolarCount++;
                            returnId = id;
                            break;
                        }
                        else
                        {
                            ResetCounts();
                            break;
                        }
                    case "rateking":
                        if (value)
                        {
                            if (_ratekingCount == 0)
                            {
                                SkySettings.TrackingRate = DriveRates.driveKing;
                                if (SkyServer.Tracking)
                                {
                                    SkyServer.TrackingSpeak = false;
                                    SkyServer.Tracking = false;
                                    SkyServer.Tracking = true;
                                    SkyServer.TrackingSpeak = true;
                                }

                                Synthesizer.Speak(Application.Current.Resources["vceKing"].ToString());
                            }
                            _ratekingCount++;
                            returnId = id;
                            break;
                        }
                        else
                        {
                            ResetCounts();
                            break;
                        }
                    default:
                        ResetCounts();
                        returnId = -1;
                        break;
                }
            }, ctsGamepad.Token);
            return returnId;
        }

        /// <summary>
        /// Stores a key value pair to dicstionay then saves to settings file
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        private void DoGamepadSetKey(string key, string val)
        {
            if (!(val != null || key != null)) return;

            switch (key)
            {
                case "tracking":
                    Tracking = val;
                    break;
                case "stop":
                    Stop = val;
                    break;
                case "park":
                    Park = val;
                    break;
                case "home":
                    Home = val;
                    break;
                case "speeddown":
                    Speeddown = val;
                    break;
                case "speedup":
                    Speedup = val;
                    break;
                case "up":
                    Up = val;
                    break;
                case "down":
                    Down = val;
                    break;
                case "left":
                    Left = val;
                    break;
                case "right":
                    Right = val;
                    break;
                case "volumedown":
                    Volumedown = val;
                    break;
                case "volumeup":
                    Volumeup = val;
                    break;
                case "ratesidereal":
                    RateSidereal = val;
                    break;
                case "ratelunar":
                    RateLunar = val;
                    break;
                case "ratesolar":
                    RateSolar = val;
                    break;
                case "rateking":
                    RateKing = val;
                    break;
            }

            _gamepad?.Update_Setting(key, val);
            _gamepad?.SaveSettings();
            LoadTextboxes();
        }

        /// <summary>
        /// Load gamepad button settings
        /// </summary>
        private void LoadTextboxes()
        {
            try
            {
                var dict = _gamepad?.GetSettings();
                if (dict == null) return;
                foreach (var setting in dict)
                {
                    var val = setting.Value;
                    var key = setting.Key;
                    switch (key)
                    {
                        case "tracking":
                            Tracking = val;
                            break;
                        case "stop":
                            Stop = val;
                            break;
                        case "park":
                            Park = val;
                            break;
                        case "home":
                            Home = val;
                            break;
                        case "speeddown":
                            Speeddown = val;
                            break;
                        case "speedup":
                            Speedup = val;
                            break;
                        case "up":
                            Up = val;
                            break;
                        case "down":
                            Down = val;
                            break;
                        case "left":
                            Left = val;
                            break;
                        case "right":
                            Right = val;
                            break;
                        case "volumedown":
                            Volumedown = val;
                            break;
                        case "volumeup":
                            Volumeup = val;
                            break;
                        case "ratesidereal":
                            RateSidereal = val;
                            break;
                        case "ratelunar":
                            RateLunar = val;
                            break;
                        case "ratesolar":
                            RateSolar = val;
                            break;
                        case "rateking":
                            RateKing = val;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message);
            }
        }

        private void ResetCounts()
        {
            _trackingCount = 0;
            _homeCount = 0;
            _parkCount = 0;
            _stopCount = 0;
            _speedupCount = 0;
            _speeddownCount = 0;
            _volumeupCount = 0;
            _volumedownCount = 0;
            _ratesiderealCount = 0;
            _ratelunarCount = 0;
            _ratesolarCount = 0;
            _ratekingCount = 0;
        }

        private ICommand _clickTextboxGotFocusCommand;
        public ICommand ClickTextboxGotFocusCommand
        {
            get
            {
                return _clickTextboxGotFocusCommand ?? (_clickTextboxGotFocusCommand = new RelayCommand(
                           param => ClickTextboxGotFocus(param)
                       ));
            }
        }
        private void ClickTextboxGotFocus(object parameter)
        {
            try
            {
                _focusTextbox = parameter.ToString().Trim().ToLower();
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickPreviewMouseDoubleClickCommand;
        public ICommand ClickPreviewMouseDoubleClickCommand
        {
            get
            {
                return _clickPreviewMouseDoubleClickCommand ?? (_clickPreviewMouseDoubleClickCommand = new RelayCommand(
                           param => ClickPreviewMouseDoubleClick(param)
                       ));
            }
        }
        private void ClickPreviewMouseDoubleClick(object parameter)
        {
            try
            {
                if (_gamepad == null) return;
                var key = parameter.ToString().Trim().ToLower();
                var update = true;
                switch (key)
                {
                    case "tracking":
                        Tracking = null;
                        break;
                    case "stop":
                        Stop = null;
                        break;
                    case "park":
                        Park = null;
                        break;
                    case "home":
                        Home = null;
                        break;
                    case "speeddown":
                        Speeddown = null;
                        break;
                    case "speedup":
                        Speedup = null;
                        break;
                    case "up":
                        Up = null;
                        break;
                    case "down":
                        Down = null;
                        break;
                    case "left":
                        Left = null;
                        break;
                    case "right":
                        Right = null;
                        break;
                    case "volumedown":
                        Volumedown = null;
                        break;
                    case "volumeup":
                        Volumeup = null;
                        break;
                    default:
                        update = false;
                        break;
                }

                if (!update) return;
                _gamepad?.Update_Setting(key, null);
                _gamepad?.SaveSettings();
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickTextboxLostFocusCommand;
        public ICommand ClickTextboxLostFocusCommand
        {
            get
            {
                return _clickTextboxLostFocusCommand ?? (_clickTextboxLostFocusCommand = new RelayCommand(
                           param => ClickTextboxLostFocus()
                       ));
            }
        }
        private void ClickTextboxLostFocus()
        {
            try
            {
                _focusTextbox = null;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickSaveCommand;
        public ICommand ClickSaveCommand
        {
            get
            {
                return _clickSaveCommand ?? (_clickSaveCommand = new RelayCommand(
                           param => ClickSave()
                       ));
            }
        }
        private void ClickSave()
        {
            try
            {
                _gamepad?.SaveSettings();
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message);
            }
        }

        #region Dialog

        private string _dialogMsg;
        public string DialogMsg
        {
            get => _dialogMsg;
            set
            {
                if (_dialogMsg == value) return;
                _dialogMsg = value;
                OnPropertyChanged();
            }
        }

        private bool _isDialogOpen;
        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set
            {
                if (_isDialogOpen == value) return;
                _isDialogOpen = value;
                OnPropertyChanged();
            }
        }

        private string _dialogCaption;
        public string DialogCaption
        {
            get => _dialogCaption;
            set
            {
                if (_dialogCaption == value) return;
                _dialogCaption = value;
                OnPropertyChanged();
            }
        }

        private object _dialogContent;
        public object DialogContent
        {
            get => _dialogContent;
            set
            {
                if (_dialogContent == value) return;
                _dialogContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openDialogCommand;
        public ICommand OpenDialogCommand
        {
            get
            {
                return _openDialogCommand ?? (_openDialogCommand = new RelayCommand(
                           param => OpenDialog(null)
                       ));
            }
        }
        private void OpenDialog(string msg, string caption = null)
        {
            if (msg != null) DialogMsg = msg;
            DialogCaption = caption ?? Application.Current.Resources["msgDialog"].ToString();
            DialogContent = new DialogOK();
            IsDialogOpen = true;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Interface,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{msg}"
            };
            MonitorLog.LogToMonitor(monitorItem);

        }

        private ICommand _clickOkDialogCommand;
        public ICommand ClickOkDialogCommand
        {
            get
            {
                return _clickOkDialogCommand ?? (_clickOkDialogCommand = new RelayCommand(
                           param => ClickOkDialog()
                       ));
            }
        }
        private void ClickOkDialog()
        {
            IsDialogOpen = false;
        }

        private ICommand _clickCancelDialogCommand;
        public ICommand ClickCancelDialogCommand
        {
            get
            {
                return _clickCancelDialogCommand ?? (_clickCancelDialogCommand = new RelayCommand(
                           param => ClickCancelDialog()
                       ));
            }
        }
        private void ClickCancelDialog()
        {
            IsDialogOpen = false;
        }

        private ICommand _runMessageDialog;
        public ICommand RunMessageDialogCommand
        {
            get
            {
                return _runMessageDialog ?? (_runMessageDialog = new RelayCommand(
                           param => ExecuteMessageDialog()
                       ));
            }
        }
        private async void ExecuteMessageDialog()
        {
            //let's set up a little MVVM, cos that's what the cool kids are doing:
            var view = new ErrorMessageDialog
            {
                DataContext = new ErrorMessageDialogVM()
            };

            //show the dialog
            await DialogHost.Show(view, "RootDialog", ClosingMessageEventHandler);
        }
        private void ClosingMessageEventHandler(object sender, DialogClosingEventArgs eventArgs)
        {
            Console.WriteLine(@"You can intercept the closing event, and cancel here.");
        }

        #endregion

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            // GC.SuppressFinalize(this);
        }
        // NOTE: Leave out the finalizer altogether if this class doesn't
        // own unmanaged resources itself, but leave the other methods
        // exactly as they are.
        ~GamepadVM()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        // The bulk of the clean-up code is implemented in Dispose(bool)
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _skyTelescopeVM?.Dispose();
                ctsGamepad?.Dispose();
                _settingsVM?.Dispose();
                _gamepad?.Dispose();
            }
            // free native resources if there are any.
            //if (nativeResource != IntPtr.Zero)
            //{
            //    Marshal.FreeHGlobal(nativeResource);
            //    nativeResource = IntPtr.Zero;
            //}
        }
        #endregion

    }
}
