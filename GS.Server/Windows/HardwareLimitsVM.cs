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
using ASCOM.DeviceInterface;
using GS.Principles;
using GS.Server.Controls.Dialogs;
using GS.Server.Helpers;
using GS.Server.SkyTelescope;
using GS.Shared;
using GS.Shared.Command;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NativeMethods = GS.Server.Helpers.NativeMethods;

namespace GS.Server.Windows
{
    public class HardwareLimitsVm : ObservableObject, IDisposable
    {
        //#region Fields

        //private readonly SkyTelescopeVm _skyTelescopeVM;

        //#endregion
        public HardwareLimitsVm()
        {
            // setup property events to monitor
            SkyServer.StaticPropertyChanged += PropertyChangedSkyServer;
            Settings.Settings.StaticPropertyChanged += PropertyChangedSettings;
            // Initialize axis limits
            AxisLowerLimitYs = new List<double>(Numbers.InclusiveRange(-90, 20, 1));
            AxisUpperLimitYs = new List<double>(Numbers.InclusiveRange(50, 90, 1));
            AxisLimitXs = new List<double>(Numbers.InclusiveRange(105, 210, 1));
            // Ra / Az axis limit
            AxisLimitX = SkySettings.AxisLimitX;
            // Polar upper limit is offset by latitude
            AxisUpperLimitY = SkySettings.AxisUpperLimitY - (SkySettings.AlignmentMode == AlignmentModes.algPolar ? Math.Abs(SkySettings.Latitude) : 0);
            AxisLowerLimitY = SkySettings.AxisLowerLimitY;
            if (SkySettings.AlignmentMode == AlignmentModes.algPolar)
            {
                using (var memory = new MemoryStream())
                {
                    var formatString = (string)Application.Current.Resources["mhlPolarHomeText"];
                    switch (SkySettings.PolarMode)
                    {
                        case PolarMode.Right:
                            if (SkyServer.SouthernHemisphere)
                            {
                                Properties.Resources.iconPolarModeRightSouth.Save(memory, ImageFormat.Png);
                                PolarModeHomeText = string.Format(formatString, "East", "North");
                            }
                            else
                            {
                                Properties.Resources.iconPolarModeRightNorth.Save(memory, ImageFormat.Png);
                                PolarModeHomeText = string.Format(formatString, "West", "South");
                            }
                            break;
                        case PolarMode.Left:
                            if (SkyServer.SouthernHemisphere)
                            {
                                Properties.Resources.iconPolarModeLeftSouth.Save(memory, ImageFormat.Png);
                                PolarModeHomeText = string.Format(formatString, "West", "North");
                            }
                            else
                            {
                                Properties.Resources.iconPolarModeLeftNorth.Save(memory, ImageFormat.Png);
                                PolarModeHomeText = string.Format(formatString, "East", "South");
                            }
                            break;
                    }
                    memory.Position = 0;
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                    PolarModeIcon = bitmapImage;
                }
            }
        }

        #region Properties
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
                     default:
                         // Handle any property changes here
                         break;
                 }
             });
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
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

        /// <summary>
        /// Property changes from the settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertyChangedSettings(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ThreadContext.BeginInvokeOnUiThread(
                    delegate
                    {
                        switch (e.PropertyName)
                        {
                            default:
                                // Handle any property changes here
                                break;
                        }
                    });
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
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

        public Visibility IsVisible => SkySettings.AlignmentMode == AlignmentModes.algPolar ? Visibility.Visible : Visibility.Collapsed;

        private ImageSource _polarModeIcon;
        public ImageSource PolarModeIcon
        {
            get => _polarModeIcon;
            set
            {
                _polarModeIcon = value;
                OnPropertyChanged();
            }
        }

        private string _polarModeHomeText;
        public string PolarModeHomeText
        {
            get => _polarModeHomeText;
            set
            {
                _polarModeHomeText = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Window Control

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
            var win = Application.Current.Windows.OfType<HardwareLimitsV>().FirstOrDefault();
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

        #region Polar Mode UI Control
        private ICommand _clickPolarModeCommand;
        public ICommand ClickPolarModeCommand
        {
            get
            {
                var command = _clickPolarModeCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickPolarModeCommand = new RelayCommand(
                    param => SetPolarMode()
                );
            }
        }

        private void SetPolarMode()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (SkySettings.AlignmentMode == AlignmentModes.algPolar)
                    {
                        using (var memory = new MemoryStream())
                        {
                            // Toggle polar mode
                            SkySettings.PolarMode = SkySettings.PolarMode == PolarMode.Right ? PolarMode.Left : PolarMode.Right;
                            var formatString = (string)Application.Current.Resources["mhlPolarHomeText"];
                            switch (SkySettings.PolarMode)
                            {
                                case PolarMode.Right:
                                    if (SkyServer.SouthernHemisphere)
                                    {
                                        Properties.Resources.iconPolarModeRightSouth.Save(memory, ImageFormat.Png);
                                        PolarModeHomeText = string.Format(formatString, "East", "North");
                                    }
                                    else
                                    {
                                        Properties.Resources.iconPolarModeRightNorth.Save(memory, ImageFormat.Png);
                                        PolarModeHomeText = string.Format(formatString, "West", "South");
                                    }
                                    break;
                                case PolarMode.Left:
                                    if (SkyServer.SouthernHemisphere)
                                    {
                                        Properties.Resources.iconPolarModeLeftSouth.Save(memory, ImageFormat.Png);
                                        PolarModeHomeText = string.Format(formatString, "West", "North");
                                    }
                                    else
                                    {
                                        Properties.Resources.iconPolarModeLeftNorth.Save(memory, ImageFormat.Png);
                                        PolarModeHomeText = string.Format(formatString, "East", "South");
                                    }
                                    break;
                            }
                            memory.Position = 0;
                            var bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.StreamSource = memory;
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.EndInit();
                            bitmapImage.Freeze();
                            PolarModeIcon = bitmapImage;
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}|{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        #endregion

        #region Hardware Limits UI Control
        private ICommand _acceptHardwareLimitsCommand;
        public ICommand AcceptHardwareLimitsCommand
        {
            get
            {
                var command = _acceptHardwareLimitsCommand;
                if (command != null)
                {
                    return command;
                }

                return _acceptHardwareLimitsCommand = new RelayCommand(
                    param => AcceptHardwareLimits()
                );
            }
        }

        private void AcceptHardwareLimits()
        {
            try
            {
                using (new WaitCursor())
                {
                    SkySettings.AxisLowerLimitY = AxisLowerLimitY;
                    SkySettings.AxisUpperLimitY = AxisUpperLimitY;
                    SkySettings.AxisLimitX = AxisLimitX;
                    IsDialogOpen = false;
                    CloseWindow();
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}|{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private ICommand _cancelHardwareLimitsCommand;
        public ICommand CancelHardwareLimitsCommand
        {
            get
            {
                var command = _cancelHardwareLimitsCommand;
                if (command != null)
                {
                    return command;
                }

                return _cancelHardwareLimitsCommand = new RelayCommand(
                    param => CancelHardwareLimits()
                );
            }
        }

        private void CancelHardwareLimits()
        {
            try
            {
                using (new WaitCursor())
                {
                    IsDialogOpen = false;
                    CloseWindow();
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}|{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
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
                Device = MonitorDevice.Ui,
                Category = MonitorCategory.Interface,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{msg}"
            };
            MonitorLog.LogToMonitor(monitorItem);
        }

        private static void OpenDialogWin(string msg, string caption = null)
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
            var view = new ErrorMessageDialog
            {
                DataContext = new ErrorMessageDialogVM()
            };

            //show the dialog
            await DialogHost.Show(view, "RootDialog", ClosingMessageEventHandler);
        }
        private static void ClosingMessageEventHandler(object sender, DialogClosingEventArgs eventArgs)
        {
            Console.WriteLine(@"You can intercept the closing event, and cancel here.");
        }

        #endregion

        #region Hardware Limits Settings

        /// <summary>
        /// Range of integer values for Ra / Az limit combo box
        /// </summary>
        public IList<double> AxisLimitXs { get; }

        private double _AxisLimitX = SkySettings.AxisLimitX;
        /// <summary>
        /// View model property for setting Ra / Az limit
        /// Settings updated if "Ok" on dialog close
        /// </summary>
        public double AxisLimitX
        {
            get => _AxisLimitX;
            set
            {
                _AxisLimitX = value;
                OnPropertyChanged();
                // Update graphic indicator
                OnPropertyChanged(nameof(PrimarySectorX));
                OnPropertyChanged(nameof(SecondarySectorX));
                OnPropertyChanged(nameof(AxisLimitSectorInfo));
            }
        }
        public Vector PrimarySectorX
        {
            get
            {
                var angle = AxisLimitX > 180.0 ? 360.0 - AxisLimitX : AxisLimitX;
                return new Vector(-angle, angle);
            }
        }
        public Vector SecondarySectorX
        {
            get
            {
                var start = AxisLimitX > 180.0 ? 360.0 - AxisLimitX : AxisLimitX;
                var end = AxisLimitX < 180.0 ? 360.0 - AxisLimitX : AxisLimitX;
                return AxisLimitX > 180.0 ? new Vector(start, end) : new Vector(0.0, 0.0);
            }
        }
        public double SlewLimitSectorRotate
        {
            get
            {
                switch (SkySettings.AlignmentMode)
                {
                    case AlignmentModes.algAltAz:
                        return 180.0;
                    case AlignmentModes.algPolar:
                    case AlignmentModes.algGermanPolar:
                    default:
                        return 0.0;
                }
            }
        }
        public IList<double> AxisUpperLimitYs { get; }
        private double _axisUpperLimitY;
        public double AxisUpperLimitY
        {
            get => _axisUpperLimitY;
            set
            {
                _axisUpperLimitY = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AxisLimitYSector));
            }
        }
        public IList<double> AxisLowerLimitYs { get; }
        private double _axisLowerLimitY;
        public double AxisLowerLimitY
        {
            get => _axisLowerLimitY;
            set
            {
                _axisLowerLimitY = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AxisLimitHorizonInfo));
                OnPropertyChanged(nameof(AxisLimitYSector));
            }
        }
        public Vector AxisLimitYSector
        {
            get
            {
                var sector = new Vector(0.0, 0.0);
                switch (SkySettings.AlignmentMode)
                {
                    case AlignmentModes.algAltAz:
                        sector.Y = AxisUpperLimitY - 90.0;
                        sector.X = AxisLowerLimitY - 90.0;
                        break;
                    case AlignmentModes.algPolar:
                        sector.Y = AxisUpperLimitY;
                        // Offset by latitude for graphic display
                        sector.X = AxisLowerLimitY - Math.Abs(SkySettings.Latitude);
                        break;
                    case AlignmentModes.algGermanPolar:
                    default:
                        break;
                }
                return sector;
            }
        }

        public double AxisLimitSectorReflect
        {
            get
            {
                switch (SkySettings.AlignmentMode)
                {
                    case AlignmentModes.algAltAz:
                        return -1.0;
                    case AlignmentModes.algPolar:
                    case AlignmentModes.algGermanPolar:
                    default:
                        return 1.0;
                }
            }
        }

        public string AxisLimitHorizonInfo
        {
            get
            {
                var horizonInfo = string.Empty;
                switch (SkySettings.AlignmentMode)
                {
                    case AlignmentModes.algPolar:
                        var angle = - 90.0 + Math.Abs(SkySettings.Latitude) - AxisLowerLimitY;
                        var sign = (angle < 0.0);
                        horizonInfo = $"Limit is {Math.Abs(angle):F1} degrees {(sign ? "above" : "below")} horizon";
                        break;
                    case AlignmentModes.algAltAz:
                    case AlignmentModes.algGermanPolar:
                    default:
                        break;
                }
                return horizonInfo;
            }
        }

        public string AxisLimitSectorInfo
        {
            get
            {
                var sectorInfo = String.Empty;
                switch (SkySettings.AlignmentMode)
                {
                    case AlignmentModes.algAltAz:
                    case AlignmentModes.algPolar:
                        if (SecondarySectorX.LengthSquared > 0.0)
                            sectorInfo = $"Ra / Az limit enables two goto positions";
                        break;
                    case AlignmentModes.algGermanPolar:
                    default:
                        break;
                }
                return sectorInfo;
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
    ~HardwareLimitsVm()
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
            Settings.Settings.StaticPropertyChanged -= PropertyChangedSettings;
        }
            NativeMethods.ClipCursor(IntPtr.Zero);
    }
        #endregion
    }
}
