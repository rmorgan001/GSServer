using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using GS.Principles;
using GS.Server.Controls.Dialogs;
using GS.Server.Helpers;
using GS.Server.Pulses;
using GS.Server.SkyTelescope;
using GS.Shared;
using GS.Shared.Command;
using MaterialDesignThemes.Wpf;
using NativeMethods = GS.Server.Helpers.NativeMethods;

namespace GS.Server.Windows
{
    public class HcPulseGuidesVm : ObservableObject, IDisposable
    {
        public HcPulseGuidesVm()
        {
            try
            {
                using (new WaitCursor())
                {
                    var monitorItem = new MonitorEntry
                        { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Opening Hand Control Window" };
                    MonitorLog.LogToMonitor(monitorItem);

                    SkyServer.StaticPropertyChanged += PropertyChangedSkyServer;
                    SkySettings.StaticPropertyChanged += PropertyChangedSkySettings;

                    Title = Application.Current.Resources["hcHc"].ToString();
                    ScreenEnabled = true;
                    TopMost = true;
                    HcPulseGuides = SkySettings.HcPulseGuides;
                }

            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                throw;
            }
        }

        #region ViewModel

        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Enable or Disable screen items if connected
        /// </summary>
        private bool _screenEnabled;
        public bool ScreenEnabled
        {
            get => _screenEnabled;
            set
            {
                if (_screenEnabled == value) return;
                _screenEnabled = value;
                OnPropertyChanged();
            }
        }


        private List<HcPulseGuide> _hcPulseGuides;
        public List<HcPulseGuide> HcPulseGuides
        {
            get => _hcPulseGuides;
            set
            {
                _hcPulseGuides = value;
                OnPropertyChanged();
            }
        }


        /// <summary>
        /// Property changes from the server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertyChangedSkyServer(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ThreadContext.BeginInvokeOnUiThread(
             delegate
             {
                 switch (e.PropertyName)
                 {
                     case "IsMountRunning":
                         ScreenEnabled = SkyServer.IsMountRunning;
                         break;
                 }
             });
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
                    Message = $"{ex.Message}|{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private void PropertyChangedSkySettings(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ThreadContext.BeginInvokeOnUiThread(
             delegate
             {
                 switch (e.PropertyName)
                 {
                     case "HcPulseGuides":
                         HcPulseGuides = SkySettings.HcPulseGuides;
                         break;
                 }
             });
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
                    Message = $"{ex.Message}|{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        #endregion

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

        #region Window Info

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

        private ICommand _openCloseWindowCmd;
        public ICommand OpenCloseWindowCmd
        {
            get
            {
                var cmd = _openCloseWindowCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _openCloseWindowCmd = new RelayCommand(
                    param => CloseWindow()
                );
            }
        }
        private void CloseWindow()
        {
            var win = Application.Current.Windows.OfType<HcPulseGuidesV>().FirstOrDefault();
            win?.Close();
        }

        private WindowState _windowState;
        public WindowState WindowStates
        {
            get => _windowState;
            set
            {
                _windowState = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            //_ctsSpiral?.Cancel();
            //_ctsSpiral?.Dispose();
            // GC.SuppressFinalize(this);
        }
        // NOTE: Leave out the finalizer altogether if this class doesn't
        // own unmanaged resources itself, but leave the other methods
        // exactly as they are.
        ~HcPulseGuidesVm()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        // The bulk of the clean-up code is implemented in Dispose(bool)
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                SkyServer.StaticPropertyChanged -= PropertyChangedSkyServer;
            }
            NativeMethods.ClipCursor(IntPtr.Zero);
        }
        #endregion
    }
}
