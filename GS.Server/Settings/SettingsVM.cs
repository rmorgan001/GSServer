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

using GS.Server.Helpers;
using GS.Server.Main;
using GS.Server.SkyTelescope;
using GS.Shared;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using GS.Principles;
using GS.Server.Controls.Dialogs;
using GS.Server.Windows;
using GS.Shared.Command;
using ASCOM.DeviceInterface;

namespace GS.Server.Settings
{
    public sealed class SettingsVM : ObservableObject, IPageVM, IDisposable
    {
        #region fields
        public string TopName => "GS Server";
        public string BottomName => "Options";
        private static string GsUrl => $"https://github.com/rmorgan001/GSServer/wiki/autodownloads";
        public int Uid => 2;
        private readonly MainWindowVM _mainWindowVm;
        public static SettingsVM _settingsVM;
        private readonly CancellationToken _cts;
        private readonly CancellationToken _ctsMonitor;

        #endregion

        public SettingsVM()
        {
            try
            {
                using (new WaitCursor())
                {
                    _settingsVM = this;

                    var monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI,
                        Category = MonitorCategory.Interface, Type = MonitorType.Information,
                        Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = "Loading SettingsVM"
                    };
                    MonitorLog.LogToMonitor(monitorItem);

                    //token to cancel UI updates
                    _cts = new CancellationToken();
                    _ctsMonitor = new CancellationToken();

                    //subscribe to Server property changes
                    Settings.StaticPropertyChanged += PropertyChangedServer;

                    //subscribe to SkySettings property changes
                    SkySettings.StaticPropertyChanged += PropertyChangedSkySettings;

                    //subscribe to Server property Monitor items
                    MonitorQueue.StaticPropertyChanged += PropertyChangedMonitorQueue;

                    MonitorEntries = new ObservableCollection<MonitorEntry>();
                    StartStopButtonText = Shared.Settings.StartMonitor ? @"Stop" : @"Start";

                    _mainWindowVm = MainWindowVM._mainWindowVm;
                    SleepMode = Settings.SleepMode;
                    Synthesizer.VoicePause = true;
                    VoiceActive = Settings.VoiceActive;
                    Synthesizer.VoicePause = false;

                    // Theme Colors
                    var swatches = new SwatchesProvider().Swatches;
                    var primaryColors = PrimaryColors = swatches is IList<Swatch> list ? list : swatches.ToList();
                    AccentColors = primaryColors.Where(item => item.IsAccented).ToList();
                    PrimaryColor = primaryColors.First(item => item.Name.Equals(Settings.PrimaryColor, StringComparison.InvariantCultureIgnoreCase));
                    AccentColor = primaryColors.First(item => item.Name.Equals(Settings.AccentColor, StringComparison.InvariantCultureIgnoreCase));

                    var paletteHelper = new PaletteHelper();
                    var theme = paletteHelper.GetTheme();
                    theme.SetBaseTheme(Settings.DarkTheme ? Theme.Dark : Theme.Light);
                    paletteHelper.SetTheme(theme);

                    


                    //Performance
                    IntervalList = new List<int>(Numbers.InclusiveIntRange(50, 500, 2));
                    //Volume Range
                    VolumeList = new List<int>(Numbers.InclusiveIntRange(0, 100));
                    DurationList = new List<int>(Numbers.InclusiveIntRange(100, 2000, 100));
                    FrequencyList = new List<int>(Numbers.InclusiveIntRange(200, 9000, 200));

                    ClearSettings();

                    RenderCapability = (System.Windows.Media.RenderCapability.Tier >> 16).ToString();
                    monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.UI,
                        Category = MonitorCategory.Interface,
                        Type = MonitorType.Information,
                        Method = MethodBase.GetCurrentMethod()?.Name,
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = $"RenderCapability|{RenderCapability}"
                    };
                    MonitorLog.LogToMonitor(monitorItem);

                    CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    Settings.SkyWatcher = true;
                    PecShow = SkyServer.PecShow;

                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface, Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        #region ViewModel

        /// <summary>
        /// Subscription to changes from Settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertyChangedServer(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ThreadContext.InvokeOnUiThread(
                    delegate
                    {
                        switch (e.PropertyName)
                        {
                            case "Focuser":
                                Focuser = Settings.Focuser;
                                break;
                            case "DarkTheme":
                                DarkTheme = Settings.DarkTheme;
                                break;
                            case "PrimaryColor":
                                // PrimaryColor = Settings.PrimaryColor;
                                break;
                            case "AccentColor":
                                //AccentColor = Settings.AccentColor;
                                break;
                            case "Gamepad":
                                GamePad = Settings.GamePad;
                                break;
                            case "SkyWatcher":
                                SkyWatcher = Settings.SkyWatcher;
                                break;
                            case "Model3D":
                                Model3D = Settings.Model3D;
                                break;
                            case "SleepMode":
                                SleepMode = Settings.SleepMode;
                                break;
                            case "StartMinimized":
                                StartMinimized = Settings.StartMinimized;
                                break;
                            case "StartOnTop":
                                StartOnTop = Settings.StartOnTop;
                                break;
                            case "VoiceActive":
                                VoiceActive = Settings.VoiceActive;
                                break;
                            case "VoiceName":
                                VoiceName = Settings.VoiceName;
                                break;
                            case "VoiceVolume":
                                VoiceVolume = Settings.VoiceVolume;
                                break;
                        }
                    }, _cts);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface, Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }
        private void PropertyChangedSkySettings(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ThreadContext.InvokeOnUiThread(
                    delegate
                    {
                        switch (e.PropertyName)
                        {
                            case "UTCDateOffset":
                               // OnPropertyChanged("UTCDateOffset");
                               // OnPropertyChanged($"UTCTime");
                                break;
                            case "SleepMode":
                                SleepMode = Settings.SleepMode;
                                break;
                        }
                    }, _cts);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface, Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        #endregion

        #region Bindings

        public bool AllowBeeps
        {
            get => Settings.AllowBeeps;
            set
            {
                Settings.AllowBeeps = value;
                if(value){Synthesizer.Beep(BeepType.Default);}
                OnPropertyChanged();
            }
        }

        public IList<int> FrequencyList { get; }
        public int BeepFreq
        {
            get => Settings.BeepFreq;
            set
            {
                Settings.BeepFreq = value;
                Synthesizer.Beep(BeepType.Default); 
                OnPropertyChanged();
            }
        }

        public IList<int> DurationList { get; }
        public int BeepDur
        {
            get => Settings.BeepDur;
            set
            {
                Settings.BeepDur = value;
                Synthesizer.Beep(BeepType.Default);
                OnPropertyChanged();
            }
        }

        private static bool _pecShow;
        /// <summary>
        /// sets up bool to load a test tab
        /// </summary>
        public bool PecShow
        {
            get => _pecShow;
            set
            {
                if (_pecShow == value) { return; }
                _pecShow = value;
                OnPropertyChanged();
            }
        }

        public bool SkyWatcher
        {
            get => Settings.SkyWatcher;
            set
            {
                Settings.SkyWatcher = value;
                //_mainWindowVm.UpdateTabViewModel("SkyWatcher");
                OnPropertyChanged();
            }
        }

        public bool Focuser
        {
            get => Settings.Focuser;
            set
            {
                Settings.Focuser = value;
                _mainWindowVm.UpdateTabViewModel("Focuser");
                OnPropertyChanged();
            }
        }

        public bool DisableHardwareAcceleration
        {
            get => Settings.DisableHardwareAcceleration;
            set
            {
                Settings.DisableHardwareAcceleration = value;
                OnPropertyChanged();
            }
        }

        private string _renderCapability;

        /// <remarks>
        /// 0x00000000	0	No graphics hardware acceleration is available for the application on the device. All graphics features use software acceleration. The DirectX version level is less than version 9.0.
        /// 0x00010000	1	Most of the graphics features of WPF will use hardware acceleration if the necessary system resources are available and have not been exhausted.This corresponds to a DirectX version that is greater than or equal to 9.0.
        /// 0x00020000	2	Most of the graphics features of WPF will use hardware acceleration provided the necessary system resources have not been exhausted.This corresponds to a DirectX version that is greater than or equal to 9.0.
        /// </remarks>
        public string RenderCapability
        {
            get => _renderCapability;
            private set
            {
                if (_renderCapability == value) return;
                _renderCapability = value;
                OnPropertyChanged();
            }
        }

        public bool GamePad
        {
            get => Settings.GamePad;
            set
            {
                Settings.GamePad = value;
                _mainWindowVm.UpdateTabViewModel("GamePad");
                OnPropertyChanged();
            }
        }

        public List<string> Languages => Shared.Languages.SupportedLanguages;

        public string Lang
        {
            get => Shared.Languages.Language;
            set
            {
                Shared.Languages.Language = value;
                OnPropertyChanged();
                OpenDialog("Restart GS Server");
            }
        }

        public bool Notes
        {
            get => Settings.Notes;
            set
            {
                Settings.Notes = value;
                _mainWindowVm.UpdateTabViewModel("Notes");
                OnPropertyChanged();
            }
        }

        public bool Model3D
        {
            get => Settings.Model3D;
            set
            {
                Settings.Model3D = value;
                _mainWindowVm.UpdateTabViewModel("Model3D");
                OnPropertyChanged();
            }
        }

        public bool Pec
        {
            get => Settings.Pec;
            set
            {
                Settings.Pec = value;
                _mainWindowVm.UpdateTabViewModel("Pec");
                OnPropertyChanged();
            }
        }
        
        public bool Plot
        {
            get => Settings.Plot;
            set
            {
                Settings.Plot = value;
                _mainWindowVm.UpdateTabViewModel("Plot");
                OnPropertyChanged();
            }
        }

        public bool PoleLocator
        {
            get => Settings.PoleLocator;
            set
            {
                Settings.PoleLocator = value;
                _mainWindowVm.UpdateTabViewModel("PoleLocator");
                OnPropertyChanged();
            }
        }

        public bool Pulses
        {
            get => Settings.Pulses;
            set
            {
                Settings.Pulses = value;
                _mainWindowVm.UpdateTabViewModel("Pulses");
                OnPropertyChanged();
            }
        }

        public bool AlignmentTabVisible
        {
            get => SkySettings.AlignmentMode != AlignmentModes.algAltAz && Settings.AlignmentTabVisible;
            set
            {
                Settings.AlignmentTabVisible = value;
                _mainWindowVm.UpdateTabViewModel("Alignment");
                OnPropertyChanged();
            }
        }

        public bool AlignmentOptionEnabled
        {
            get => (SkySettings.AlignmentMode != AlignmentModes.algAltAz);
            set
            {
                Settings.AlignmentTabVisible = (SkySettings.AlignmentMode != AlignmentModes.algAltAz);
                _mainWindowVm.UpdateTabViewModel("Alignment");
                OnPropertyChanged();
            }
        }

        private SleepMode _sleepMode;
        private bool sleepMode;
        public bool SleepMode
        {
            get => Settings.SleepMode;
            set
            {
                if(sleepMode == value) {return;}
                if (value)
                {
                    if (_sleepMode == null)
                    {
                        _sleepMode = new SleepMode();
                        _sleepMode.SleepOn();
                    }
                }
                else
                {
                    if (_sleepMode != null)
                    {
                        _sleepMode.SleepOff();
                        _sleepMode = null;
                    }
                }

                sleepMode = value;
                Settings.SleepMode = value;
                OnPropertyChanged();
            }
        }
        
        public bool Snap
        {
            get => Settings.Snap;
            set
            {
                Settings.Snap = value;
                _mainWindowVm.UpdateTabViewModel("Snap");
                OnPropertyChanged();
            }
        }

        public bool StartMinimized
        {
            get => Settings.StartMinimized;
            set
            {
                Settings.StartMinimized = value;
                OnPropertyChanged();
            }
        }

        public bool StartOnTop
        {
            get => Settings.StartOnTop;
            set
            {
                Settings.StartOnTop = value;
                OnPropertyChanged();
            }
        }

        public IList<string> VoiceNames => Synthesizer.GetVoices();

        public string VoiceName
        {
            get => Settings.VoiceName;
            set
            {
                Settings.VoiceName = value;
                Synthesizer.Speak(value);
                OnPropertyChanged();
            }
        }

        private bool _voiceActive;
        public bool VoiceActive
        {
            get => _voiceActive;
            set
            {
                if ( _voiceActive == value){return;}
                _voiceActive = value;
                Settings.VoiceActive = value;
                if (Synthesizer.LastError != null)
                {
                    OpenDialog(Synthesizer.LastError.Message);
                    Synthesizer.LastError = null;
                }
                else
                {
                    if (value) { Synthesizer.Speak(Application.Current.Resources["vceActive"].ToString()); }
                }
                OnPropertyChanged();
            }
        }

        public IList<int> VolumeList { get; }

        public int VoiceVolume
        {
            get => Settings.VoiceVolume;
            set
            {
                Settings.VoiceVolume = value;
                Synthesizer.Speak($"{value}");
                OnPropertyChanged();
            }
        }

        public bool DarkTheme
        {
            get => Settings.DarkTheme;
            set
            {
                Settings.DarkTheme = value;
                OnPropertyChanged();
            }
        }

        public IList<Swatch> PrimaryColors { get; }
        private Swatch _primaryColor;

        public Swatch PrimaryColor
        {
            get => _primaryColor;
            set
            {
                _primaryColor = value;
                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();
                theme.SetPrimaryColor(_primaryColor.ExemplarHue.Color);
                paletteHelper.SetTheme(theme);
                Settings.PrimaryColor = _primaryColor.Name;
                OnPropertyChanged();
            }
        }

        public IList<Swatch> AccentColors { get; }
        private Swatch _accentColor;

        public Swatch AccentColor
        {
            get => _accentColor;
            set
            {
                _accentColor = value;
                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();
                theme.SetSecondaryColor(_accentColor.ExemplarHue.Color);
                paletteHelper.SetTheme(theme);
                Settings.AccentColor = _accentColor.Name;
                OnPropertyChanged();
            }
        }

        public IList<int> IntervalList { get; }
        private int _displayInterval;

        public int DisplayInterval
        {
            get => SkySettings.DisplayInterval;
            set
            {
                if (value == _displayInterval) return;
                _displayInterval = value;
                SkySettings.DisplayInterval = value;
                OnPropertyChanged();
            }
        }

        public bool HomeWarning
        {
            get => SkySettings.HomeWarning;
            set
            {
                if (HomeWarning == value) return;
                SkySettings.HomeWarning = value;
                OnPropertyChanged();
            }
        }

        public bool HomeDialog
        {
            get => SkySettings.HomeDialog;
            set
            {
                if (HomeDialog == value) return;
                SkySettings.HomeDialog = value;
                OnPropertyChanged();
            }
        }

        public bool ParkDialog
        {
            get => SkySettings.ParkDialog;
            set
            {
                if (ParkDialog == value) return;
                SkySettings.ParkDialog = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Time

       //public TimeSpan UTCDateOffset => SkySettings.UTCDateOffset;

        //public DateTime UTCTime => HiResDateTime.UtcNow.Add(SkySettings.UTCDateOffset);

        //private ICommand _clickResetUtcOffsetCmd;

        //public ICommand ClickResetUtcOffsetCmd
        //{
        //    get
        //    {
        //        var cmd = _clickResetUtcOffsetCmd;
        //        if (cmd != null)
        //        {
        //            return cmd;
        //        }

        //        return _clickResetUtcOffsetCmd = new RelayCommand(
        //            param => ClickResetUtcOffset()
        //        );
        //    }
        //}

        //private void ClickResetUtcOffset()
        //{
        //    SkySettings.UTCDateOffset = new TimeSpan();
        //}

        #endregion

        #region Utilities

        
        private ICommand _clickUtilCmd;

        public ICommand ClickUtilCmd
        {
            get
            {
                var cmd = _clickUtilCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _clickUtilCmd = new RelayCommand(
                    param => OpenUtilitiesCmd()
                );
            }
        }

        private void OpenUtilitiesCmd()
        {
            try
            {
                Process.Start(new ProcessStartInfo(Path.Combine(Environment.CurrentDirectory, "GS.Utilities.exe")));
            }
            catch (Exception ex)
            {
                OpenDialog(ex.Message);
            }
        }

        #endregion

        #region Monitor

        public ObservableCollection<MonitorEntry> MonitorEntries { get; }

        private void PropertyChangedMonitorQueue(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ThreadContext.InvokeOnUiThread(
                    delegate
                    {
                        switch (e.PropertyName)
                        {
                            case "MonitorEntry":
                                UpdateUi(MonitorQueue.MonitorEntry);
                                break;
                        }
                    }, _ctsMonitor);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface, Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private void UpdateUi(MonitorEntry monitorEntry)
        {
            try
            {
                if (MonitorEntries.Count + 1 > 99998)
                {
                    MonitorEntries.Clear();
                    MonitorLog.ResetIndex();
                }

                monitorEntry.Message = monitorEntry.Message.Replace("\r", string.Empty);
                MonitorEntries.Add(monitorEntry);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface, Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        public static bool ServerDevice
        {
            get => MonitorLog.InDevices(MonitorDevice.Server);
            set => MonitorLog.DevicesToMonitor(MonitorDevice.Server, value);
        }

        public static bool Telescope
        {
            get => MonitorLog.InDevices(MonitorDevice.Telescope);
            set => MonitorLog.DevicesToMonitor(MonitorDevice.Telescope, value);
        }

        public static bool UI
        {
            get => MonitorLog.InDevices(MonitorDevice.UI);
            set => MonitorLog.DevicesToMonitor(MonitorDevice.UI, value);
        }

        public bool Driver
        {
            get => MonitorLog.InCategory(MonitorCategory.Driver);
            set => MonitorLog.CategoriesToMonitor(MonitorCategory.Driver, value);
        }

        public bool Interface
        {
            get => MonitorLog.InCategory(MonitorCategory.Interface);
            set => MonitorLog.CategoriesToMonitor(MonitorCategory.Interface, value);
        }

        public bool Server
        {
            get => MonitorLog.InCategory(MonitorCategory.Server);
            set => MonitorLog.CategoriesToMonitor(MonitorCategory.Server, value);
        }

        public bool Mount
        {
            get => MonitorLog.InCategory(MonitorCategory.Mount);
            set => MonitorLog.CategoriesToMonitor(MonitorCategory.Mount, value);
        }

        public bool Alignment
        {
            get => MonitorLog.InCategory(MonitorCategory.Alignment);
            set => MonitorLog.CategoriesToMonitor(MonitorCategory.Alignment, value);
        }

        public bool Information
        {
            get => MonitorLog.InTypes(MonitorType.Information);
            set => MonitorLog.TypesToMonitor(MonitorType.Information, value);
        }

        public bool Data
        {
            get => MonitorLog.InTypes(MonitorType.Data);
            set => MonitorLog.TypesToMonitor(MonitorType.Data, value);
        }

        public bool Warning
        {
            get => MonitorLog.InTypes(MonitorType.Warning);
            set => MonitorLog.TypesToMonitor(MonitorType.Warning, value);
        }

        public bool Error
        {
            get => MonitorLog.InTypes(MonitorType.Error);
            set => MonitorLog.TypesToMonitor(MonitorType.Error, value);
        }

        public bool Debug
        {
            get => MonitorLog.InTypes(MonitorType.Debug);
            set => MonitorLog.TypesToMonitor(MonitorType.Debug, value);
        }

        public bool SessionLog
        {
            get => Shared.Settings.LogSession;
            set
            {
                if (MonitorToFile == value) return;
                Shared.Settings.LogSession = value;
                OnPropertyChanged();
            }
        }

        public bool MonitorToFile
        {
            get => Shared.Settings.LogMonitor;
            set
            {
                if (MonitorToFile == value) return;
                Shared.Settings.LogMonitor = value;
                OnPropertyChanged();
            }
        }

        private ICommand _volumeUpCommand;

        public ICommand VolumeUpCommand
        {
            get
            {
                var command = _volumeUpCommand;
                if (command != null)
                {
                    return command;
                }

                return _volumeUpCommand = new RelayCommand(
                    param => VolumeUp()
                );
            }
        }

        private void VolumeUp()
        {
            try
            {
                var currentvVolume = VoiceVolume;
                if (currentvVolume <= 90)
                {
                    VoiceVolume += 10;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message);
            }
        }

        private ICommand _volumeDownCommand;

        public ICommand VolumeDownCommand
        {
            get
            {
                var command = _volumeDownCommand;
                if (command != null)
                {
                    return command;
                }

                return _volumeDownCommand = new RelayCommand(
                    param => VolumeDown()
                );
            }
        }

        private void VolumeDown()
        {
            try
            {
                var currentvVolume = VoiceVolume;
                if (currentvVolume >= 10)
                {
                    VoiceVolume -= 10;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message);
            }
        }

        private string _startStopButtonText;

        public string StartStopButtonText
        {
            get => _startStopButtonText;
            set
            {
                if (_startStopButtonText == value) return;
                _startStopButtonText = value;
                OnPropertyChanged();
            }
        }

        private ICommand _clickStartStopCommand;

        public ICommand ClickStartStopCommand
        {
            get
            {
                var command = _clickStartStopCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickStartStopCommand = new RelayCommand(
                    param => ClickStartStop()
                );
            }
        }

        private void ClickStartStop()
        {
            try
            {
                using (new WaitCursor())
                {
                    Shared.Settings.StartMonitor = !Shared.Settings.StartMonitor;
                    StartStopButtonText = StartStopButtonText == "Start" ? "Stop" : "Start";
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface, Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickClearCommand;

        public ICommand ClickClearCommand
        {
            get
            {
                var command = _clickClearCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickClearCommand = new RelayCommand(
                    param => ClickClear()
                );
            }
        }

        private void ClickClear()
        {
            try
            {
                using (new WaitCursor())
                {
                    MonitorEntries.Clear();
                    MonitorLog.ResetIndex();
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface, Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickExportCommand;

        public ICommand ClickExportCommand
        {
            get
            {
                var command = _clickExportCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickExportCommand = new RelayCommand(
                    param => ClickExport()
                );
            }
        }

        private void ClickExport()
        {
            StreamWriter stream = null;
            try
            {
                Shared.Settings.StartMonitor = false;
                var saveFileDialog = new SaveFileDialog
                {
                    InitialDirectory = @"C:\",
                    Title = @"Save Monitor Information",
                    CheckFileExists = false,
                    CheckPathExists = false,
                    DefaultExt = "txt",
                    Filter = @"Txt files (*.txt)|*.txt|All files (*.*)|*.*",
                    FilterIndex = 2,
                    RestoreDirectory = true,
                    FileName = $"GSServerMonitor_{DateTime.Now:yyyy-dd-M--HH-mm-ss}.txt"
                };
                if (saveFileDialog.ShowDialog() != true) return;
                using (new WaitCursor())
                {
                    stream = new StreamWriter(saveFileDialog.FileName);
                    foreach (var item in MonitorEntries)
                    {
                        stream.WriteLine(
                            $"{item.Index}|{item.Datetime.ToLocalTime():dd/MM/yyyy HH:mm:ss.fff}|{item.Device}|{item.Category}|{item.Type}|{item.Thread}|{item.Method}|{item.Message}");
                    }

                    stream.Close();
                    stream = null;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface, Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
            finally
            {
                stream?.Dispose();
            }
        }

        private ICommand _clickCopyCommand;

        public ICommand ClickCopyCommand
        {
            get
            {
                var command = _clickCopyCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickCopyCommand = new RelayCommand(
                    param => ClickCopy()
                );
            }
        }

        private void ClickCopy()
        {
            try
            {
                using (new WaitCursor())
                {
                    using (var stream = new MemoryStream())
                    {
                        var sw = new StreamWriter(stream);
                        foreach (var item in MonitorEntries)
                        {
                            sw.Write(
                                $"{item.Index}|{item.Datetime.ToLocalTime():dd/MM/yyyy HH:mm:ss.fff}|{item.Device}|{item.Category}|{item.Type}|{item.Thread}|{item.Method}|{item.Message}{Environment.NewLine}");
                            sw.Flush();
                        }

                        sw.Flush();
                        stream.Position = 0;
                        using (var streamReader = new StreamReader(stream))
                        {
                            Clipboard.SetText(streamReader.ReadToEnd());
                        }

                        //stream.Close();
                        OpenDialog($"{Application.Current.Resources["optCopiedClip"]}");
                    }
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface, Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickBaseCommand;

        public ICommand ClickBaseCommand
        {
            get
            {
                var command = _clickBaseCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickBaseCommand = new RelayCommand(
                    param => ClickBase((bool) param)
                );
            }
        }

        private void ClickBase(bool isDark)
        {
            try
            {
                using (new WaitCursor())
                {
                    var paletteHelper = new PaletteHelper();
                    var theme = paletteHelper.GetTheme();
                    theme.SetBaseTheme(isDark ? Theme.Dark : Theme.Light);
                    paletteHelper.SetTheme(theme);
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface, Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _openFolderDialogCmd;

        public ICommand OpenFolderDialogCmd
        {
            get
            {
                var command = _openFolderDialogCmd;
                if (command != null)
                {
                    return command;
                }

                return _openFolderDialogCmd = new RelayCommand(
                    param => OpenFolderDialog()
                );
            }
        }

        private void OpenFolderDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    var folder = GSFile.GetFolderName(GSFile.GetLogPath());
                    if(string.IsNullOrEmpty(folder)){ return; }
                    if (Directory.Exists(folder)) { GSFile.SaveLogPath(folder); }
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }
        #endregion

        #region Dialog

        private string _DialogMsg;
        public string DialogMsg
        {
            get => _DialogMsg;
            set
            {
                if (_DialogMsg == value) return;
                _DialogMsg = value;
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

        private ICommand _cancelDialogCommand;
        public ICommand CancelDialogCommand
        {
            get
            {
                var command = _cancelDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _cancelDialogCommand = new RelayCommand(
                    param => CancelDialog()
                );
            }
        }
        private void CancelDialog()
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

        #region Reset Settings Dialog

        private bool _skyTelescopeSettings;

        public bool SkyTelescopeSettings
        {
            get => _skyTelescopeSettings;
            set
            {
                _skyTelescopeSettings = value;
                OnPropertyChanged();
            }
        }

        private bool _serverSettings;

        public bool ServerSettings
        {
            get => _serverSettings;
            set
            {
                _serverSettings = value;
                OnPropertyChanged();
            }
        }

        private bool _gamePadSettings;

        public bool GamePadSettings
        {
            get => _gamePadSettings;
            set
            {
                _gamePadSettings = value;
                OnPropertyChanged();
            }
        }

        private bool _monitorSettings;

        public bool MonitorSettings
        {
            get => _monitorSettings;
            set
            {
                _monitorSettings = value;
                OnPropertyChanged();
            }

        }

        private void ClearSettings()
        {
            SkyTelescopeSettings = false;
            ServerSettings = false;
            GamePadSettings = false;
            MonitorSettings = false;
        }


        private ICommand _openSettingsResetDialogCommand;

        public ICommand OpenSettingsResetDialogCommand
        {
            get
            {
                var command = _openSettingsResetDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _openSettingsResetDialogCommand = new RelayCommand(
                    param => OpenSettingsResetDialog()
                );
            }
        }

        private void OpenSettingsResetDialog()
        {
            try
            {
                if (!SkyTelescopeSettings && !ServerSettings && !GamePadSettings && !MonitorSettings)
                {
                    OpenDialog(Application.Current.Resources["optNoResetSettings"].ToString());
                    return;
                }

                DialogContent = new ResetSettingsDialog();
                IsDialogOpen = true;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }

        }

        private ICommand _acceptSettingsResetDialogCommand;

        public ICommand AcceptResetDialogCommand
        {
            get
            {
                var command = _acceptSettingsResetDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _acceptSettingsResetDialogCommand = new RelayCommand(
                    param => AcceptSettingsResetDialog()
                );
            }
        }

        private void AcceptSettingsResetDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (SkyTelescopeSettings)
                    {
                        Properties.SkyTelescope.Default.Reset();
                        Properties.SkyTelescope.Default.Save();
                        Properties.SkyTelescope.Default.Reload();
                    }

                    if (ServerSettings)
                    {
                        Properties.Server.Default.Reset();
                        Properties.Server.Default.Save();
                        Properties.Server.Default.Reload();
                    }

                    if (GamePadSettings)
                    {
                        Properties.Gamepad.Default.Reset();
                        Properties.Gamepad.Default.Save();
                        Properties.Gamepad.Default.Reload();
                    }

                    if (MonitorSettings)
                    {
                        Shared.Properties.Monitor.Default.Reset();
                        Shared.Properties.Monitor.Default.Save();
                        Shared.Properties.Monitor.Default.Reload();
                    }

                    IsDialogOpen = false;
                    OpenDialog(Application.Current.Resources["optRestart"].ToString());
                    ClearSettings();
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        private ICommand _cancelSettingsResetDialogCommand;

        public ICommand CancelResetDialogCommand
        {
            get
            {
                var command = _cancelSettingsResetDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _cancelSettingsResetDialogCommand = new RelayCommand(
                    param => CancelSettingsResetDialog()
                );
            }
        }

        private void CancelSettingsResetDialog()
        {
            try
            {
                ClearSettings();
                IsDialogOpen = false;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        #endregion

        #region Update Dialog

        private bool _updateEnabled;

        public bool UpdateEnabled
        {
            get => _updateEnabled;
            set
            {
                _updateEnabled = value;
                OnPropertyChanged();
            }
        }

        private string UpdateLink { get; set; }

        private string _currentVersion;

        public string CurrentVersion
        {
            get => _currentVersion;
            set
            {
                _currentVersion = value;
                OnPropertyChanged();
            }
        }

        private string _updateVersion;

        public string UpdateVersion
        {
            get => _updateVersion;
            set
            {
                _updateVersion = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openUpdateDialogCmd;

        public ICommand OpenUpdateDialogCmd
        {
            get
            {
                var cmd = _openUpdateDialogCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _openUpdateDialogCmd = new RelayCommand(
                    param => OpenUpdateDialog()
                );
            }
        }

        private void OpenUpdateDialog()
        {
            try
            {
                if (SkySettings.Mount == MountType.SkyWatcher && SkyServer.IsMountRunning || GSServer.AppCount > 0)
                {
                    OpenDialog(Application.Current.Resources["optDisconnects"].ToString());
                    return;
                }
                using (new WaitCursor())
                {
                    UpdateEnabled = false;
                    DialogContent = new DownloadUpdateDialog();
                    // get html page
                    var w = new WebClient();
                    var s = w.DownloadString(GsUrl);

                    var html = new HTML();
                    var linklist = html.FindLinks(s);
                    foreach (var i in linklist)
                    {
                        if (i.Text.IndexOf("GS Server Version:", StringComparison.OrdinalIgnoreCase) < 0) continue;
                        var ver = i.Text.Split();
                        // valid array
                        if (ver.Length < 3)
                        {
                            OpenDialog(Application.Current.Resources["optNotFound"].ToString());
                            return;
                        }
                        // valid URL?
                        if (!html.IsValidUri(i.Href))
                        {
                            OpenDialog(Application.Current.Resources["optNotFound"].ToString());
                            return;
                        }
                        UpdateVersion = ver[3];
                        UpdateLink = i.Href;

                        //check version numbers
                        UpdateEnabled = true;
                        IsDialogOpen = true;

                        return;
                    }
                    OpenDialog(Application.Current.Resources["optNotAvail"].ToString());
                }
            }
            catch (Exception ex)
            {
                OpenDialog(ex.Message);
            }

        }

        private ICommand _clickDownloadUpdateCmd;

        public ICommand ClickDownloadUpdateCmd
        {
            get
            {
                var cmd = _clickDownloadUpdateCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _clickDownloadUpdateCmd = new RelayCommand(
                    param => ClickDownloadUpdate()
                );
            }
        }

        private void ClickDownloadUpdate()
        {
            try
            {
                var html = new HTML();
                if (!html.IsValidUri(UpdateLink))
                {
                    OpenDialog(Application.Current.Resources["optNotFound"].ToString());
                    return;
                }
                html.OpenUri(UpdateLink);
                IsDialogOpen = false;

                SkyServer.IsMountRunning = false;

                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Downloading Update, Closing Window" };
                MonitorLog.LogToMonitor(monitorItem);

                if (Application.Current.MainWindow != null) Application.Current.MainWindow.Close();
            }
            catch (Exception ex)
            {
                OpenDialog(ex.Message);
            }
        }

        private ICommand _cancelUpdateDialogCmd;

        public ICommand CancelUpdateDialogCmd
        {
            get
            {
                var cmd = _cancelUpdateDialogCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _cancelUpdateDialogCmd = new RelayCommand(
                    param => CancelUpdateDialog()
                );
            }
        }

        private void CancelUpdateDialog()
        {
            IsDialogOpen = false;
        }

        private ICommand _clickDonateCmd;

        public ICommand ClickDonateCmd
        {
            get
            {
                var cmd = _clickDonateCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _clickDonateCmd = new RelayCommand(
                    param => ClickDonate()
                );
            }
        }

        private void ClickDonate()
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://www.greenswamp.org/?page_id=566"));
            }
            catch (Exception ex)
            {
                OpenDialog(ex.Message);
            }
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
        ~SettingsVM()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }

        // The bulk of the clean-up code is implemented in Dispose(bool)
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _mainWindowVm?.Dispose();
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
