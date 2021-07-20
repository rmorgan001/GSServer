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
using ASCOM.Utilities;
using GS.Principles;
using GS.Server.Helpers;
using GS.Server.Main;
using GS.Server.SkyTelescope;
using GS.Shared;
using HelixToolkit.Wpf;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using GS.Server.Controls.Dialogs;
using GS.Server.Windows;
using GS.Shared.Command;

namespace GS.Server.Model3D
{
    public class Model3DVM : ObservableObject, IPageVM, IDisposable
    {
        #region fields
        public string TopName => "";
        public string BottomName => "3D";
        public int Uid => 4;
        private bool _disposed;
        private readonly Util _util = new Util();
        #endregion

        public Model3DVM()
        {
            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = " Loading Model3D" };
            MonitorLog.LogToMonitor(monitorItem);

            SkyServer.StaticPropertyChanged += PropertyChangedSkyServer;
            Settings.Settings.StaticPropertyChanged += PropertyChangedSettings;

            LookDirection = Settings.Settings.ModelLookDirection1;
            UpDirection = Settings.Settings.ModelUpDirection1;
            Position = Settings.Settings.ModelPosition1;

            LoadTopBar();
            LoadGEM();
            Rotate();

            ActualAxisX = "--.--";
            ActualAxisY = "--.--";
            CameraVis = false;
            RaAxisVis = false;
            DecAxisVis = false;
            RaVis = true;
            DecVis = true;
            AzVis = true;
            AltVis = true;
            TopVis = true;
            ScreenEnabled = SkyServer.IsMountRunning;
            ModelWinVisibility = true;
            ModelType = Settings.Settings.ModelType;
        }

        #region ViewModel
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
                     case "Altitude":
                         if (!AltVis) return;
                         Altitude = _util.DegreesToDMS(SkyServer.Altitude, "° ", ":", "", 2);
                         break;
                     case "Azimuth":
                         if (!AzVis) return;
                         Azimuth = _util.DegreesToDMS(SkyServer.Azimuth, "° ", ":", "", 2);
                         break;
                     case "DeclinationXForm":
                         if (!DecVis) return;
                         Declination = _util.DegreesToDMS(SkyServer.DeclinationXForm, "° ", ":", "", 2);
                         break;
                     case "Lha":
                         Lha = _util.HoursToHMS(SkyServer.Lha, "h ", ":", "", 2);
                         break;
                     case "RightAscensionXForm":
                         RightAscension = _util.HoursToHMS(SkyServer.RightAscensionXForm, "h ", ":", "", 2);
                         Rotate();
                         break;
                     case "IsMountRunning":
                         ScreenEnabled = SkyServer.IsMountRunning;
                         break;
                     case "SiderealTime":
                         if (!SideVis) return;
                         SiderealTime = _util.HoursToHMS(SkyServer.SiderealTime);
                         break;
                     case "ActualAxisX":
                         if (!RaAxisVis) return;
                         ActualAxisX = $"{Numbers.TruncateD(SkyServer.ActualAxisX, 2)}";
                         break;
                     case "ActualAxisY":
                         if (!DecAxisVis) return;
                         ActualAxisY = $"{Numbers.TruncateD(SkyServer.ActualAxisY, 3)}";
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
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
            }
        }

        /// <summary>
        /// Property changes from option settings
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
                            case "AccentColor":
                            case "ModelType":
                                ModelType = Settings.Settings.ModelType;
                                LoadGEM();
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
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
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

        private bool _modelWinVisibility;
        public bool ModelWinVisibility
        {
            get => _modelWinVisibility;
            set
            {
                if (_modelWinVisibility == value) return;
                _modelWinVisibility = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openModelWindowCmd;
        public ICommand OpenModelWindowCmd
        {
            get
            {
                var cmd = _openModelWindowCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _openModelWindowCmd = new RelayCommand(param => OpenModelWindow());
            }
        }
        private void OpenModelWindow()
        {
            try
            {
                var win = Application.Current.Windows.OfType<ModelV>().FirstOrDefault();
                if (win != null) return;
                var bWin = new ModelV();
                var _modelVM = ModelVM._modelVM;
                _modelVM.WinHeight = 220;
                _modelVM.WinWidth = 250;
                _modelVM.Position = Position;
                _modelVM.LookDirection = LookDirection;
                _modelVM.UpDirection = UpDirection;
                _modelVM.ImageFile = ImageFile;
                _modelVM.CameraIndex = 1;
                bWin.Show();
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        private ICommand _openResetViewCmd;
        public ICommand OpenResetViewCmd
        {
            get
            {
                var cmd = _openResetViewCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _openResetViewCmd = new RelayCommand(param => OpenResetView());
            }
        }
        private void OpenResetView()
        {
            try
            {
                Settings.Settings.ModelLookDirection1 = new Vector3D(-2616, -3167, -1170);
                Settings.Settings.ModelUpDirection1 = new Vector3D(.35, .43, .82);
                Settings.Settings.ModelPosition1 = new Point3D(2523, 3000, 1379);
                LoadGEM();
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        #endregion

        #region Viewport3D
        private double xAxisOffset;
        private double yAxisOffset;
        private double zAxisOffset;
        private string _imageFile;
        public string ImageFile
        {
            get => _imageFile;
            set
            {
                if (_imageFile == value) return;
                _imageFile = value;
                OnPropertyChanged();
            }
        }

        private Point3D _position;
        public Point3D Position
        {
            get => _position;
            set
            {
                if (_position == value) return;
                _position = value;
                OnPropertyChanged();
            }
        }

        private Vector3D _lookDirection;
        public Vector3D LookDirection
        {
            get => _lookDirection;
            set
            {
                if (_lookDirection == value) return;
                _lookDirection = value;
                OnPropertyChanged();
            }
        }

        private Vector3D _upDirection;
        public Vector3D UpDirection
        {
            get => _upDirection;
            set
            {
                if (_upDirection == value) return;
                _upDirection = value;
                OnPropertyChanged();
            }
        }

        private bool _raVis;
        public bool RaVis
        {
            get => _raVis;
            set
            {
                if (_raVis == value) return;
                _raVis = value;
                OnPropertyChanged();
            }
        }

        private bool _decVis;
        public bool DecVis
        {
            get => _decVis;
            set
            {
                if (_decVis == value) return;
                _decVis = value;
                OnPropertyChanged();
            }
        }

        private bool _azVis;
        public bool AzVis
        {
            get => _azVis;
            set
            {
                if (_azVis == value) return;
                _azVis = value;
                OnPropertyChanged();
            }
        }

        private bool _altVis;
        public bool AltVis
        {
            get => _altVis;
            set
            {
                if (_altVis == value) return;
                _altVis = value;
                OnPropertyChanged();
            }
        }

        private bool _sideVis;
        public bool SideVis
        {
            get => _sideVis;
            set
            {
                if (_sideVis == value) return;
                _sideVis = value;
                OnPropertyChanged();
            }
        }

        private bool _cameraVis;
        public bool CameraVis
        {
            get => _cameraVis;
            set
            {
                if (_cameraVis == value) return;
                _cameraVis = value;
                OnPropertyChanged();
            }
        }

        private string _siderealTime;
        public string SiderealTime
        {
            get => _siderealTime;
            set
            {
                if (value == _siderealTime) return;
                _siderealTime = value;
                OnPropertyChanged();
            }
        }

        private bool _raAxisVis;
        public bool RaAxisVis
        {
            get => _raAxisVis;
            set
            {
                if (_raAxisVis == value) return;
                _raAxisVis = value;
                OnPropertyChanged();
            }
        }

        private string _actualAxisX;
        public string ActualAxisX
        {
            get => _actualAxisX;
            private set
            {
                if (_actualAxisX == value) return;
                _actualAxisX = value;
                OnPropertyChanged();
            }
        }

        private string _actualAxisY;
        public string ActualAxisY
        {
            get => _actualAxisY;
            private set
            {
                if (_actualAxisY == value) return;
                _actualAxisY = value;
                OnPropertyChanged();
            }
        }

        private bool _decAxisVis;
        public bool DecAxisVis
        {
            get => _decAxisVis;
            set
            {
                if (_decAxisVis == value) return;
                _decAxisVis = value;
                OnPropertyChanged();
            }
        }

        private bool _topVis;
        public bool TopVis
        {
            get => _topVis;
            set
            {
                if (_topVis == value) return;
                _topVis = value;
                OnPropertyChanged();
            }
        }

        private System.Windows.Media.Media3D.Model3D _model;
        public System.Windows.Media.Media3D.Model3D Model
        {
            get => _model;
            set
            {
                if (_model == value) return;
                _model = value;
                OnPropertyChanged();
            }
        }

        private double _xAxis;
        public double XAxis
        {
            get => _xAxis;
            set
            {
                _xAxis = value;
                XAxisOffset = value + xAxisOffset;
                OnPropertyChanged();
            }
        }

        private double _yAxis;
        public double YAxis
        {
            get => _yAxis;
            set
            {
                _yAxis = value;
                YAxisOffset = value + yAxisOffset;
                OnPropertyChanged();
            }
        }

        private double _zAxis;
        public double ZAxis
        {
            get => _zAxis;
            set
            {
                _zAxis = value;
                ZAxisOffset = zAxisOffset - value;
                OnPropertyChanged();
            }
        }

        private double _xAxisOffset;
        public double XAxisOffset
        {
            get => _xAxisOffset;
            set
            {
                _xAxisOffset = value;
                OnPropertyChanged();
            }
        }

        private double _yAxisOffset;
        public double YAxisOffset
        {
            get => _yAxisOffset;
            set
            {
                _yAxisOffset = value;
                OnPropertyChanged();
            }
        }

        private double _zAxisOffset;
        public double ZAxisOffset
        {
            get => _zAxisOffset;
            set
            {
                _zAxisOffset = value;
                OnPropertyChanged();
            }
        }

        private Model3DType _modelType;
        public Model3DType ModelType
        {
            get => _modelType;
            set
            {
                if (_modelType == value) return;
                _modelType = value;
                Settings.Settings.ModelType = value;
                OnPropertyChanged();
            }
        }

        private Material _compass;
        public Material Compass
        {
            get => _compass;
            set
            {
                _compass = value;
                OnPropertyChanged();
            }
        }

        private void LoadGEM()
        {
            try
            {
                CameraVis = false;

                //camera direction
                LookDirection = Settings.Settings.ModelLookDirection1;
                UpDirection = Settings.Settings.ModelUpDirection1;
                Position = Settings.Settings.ModelPosition1;

                //offset for model to match start position
                xAxisOffset = 90;
                yAxisOffset = -90;
                zAxisOffset = 0;

                //start position
                XAxis = -90;
                YAxis = 90;
                ZAxis = Math.Round(Math.Abs(SkySettings.Latitude),2);

                //load model and compass
                var import = new ModelImporter();
                var model = import.Load(Shared.Model3D.GetModelFile(Settings.Settings.ModelType));
                Compass = MaterialHelper.CreateImageMaterial(Shared.Model3D.GetCompassFile(SkyServer.SouthernHemisphere), 100);

                //color OTA
                var accentColor = Settings.Settings.AccentColor;
                if (!string.IsNullOrEmpty(accentColor))
                {
                    var swatches = new SwatchesProvider().Swatches;
                    foreach (var swatch in swatches)
                    {
                        if (swatch.Name != Settings.Settings.AccentColor) continue;
                        var converter = new BrushConverter();
                        var accentbrush = (Brush)converter.ConvertFromString(swatch.ExemplarHue.Color.ToString());

                        var materialota = MaterialHelper.CreateMaterial(accentbrush);
                        if (model.Children[0] is GeometryModel3D ota) ota.Material = materialota;
                    }
                }
                //color weights
                var materialweights = MaterialHelper.CreateMaterial(new SolidColorBrush(Color.FromRgb(64, 64, 64)));
                if (model.Children[1] is GeometryModel3D weights) weights.Material = materialweights;
                //color bar
                var materialbar = MaterialHelper.CreateMaterial(Brushes.Gainsboro);
                if (model.Children[2] is GeometryModel3D bar) bar.Material = materialbar;

                Model = model;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }
        private void Rotate()
        {
            var axes = Shared.Model3D.RotateModel(SkySettings.Mount.ToString(), SkyServer.ActualAxisX,
                SkyServer.ActualAxisY, SkyServer.SouthernHemisphere);

            YAxis = axes[0];
            XAxis = axes[1];
        }

        #endregion

        #region Top Coord Bar

        private void LoadTopBar()
        {
            RightAscension = "00h 00m 00s";
            Declination = "00° 00m 00s";
            Azimuth = "00° 00m 00s";
            Altitude = "00° 00m 00s";
            Lha = "00h 00m 00s";
        }

        private string _altitude;
        public string Altitude
        {
            get => _altitude;
            set
            {
                if (value == _altitude) return;
                _altitude = value;
                OnPropertyChanged();
            }
        }

        private string _azimuth;
        public string Azimuth
        {
            get => _azimuth;
            set
            {
                if (value == _azimuth) return;
                _azimuth = value;
                OnPropertyChanged();
            }
        }

        private string _declination;
        public string Declination
        {
            get => _declination;
            set
            {
                if (value == _declination) return;
                _declination = value;
                OnPropertyChanged();
            }
        }

        private string _lha;
        public string Lha
        {
            get => _lha;
            set
            {
                if (value == _lha) return;
                _lha = value;
                OnPropertyChanged();
            }
        }

        private string _rightAscension;
        public string RightAscension
        {
            get => _rightAscension;
            set
            {
                if (value == _rightAscension) return;
                _rightAscension = value;
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
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Interface,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod().Name,
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
            Dispose(disposing: true);
            //GC.SuppressFinalize(obj: this);
        }

        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (_disposed) return;
            // If disposing equals true, dispose all managed
            // and unmanaged resources.
            if (disposing)
            {
                // Dispose managed resources.
                _util.Dispose();
            }

            // Note disposing has been done.
            _disposed = true;
        }

        ~Model3DVM()
        {
            Settings.Settings.ModelLookDirection1 = LookDirection;
            Settings.Settings.ModelUpDirection1 = UpDirection;
            Settings.Settings.ModelPosition1 = Position;
            Settings.Settings.Save();
        }
        #endregion
    }
}
