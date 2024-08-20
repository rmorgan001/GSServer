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
using GS.Server.Windows;
using GS.Shared.Command;
using GS.Server.Focuser;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace GS.Server.GamePad
{
    public sealed class GamePadVM : ObservableObject, IPageVM, IDisposable
    {
        public string TopName => "";
        public string BottomName => "GamePad";
        public int Uid => 3;

        private SkyTelescopeVM _skyTelescopeVM;
        private FocuserVM _focuserVM;
        private SettingsVM _settingsVM;
        private CancellationTokenSource ctsGamePad;
        private string _focusTextBox;
        private IGamePad _gamePad;

        private int _trackingCount;
        private int _homeCount;
        private int _parkCount;
        private int _stopCount;
        private int _speedupCount;
        private int _speedDownCount;
        private int _volumeUpCount;
        private int _volumeDownCount;
        private int _rateSiderealCount;
        private int _rateLunarCount;
        private int _rateSolarCount;
        private int _rateKingCount;
        private int _abortCount;
        private int _syncCount;
        private readonly Stopwatch _syncClickTimer = new Stopwatch();
        private int _spiralInCount;
        private int _spiralOutCount;
        private int _newSpiralCount;
        private int _focusInCount;
        private int _focusOutCount;

        private const float _vibrateLeft = 0.0f;
        private float _vibrateRight;
        private readonly object _vibrateLock = new object();

        public GamePadVM()
        {
            try
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = "Loading GamePadVM"
                };
                MonitorLog.LogToMonitor(monitorItem);

                GamePadSettings.Load();
                SkyTelescope();
                Settings();
                IsGamePadRunning = GamePadSettings.Startup;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message);
            }


        }

        private bool _isGamePadRunning;

        public bool IsGamePadRunning
        {
            get => GamePadSettings.Startup;
            set
            {
                if (_isGamePadRunning == value) return;
                if (value)
                {
                    GamePadLoopAsync();

                }
                else
                {
                    EnableTextBoxes = false;
                    ctsGamePad?.Cancel();
                    ctsGamePad?.Dispose();
                    ctsGamePad = null;
                }

                _isGamePadRunning = value;
                GamePadSettings.Startup = _isGamePadRunning;
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

        private string _speedUp;

        public string SpeedUp
        {
            get => _speedUp;
            set
            {
                _speedUp = value;
                OnPropertyChanged();
            }
        }

        private string _speedDown;

        public string SpeedDown
        {
            get => _speedDown;
            set
            {
                _speedDown = value;
                OnPropertyChanged();
            }
        }

        private string _volumeUp;

        public string VolumeUp
        {
            get => _volumeUp;
            set
            {
                _volumeUp = value;
                OnPropertyChanged();
            }
        }

        private string _volumeDown;

        public string VolumeDown
        {
            get => _volumeDown;
            set
            {
                _volumeDown = value;
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

        private string _abort;

        public string Abort
        {
            get => _abort;
            set
            {
                _abort = value;
                OnPropertyChanged();
            }
        }

        private string _spiralIn;

        public string SpiralIn
        {
            get => _spiralIn;
            set
            {
                _spiralIn = value;
                OnPropertyChanged();
            }
        }

        private string _spiralOut;

        public string SpiralOut
        {
            get => _spiralOut;
            set
            {
                _spiralOut = value;
                OnPropertyChanged();
            }
        }

        private string _newSpiral;

        public string NewSpiral
        {
            get => _newSpiral;
            set
            {
                _newSpiral = value;
                OnPropertyChanged();
            }
        }

        private string _sync;

        public string Sync
        {
            get => _sync;
            set
            {
                _sync = value;
                OnPropertyChanged();
            }
        }

        private string _focusIn;

        public string FocusIn
        {
            get => _focusIn;
            set
            {
                _focusIn = value;
                OnPropertyChanged();
            }
        }

        private string _focusOut;

        public string FocusOut
        {
            get => _focusOut;
            set
            {
                _focusOut = value;
                OnPropertyChanged();
            }
        }

        private int _delay;

        public int Delay
        {
            get => GamePadSettings.Delay;
            set
            {
                _delay = value;
                GamePadSettings.Delay = _delay;
                OnPropertyChanged();
            }
        }

        private int _doubleClickSpeed;

        public int DoubleClickSpeed
        {
            get => GamePadSettings.DoubleClickSpeed;
            set
            {
                _doubleClickSpeed = value;
                GamePadSettings.DoubleClickSpeed = _doubleClickSpeed;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Sets up a reference back to run any SkyTelescope commands
        /// </summary>
        /// <returns></returns>
        private bool SkyTelescope()
        {
            if (_skyTelescopeVM == null) _skyTelescopeVM = SkyTelescopeVM._skyTelescopeVM;
            return _skyTelescopeVM != null;
        }

        /// <summary>
        /// Sets up a reference back to run any Focuser commands
        /// </summary>
        /// <returns></returns>
        private bool Focuser()
        {
            if (_focuserVM == null) _focuserVM = FocuserVM._focuserVM;
            return _focuserVM != null;
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
        private async void GamePadLoopAsync()
        {
            float vibrateL;
            float vibrateR;
            try
            {
                if (ctsGamePad == null) ctsGamePad = new CancellationTokenSource();
                var ct = ctsGamePad.Token;
                var task = Task.Run(() =>
                {

                    _gamePad = new GamePadXInput();    // Try for XInput
                    if (!_gamePad.IsAvailable)
                    {
                        var windowHandle = new IntPtr();
                        ThreadContext.InvokeOnUiThread(
                            delegate { windowHandle = Process.GetCurrentProcess().MainWindowHandle; }, ct);
                        _gamePad = new GamePadDirectX(windowHandle);

                        var monitorItem = new MonitorEntry
                        {
                            Datetime = HiResDateTime.UtcNow,
                            Device = MonitorDevice.UI,
                            Category = MonitorCategory.Server,
                            Type = MonitorType.Error,
                            Method = MethodBase.GetCurrentMethod()?.Name,
                            Thread = Thread.CurrentThread.ManagedThreadId,
                            Message = $"GamePad Handle Available|{_gamePad.IsAvailable}"
                        };
                        MonitorLog.LogToMonitor(monitorItem);
                    }
                    LoadTextboxes();
                    ResetCounts();
                    EnableTextBoxes = true;
                    var buttontocheck = -1;
                    var povtocheck = new PovPair(-1, 0);
                    var xaxistocheck = new AxisPair(-1, String.Empty);
                    var yaxistocheck = new AxisPair(-1, String.Empty);
                    var zaxistocheck = new AxisPair(-1, String.Empty);
                    var xrotationtocheck = new AxisPair(-1, String.Empty);
                    var yrotationtocheck = new AxisPair(-1, String.Empty);
                    var zrotationtocheck = new AxisPair(-1, String.Empty);
                    var KeepRunning = true;
                    while (KeepRunning)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            KeepRunning = false;
                            continue;
                        }

                        if (!_gamePad.IsAvailable)
                        {
                            Thread.Sleep(2000);
                            _gamePad.Find();
                            continue;
                        }

                        lock (_vibrateLock)
                        {
                            vibrateL = _vibrateLeft;
                            vibrateR = _vibrateRight;
                        }
                        _gamePad.Poll(vibrateL, vibrateR);
                        var key = _focusTextBox;

                        // Check buttons
                        var gamepadButtons = _gamePad.Buttons;
                        if (gamepadButtons != null && gamepadButtons.Length > 0)
                        {
                            if (buttontocheck > -1)
                            {
                                if (String.IsNullOrEmpty(key))
                                {
                                    buttontocheck = DoGamePadCommand(buttontocheck, gamepadButtons[buttontocheck],
                                        _gamePad.Get_KeyByValue("button" + " " + buttontocheck));
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
                                        DoGamePadSetKey(key, cmd);
                                        break;
                                    }

                                    buttontocheck = DoGamePadCommand(i, gamepadButtons[i],
                                        _gamePad.Get_KeyByValue(cmd));
                                    break;
                                }
                            }
                        }

                        // Check PoVs
                        var gamepadPovs = _gamePad.POVs;
                        if (gamepadPovs != null && gamepadPovs.Length > 0)
                        {
                            if (povtocheck.Key > -1)
                            {
                                if (String.IsNullOrEmpty(key))
                                {
                                    var pushed = povtocheck.Value == gamepadPovs[povtocheck.Key];
                                    var dir = PovDirection(povtocheck.Value);
                                    var cmd = _gamePad.Get_KeyByValue("pov" + " " + dir + " " + povtocheck.Key);
                                    var id = DoGamePadCommand(povtocheck.Key, pushed, cmd);
                                    if (id == -1) povtocheck = new PovPair(-1, 0);
                                }
                            }
                            else
                            {
                                for (var i = 0; i < gamepadPovs.Length; i++)
                                {
                                    if (gamepadPovs[i] == -1) continue;
                                    var newhit = new PovPair(i, gamepadPovs[i]);
                                    var dir = PovDirection(newhit.Value);
                                    var val = "pov" + " " + dir + " " + newhit.Key;
                                    var cmd = _gamePad.Get_KeyByValue(val);

                                    if (key != null)
                                    {
                                        DoGamePadSetKey(key, val);
                                        break;
                                    }

                                    var id = DoGamePadCommand(newhit.Key, true, cmd);
                                    povtocheck = id == -1 ? new PovPair(-1, 0) : newhit;
                                    break;
                                }
                            }
                        }
                        // Check X Axis
                        if (_gamePad.XAxis.HasValue)
                        {
                            if (xaxistocheck.Key > -1)
                            {
                                var xDirection = AxisDirection(_gamePad.XAxis.Value);
                                var pushed = xaxistocheck.Value == xDirection;
                                if (String.IsNullOrEmpty(key))
                                {
                                    var cmd = _gamePad.Get_KeyByValue("xaxis" + " " + xaxistocheck.Value);
                                    var id = DoGamePadCommand(xaxistocheck.Key, pushed, cmd);
                                    if (id == -1) xaxistocheck = new AxisPair(-1, String.Empty);
                                }
                            }
                            else
                            {
                                var xDirection = AxisDirection(_gamePad.XAxis.Value);
                                if (xDirection != "normal")
                                {
                                    var newaxis = new AxisPair(1, xDirection);
                                    var val = "xaxis" + " " + xDirection;
                                    var cmd = _gamePad.Get_KeyByValue(val);

                                    if (key != null)
                                    {
                                        DoGamePadSetKey(key, val);
                                    }
                                    else
                                    {
                                        var id = DoGamePadCommand(1, true, cmd);
                                        xaxistocheck = id == -1 ? new AxisPair(-1, String.Empty) : newaxis;
                                    }
                                }
                            }
                        }
                        // Check Y Axis
                        if (_gamePad.YAxis.HasValue)
                        {
                            if (yaxistocheck.Key > -1)
                            {
                                var yDirection = AxisDirection(_gamePad.YAxis.Value);
                                var pushed = yaxistocheck.Value == yDirection;
                                if (String.IsNullOrEmpty(key))
                                {
                                    var cmd = _gamePad.Get_KeyByValue("yaxis" + " " + yaxistocheck.Value);
                                    var id = DoGamePadCommand(yaxistocheck.Key, pushed, cmd);
                                    if (id == -1) yaxistocheck = new AxisPair(-1, String.Empty);
                                }
                            }
                            else
                            {
                                var yDirection = AxisDirection(_gamePad.YAxis.Value);
                                if (yDirection != "normal")
                                {
                                    var newaxis = new AxisPair(1, yDirection);
                                    var val = "yaxis" + " " + yDirection;
                                    var cmd = _gamePad.Get_KeyByValue(val);

                                    if (key != null)
                                    {
                                        DoGamePadSetKey(key, val);
                                    }
                                    else
                                    {
                                        var id = DoGamePadCommand(1, true, cmd);
                                        yaxistocheck = id == -1 ? new AxisPair(-1, String.Empty) : newaxis;
                                    }
                                }
                            }
                        }

                        // Check Z Axis
                        if (_gamePad.ZAxis.HasValue)
                        {
                            if (zaxistocheck.Key > -1)
                            {
                                var zDirection = AxisDirection(_gamePad.ZAxis.Value);
                                var pushed = zaxistocheck.Value == zDirection;
                                if (String.IsNullOrEmpty(key))
                                {
                                    var cmd = _gamePad.Get_KeyByValue("zaxis" + " " + zaxistocheck.Value);
                                    var id = DoGamePadCommand(zaxistocheck.Key, pushed, cmd);
                                    if (id == -1) zaxistocheck = new AxisPair(-1, String.Empty);
                                }
                            }
                            else
                            {
                                var zDirection = AxisDirection(_gamePad.ZAxis.Value);
                                if (zDirection != "normal")
                                {
                                    var newaxis = new AxisPair(1, zDirection);
                                    var val = "zaxis" + " " + zDirection;
                                    var cmd = _gamePad.Get_KeyByValue(val);

                                    if (key != null)
                                    {
                                        DoGamePadSetKey(key, val);
                                    }
                                    else
                                    {
                                        var id = DoGamePadCommand(1, true, cmd);
                                        zaxistocheck = id == -1 ? new AxisPair(-1, string.Empty) : newaxis;
                                    }
                                }
                            }
                        }

                        // Check X Rotation
                        if (_gamePad.XRotation.HasValue)
                        {
                            if (xrotationtocheck.Key > -1)
                            {
                                var xDirection = AxisDirection(_gamePad.XRotation.Value);
                                var pushed = xrotationtocheck.Value == xDirection;
                                if (!pushed)
                                {
                                    if (String.IsNullOrEmpty(key))
                                    {
                                        var cmd = _gamePad.Get_KeyByValue("xrotation" + " " + xrotationtocheck.Value);
                                        var id = DoGamePadCommand(xrotationtocheck.Key, pushed, cmd);
                                        if (id == -1) xrotationtocheck = new AxisPair(-1, String.Empty);
                                    }
                                }
                            }
                            else
                            {
                                var xDirection = AxisDirection(_gamePad.XRotation.Value);
                                if (xDirection != "normal")
                                {
                                    var newaxis = new AxisPair(1, xDirection);
                                    var val = "xrotation" + " " + xDirection;
                                    var cmd = _gamePad.Get_KeyByValue(val);

                                    if (key != null)
                                    {
                                        DoGamePadSetKey(key, val);
                                    }
                                    else
                                    {
                                        var id = DoGamePadCommand(1, true, cmd);
                                        xrotationtocheck = id == -1 ? new AxisPair(-1, string.Empty) : newaxis;
                                    }
                                }
                            }
                        }
                        // Check Y Rotation
                        if (_gamePad.YRotation.HasValue)
                        {
                            if (yrotationtocheck.Key > -1)
                            {
                                var yDirection = AxisDirection(_gamePad.YRotation.Value);
                                var pushed = yrotationtocheck.Value == yDirection;
                                if (String.IsNullOrEmpty(key))
                                {
                                    var cmd = _gamePad.Get_KeyByValue("yrotation" + " " + yrotationtocheck.Value);
                                    var id = DoGamePadCommand(yrotationtocheck.Key, pushed, cmd);
                                    if (id == -1) yrotationtocheck = new AxisPair(-1, String.Empty);
                                }
                            }
                            else
                            {
                                var yDirection = AxisDirection(_gamePad.YRotation.Value);
                                if (yDirection != "normal")
                                {
                                    var newaxis = new AxisPair(1, yDirection);
                                    var val = "yrotation" + " " + yDirection;
                                    var cmd = _gamePad.Get_KeyByValue(val);

                                    if (key != null)
                                    {
                                        DoGamePadSetKey(key, val);
                                    }
                                    else
                                    {
                                        var id = DoGamePadCommand(1, true, cmd);
                                        yrotationtocheck = id == -1 ? new AxisPair(-1, string.Empty) : newaxis;
                                    }
                                }
                            }
                        }

                        // Check Z Rotation
                        if (_gamePad.ZRotation.HasValue)
                        {
                            if (zrotationtocheck.Key > -1)
                            {
                                var zDirection = AxisDirection(_gamePad.ZRotation.Value);
                                var pushed = zrotationtocheck.Value == zDirection;
                                if (String.IsNullOrEmpty(key))
                                {
                                    var cmd = _gamePad.Get_KeyByValue("zrotation" + " " + zrotationtocheck.Value);
                                    var id = DoGamePadCommand(zrotationtocheck.Key, pushed, cmd);
                                    if (id == -1) zrotationtocheck = new AxisPair(-1, String.Empty);
                                }
                            }
                            else
                            {
                                var zDirection = AxisDirection(_gamePad.ZRotation.Value);
                                if (zDirection != "normal")
                                {
                                    var newaxis = new AxisPair(1, zDirection);
                                    var val = "zrotation" + " " + zDirection;
                                    var cmd = _gamePad.Get_KeyByValue(val);

                                    if (key != null)
                                    {
                                        DoGamePadSetKey(key, val);
                                    }
                                    else
                                    {
                                        var id = DoGamePadCommand(1, true, cmd);
                                        zrotationtocheck = id == -1 ? new AxisPair(-1, string.Empty) : newaxis;
                                    }
                                }
                            }
                        }

                        Thread.Sleep(Delay);
                    }
                }, ct);
                await task;
                task.Wait(ct);
                IsGamePadRunning = false;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                IsGamePadRunning = false;
                OpenDialog(ex.Message);

            }
        }

        private Dictionary<string, bool> pressedState = new Dictionary<string, bool>();

        private bool GetPressedState(string command)
        {
            bool pressed = false;
            if (!pressedState.TryGetValue(command, out pressed)){
                pressedState.Add(command, false);
            }
            return pressed;
        }

        private void SetPressedState(string command, bool pressed)
        {
            if (pressedState.ContainsKey(command)){
                pressedState[command] = pressed;
            }
            else
            {
                pressedState.Add(command, pressed);
            }
        }


        /// <summary>
        /// Execute relay commands from a game pad
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        private int DoGamePadCommand(int id, bool value, string command)
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.UI,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{id}|{value}|{command}"
            };
            MonitorLog.LogToMonitor(monitorItem);

            var returnId = -1;
            if (command == null) return returnId;
            switch (command)
            {
                case "volumeup":
                case "volumedown":
                    if (!Settings()) return returnId;
                    if (value == GetPressedState(command)) return id; // All but the focus commands should ignore repeated presses.
                    break;

                case "focusin":
                case "focusout":
                    if (!Focuser()) return returnId;
                    break;
                default:
                    if (!SkyTelescope()) return returnId;
                    if (!SkyServer.IsMountRunning) return returnId;
                    if (value == GetPressedState(command)) return id; // All but the focus commands should ignore repeated presses.
                    break;

            }

            SetPressedState(command, value);    // Record current value.

            if (ctsGamePad.IsCancellationRequested) return returnId;
            ThreadContext.InvokeOnUiThread(delegate
            {
                if (command != "sync")
                {
                    // Stop the double click timer
                    _syncClickTimer.Reset();

                }

                switch (command)
                {
                    case "home":
                        if (value)
                        {
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
                            if (_skyTelescopeVM.HcMouseDownUpCommand.CanExecute(null))
                                _skyTelescopeVM.HcMouseDownUpCommand.Execute(null);
                            returnId = id;
                            break;
                        }
                        else
                        {
                            if (_skyTelescopeVM.HcMouseUpUpCommand.CanExecute(null))
                                _skyTelescopeVM.HcMouseUpUpCommand.Execute(null);
                            break;
                        }
                    case "down":
                        if (value)
                        {
                            if (_skyTelescopeVM.HcMouseDownDownCommand.CanExecute(null))
                                _skyTelescopeVM.HcMouseDownDownCommand.Execute(null);
                            returnId = id;
                            break;
                        }
                        else
                        {
                            if (_skyTelescopeVM.HcMouseUpDownCommand.CanExecute(null))
                                _skyTelescopeVM.HcMouseUpDownCommand.Execute(null);
                            break;
                        }
                    case "left":
                        if (value)
                        {
                            if (_skyTelescopeVM.HcMouseDownLeftCommand.CanExecute(null))
                                _skyTelescopeVM.HcMouseDownLeftCommand.Execute(null);
                            returnId = id;
                            break;
                        }
                        else
                        {
                            if (_skyTelescopeVM.HcMouseUpLeftCommand.CanExecute(null))
                                _skyTelescopeVM.HcMouseUpLeftCommand.Execute(null);
                            break;
                        }
                    case "right":
                        if (value)
                        {
                            if (_skyTelescopeVM.HcMouseDownRightCommand.CanExecute(null))
                                _skyTelescopeVM.HcMouseDownRightCommand.Execute(null);
                            returnId = id;
                            break;
                        }
                        else
                        {
                            if (_skyTelescopeVM.HcMouseUpRightCommand.CanExecute(null))
                                _skyTelescopeVM.HcMouseUpRightCommand.Execute(null);
                            break;
                        }
                    case "speedup":
                        if (value)
                        {
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
                            if (_speedDownCount == 0)
                            {
                                if (_skyTelescopeVM.HcSpeedDownCommand.CanExecute(null))
                                    _skyTelescopeVM.HcSpeedDownCommand.Execute(null);
                            }

                            _speedDownCount++;
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
                            if (_volumeUpCount == 0)
                            {
                                if (_settingsVM.VolumeUpCommand.CanExecute(null))
                                    _settingsVM.VolumeUpCommand.Execute(null);
                            }

                            _volumeUpCount++;
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
                            if (_volumeDownCount == 0)
                            {
                                if (_settingsVM.VolumeDownCommand.CanExecute(null))
                                    _settingsVM.VolumeDownCommand.Execute(null);
                            }

                            _volumeDownCount++;
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
                            if (_rateSiderealCount == 0)
                            {
                                _skyTelescopeVM.TrackingRate = DriveRates.driveSidereal;
                                Synthesizer.Speak(Application.Current.Resources["vceSidereal"].ToString());
                            }

                            _rateSiderealCount++;
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
                            if (_rateLunarCount == 0)
                            {
                                _skyTelescopeVM.TrackingRate = DriveRates.driveLunar;
                                Synthesizer.Speak(Application.Current.Resources["vceLunar"].ToString());
                            }

                            _rateLunarCount++;
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
                            if (_rateSolarCount == 0)
                            {
                                _skyTelescopeVM.TrackingRate = DriveRates.driveSolar;
                                Synthesizer.Speak(Application.Current.Resources["vceSolar"].ToString());
                            }

                            _rateSolarCount++;
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
                            if (_rateKingCount == 0)
                            {
                                _skyTelescopeVM.TrackingRate = DriveRates.driveKing;
                                Synthesizer.Speak(Application.Current.Resources["vceKing"].ToString());
                            }

                            _rateKingCount++;
                            returnId = id;
                            break;
                        }
                        else
                        {
                            ResetCounts();
                            break;
                        }
                    case "abort":
                        if (value)
                        {
                            if (_abortCount == 0)
                            {
                                if (_skyTelescopeVM.AbortCmd.CanExecute(null))
                                    _skyTelescopeVM.AbortCmd.Execute(null);
                            }

                            _abortCount++;
                            returnId = id;
                            break;
                        }
                        else
                        {
                            ResetCounts();
                            break;
                        }
                    case "sync":
                        if (value)
                        {
                            if (_syncCount == 0) // Required a double click (for confirmation)
                            {
                                if (_syncClickTimer.IsRunning)
                                {
                                    _syncClickTimer.Stop();
                                    TimeSpan limit = new TimeSpan(0, 0, 0, 0, Delay + DoubleClickSpeed);
                                    TimeSpan ts = _syncClickTimer.Elapsed;
                                    _syncClickTimer.Reset();
                                    if (ts <= limit)
                                    {
                                        // It's a double click so run the command
                                        if (_skyTelescopeVM.SyncCmd.CanExecute(null))
                                        {
                                            Task.Run(() =>
                                            {
                                                lock (_vibrateLock) _vibrateRight = 0.9f;
                                                Thread.Sleep(500);
                                                lock (_vibrateLock) _vibrateRight = 0.0f;
                                            });
                                            _skyTelescopeVM.SyncCmd.Execute(null);
                                        }
                                    }
                                    else
                                    {
                                        // Start again.
                                        _syncClickTimer.Restart();
                                    }
                                }
                                else
                                {
                                    _syncClickTimer.Restart();
                                }
                            }

                            _syncCount++;
                            returnId = id;
                            break;
                        }
                        else
                        {
                            ResetCounts();
                            break;
                        }
                    case "spiralin":
                        if (value)
                        {
                            if (_spiralInCount == 0)
                            {
                                if (_skyTelescopeVM.SpiralInCmd.CanExecute(null))
                                    _skyTelescopeVM.SpiralInCmd.Execute(null);
                            }

                            _spiralInCount++;
                            returnId = id;
                            break;
                        }
                        else
                        {
                            ResetCounts();
                            break;
                        }
                    case "spiralout":
                        if (value)
                        {
                            if (_spiralOutCount == 0)
                            {
                                if (_skyTelescopeVM.SpiralOutCmd.CanExecute(null))
                                    _skyTelescopeVM.SpiralOutCmd.Execute(null);
                            }

                            _spiralOutCount++;
                            returnId = id;
                            break;
                        }
                        else
                        {
                            ResetCounts();
                            break;
                        }
                    case "newspiral":
                        if (value)
                        {
                            if (_newSpiralCount == 0)
                            {
                                if (_skyTelescopeVM.SpiralGenerateCmd.CanExecute(null))
                                    _skyTelescopeVM.SpiralGenerateCmd.Execute(null);
                            }

                            _newSpiralCount++;
                            returnId = id;
                            break;
                        }
                        else
                        {
                            ResetCounts();
                            break;
                        }
                    case "focusin":
                        if (value)
                        {
                            if (_focuserVM.MoveFocuserInCommand.CanExecute(null))
                                _focuserVM.MoveFocuserInCommand.Execute(null);
                            _focusInCount++;

                            returnId = id;
                            break;
                        }
                        else
                        {
                            ResetCounts();
                            break;
                        }
                    case "focusout":
                        if (value)
                        {
                            if (_focuserVM.MoveFocuserOutCommand.CanExecute(null))
                                _focuserVM.MoveFocuserOutCommand.Execute(null);
                            _focusOutCount++;
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
            }, ctsGamePad.Token);
            return returnId;
        }

        /// <summary>
        /// Stores a key value pair to dictionary then saves to settings file
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        private void DoGamePadSetKey(string key, string val)
        {

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.UI,
                Category = MonitorCategory.Server,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{key}|{val}"
            };
            MonitorLog.LogToMonitor(monitorItem);

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
                    SpeedDown = val;
                    break;
                case "speedup":
                    SpeedUp = val;
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
                    VolumeDown = val;
                    break;
                case "volumeup":
                    VolumeUp = val;
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
                case "abort":
                    Abort = val;
                    break;
                case "sync":
                    Sync = val;
                    break;
                case "spiralin":
                    SpiralIn = val;
                    break;
                case "spiralout":
                    SpiralOut = val;
                    break;
                case "newspiral":
                    NewSpiral = val;
                    break;
                case "focusin":
                    FocusIn = val;
                    break;
                case "focusout":
                    FocusOut = val;
                    break;
            }

            _gamePad?.Update_Setting(key, val);
            _gamePad?.SaveSettings();
            LoadTextboxes();
        }

        /// <summary>
        /// Load gamepad button settings
        /// </summary>
        private void LoadTextboxes()
        {
            try
            {
                var dict = _gamePad?.GetSettings();
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
                            SpeedDown = val;
                            break;
                        case "speedup":
                            SpeedUp = val;
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
                            VolumeDown = val;
                            break;
                        case "volumeup":
                            VolumeUp = val;
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
                        case "abort":
                            Abort = val;
                            break;
                        case "sync":
                            Sync = val;
                            break;
                        case "spiralin":
                            SpiralIn = val;
                            break;
                        case "spiralout":
                            SpiralOut = val;
                            break;
                        case "newspiral":
                            NewSpiral = val;
                            break;
                        case "focusin":
                            FocusIn = val;
                            break;
                        case "focusout":
                            FocusOut = val;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
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
            _speedDownCount = 0;
            _volumeUpCount = 0;
            _volumeDownCount = 0;
            _rateSiderealCount = 0;
            _rateLunarCount = 0;
            _rateSolarCount = 0;
            _rateKingCount = 0;
            _abortCount = 0;
            _syncCount = 0;
            _spiralInCount = 0;
            _spiralOutCount = 0;
            _newSpiralCount = 0;
            _focusInCount = 0;
            _focusOutCount = 0;
        }

        private ICommand _clickTextBoxGotFocusCommand;

        public ICommand ClickTextBoxGotFocusCommand
        {
            get
            {
                var command = _clickTextBoxGotFocusCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickTextBoxGotFocusCommand = new RelayCommand(
                    ClickTextBoxGotFocus
                );
            }
        }

        private void ClickTextBoxGotFocus(object parameter)
        {
            try
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"|Parameter|{parameter}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                _focusTextBox = parameter.ToString().Trim().ToLower();
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
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
                var command = _clickPreviewMouseDoubleClickCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickPreviewMouseDoubleClickCommand = new RelayCommand(
                    ClickPreviewMouseDoubleClick
                );
            }
        }

        private void ClickPreviewMouseDoubleClick(object parameter)
        {
            try
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Information,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"|Parameter|{parameter}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                if (_gamePad == null) return;
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
                        SpeedDown = null;
                        break;
                    case "speedup":
                        SpeedUp = null;
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
                        VolumeDown = null;
                        break;
                    case "volumeup":
                        VolumeUp = null;
                        break;
                    case "ratesidereal":
                        RateSidereal = null;
                        break;
                    case "ratelunar":
                        RateLunar = null;
                        break;
                    case "ratesolar":
                        RateSolar = null;
                        break;
                    case "rateking":
                        RateKing = null;
                        break;
                    case "abort":
                        Abort = null;
                        break;
                    case "sync":
                        Sync = null;
                        break;
                    case "spiralin":
                        SpiralIn = null;
                        break;
                    case "spiralout":
                        SpiralOut = null;
                        break;
                    case "newspiral":
                        NewSpiral = null;
                        break;
                    case "focusin":
                        FocusIn = null;
                        break;
                    case "focusout":
                        FocusOut = null;
                        break;
                    default:
                        update = false;
                        break;
                }

                if (!update) return;
                _gamePad?.Update_Setting(key, null);
                _gamePad?.SaveSettings();
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickTextBoxLostFocusCommand;

        public ICommand ClickTextBoxLostFocusCommand
        {
            get
            {
                var command = _clickTextBoxLostFocusCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickTextBoxLostFocusCommand = new RelayCommand(
                    param => ClickTextBoxLostFocus()
                );
            }
        }

        private void ClickTextBoxLostFocus()
        {
            try
            {
                _focusTextBox = null;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
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
                var command = _clickSaveCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickSaveCommand = new RelayCommand(
                    param => ClickSave()
                );
            }
        }

        private void ClickSave()
        {
            try
            {
                _gamePad?.SaveSettings();
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
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
                var command = _openDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _openDialogCommand = new RelayCommand(
                    param => OpenDialog(null)
                );
            }
        }
        private void OpenDialog(string msg, string caption = null)
        {
            if (IsDialogOpen)
            {
                OpenDialogWin(msg, caption);
            }
            else
            {
                if (msg != null) DialogMsg = msg;
                DialogCaption = caption ?? Application.Current.Resources["diaDialog"].ToString();
                DialogContent = new DialogOK();
                IsDialogOpen = true;
            }

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.UI,
                Category = MonitorCategory.Interface,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{msg}"
            };
            MonitorLog.LogToMonitor(monitorItem);
        }

        private void OpenDialogWin(string msg, string caption = null)
        {
            //Open as new window
            var bWin = new MessageControlV(caption, msg) { Owner = Application.Current.MainWindow };
            bWin.Show();
        }

        private ICommand _clickOkDialogCommand;

        public ICommand ClickOkDialogCommand
        {
            get
            {
                var command = _clickOkDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickOkDialogCommand = new RelayCommand(
                    param => ClickOkDialog()
                );
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
                var command = _clickCancelDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickCancelDialogCommand = new RelayCommand(
                    param => ClickCancelDialog()
                );
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
                var dialog = _runMessageDialog;
                if (dialog != null)
                {
                    return dialog;
                }

                return _runMessageDialog = new RelayCommand(
                    param => ExecuteMessageDialog()
                );
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
        ~GamePadVM()
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
                ctsGamePad?.Dispose();
                _settingsVM?.Dispose();
                _gamePad?.Dispose();
            }

            // free native resources if there are any.
            //if (nativeResource != IntPtr.Zero)
            //{
            //    Marshal.FreeHGlobal(nativeResource);
            //    nativeResource = IntPtr.Zero;
            //}
        }

        #endregion

        #region Utility functions ...

        /// <summary>
        /// PoV commands to int conversions
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        private string PovDirection(int degrees)
        {
            switch (degrees)
            {
                case 0:
                    return "up";
                case 9000:
                    return "right";
                case 18000:
                    return "down";
                case 27000:
                    return "left";
                default:
                    return "";
            }
        }

        /// <summary>
        /// Axis values to commands
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private string AxisDirection(int number)
        {
            if (number >= 0 && number < 2000) return "low";
            if (number >= 2000 && number <= 64000) return "normal";
            if (number > 64000 && number <= 66000) return "high";
            return null;
        }
        #endregion
    }
}
