/* Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using GS.Server.Domain;
using GS.Server.Focuser;
using GS.Server.Charting;
using GS.Server.Gamepad;
using GS.Server.Helpers;
using GS.Server.Notes;
using GS.Server.Settings;
using GS.Server.SkyTelescope;
using GS.Shared;

namespace GS.Server.Main
{
    public sealed class MainWindowVM : ObservableObject, IDisposable
    {
        #region Fields

        private IPageVM _currentPageViewModel;
        private List<IPageVM> _pageViewModels;
        private SkyTelescopeVM _skyTelescopeVM;
        private ChartingVM _chartingVM;
        private FocuserVM _focuserVM;
        private NotesVM _notesVM;
        private SettingsVM _settingsVM;
        private GamepadVM _gamepadVM;
        public static MainWindowVM _mainWindowVm;

        #endregion

        #region Main Page

        public MainWindowVM()
        {
            try
            {
                using (new WaitCursor())
                {
                    //setup property info from the GSServer
                    GSServer.StaticPropertyChanged += PropertyChangedServer;
                    Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                    var monitorItem = new MonitorEntry
                        { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{Assembly.GetExecutingAssembly()}" };
                    MonitorLog.LogToMonitor(monitorItem);

                    monitorItem = new MonitorEntry
                        { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Loading MainWindowVM" };
                    MonitorLog.LogToMonitor(monitorItem);

                    Settings.Settings.LogSettings();

                    AppCount = GSServer.AppCount;
                    Settings.Settings.Load();
                    if (Properties.Server.Default.StartMinimized)
                        Properties.Server.Default.WindowState = WindowState.Minimized;

                    _mainWindowVm = this;

                    //todo maybe search for installed drivers and load only them
                    // Sets up the Tab menu items
                    UpdateTabViewModel("SkyWatcher");
                    UpdateTabViewModel("Focuser");
                    UpdateTabViewModel("Charts");
                    UpdateTabViewModel("Notes");
                    UpdateTabViewModel("Settings");
                    UpdateTabViewModel("Gamepad");

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
            Properties.Server.Default.WindowState = WindowState.Minimized;
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
            Properties.Server.Default.WindowState = WindowState.Normal;
        }

        public void Dispose()
        {
            _skyTelescopeVM?.Dispose();
            _chartingVM?.Dispose();
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
                    if (Properties.Server.Default.Focuser)
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
                case "Charts":
                    if (Properties.Server.Default.Charting)
                    {
                        if (!PageViewModels.Contains(_chartingVM))
                        {
                            _chartingVM = new ChartingVM();
                            PageViewModels.Add(_chartingVM);
                        }
                        ChartingRadioVisable = true;
                    }
                    else
                    {
                        if (PageViewModels.Contains(_chartingVM))
                        {
                            PageViewModels.Remove(_chartingVM);
                        }
                        ChartingRadioVisable = false;
                    }
                    break;
                case "Notes":
                    if (Properties.Server.Default.Notes)
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
                    if (Properties.Server.Default.SkyWatcher)
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
                    if (Properties.Server.Default.Gamepad)
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

        private bool _chartingVMRadio;
        public bool ChartingVMRadioRadio
        {
            get => _chartingVMRadio;
            set
            {
                using (new WaitCursor())
                {
                    if (_chartingVMRadio == value) return;
                    _chartingVMRadio = value;
                    if (value) ChangeViewModel(_chartingVM);
                    OnPropertyChanged();
                }
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
