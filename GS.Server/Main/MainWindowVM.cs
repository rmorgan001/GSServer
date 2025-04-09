/* Copyright(C) 2019-2025 Rob Morgan (robert.morgan.e@gmail.com)

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

using GS.Server.Controls.Dialogs;
using GS.Server.Focuser;
using GS.Server.GamePad;
using GS.Server.Helpers;
using GS.Server.Model3D;
using GS.Server.Notes;
using GS.Server.Plot;
using GS.Server.PoleLocator;
using GS.Server.Pulses;
using GS.Server.Settings;
using GS.Server.SkyTelescope;
using GS.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GS.Shared.Command;
using GS.Server.Pec;
using GS.Server.Snap;
using GS.Server.Alignment;
using MaterialDesignColors;
using System.Threading.Tasks;
using ASCOM.DeviceInterface;

namespace GS.Server.Main
{
    public sealed class MainWindowVm : ObservableObject, IDisposable
    {
        #region Fields

        private IPageVM _currentPageViewModel;
        private List<IPageVM> _pageViewModels;
        private SkyTelescopeVm _skyTelescopeVm;
        private FocuserVM _focuserVm;
        private NotesVM _notesVm;
        private SettingsVm _settingsVm;
        private GamePadVM _gamePadVm;
        private Model3DVM _model3dVm;
        private PlotVm _plotVm;
        private PoleLocatorVM _poleLocatorVm;
        private PulsesVM _pulsesVm;
        private PecVM _pecVm;
        private SnapVM _snapVm;
        private AlignmentVM _alignmentVm;
        public static MainWindowVm MainWindow1Vm;
        private double _tempHeight = 510;
        private double _tempWidth = 850;
        private WindowState _tempWindowState = WindowState.Normal;
        private const double MinHeight = 100;
        private const double MinWidth = 200;

        #endregion

        public MainWindowVm()
        {
            try
            {
                using (new WaitCursor())
                {
                    //setup property info from the GSServer
                    GSServer.StaticPropertyChanged += PropertyChangedServer;
                    SkySettings.StaticPropertyChanged += PropertyChangedSettings;
                    Settings.Settings.StaticPropertyChanged += PropertyChangedSettingsSettings;
                    Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                    var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{Assembly.GetExecutingAssembly()}" };
                    MonitorLog.LogToMonitor(monitorItem);

                    monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Loading MainWindowVM" };
                    MonitorLog.LogToMonitor(monitorItem);

                    AppCount = GSServer.AppCount;
                    Settings.Settings.Load();
                    WindowStates = Settings.Settings.StartMinimized ? WindowState.Minimized : WindowState.Normal;
                    //Settings.Settings.WindowState = Settings.Settings.StartMinimized ? WindowState.Minimized : WindowState.Normal;

                    MountType = SkySettings.Mount;

                    MainWindow1Vm = this;

                    // Sets up the Tab menu items
                    UpdateTabViewModel("SkyWatcher");
                    UpdateTabViewModel("Focuser");
                    UpdateTabViewModel("Charts");
                    UpdateTabViewModel("Notes");
                    UpdateTabViewModel("Settings");
                    UpdateTabViewModel("GamePad");
                    UpdateTabViewModel("Model3D");
                    UpdateTabViewModel("Plot");
                    UpdateTabViewModel("PoleLocator");
                    UpdateTabViewModel("Pulses");
                    UpdateTabViewModel("Pec");
                    UpdateTabViewModel("Snap");
                    UpdateTabViewModel("Alignment");

                    // Set starting page
                    CurrentPageViewModel = PageViewModels[0];
                    SkyWatcherVmRadio = true;

                    TopMost = Properties.Server.Default.StartOnTop || SkyServer.OpenSetupDialog;
                }

            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                throw;
            }
        }

        #region Model

        private void PropertyChangedServer(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "AppCount":
                    AppCount = GSServer.AppCount;
                    break;
            }
        }

        private void PropertyChangedSettings(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Mount":
                    MountType = SkySettings.Mount;
                    break;
            }
        }

        private void PropertyChangedSettingsSettings(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "AccentColor":
                    MountType = SkySettings.Mount;
                    break;
            }
        }

        #endregion

        #region Tab Control

        //to add a page: add button to tab bar view, add the property below, add to properties server settings ,add property and checkbox to settingsVM and View

        public List<IPageVM> PageViewModels
        {
            get
            {
                var vms = _pageViewModels;
                if (vms != null)
                {
                    return vms;
                }

                return (_pageViewModels = new List<IPageVM>());
            }
        }

        public void UpdateTabViewModel(string name)
        {
            switch (name)
            {
                case "Focuser":
                    if (Settings.Settings.Focuser)
                    {
                        if (!PageViewModels.Contains(_focuserVm))
                        {
                            _focuserVm = new FocuserVM();
                            PageViewModels.Add(_focuserVm);
                        }
                        FocuserRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_focuserVm))
                        {
                            PageViewModels.Remove(_focuserVm);
                        }
                        FocuserRadioVisible = false;
                    }
                    break;
                case "Notes":
                    if (Settings.Settings.Notes)
                    {
                        if (!PageViewModels.Contains(_notesVm))
                        {
                            _notesVm = new NotesVM();
                            PageViewModels.Add(_notesVm);
                        }
                        NotesRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_notesVm))
                        {
                            PageViewModels.Remove(_notesVm);
                        }
                        NotesRadioVisible = false;
                    }
                    break;
                case "SkyWatcher":
                    if (Settings.Settings.SkyWatcher)
                    {
                        if (!PageViewModels.Contains(_skyTelescopeVm))
                        {
                            _skyTelescopeVm = new SkyTelescopeVm();
                            PageViewModels.Add(_skyTelescopeVm);
                        }
                        SkyWatcherRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_skyTelescopeVm))
                        {
                            PageViewModels.Remove(_skyTelescopeVm);
                        }
                        SkyWatcherRadioVisible = false;
                    }
                    break;
                case "GamePad":
                    if (Settings.Settings.GamePad)
                    {
                        if (!PageViewModels.Contains(_gamePadVm))
                        {
                            _gamePadVm = new GamePadVM();
                            PageViewModels.Add(_gamePadVm);
                        }
                        GamePadRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_gamePadVm))
                        {
                            PageViewModels.Remove(_gamePadVm);
                        }
                        GamePadRadioVisible = false;
                    }
                    break;
                case "Settings":
                    _settingsVm = new SettingsVm();
                    PageViewModels.Add(_settingsVm);
                    SettingsRadioVisible = true;
                    break;
                case "Model3D":
                    if (Settings.Settings.Model3D)
                    {
                        if (!PageViewModels.Contains(_model3dVm))
                        {
                            _model3dVm = new Model3DVM();
                            PageViewModels.Add(_model3dVm);
                        }
                        Model3DRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_model3dVm))
                        {
                            PageViewModels.Remove(_model3dVm);
                        }
                        Model3DRadioVisible = false;
                    }
                    break;
                case "Plot":
                    if (Settings.Settings.Plot)
                    {
                        if (!PageViewModels.Contains(_plotVm))
                        {
                            _plotVm = new PlotVm();
                            PageViewModels.Add(_plotVm);
                        }
                        PlotRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_plotVm))
                        {
                            PageViewModels.Remove(_plotVm);
                        }
                        PlotRadioVisible = false;
                    }
                    break;
                case "PoleLocator":
                    if (Settings.Settings.PoleLocator)
                    {
                        if (!PageViewModels.Contains(_poleLocatorVm))
                        {
                            _poleLocatorVm = new PoleLocatorVM();
                            PageViewModels.Add(_poleLocatorVm);
                        }
                        PoleLocatorRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_poleLocatorVm))
                        {
                            PageViewModels.Remove(_poleLocatorVm);
                        }
                        PoleLocatorRadioVisible = false;
                    }
                    break;
                case "Pulses":
                    if (Settings.Settings.Pulses)
                    {
                        if (!PageViewModels.Contains(_pulsesVm))
                        {
                            _pulsesVm = new PulsesVM();
                            PageViewModels.Add(_pulsesVm);
                        }
                        PulsesRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_pulsesVm))
                        {
                            PageViewModels.Remove(_pulsesVm);
                        }
                        PulsesRadioVisible = false;
                    }
                    break;
                case "Pec":
                    //if (Settings.Settings.Pec)
                    if (SkyServer.PecShow && Settings.Settings.Pec)
                    {
                        if (!PageViewModels.Contains(_pecVm))
                        {
                            _pecVm = new PecVM();
                            PageViewModels.Add(_pecVm);
                        }
                        PecRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_pecVm))
                        {
                            PageViewModels.Remove(_pecVm);
                        }
                        PecRadioVisible = false;
                    }
                    break;
                case "Snap":
                    if (Settings.Settings.Snap)
                    {
                        if (!PageViewModels.Contains(_snapVm))
                        {
                            _snapVm = new SnapVM();
                            PageViewModels.Add(_snapVm);
                        }
                        SnapRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_snapVm))
                        {
                            PageViewModels.Remove(_snapVm);
                        }
                        SnapRadioVisible = false;
                    }
                    break;
                case "Alignment":
                    if (SkySettings.AlignmentMode != AlignmentModes.algAltAz && Settings.Settings.AlignmentTabVisible)
                    {
                        if (!PageViewModels.Contains(_alignmentVm))
                        {
                            _alignmentVm = new AlignmentVM();
                            _alignmentVm.PropertyChanged += AlignmentVM_PropertyChanged;
                            PageViewModels.Add(_alignmentVm);
                        }
                        AlignmentRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_alignmentVm))
                        {
                            PageViewModels.Remove(_alignmentVm);
                        }

                        AlignmentRadioVisible = false;
                    }
                    break;
            }
        }

        private void AlignmentVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "AlertBadge")
            {
                OnPropertyChanged(nameof(this.AlignmentAlertBadge));
            }
        }

        public IPageVM CurrentPageViewModel
        {
            get => _currentPageViewModel;
            private set
            {
                using (new WaitCursor())
                {
                    if (_currentPageViewModel == value) return;
                    _currentPageViewModel = value;
                    SkyServer.SelectedTab = value;
                    OnPropertyChanged();
                }
            }
        }

        private void ChangeViewModel(IPageVM viewModel)
        {
            if (!PageViewModels.Contains(viewModel))
                PageViewModels.Add(viewModel);

            CurrentPageViewModel = PageViewModels
                .FirstOrDefault(vm => vm == viewModel);

            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{viewModel}" };
            MonitorLog.LogToMonitor(monitorItem);
        }

        #endregion

        #region Radio buttons

        private bool _settingsVmRadio;
        public bool SettingsVmRadio
        {
            get => _settingsVmRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_settingsVmRadio == value) return;
                    _settingsVmRadio = value;
                    if (value) ChangeViewModel(_settingsVm);
                    OnPropertyChanged();
                }
            }
        }

        private bool _settingsRadioVisible;
        public bool SettingsRadioVisible
        {
            get => _settingsRadioVisible;
            set
            {
                if (_settingsRadioVisible == value) return;
                _settingsRadioVisible = value;
                OnPropertyChanged();
            }
        }

        private bool _skyWatcherVmRadio;
        public bool SkyWatcherVmRadio
        {
            get => _skyWatcherVmRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_skyWatcherVmRadio == value) return;
                    _skyWatcherVmRadio = value;
                    if (value) ChangeViewModel(_skyTelescopeVm);
                    OnPropertyChanged();
                }
            }
        }

        private bool _skyWatcherRadioVisible;
        public bool SkyWatcherRadioVisible
        {
            get => _skyWatcherRadioVisible;
            set
            {
                if (_skyWatcherRadioVisible == value) return;
                _skyWatcherRadioVisible = value;
                OnPropertyChanged();
            }
        }

        private bool _notesVmRadio;
        public bool NotesVmRadioRadio
        {
            get => _notesVmRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_notesVmRadio == value) return;
                    _notesVmRadio = value;
                    if (value) ChangeViewModel(_notesVm);
                    OnPropertyChanged();
                }
            }
        }

        private bool _notesRadioVisible;
        public bool NotesRadioVisible
        {
            get => _notesRadioVisible;
            set
            {
                if (_notesRadioVisible == value) return;
                _notesRadioVisible = value;
                OnPropertyChanged();
            }
        }

        private bool _focuserVmRadio;
        public bool FocuserVmRadioRadio
        {
            get => _focuserVmRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_focuserVmRadio == value) return;
                    _focuserVmRadio = value;
                    if (value) ChangeViewModel(_focuserVm);
                    OnPropertyChanged();
                }
            }
        }

        private bool _focuserRadioVisible;
        public bool FocuserRadioVisible
        {
            get => _focuserRadioVisible;
            set
            {
                if (_focuserRadioVisible == value) return;
                _focuserRadioVisible = value;
                OnPropertyChanged();
            }
        }

        private bool _gamePadVmRadio;
        public bool GamePadVmRadioRadio
        {
            get => _gamePadVmRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_gamePadVmRadio == value) return;
                    _gamePadVmRadio = value;
                    if (value) ChangeViewModel(_gamePadVm);
                    OnPropertyChanged();
                }
            }
        }

        private bool _gamePadRadioVisible;
        public bool GamePadRadioVisible
        {
            get => _gamePadRadioVisible;
            set
            {
                if (_gamePadRadioVisible == value) return;
                _gamePadRadioVisible = value;
                OnPropertyChanged();
            }
        }

        private bool _model3dVmRadio;
        public bool Model3DvmRadioRadio
        {
            get => _model3dVmRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_model3dVmRadio == value) return;
                    _model3dVmRadio = value;
                    if (value) ChangeViewModel(_model3dVm);
                    OnPropertyChanged();
                }
            }
        }

        private bool _model3dRadioVisible;
        public bool Model3DRadioVisible
        {
            get => _model3dRadioVisible;
            set
            {
                if (_model3dRadioVisible == value) return;
                _model3dRadioVisible = value;
                OnPropertyChanged();
            }
        }

        private bool _plotVmRadio;
        public bool PlotVmRadioRadio
        {
            get => _plotVmRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_plotVmRadio == value) return;
                    _plotVmRadio = value;
                    if (value) ChangeViewModel(_plotVm);
                    OnPropertyChanged();
                }
            }
        }

        private bool _plotRadioVisible;
        public bool PlotRadioVisible
        {
            get => _plotRadioVisible;
            set
            {
                if (_plotRadioVisible == value) return;
                _plotRadioVisible = value;
                OnPropertyChanged();
            }
        }

        private bool _poleLocatorVmRadio;
        public bool PoleLocatorVmRadioRadio
        {
            get => _poleLocatorVmRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_poleLocatorVmRadio == value) return;
                    _poleLocatorVmRadio = value;
                    if (value) ChangeViewModel(_poleLocatorVm);
                    OnPropertyChanged();
                }
            }
        }

        private bool _poleLocatorRadioVisible;
        public bool PoleLocatorRadioVisible
        {
            get => _poleLocatorRadioVisible;
            set
            {
                if (_poleLocatorRadioVisible == value) return;
                _poleLocatorRadioVisible = value;
                OnPropertyChanged();
            }
        }

        private bool _pulsesVmRadio;
        public bool PulsesVmRadioRadio
        {
            get => _pulsesVmRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_pulsesVmRadio == value) return;
                    _pulsesVmRadio = value;
                    if (value) ChangeViewModel(_pulsesVm);
                    OnPropertyChanged();
                }
            }
        }

        private bool _pulsesRadioVisible;
        public bool PulsesRadioVisible
        {
            get => _pulsesRadioVisible;
            set
            {
                if (_pulsesRadioVisible == value) return;
                _pulsesRadioVisible = value;
                OnPropertyChanged();
            }
        }

        private bool _pecVmRadio;
        public bool PecVmRadioRadio
        {
            get => _pecVmRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_pecVmRadio == value) return;
                    _pecVmRadio = value;
                    if (value) ChangeViewModel(_pecVm);
                    OnPropertyChanged();
                }
            }
        }

        private bool _pecRadioVisible;
        public bool PecRadioVisible
        {
            get => _pecRadioVisible;
            set
            {
                if (_pecRadioVisible == value) return;
                _pecRadioVisible = value;
                OnPropertyChanged();
            }
        }

        private bool _snapVmRadio;
        public bool SnapVmRadioRadio
        {
            get => _snapVmRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_snapVmRadio == value) return;
                    _snapVmRadio = value;
                    if (value) ChangeViewModel(_snapVm);
                    OnPropertyChanged();
                }
            }
        }

        private bool _snapRadioVisible;
        public bool SnapRadioVisible
        {
            get => _snapRadioVisible;
            set
            {
                if (_snapRadioVisible == value) return;
                _snapRadioVisible = value;
                OnPropertyChanged();
            }
        }

        private bool _alignmentVmRadio;
        public bool AlignmentVmRadioRadio
        {
            get => _alignmentVmRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_alignmentVmRadio == value) return;
                    _alignmentVmRadio = value;
                    if (value) ChangeViewModel(_alignmentVm);
                    OnPropertyChanged();
                }
            }
        }

        public string AlignmentAlertBadge => _alignmentVm?.AlertBadge;

        private bool _alignmentRadioVisible;
        public bool AlignmentRadioVisible
        {
            get => _alignmentRadioVisible;
            set
            {
                if (_alignmentRadioVisible == value) return;
                _alignmentRadioVisible = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Window Info

        private string _version;
        public string Version
        {
            get => _version;
            set
            {
                _version = value;
                OnPropertyChanged();
            }
        }

        private int _appCount;
        public int AppCount
        {
            get => _appCount;
            set
            {
                _appCount = value;
                OnPropertyChanged();
            }
        }

        private MountType _mountType;
        public MountType MountType
        {
            get => _mountType;
            set
            {
                if (_mountType == value)
                {
                    return;
                }

                _mountType = value;
                OnPropertyChanged();
                // ReSharper disable once ExplicitCallerInfoArgument
                OnPropertyChanged("MountTypeColor");
            }
        }
        
        public Brush MountTypeColor
        {
            get
            {
                var accentBrush = new SolidColorBrush(Colors.Transparent);
                if (MountType == MountType.Simulator)
                {
                    if (!string.IsNullOrEmpty(Settings.Settings.AccentColor))
                    {
                        var swatches = new SwatchesProvider().Swatches;
                        foreach (var swatch in swatches)
                        {
                            if (swatch.Name != Settings.Settings.AccentColor) continue;
                            var converter = new BrushConverter();
                            accentBrush = (SolidColorBrush)converter.ConvertFromString(swatch.ExemplarHue.Color.ToString());
                        }
                    }

                }

                return accentBrush;
            }
        }

        private bool _topMost;
        public bool TopMost
        {
            get => _topMost;
            set
            {
                if (_topMost == value) return;
                _topMost = value;
                OnPropertyChanged();
            }
        }

        private ICommand _minimizeWindowCommand;
        public ICommand MinimizeWindowCommand
        {
            get
            {
                var command = _minimizeWindowCommand;
                if (command != null)
                {
                    return command;
                }

                return _minimizeWindowCommand = new RelayCommand(
                    param => MinimizeWindow()
                );
            }
        }
        private void MinimizeWindow()
        {
            WindowStates = WindowState.Minimized;
            Memory.FlushMemory();
        }

        private ICommand _maximizeWindowCommand;
        public ICommand MaximizeWindowCommand
        {
            get
            {
                var command = _maximizeWindowCommand;
                if (command != null)
                {
                    return command;
                }

                return _maximizeWindowCommand = new RelayCommand(
                    param => MaximizeWindow()
                );
            }
        }
        private void MaximizeWindow()
        {
            WindowStates = WindowStates != WindowState.Maximized ? WindowState.Maximized : WindowState.Normal;
        }

        private ICommand _normalWindowCommand;
        public ICommand NormalWindowCommand
        {
            get
            {
                var command = _normalWindowCommand;
                if (command != null)
                {
                    return command;
                }

                return _normalWindowCommand = new RelayCommand(
                    param => NormalWindow()
                );
            }
        }
        private void NormalWindow()
        {
            WindowStates = WindowState.Normal;
        }

        public WindowState WindowStates
        {
            get => Settings.Settings.WindowState;
            set
            {
                Settings.Settings.WindowState = value;
                OnPropertyChanged();
            }
        }

        public double WindowHeight
        {
            get => Settings.Settings.WindowHeight;
            set
            {
                if (value <= MinHeight){return;}
                Settings.Settings.WindowHeight = value;
                OnPropertyChanged();
            }
        }

        public double WindowWidth
        {
            get => Settings.Settings.WindowWidth;
            set
            {
                if (value <= MinWidth) { return; }
                Settings.Settings.WindowWidth = value;
                OnPropertyChanged();
            }
        }

        public double WindowLeft
        {
            get => Settings.Settings.WindowLeft;
            set
            {
                Settings.Settings.WindowLeft = value;
                OnPropertyChanged();
            }
        }

        public double WindowTop
        {
            get => Settings.Settings.WindowTop;
            set
            {
                Settings.Settings.WindowTop = value;
                OnPropertyChanged();
            }
        }

        private ICommand _resetWindowCommand;
        public ICommand ResetWindowCommand
        {
            get
            {
                var command = _resetWindowCommand;
                if (command != null)
                {
                    return command;
                }

                return _resetWindowCommand = new RelayCommand(
                    param => ResetWindow()
                );
            }
        }
        private void ResetWindow()
        {
            var h = WindowHeight;
            var w = WindowWidth;

            if (Math.Abs(h - 510) > 1 || Math.Abs(w - 850) > 1)
            {
                _tempHeight = WindowHeight;
                _tempWidth = WindowWidth;
                _tempWindowState = WindowStates;

                WindowStates = WindowState.Normal;
                WindowHeight = 510;
                WindowWidth = 850;
            }
            else
            {
                WindowStates = _tempWindowState;
                WindowHeight = _tempHeight;
                WindowWidth = _tempWidth;
            }

        }
        #endregion

        #region Close Dialog
        private void CloseServer()
        {
            Task.Run(()=> _focuserVm?.Disconnect());
            SkyServer.ShutdownServer();
        }

        private bool _isCloseDialogOpen;
        public bool IsCloseDialogOpen
        {
            get => _isCloseDialogOpen;
            set
            {
                if (_isCloseDialogOpen == value) return;
                _isCloseDialogOpen = value;
                OnPropertyChanged();
            }
        }

        private object _closeContent;
        public object CloseContent
        {
            get => _closeContent;
            set
            {
                if (_closeContent == value) return;
                _closeContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openCloseDialogCommand;
        public ICommand OpenCloseDialogCommand
        {
            get
            {
                var command = _openCloseDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _openCloseDialogCommand = new RelayCommand(
                    param => OpenCloseDialog()
                );
            }
        }
        private void OpenCloseDialog()
        {
            if (GSServer.AppCount < 1)
            {
                CloseServer();
            }
            else
            {
                SkyServer.OpenSetupDialog = false;
                SkyServer.OpenSetupDialogFinished = true;
                CloseContent = new CloseDialog();
                Tasks.DelayHandler(1000);
                IsCloseDialogOpen = true;
            }
        }

        private ICommand _acceptCloseDialogCommand;
        public ICommand AcceptCloseDialogCommand
        {
            get
            {
                var command = _acceptCloseDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _acceptCloseDialogCommand = new RelayCommand(
                    param => AcceptCloseDialog()
                );
            }
        }
        private void AcceptCloseDialog()
        {
            IsCloseDialogOpen = false;
            CloseServer();
        }

        private ICommand _cancelCloseDialogCommand;
        public ICommand CancelCloseDialogCommand
        {
            get
            {
                var command = _cancelCloseDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _cancelCloseDialogCommand = new RelayCommand(
                    param => CancelCloseDialog()
                );
            }
        }

        private void CancelCloseDialog()
        {
            IsCloseDialogOpen = false;
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
        ~MainWindowVm()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        // The bulk of the clean-up code is implemented in Dispose(bool)
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _skyTelescopeVm?.Dispose();
                _focuserVm?.Dispose();
                _notesVm?.Dispose();
                _settingsVm?.Dispose();
                _gamePadVm?.Dispose();
                _model3dVm?.Dispose();
                _pulsesVm?.Dispose();
                MainWindow1Vm?.Dispose();
                _pecVm?.Dispose();
                _plotVm?.Dispose();
                _poleLocatorVm?.Dispose();
                _alignmentVm?.Dispose();
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
