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
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Media;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using GS.Principles;
using GS.Server.Controls.Dialogs;
using GS.Server.Helpers;
using GS.Server.SkyTelescope;
using GS.Shared;
using GS.Shared.Command;
using MaterialDesignThemes.Wpf;

using NativeMethods = GS.Server.Helpers.NativeMethods;
using ASCOM.Utilities;


namespace GS.Server.Windows
{
    public class SpiralVM : ObservableObject, IDisposable
    {
        #region Fields

        private readonly SkyTelescopeVM _skyTelescopeVM;
        private readonly Util _util = new Util();

        #endregion

        public SpiralVM()
        {
            try
            {
                using (new WaitCursor())
                {
                    var monitorItem = new MonitorEntry
                        { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Opening Hand Control Window" };
                    MonitorLog.LogToMonitor(monitorItem);

                    _skyTelescopeVM = SkyTelescopeVM._skyTelescopeVM;
                    SkyServer.StaticPropertyChanged += PropertyChangedSkyServer;
                    SkySettings.StaticPropertyChanged += PropertyChangedSkySettings;


                    Title = $"GSS {Application.Current.Resources["1021SpiralSearch"]}";
                    ScreenEnabled = SkyServer.IsMountRunning;
                    TopMost = false;
                    Distance = SkySettings.SpiralDistance.ToString(CultureInfo.InvariantCulture);

                    SkyServer.SpiralChanged = true;
                }

            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
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
                     case "SpiralChanged":
                         UpdateBackGrounds();
                         break;
                     case "IsSlewing":
                         IsSlewing = SkyServer.IsSlewing;
                         break;
                 }
             });
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
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
        /// Property changes from settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertyChangedSkySettings(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ThreadContext.BeginInvokeOnUiThread(
             delegate
             {
                 switch (e.PropertyName)
                 {
                     case "SpiralReset":
                         OnPropertyChanged($"Reset");
                         break;
                     case "SpiralDistance":
                         Distance = SkySettings.SpiralDistance.ToString(CultureInfo.InvariantCulture);
                         break;
                 }
             });
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
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

        #region backgrounds

        private Brush _c0;
        public Brush C0
        {
            get => _c0;
            set
            {
                _c0 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c1;
        public Brush C1
        {
            get => _c1;
            set
            {
                _c1 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c2;
        public Brush C2
        {
            get => _c2;
            set
            {
                _c2 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c3;
        public Brush C3
        {
            get => _c3;
            set
            {
                _c3 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c4;
        public Brush C4
        {
            get => _c4;
            set
            {
                _c4 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c5;
        public Brush C5
        {
            get => _c5;
            set
            {
                _c5 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c6;
        public Brush C6
        {
            get => _c6;
            set
            {
                _c6 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c7;
        public Brush C7
        {
            get => _c7;
            set
            {
                _c7 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c8;
        public Brush C8
        {
            get => _c8;
            set
            {
                _c8 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c9;
        public Brush C9
        {
            get => _c9;
            set
            {
                _c9 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c10;
        public Brush C10
        {
            get => _c10;
            set
            {
                _c10 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c11;
        public Brush C11
        {
            get => _c11;
            set
            {
                _c11 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c12;
        public Brush C12
        {
            get => _c12;
            set
            {
                _c12 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c13;
        public Brush C13
        {
            get => _c13;
            set
            {
                _c13 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c14;
        public Brush C14
        {
            get => _c14;
            set
            {
                _c14 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c15;
        public Brush C15
        {
            get => _c15;
            set
            {
                _c15 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c16;
        public Brush C16
        {
            get => _c16;
            set
            {
                _c16 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c17;
        public Brush C17
        {
            get => _c17;
            set
            {
                _c17 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c18;
        public Brush C18
        {
            get => _c18;
            set
            {
                _c18 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c19;
        public Brush C19
        {
            get => _c19;
            set
            {
                _c19 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c20;
        public Brush C20
        {
            get => _c20;
            set
            {
                _c20 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c21;
        public Brush C21
        {
            get => _c21;
            set
            {
                _c21 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c22;
        public Brush C22
        {
            get => _c22;
            set
            {
                _c22 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c23;
        public Brush C23
        {
            get => _c23;
            set
            {
                _c23 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c24;
        public Brush C24
        {
            get => _c24;
            set
            {
                _c24 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c25;
        public Brush C25
        {
            get => _c25;
            set
            {
                _c25 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c26;
        public Brush C26
        {
            get => _c26;
            set
            {
                _c26 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c27;
        public Brush C27
        {
            get => _c27;
            set
            {
                _c27 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c28;
        public Brush C28
        {
            get => _c28;
            set
            {
                _c28 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c29;
        public Brush C29
        {
            get => _c29;
            set
            {
                _c29 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c30;
        public Brush C30
        {
            get => _c30;
            set
            {
                _c30 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c31;
        public Brush C31
        {
            get => _c31;
            set
            {
                _c31 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c32;
        public Brush C32
        {
            get => _c32;
            set
            {
                _c32 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c33;
        public Brush C33
        {
            get => _c33;
            set
            {
                _c33 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c34;
        public Brush C34
        {
            get => _c34;
            set
            {
                _c34 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c35;
        public Brush C35
        {
            get => _c35;
            set
            {
                _c35 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c36;
        public Brush C36
        {
            get => _c36;
            set
            {
                _c36 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c37;
        public Brush C37
        {
            get => _c37;
            set
            {
                _c37 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c38;
        public Brush C38
        {
            get => _c38;
            set
            {
                _c38 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c39;
        public Brush C39
        {
            get => _c39;
            set
            {
                _c39 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c40;
        public Brush C40
        {
            get => _c40;
            set
            {
                _c40 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c41;
        public Brush C41
        {
            get => _c41;
            set
            {
                _c41 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c42;
        public Brush C42
        {
            get => _c42;
            set
            {
                _c42 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c43;
        public Brush C43
        {
            get => _c43;
            set
            {
                _c43 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c44;
        public Brush C44
        {
            get => _c44;
            set
            {
                _c44 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c45;
        public Brush C45
        {
            get => _c45;
            set
            {
                _c45 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c46;
        public Brush C46
        {
            get => _c46;
            set
            {
                _c46 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c47;
        public Brush C47
        {
            get => _c47;
            set
            {
                _c47 = value;
                OnPropertyChanged();
            }
        }

        private Brush _c48;
        public Brush C48
        {
            get => _c48;
            set
            {
                _c48 = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Spiral

        private bool _isSlewing;
        public bool IsSlewing
        {
            get => _isSlewing;
            set
            {
                if (IsSlewing == value) return;
                _isSlewing = value;
                OnPropertyChanged();
            }
        }

        private string _goToRaString;
        public string GoToRaString
        {
            get => _goToRaString;
            set
            {
                _goToRaString = value;
                OnPropertyChanged();
            }
        }
        
        private string _goToDecString;
        public string GoToDecString
        {
            get => _goToDecString;
            set
            {
                _goToDecString = value;
                OnPropertyChanged();
            }
        }

        public int Height
        {
            get => SkySettings.SpiralHeight;
            set
            {
                if (SkySettings.SpiralHeight == value) return;
                SkySettings.SpiralHeight = value;
                OnPropertyChanged();
            }
        }

        public int Width
        {
            get => SkySettings.SpiralWidth;
            set
            {
                if (SkySettings.SpiralWidth == value) return;
                SkySettings.SpiralWidth = value;
                OnPropertyChanged();
            }
        }

        private int _calcHeight;
        public int CalcHeight
        {
            get => _calcHeight;
            set
            {
                if (_calcHeight == value) return;
                _calcHeight = value;
                OnPropertyChanged();
            }
        }

        private int _calcWidth;
        public int CalcWidth
        {
            get => _calcWidth;
            set
            {
                if (_calcWidth == value) return;
                _calcWidth = value;
                OnPropertyChanged();
            }
        }

        public double CameraHeight
        {
            get => SkySettings.CameraHeight;
            set
            {
                if (Math.Abs(SkySettings.CameraHeight - value) < 0.0) return;
                SkySettings.CameraHeight = value;
                OnPropertyChanged();
            }
        }

        public double CameraWidth
        {
            get => SkySettings.CameraWidth;
            set
            {
                if (Math.Abs(SkySettings.CameraWidth - value) < 0.0) return;
                SkySettings.CameraWidth = value;
                OnPropertyChanged();
            }
        }
        
        public double EyepieceFS
        {
            get => SkySettings.EyepieceFS;
            set
            {
                if (Math.Abs(SkySettings.EyepieceFS - value) < 0.0) return;
                SkySettings.EyepieceFS = value;
                OnPropertyChanged();
            }
        }

        public double FocalLength
        {
            get => SkySettings.FocalLength * 1000.0;
            set
            {
                var a = value / 1000;
                if (Math.Abs(SkySettings.FocalLength - a) < 0.0) return;
                SkySettings.FocalLength = a ;
                OnPropertyChanged();
            }
        }

        private string _distance;
        public string Distance
        {
            get => _distance;
            set
            {
                if (_distance == value) return;
                _distance = value;
                OnPropertyChanged();
            }
        }

        public bool SpiralLimits
        {
            get => SkySettings.SpiralLimits;
            set
            {
                if (SkySettings.SpiralLimits == value) return;
                SkySettings.SpiralLimits = value;
                OnPropertyChanged();
            }
        }

        private int _selectedDot;
        public int SelectedDot
        {
            get => _selectedDot;
            set
            {
                if (_selectedDot == value) return;
                _selectedDot = value;
                GotoHeader = $"{Application.Current.Resources["goGoTo"]} {value}";
                OnPropertyChanged();
            }
        }

        public SpiralPoint _selectedPoint;

        public SpiralPoint SelectedPoint
        {
            get => _selectedPoint;
            set
            {
                if (_selectedPoint == value) return;
                _selectedPoint = value;
                OnPropertyChanged();
            }
        }

        private string _gotoHeader;
        public string GotoHeader
        {
            get => _gotoHeader;
            set
            {
                if (_gotoHeader == value) return;
                _gotoHeader = value;
                OnPropertyChanged();
            }
        }
        
        private bool _isSpiralFovDialogOpen;
        public bool IsSpiralFovDialogOpen
        {
            get => _isSpiralFovDialogOpen;
            set
            {
                if (_isSpiralFovDialogOpen == value) return;
                _isSpiralFovDialogOpen = value;
                OnPropertyChanged();
            }
        }

        private object _spiralFovContent;
        public object SpiralFovContent
        {
            get => _spiralFovContent;
            set
            {
                if (_spiralFovContent == value) return;
                _spiralFovContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openSpiralFovDialogCmd;
        public ICommand OpenSpiralFovDialogCmd
        {
            get
            {
                var cmd = _openSpiralFovDialogCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _openSpiralFovDialogCmd = new RelayCommand(
                    OpenSpiralFov
                );
            }
        }
        private void OpenSpiralFov(object param)
        {
            try
            {
                SpiralFovContent = new SpiralFovDialog();
                IsSpiralFovDialogOpen = true;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
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

        private ICommand _cancelSpiralFovDialogCmd;
        public ICommand CancelSpiralFovDialogCmd
        {
            get
            {
                var command = _cancelSpiralFovDialogCmd;
                if (command != null)
                {
                    return command;
                }

                return _cancelSpiralFovDialogCmd = new RelayCommand(
                    param => CancelSpiralFovDialog()
                );
            }
        }
        private void CancelSpiralFovDialog()
        {
            try
            {
                IsSpiralFovDialogOpen = false;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
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

        private ICommand _acceptSpiralFovDialogCmd;
        public ICommand AcceptSpiralFovDialogCmd
        {
            get
            {
                var command = _acceptSpiralFovDialogCmd;
                if (command != null)
                {
                    return command;
                }

                return _acceptSpiralFovDialogCmd = new RelayCommand(
                    param => AcceptSpiralFovDialog()
                );
            }
        }
        private void AcceptSpiralFovDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    Height = CalcHeight;
                    Width = CalcWidth;
                    IsSpiralFovDialogOpen = false;
                    SkyServer.SpiralChanged = true;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
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

        private ICommand _eyepieceFovDialogCmd;
        public ICommand EyepieceFovDialogCmd
        {
            get
            {
                var command = _eyepieceFovDialogCmd;
                if (command != null)
                {
                    return command;
                }

                return _eyepieceFovDialogCmd = new RelayCommand(
                    param => EyepieceFovDialog()
                );
            }
        }
        private void EyepieceFovDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    var h = (EyepieceFS / FocalLength) * 57.3 * 3600;
                    var w = (EyepieceFS / FocalLength) * 57.3 * 3600;

                    CalcHeight = (int)h;
                    CalcWidth = (int)w;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
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

        private ICommand _cameraFovDialogCmd;
        public ICommand CameraFovDialogCmd
        {
            get
            {
                var command = _cameraFovDialogCmd;
                if (command != null)
                {
                    return command;
                }

                return _cameraFovDialogCmd = new RelayCommand(
                    param => CameraFovDialog()
                );
            }
        }
        private void CameraFovDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    var h = (CameraHeight * 3460 / FocalLength) * 60.0;
                    var w = (CameraWidth * 3460 / FocalLength) * 60.0;
                    CalcHeight = (int)h;
                    CalcWidth = (int)w;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
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
        
        private bool _isSpiralGoToDialogOpen;
        public bool IsSpiralGoToDialogOpen
        {
            get => _isSpiralGoToDialogOpen;
            set
            {
                if (_isSpiralGoToDialogOpen == value) return;
                _isSpiralGoToDialogOpen = value;
                OnPropertyChanged();
            }
        }

        private object _spiralGoToContent;
        public object SpiralGoToContent
        {
            get => _spiralGoToContent;
            set
            {
                if (_spiralGoToContent == value) return;
                _spiralGoToContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openSpiralGoToDialogCmd;
        public ICommand OpenSpiralGoToDialogCmd
        {
            get
            {
                var cmd = _openSpiralGoToDialogCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _openSpiralGoToDialogCmd = new RelayCommand(
                    OpenSpiralGoTo
                );
            }
        }
        private void OpenSpiralGoTo(object param)
        {
            try
            {
                if (IsSpiralGoToDialogOpen)
                {
                    IsSpiralGoToDialogOpen = false;
                }

                var result = int.TryParse(param.ToString(), out var dot);
                if (!result) return;

                SelectedDot = dot;
                if (SkyServer.SpiralCollection == null) return;

                var point = SkyServer.SpiralCollection.Find(x => x.Index == dot);
                if (point == null) return;

                SelectedPoint = point;
                GoToRaString = _util.HoursToHMS(point.RaDec.X, "h ", "m ", "s", 2);
                GoToDecString = _util.DegreesToDMS(point.RaDec.Y, "° ", "m ", "s", 2);

                SpiralGoToContent = new SpiralGoToDialog();
                IsSpiralGoToDialogOpen = true;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
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

        private ICommand _cancelSpiralGoToDialogCmd;
        public ICommand CancelSpiralGoToDialogCmd
        {
            get
            {
                var command = _cancelSpiralGoToDialogCmd;
                if (command != null)
                {
                    return command;
                }

                return _cancelSpiralGoToDialogCmd = new RelayCommand(
                    param => CancelSpiralGoToDialog()
                );
            }
        }
        private void CancelSpiralGoToDialog()
        {
            try
            {
                SkyServer.SpiralChanged = true;
                IsSpiralGoToDialogOpen = false;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
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

        private ICommand _acceptSpiralGoToDialogCmd;
        public ICommand AcceptSpiralGoToDialogCmd
        {
            get
            {
                var command = _acceptSpiralGoToDialogCmd;
                if (command != null)
                {
                    return command;
                }

                return _acceptSpiralGoToDialogCmd = new RelayCommand(
                    param => AcceptSpiralGoToDialog()
                );
            }
        }
        private void AcceptSpiralGoToDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (SelectedPoint == null) return;
                    var radec = Transforms.CoordTypeToInternal(SelectedPoint.RaDec.X, SelectedPoint.RaDec.Y);

                    // check for possible flip
                    if (SkySettings.SpiralLimits)
                    {
                        var flipRequired = Axes.IsFlipRequired(new[] { radec.X, radec.Y });
                        if (flipRequired)
                        {
                            OpenDialog($"{Application.Current.Resources["1021FlipLimit"]}");  
                            return;
                        }
                    }

                    SkyServer.SlewRaDec(radec.X, radec.Y);

                    var currentpoint = SkyServer.SpiralCollection.Find(x => x.Status == SpiralPointStatus.Current);
                    var newpoint = SkyServer.SpiralCollection.Find(x => x.Index == SelectedPoint.Index);
                    if (newpoint != null) newpoint.Status = SpiralPointStatus.Current;
                    if (currentpoint != null) currentpoint.Status = SpiralPointStatus.Visited;

                    IsSpiralGoToDialogOpen = false;
                    SkyServer.SpiralChanged = true;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
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

        private ICommand _spiralInCmd;
        public ICommand SpiralInCmd
        {
            get
            {
                var command = _spiralInCmd;
                if (command != null)
                {
                    return command;
                }

                return _spiralInCmd = new RelayCommand(
                    param => SpiralIn()
                );
            }
        }
        private void SpiralIn()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (_skyTelescopeVM.SpiralInCmd.CanExecute(null))
                        _skyTelescopeVM.SpiralInCmd.Execute(null);
                }
                SkyServer.SpiralChanged = true;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
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

        private ICommand _spiralOutCmd;
        public ICommand SpiralOutCmd
        {
            get
            {
                var command = _spiralOutCmd;
                if (command != null)
                {
                    return command;
                }

                return _spiralOutCmd = new RelayCommand(
                    param => SpiralOut()
                );
            }
        }
        private void SpiralOut()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (_skyTelescopeVM.SpiralOutCmd.CanExecute(null))
                        _skyTelescopeVM.SpiralOutCmd.Execute(null);
                }
                SkyServer.SpiralChanged = true;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
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

        private ICommand _spiralStopCmd;
        public ICommand SpiralStopCmd
        {
            get
            {
                var command = _spiralStopCmd;
                if (command != null)
                {
                    return command;
                }

                return _spiralStopCmd = new RelayCommand(
                    param => SpiralStop()
                );
            }
        }
        private void SpiralStop()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (_skyTelescopeVM.AbortCmd.CanExecute(null))
                        _skyTelescopeVM.AbortCmd.Execute(null);
                    SkyServer.AbortSlew(true);
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
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

        private ICommand _spiralClearCmd;
        public ICommand SpiralClearCmd
        {
            get
            {
                var command = _spiralClearCmd;
                if (command != null)
                {
                    return command;
                }

                return _spiralClearCmd = new RelayCommand(
                    param => SpiralClear()
                );
            }
        }
        private void SpiralClear()
        {
            try
            {
                using (new WaitCursor())
                {
                    SkyServer.SpiralCollection.Clear();
                    SkySettings.SpiralDistance = 0;
                    SkyServer.SpiralChanged = true;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
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

        private ICommand _spiralGenerateCmd;
        public ICommand SpiralGenerateCmd
        {
            get
            {
                var command = _spiralGenerateCmd;
                if (command != null)
                {
                    return command;
                }

                return _spiralGenerateCmd = new RelayCommand(
                    param => SpiralGenerate()
                );
            }
        }
        private void SpiralGenerate()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (_skyTelescopeVM.SpiralGenerateCmd.CanExecute(null))
                        _skyTelescopeVM.SpiralGenerateCmd.Execute(null);
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
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

        private void UpdateBackGrounds()
        {
            if (!SkyServer.SpiralChanged){return;}
            ClearBackGrounds();
            foreach (var point in SkyServer.SpiralCollection)
            {
                switch (point.Index)
                {
                    case 0:
                        C0 = GetPointBrush(point.Status);
                        break;
                    case 1:
                        C1 = GetPointBrush(point.Status);
                        break;
                    case 2:
                        C2 = GetPointBrush(point.Status);
                        break;
                    case 3:
                        C3 = GetPointBrush(point.Status);
                        break;
                    case 4:
                        C4 = GetPointBrush(point.Status);
                        break;
                    case 5:
                        C5 = GetPointBrush(point.Status);
                        break;
                    case 6:
                        C6 = GetPointBrush(point.Status);
                        break;
                    case 7:
                        C7 = GetPointBrush(point.Status);
                        break;
                    case 8:
                        C8 = GetPointBrush(point.Status);
                        break;
                    case 9:
                        C9 = GetPointBrush(point.Status);
                        break;
                    case 10:
                        C10 = GetPointBrush(point.Status);
                        break;
                    case 11:
                        C11 = GetPointBrush(point.Status);
                        break;
                    case 12:
                        C12 = GetPointBrush(point.Status);
                        break;
                    case 13:
                        C13 = GetPointBrush(point.Status);
                        break;
                    case 14:
                        C14 = GetPointBrush(point.Status);
                        break;
                    case 15:
                        C15 = GetPointBrush(point.Status);
                        break;
                    case 16:
                        C16 = GetPointBrush(point.Status);
                        break;
                    case 17:
                        C17 = GetPointBrush(point.Status);
                        break;
                    case 18:
                        C18 = GetPointBrush(point.Status);
                        break;
                    case 19:
                        C19 = GetPointBrush(point.Status);
                        break;
                    case 20:
                        C20 = GetPointBrush(point.Status);
                        break;
                    case 21:
                        C21 = GetPointBrush(point.Status);
                        break;
                    case 22:
                        C22 = GetPointBrush(point.Status);
                        break;
                    case 23:
                        C23 = GetPointBrush(point.Status);
                        break;
                    case 24:
                        C24 = GetPointBrush(point.Status);
                        break;
                    case 25:
                        C25 = GetPointBrush(point.Status);
                        break;
                    case 26:
                        C26 = GetPointBrush(point.Status);
                        break;
                    case 27:
                        C27 = GetPointBrush(point.Status);
                        break;
                    case 28:
                        C28 = GetPointBrush(point.Status);
                        break;
                    case 29:
                        C29 = GetPointBrush(point.Status);
                        break;
                    case 30:
                        C30 = GetPointBrush(point.Status);
                        break;
                    case 31:
                        C31 = GetPointBrush(point.Status);
                        break;
                    case 32:
                        C32 = GetPointBrush(point.Status);
                        break;
                    case 33:
                        C33 = GetPointBrush(point.Status);
                        break;
                    case 34:
                        C34 = GetPointBrush(point.Status);
                        break;
                    case 35:
                        C35 = GetPointBrush(point.Status);
                        break;
                    case 36:
                        C36 = GetPointBrush(point.Status);
                        break;
                    case 37:
                        C37 = GetPointBrush(point.Status);
                        break;
                    case 38:
                        C38 = GetPointBrush(point.Status);
                        break;
                    case 39:
                        C39 = GetPointBrush(point.Status);
                        break;
                    case 40:
                        C40 = GetPointBrush(point.Status);
                        break;
                    case 41:
                        C41 = GetPointBrush(point.Status);
                        break;
                    case 42:
                        C42 = GetPointBrush(point.Status);
                        break;
                    case 43:
                        C43 = GetPointBrush(point.Status);
                        break;
                    case 44:
                        C44 = GetPointBrush(point.Status);
                        break;
                    case 45:
                        C45 = GetPointBrush(point.Status);
                        break;
                    case 46:
                        C46 = GetPointBrush(point.Status);
                        break;
                    case 47:
                        C47 = GetPointBrush(point.Status);
                        break;
                    case 48:
                        C48 = GetPointBrush(point.Status);
                        break;
                }
            }
        }

        private static Brush GetPointBrush(SpiralPointStatus status)
        {
            switch (status)
            {
                case SpiralPointStatus.Clear:
                    return Application.Current.TryFindResource("SecondaryHueMidBrush") as SolidColorBrush;
                case SpiralPointStatus.Visited:
                    return Application.Current.TryFindResource("SecondaryHueDarkBrush") as SolidColorBrush;
                case SpiralPointStatus.Current:
                    return Application.Current.TryFindResource("SecondaryHueLightBrush") as SolidColorBrush;
                default:
                    return Brushes.DarkRed;
            }
        }
        
        private void ClearBackGrounds()
        {
            C0 = Brushes.Transparent;
            C1 = Brushes.Transparent;
            C2 = Brushes.Transparent;
            C3 = Brushes.Transparent;
            C4 = Brushes.Transparent;
            C5 = Brushes.Transparent;
            C6 = Brushes.Transparent;
            C7 = Brushes.Transparent;
            C8 = Brushes.Transparent;
            C9 = Brushes.Transparent;
            C10 = Brushes.Transparent;
            C11 = Brushes.Transparent;
            C12 = Brushes.Transparent;
            C13 = Brushes.Transparent;
            C14 = Brushes.Transparent;
            C15 = Brushes.Transparent;
            C16 = Brushes.Transparent;
            C17 = Brushes.Transparent;
            C18 = Brushes.Transparent;
            C19 = Brushes.Transparent;
            C20 = Brushes.Transparent;
            C21 = Brushes.Transparent;
            C22 = Brushes.Transparent;
            C23 = Brushes.Transparent;
            C24 = Brushes.Transparent;
            C25 = Brushes.Transparent;
            C26 = Brushes.Transparent;
            C27 = Brushes.Transparent;
            C28 = Brushes.Transparent;
            C29 = Brushes.Transparent;
            C30 = Brushes.Transparent;
            C31 = Brushes.Transparent;
            C32 = Brushes.Transparent;
            C33 = Brushes.Transparent;
            C34 = Brushes.Transparent;
            C35 = Brushes.Transparent;
            C36 = Brushes.Transparent;
            C37 = Brushes.Transparent;
            C38 = Brushes.Transparent;
            C39 = Brushes.Transparent;
            C40 = Brushes.Transparent;
            C41 = Brushes.Transparent;
            C42 = Brushes.Transparent;
            C40 = Brushes.Transparent;
            C43 = Brushes.Transparent;
            C44 = Brushes.Transparent;
            C45 = Brushes.Transparent;
            C46 = Brushes.Transparent;
            C47 = Brushes.Transparent;
            C48 = Brushes.Transparent;

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
            WindowState = WindowState.Minimized;
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
            WindowState = WindowState != WindowState.Maximized ? WindowState.Maximized : WindowState.Normal;
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
            WindowState = WindowState.Normal;
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
            var win = Application.Current.Windows.OfType<SpiralV>().FirstOrDefault();
            win?.Close();
        }

        private WindowState _windowState;
        public WindowState WindowState
        {
            get => _windowState;
            set
            {
                _windowState = value;
                OnPropertyChanged();
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
                Device = MonitorDevice.Telescope,
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
        ~SpiralVM()
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

    public class SpiralPoint
    {
        public int Index { get; set; }
        public Point RaDec { get; set; }
        public SpiralPointStatus Status { get; set; }
    }

    public enum SpiralPointStatus
    {
        Clear,
        Visited,
        Current
    }

}
