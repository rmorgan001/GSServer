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
using GS.Server.Domain;
using GS.Server.Focuser;
using GS.Server.Gamepad;
using GS.Server.Helpers;
using GS.Server.Model3D;
using GS.Server.Notes;
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
using GS.Server.Pulses;

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
        private GamepadVM _gamepadVM;
        private Model3DVM _model3dVM;
        private PulsesVM _pulsesVM;
        public static MainWindowVM _mainWindowVm;

        private double _tempHeight = 510;
        private double _tempWidth = 850;
        private WindowState _tempWindowState = WindowState.Normal;

        #endregion

        #region Main Page

        public MainWindowVM()
        {
            try
            {
                using (new WaitCursor())
                {
                    //Load language resource file
                    var languages = new Languages();
                    languages.SetLanguageDictionary();

                    //setup property info from the GSServer
                    GSServer.StaticPropertyChanged += PropertyChangedServer;
                    SkySettings.StaticPropertyChanged += PropertyChangedSettings;
                    Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                    var monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{Assembly.GetExecutingAssembly()}" };
                    MonitorLog.LogToMonitor(monitorItem);

                    monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Loading MainWindowVM" };
                    MonitorLog.LogToMonitor(monitorItem);

                    AppCount = GSServer.AppCount;
                    MountType = SkySettings.Mount;
                    Settings.Settings.Load();
                    if (Settings.Settings.StartMinimized)
                        Settings.Settings.Windowstate = WindowState.Minimized;

                    _mainWindowVm = this;

                    // Sets up the Tab menu items
                    UpdateTabViewModel("SkyWatcher");
                    UpdateTabViewModel("Focuser");
                    UpdateTabViewModel("Charts");
                    UpdateTabViewModel("Notes");
                    UpdateTabViewModel("Settings");
                    UpdateTabViewModel("Gamepad");
                    UpdateTabViewModel("Model3D");
                    UpdateTabViewModel("Pulses");

                    // Set starting page
                    CurrentPageViewModel = PageViewModels[0];
                    SkyWatcherVMRadio = true;

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

        private int _appcount;
        public int AppCount
        {
            get => _appcount;
            set
            {
                _appcount = value;
                OnPropertyChanged();
            }
        }

        private MountType _mounttype;
        public MountType MountType
        {
            get => _mounttype;
            set
            {
                _mounttype = value;
                OnPropertyChanged();
            }
        }

        private ICommand _minimizeWindowCommand;
        public ICommand MinimizeWindowCommand
        {
            get
            {
                return _minimizeWindowCommand ?? (_minimizeWindowCommand = new RelayCommand(
                           param => MinimizeWindow()
                       ));
            }
        }
        private void MinimizeWindow()
        {
            Windowstate = WindowState.Minimized;
        }

        private ICommand _maxmizeWindowCommand;
        public ICommand MaximizeWindowCommand
        {
            get
            {
                return _maxmizeWindowCommand ?? (_maxmizeWindowCommand = new RelayCommand(
                           param => MaxmizeWindow()
                       ));
            }
        }
        private void MaxmizeWindow()
        {
            Windowstate = Windowstate != WindowState.Maximized ? WindowState.Maximized : WindowState.Normal;
        }

        private ICommand _normalWindowCommand;
        public ICommand NormalWindowCommand
        {
            get
            {
                return _normalWindowCommand ?? (_normalWindowCommand = new RelayCommand(
                           param => NormalWindow()
                       ));
            }
        }
        private void NormalWindow()
        {
            Windowstate = WindowState.Normal;
        }

        public void Dispose()
        {
            _skyTelescopeVM?.Dispose();
            // _notesV?.Dispose();
        }

        #endregion

        #region Tab Control

        //to add a page: add button to tabbar view, add the property below, add to properties server settings ,add property and checkbox to settingsVM and View

        public List<IPageVM> PageViewModels => _pageViewModels ?? (_pageViewModels = new List<IPageVM>());

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
                        FocuserRadioVisable = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_focuserVM))
                        {
                            PageViewModels.Remove(_focuserVM);
                        }
                        FocuserRadioVisable = false;
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
                        NotesRadioVisable = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_notesVM))
                        {
                            PageViewModels.Remove(_notesVM);
                        }
                        NotesRadioVisable = false;
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
                        SkyWatcherRadioVisable = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_skyTelescopeVM))
                        {
                            PageViewModels.Remove(_skyTelescopeVM);
                        }
                        SkyWatcherRadioVisable = false;
                    }
                    break;
                case "Gamepad":
                    if (Settings.Settings.Gamepad)
                    {
                        if (!PageViewModels.Contains(_gamepadVM))
                        {
                            _gamepadVM = new GamepadVM();
                            PageViewModels.Add(_gamepadVM);
                        }
                        GamepadRadioVisable = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_gamepadVM))
                        {
                            PageViewModels.Remove(_gamepadVM);
                        }
                        GamepadRadioVisable = false;
                    }
                    break;
                case "Settings":
                    _settingsVM = new SettingsVM();
                    PageViewModels.Add(_settingsVM);
                    SettingsRadioVisable = true;
                    break;
                case "Model3D":
                    if (Settings.Settings.Model3D)
                    {
                        if (!PageViewModels.Contains(_model3dVM))
                        {
                            _model3dVM = new Model3DVM();
                            PageViewModels.Add(_model3dVM);
                        }
                        Model3DRadioVisable = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_model3dVM))
                        {
                            PageViewModels.Remove(_model3dVM);
                        }
                        Model3DRadioVisable = false;
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
                        PulsesRadioVisable = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_pulsesVM))
                        {
                            PageViewModels.Remove(_pulsesVM);
                        }
                        PulsesRadioVisable = false;
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
                    Memory.FlushMemory();
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

        private bool _settingsRadioVisable;
        public bool SettingsRadioVisable
        {
            get => _settingsRadioVisable;
            set
            {
                if (_settingsRadioVisable == value) return;
                _settingsRadioVisable = value;
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

        private bool _skyWatcherRadioVisable;
        public bool SkyWatcherRadioVisable
        {
            get => _skyWatcherRadioVisable;
            set
            {
                if (_skyWatcherRadioVisable == value) return;
                _skyWatcherRadioVisable = value;
                OnPropertyChanged();
            }
        }

        private bool __chartingRadioVisable;
        public bool ChartingRadioVisable
        {
            get => __chartingRadioVisable;
            set
            {
                if (__chartingRadioVisable == value) return;
                __chartingRadioVisable = value;
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

        private bool _notesRadioVisable;
        public bool NotesRadioVisable
        {
            get => _notesRadioVisable;
            set
            {
                if (_notesRadioVisable == value) return;
                _notesRadioVisable = value;
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

        private bool _focuserRadioVisable;
        public bool FocuserRadioVisable
        {
            get => _focuserRadioVisable;
            set
            {
                if (_focuserRadioVisable == value) return;
                _focuserRadioVisable = value;
                OnPropertyChanged();
            }
        }

        private bool _gamepadVMRadio;
        public bool GamepadVMRadioRadio
        {
            get => _gamepadVMRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_gamepadVMRadio == value) return;
                    _gamepadVMRadio = value;
                    if (value) ChangeViewModel(_gamepadVM);
                    OnPropertyChanged();
                }
            }
        }

        private bool _gamepadRadioVisable;
        public bool GamepadRadioVisable
        {
            get => _gamepadRadioVisable;
            set
            {
                if (_gamepadRadioVisable == value) return;
                _gamepadRadioVisable = value;
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

        private bool _model3dRadioVisable;
        public bool Model3DRadioVisable
        {
            get => _model3dRadioVisable;
            set
            {
                if (_model3dRadioVisable == value) return;
                _model3dRadioVisable = value;
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

        private bool _pulsesRadioVisable;
        public bool PulsesRadioVisable
        {
            get => _pulsesRadioVisable;
            set
            {
                if (_pulsesRadioVisable == value) return;
                _pulsesRadioVisable = value;
                OnPropertyChanged();
            }
        }

        public WindowState Windowstate
        {
            get => Settings.Settings.Windowstate;
            set
            {
                Settings.Settings.Windowstate = value;
                OnPropertyChanged();
            }
        }

        public double Windowheight
        {
            get => Settings.Settings.Windowheight;
            set
            {
                Settings.Settings.Windowheight = value;
                OnPropertyChanged();
            }
        }

        public double Windowwidth
        {
            get => Settings.Settings.Windowwidth;
            set
            {
                Settings.Settings.Windowwidth = value;
                OnPropertyChanged();
            }
        }

        public double Windowleft
        {
            get => Settings.Settings.Windowleft;
            set
            {
                Settings.Settings.Windowleft = value;
                OnPropertyChanged();
            }
        }

        public double Windowtop
        {
            get => Settings.Settings.Windowtop;
            set
            {
                Settings.Settings.Windowtop = value;
                OnPropertyChanged();
            }
        }

        private ICommand _resetWindowCommand;
        public ICommand ResetWindowCommand
        {
            get
            {
                return _resetWindowCommand ?? (_resetWindowCommand = new RelayCommand(
                           param => ResetWindow()
                       ));
            }
        }
        private void ResetWindow()
        {
            var h = Windowheight;
            var w = Windowwidth;

            if (Math.Abs(h - 510) > 0 || Math.Abs(w - 850) > 0)
            {
                _tempHeight = Windowheight;
                _tempWidth = Windowwidth;
                _tempWindowState = Windowstate;

                Windowstate = WindowState.Normal;
                Windowheight = 510;
                Windowwidth = 850;
            }
            else
            {
                Windowstate = _tempWindowState;
                Windowheight = _tempHeight;
                Windowwidth = _tempWidth;
            }
        }
        #endregion

        #region Close Button and Dialog     

        private void CloseServer()
        {
            SkyServer.IsMountRunning = false;

            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Server, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "MainWindow Closing" };
            MonitorLog.LogToMonitor(monitorItem);

            if (Application.Current.MainWindow != null) Application.Current.MainWindow.Close();
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
                return _openCloseDialogCommand ?? (_openCloseDialogCommand = new RelayCommand(
                           param => OpenCloseDialog()
                       ));
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
                CloseContent = new CloseDialog();
                IsCloseDialogOpen = true;
            }
        }

        private ICommand _acceptCloseDialogCommand;
        public ICommand AcceptCloseDialogCommand
        {
            get
            {
                return _acceptCloseDialogCommand ?? (_acceptCloseDialogCommand = new RelayCommand(
                           param => AcceptCloseDialog()
                       ));
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
                return _cancelCloseDialogCommand ?? (_cancelCloseDialogCommand = new RelayCommand(
                           param => CancelCloseDialog()
                       ));
            }
        }

        private void CancelCloseDialog()
        {
            IsCloseDialogOpen = false;
        }

        #endregion
    }
}
