/* Copyright(C) 2019-2021  Rob Morgan (robert.morgan.e@gmail.com)

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

namespace GS.Server.Main
{
    public sealed class MainWindowVM : ObservableObject, IDisposable
    {
        #region Fields

        private IPageVM _currentPageViewModel;
        private List<IPageVM> _pageViewModels;
        private SkyTelescopeVM _skyTelescopeVM;
        private FocuserVM _focuserVM;
        private NotesVM _notesVM;
        private SettingsVM _settingsVM;
        private GamePadVM _gamePadVM;
        private Model3DVM _model3dVM;
        private PlotVM _plotVM;
        private PoleLocatorVM _poleLocatorVM;
        private PulsesVM _pulsesVM;
        private PecVM _pecVM;
        private SnapVM _snapVM;
        private AlignmentVM _alignmentVM;
        public static MainWindowVM _mainWindowVm;

        private double _tempHeight = 510;
        private double _tempWidth = 850;
        private WindowState _tempWindowState = WindowState.Normal;
        private double MinHeight = 100;
        private double MinWidth = 200;

        #endregion

        public MainWindowVM()
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
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{Assembly.GetExecutingAssembly()}" };
                    MonitorLog.LogToMonitor(monitorItem);

                    monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Loading MainWindowVM" };
                    MonitorLog.LogToMonitor(monitorItem);

                    AppCount = GSServer.AppCount;
                    Settings.Settings.Load();
                    Settings.Settings.WindowState = Settings.Settings.StartMinimized ? WindowState.Minimized : WindowState.Normal;

                    MountType = SkySettings.Mount;

                    _mainWindowVm = this;

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
                    SkyWatcherVMRadio = true;

                    TopMost = Properties.Server.Default.StartOnTop || SkyServer.OpenSetupDialog;
                }

            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
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
                        if (!PageViewModels.Contains(_focuserVM))
                        {
                            _focuserVM = new FocuserVM();
                            PageViewModels.Add(_focuserVM);
                        }
                        FocuserRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_focuserVM))
                        {
                            PageViewModels.Remove(_focuserVM);
                        }
                        FocuserRadioVisible = false;
                    }
                    break;
                case "Notes":
                    if (Settings.Settings.Notes)
                    {
                        if (!PageViewModels.Contains(_notesVM))
                        {
                            _notesVM = new NotesVM();
                            PageViewModels.Add(_notesVM);
                        }
                        NotesRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_notesVM))
                        {
                            PageViewModels.Remove(_notesVM);
                        }
                        NotesRadioVisible = false;
                    }
                    break;
                case "SkyWatcher":
                    if (Settings.Settings.SkyWatcher)
                    {
                        if (!PageViewModels.Contains(_skyTelescopeVM))
                        {
                            _skyTelescopeVM = new SkyTelescopeVM();
                            PageViewModels.Add(_skyTelescopeVM);
                        }
                        SkyWatcherRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_skyTelescopeVM))
                        {
                            PageViewModels.Remove(_skyTelescopeVM);
                        }
                        SkyWatcherRadioVisible = false;
                    }
                    break;
                case "GamePad":
                    if (Settings.Settings.GamePad)
                    {
                        if (!PageViewModels.Contains(_gamePadVM))
                        {
                            _gamePadVM = new GamePadVM();
                            PageViewModels.Add(_gamePadVM);
                        }
                        GamePadRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_gamePadVM))
                        {
                            PageViewModels.Remove(_gamePadVM);
                        }
                        GamePadRadioVisible = false;
                    }
                    break;
                case "Settings":
                    _settingsVM = new SettingsVM();
                    PageViewModels.Add(_settingsVM);
                    SettingsRadioVisible = true;
                    break;
                case "Model3D":
                    if (Settings.Settings.Model3D)
                    {
                        if (!PageViewModels.Contains(_model3dVM))
                        {
                            _model3dVM = new Model3DVM();
                            PageViewModels.Add(_model3dVM);
                        }
                        Model3DRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_model3dVM))
                        {
                            PageViewModels.Remove(_model3dVM);
                        }
                        Model3DRadioVisible = false;
                    }
                    break;
                case "Plot":
                    if (Settings.Settings.Plot)
                    {
                        if (!PageViewModels.Contains(_plotVM))
                        {
                            _plotVM = new PlotVM();
                            PageViewModels.Add(_plotVM);
                        }
                        PlotRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_plotVM))
                        {
                            PageViewModels.Remove(_plotVM);
                        }
                        PlotRadioVisible = false;
                    }
                    break;
                case "PoleLocator":
                    if (Settings.Settings.PoleLocator)
                    {
                        if (!PageViewModels.Contains(_poleLocatorVM))
                        {
                            _poleLocatorVM = new PoleLocatorVM();
                            PageViewModels.Add(_poleLocatorVM);
                        }
                        PoleLocatorRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_poleLocatorVM))
                        {
                            PageViewModels.Remove(_poleLocatorVM);
                        }
                        PoleLocatorRadioVisible = false;
                    }
                    break;
                case "Pulses":
                    if (Settings.Settings.Pulses)
                    {
                        if (!PageViewModels.Contains(_pulsesVM))
                        {
                            _pulsesVM = new PulsesVM();
                            PageViewModels.Add(_pulsesVM);
                        }
                        PulsesRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_pulsesVM))
                        {
                            PageViewModels.Remove(_pulsesVM);
                        }
                        PulsesRadioVisible = false;
                    }
                    break;
                case "Pec":
                    //if (Settings.Settings.Pec)
                    if (SkyServer.PecShow)
                    {
                        if (!PageViewModels.Contains(_pecVM))
                        {
                            _pecVM = new PecVM();
                            PageViewModels.Add(_pecVM);
                        }
                        PecRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_pecVM))
                        {
                            PageViewModels.Remove(_pecVM);
                        }
                        PecRadioVisible = false;
                    }
                    break;
                case "Snap":
                    if (Settings.Settings.Snap)
                    {
                        if (!PageViewModels.Contains(_snapVM))
                        {
                            _snapVM = new SnapVM();
                            PageViewModels.Add(_snapVM);
                        }
                        SnapRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_snapVM))
                        {
                            PageViewModels.Remove(_snapVM);
                        }
                        SnapRadioVisible = false;
                    }
                    break;
                case "Alignment":
                    if (SkyServer.AlignmentShow && Settings.Settings.AlignmentTabVisible)
                    {
                        if (!PageViewModels.Contains(_alignmentVM))
                        {
                            _alignmentVM = new AlignmentVM();
                            PageViewModels.Add(_alignmentVM);
                        }
                        AlignmentRadioVisible = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_alignmentVM))
                        {
                            PageViewModels.Remove(_alignmentVM);
                        }

                        AlignmentRadioVisible = false;
                    }
                    break;
            }
        }

        public IPageVM CurrentPageViewModel
        {
            get => _currentPageViewModel;
            set
            {
                using (new WaitCursor())
                {
                    if (_currentPageViewModel == value) return;
                    _currentPageViewModel = value;
                    //Memory.Collect();
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
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{viewModel}" };
            MonitorLog.LogToMonitor(monitorItem);
        }

        #endregion

        #region Radio buttons

        private bool _settingsVMRadio;
        public bool SettingsVMRadio
        {
            get => _settingsVMRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_settingsVMRadio == value) return;
                    _settingsVMRadio = value;
                    if (value) ChangeViewModel(_settingsVM);
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

        private bool _skyWatcherVMRadio;
        public bool SkyWatcherVMRadio
        {
            get => _skyWatcherVMRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_skyWatcherVMRadio == value) return;
                    _skyWatcherVMRadio = value;
                    if (value) ChangeViewModel(_skyTelescopeVM);
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

        private bool _notesVMRadio;
        public bool NotesVMRadioRadio
        {
            get => _notesVMRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_notesVMRadio == value) return;
                    _notesVMRadio = value;
                    if (value) ChangeViewModel(_notesVM);
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

        private bool _focuserVMRadio;
        public bool FocuserVMRadioRadio
        {
            get => _focuserVMRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_focuserVMRadio == value) return;
                    _focuserVMRadio = value;
                    if (value) ChangeViewModel(_focuserVM);
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

        private bool _gamePadVMRadio;
        public bool GamePadVMRadioRadio
        {
            get => _gamePadVMRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_gamePadVMRadio == value) return;
                    _gamePadVMRadio = value;
                    if (value) ChangeViewModel(_gamePadVM);
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

        private bool _model3dVMRadio;
        public bool Model3DVMRadioRadio
        {
            get => _model3dVMRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_model3dVMRadio == value) return;
                    _model3dVMRadio = value;
                    if (value) ChangeViewModel(_model3dVM);
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

        private bool _plotVMRadio;
        public bool PlotVMRadioRadio
        {
            get => _plotVMRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_plotVMRadio == value) return;
                    _plotVMRadio = value;
                    if (value) ChangeViewModel(_plotVM);
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
        
        private bool _poleLocatorVMRadio;
        public bool PoleLocatorVMRadioRadio
        {
            get => _poleLocatorVMRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_poleLocatorVMRadio == value) return;
                    _poleLocatorVMRadio = value;
                    if (value) ChangeViewModel(_poleLocatorVM);
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

        private bool _pulsesVMRadio;
        public bool PulsesVMRadioRadio
        {
            get => _pulsesVMRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_pulsesVMRadio == value) return;
                    _pulsesVMRadio = value;
                    if (value) ChangeViewModel(_pulsesVM);
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

        private bool _pecVMRadio;
        public bool PecVMRadioRadio
        {
            get => _pecVMRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_pecVMRadio == value) return;
                    _pecVMRadio = value;
                    if (value) ChangeViewModel(_pecVM);
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

        private bool _snapVMRadio;
        public bool SnapVMRadioRadio
        {
            get => _snapVMRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_snapVMRadio == value) return;
                    _snapVMRadio = value;
                    if (value) ChangeViewModel(_snapVM);
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

        private bool _alignmentVMRadio;
        public bool AlignmentVMRadioRadio
        {
            get => _alignmentVMRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_alignmentVMRadio == value) return;
                    _alignmentVMRadio = value;
                    if (value) ChangeViewModel(_alignmentVM);
                    OnPropertyChanged();
                }
            }
        }

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
                _mountType = value;
                var accentbrush = new SolidColorBrush(Colors.Transparent);
                if (value == MountType.Simulator)
                {
                    if (!string.IsNullOrEmpty(Settings.Settings.AccentColor))
                    {
                        var swatches = new SwatchesProvider().Swatches;
                        foreach (var swatch in swatches)
                        {
                            if (swatch.Name != Settings.Settings.AccentColor) continue;
                            var converter = new BrushConverter();
                            accentbrush = (SolidColorBrush)converter.ConvertFromString(swatch.ExemplarHue.Color.ToString());
                        }
                    }

                }
                MountTypeColor = accentbrush;
                OnPropertyChanged();
            }
        }

        private Brush _mountTypeColor;
        public Brush MountTypeColor
        {
            get => _mountTypeColor;
            set
            {
                _mountTypeColor = value;
                OnPropertyChanged();
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

        #region Close 
        private void CloseServer()
        {
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
        ~MainWindowVM()
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
                _focuserVM?.Dispose();
                _notesVM?.Dispose();
                _settingsVM?.Dispose();
                _gamePadVM?.Dispose();
                _model3dVM?.Dispose();
                _pulsesVM?.Dispose();
                _mainWindowVm?.Dispose();
                _pecVM?.Dispose();
                _plotVM?.Dispose();
                _poleLocatorVM?.Dispose();
                _alignmentVM?.Dispose();
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
