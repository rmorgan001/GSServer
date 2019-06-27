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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using GS.Server.Domain;
using GS.Server.Helpers;
using GS.Server.Main;
using GS.Server.Settings;
using GS.Server.SkyTelescope;
using GS.Shared;
using MaterialDesignThemes.Wpf;

namespace GS.Server.Gamepad
{
    public class GamepadVM : ObservableObject, IPageVM
    {
        public string TopName => "";
        public string BottomName => "Gamepad";
        public int Uid => 2;

        private SkyTelescopeVM _skyTelescopeVM;
        private SettingsVM _settingsVM;
        private CancellationTokenSource ctsGamepad;
        private string _focusTextbox;
        private Gamepad _gamepad;

        public GamepadVM()
        {
            try
            {
                SkyTelescope();
                Settings();
                Delay = Properties.Gamepad.Default.Delay;
                if (Properties.Gamepad.Default.Startup) IsGamepadRunning = true;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
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
            get => _isGamepadRunning;
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
                Properties.Gamepad.Default.Startup = value;
                Properties.Gamepad.Default.Save();
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

        private int _delay;
        public int Delay
        {
            get => _delay;
            set
            {
                _delay = value;
                Properties.Gamepad.Default.Delay = value;
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
                        //ct.ThrowIfCancellationRequested();
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

                        //var sw = Stopwatch.StartNew();
                        //while (sw.Elapsed.TotalMilliseconds < Delay){ }
                        //sw.Stop();
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
                    Datetime = Principles.HiResDateTime.UtcNow,
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
                            if (_skyTelescopeVM.ClickHomeCommand.CanExecute(null))
                                _skyTelescopeVM.ClickHomeCommand.Execute(null);
                            returnId = id;
                            break;
                        }
                        else
                        {

                            break;
                        }
                    case "park":
                        if (value)
                        {
                            if (!SkyTelescope()) return;
                            if (_skyTelescopeVM.ClickParkCommand.CanExecute(null))
                                _skyTelescopeVM.ClickParkCommand.Execute(null);
                            returnId = id;
                            break;
                        }
                        else
                        {

                            break;
                        }
                    case "stop":
                        if (value)
                        {
                            if (!SkyTelescope()) return;
                            if (_skyTelescopeVM.ClickStopCommand.CanExecute(null))
                                _skyTelescopeVM.ClickStopCommand.Execute(null);
                            returnId = id;
                            break;
                        }
                        else
                        {

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
                            if (_skyTelescopeVM.HcSpeedupCommand.CanExecute(null))
                                _skyTelescopeVM.HcSpeedupCommand.Execute(null);
                            returnId = id;
                            break;
                        }
                        else
                        {

                            break;
                        }
                    case "speeddown":
                        if (value)
                        {
                            if (!SkyTelescope()) return;
                            if (_skyTelescopeVM.HcSpeeddownCommand.CanExecute(null))
                                _skyTelescopeVM.HcSpeeddownCommand.Execute(null);
                            returnId = id;
                            break;
                        }
                        else
                        {

                            break;
                        }
                    case "volumeup":
                        if (value)
                        {
                            if (!Settings()) return;
                            if (_settingsVM.VolumeupCommand.CanExecute(null))
                                _settingsVM.VolumeupCommand.Execute(null);
                            break;
                        }
                        else
                        {
                            returnId = id;
                            break;
                        }
                    case "volumedown":
                        if (value)
                        {
                            if (!Settings()) return;
                            if (_settingsVM.VolumedownCommand.CanExecute(null))
                                _settingsVM.VolumedownCommand.Execute(null);
                            break;
                        }
                        else
                        {
                            returnId = id;
                            break;
                        }
                    case "tracking":
                        if (value)
                        {
                            if (!SkyTelescope()) return;
                            if (_skyTelescopeVM.ClickTrackingCommand.CanExecute(null))
                                _skyTelescopeVM.ClickTrackingCommand.Execute(null);
                            returnId = id;
                            break;
                        }
                        else
                        {

                            break;
                        }
                    default:
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
                    }
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = Principles.HiResDateTime.UtcNow,
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
                    Datetime = Principles.HiResDateTime.UtcNow,
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
                    Datetime = Principles.HiResDateTime.UtcNow,
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
                    Datetime = Principles.HiResDateTime.UtcNow,
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
                    Datetime = Principles.HiResDateTime.UtcNow,
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

        #region Dialog Message

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
        private void OpenDialog(string msg)
        {
            if (msg != null) DialogMsg = msg;
            DialogContent = new Dialog();
            IsDialogOpen = true;

            var monitorItem = new MonitorEntry
            {
                Datetime = Principles.HiResDateTime.UtcNow,
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
    }
}
