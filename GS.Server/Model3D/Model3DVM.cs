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
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using GS.Server.Controls.Dialogs;


namespace GS.Server.Model3D
{
    public class Model3DVM : ObservableObject, IPageVM, IDisposable
    {
        public string TopName => "";
        public string BottomName => "3D";
        public int Uid => 4;
        private bool _disposed;

        #region Model

        private readonly string _directoryPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
        private readonly Util _util = new Util();

        public Model3DVM()
        {
            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = " Loading Model3D" };
            MonitorLog.LogToMonitor(monitorItem);

            SkyServer.StaticPropertyChanged += PropertyChangedSkyServer;
            Settings.Settings.StaticPropertyChanged += PropertyChangedSettings;


            LookDirection = new Vector3D(-1.2, -140, -133);
            UpDirection = new Vector3D(-.006, -0.6, 0.7);
            Position = new Point3D(.7, 139.7, 184.2);

            LoadTopBar();
            LoadCompass();
            LoadGEM();
            Rotate();

            ActualAxisX = $"--.--";
            ActualAxisY = $"--.--";
            CameraVis = false;
            RaAxisVis = false;
            DecAxisVis = false;
            RaVis = true;
            DecVis = true;
            AzVis = true;
            AltVis = true;
            TopVis = true;
            ScreenEnabled = SkyServer.IsMountRunning;
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
                     case "Altitude":
                         if (!AltVis) return;
                         Altitude = _util.DegreesToDMS(SkyServer.Altitude, "° ", ":", "", 2);
                         break;
                     case "Azimuth":
                         if (!AzVis) return;
                         Azimuth = _util.DegreesToDMS(SkyServer.Azimuth, "° ", ":", "", 2);
                         break;
                     case "DeclinationXform":
                         if (!DecVis) return;
                         Declination = _util.DegreesToDMS(SkyServer.DeclinationXform, "° ", ":", "", 2);
                         break;
                     case "Lha":
                         Lha = _util.HoursToHMS(SkyServer.Lha, "h ", ":", "", 2);
                         break;
                     case "RightAscensionXform":
                         RightAscension = _util.HoursToHMS(SkyServer.RightAscensionXform, "h ", ":", "", 2);
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

        #endregion

        #region Viewport3D

        private Point3D _position;
        public Point3D Position
        {
            get => _position;
            set
            {
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

        private double _xaxis;
        public double Xaxis
        {
            get => _xaxis;
            set
            {
                _xaxis = value;
                OnPropertyChanged();
            }
        }

        private double _yaxis;
        public double Yaxis
        {
            get => _yaxis;
            set
            {
                _yaxis = value;
                OnPropertyChanged();
            }
        }

        private double _zaxis;
        public double Zaxis
        {
            get => _zaxis;
            set
            {
                _zaxis = value;
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
                const string gpModel = @"Models/GEM1.obj";
                var filePath = System.IO.Path.Combine(_directoryPath ?? throw new InvalidOperationException(), gpModel);
                var file = new Uri(filePath).LocalPath;
                var import = new ModelImporter();
                var color = Colors.Crimson;
                Material material = new DiffuseMaterial(new SolidColorBrush(color));
                import.DefaultMaterial = material;

                //color weights
                var a = import.Load(file);
                Material materialweights = new DiffuseMaterial(new SolidColorBrush(Colors.Black));
                if (a.Children[0] is GeometryModel3D weights) weights.Material = materialweights;

                //color OTA
                var accentColor = Settings.Settings.AccentColor;
                if (!string.IsNullOrEmpty(accentColor))
                {
                    var swatches = new SwatchesProvider().Swatches;
                    foreach (var swatch in swatches)
                    {
                        if (swatch.Name != Settings.Settings.AccentColor) continue;
                        var converter = new BrushConverter();
                        var brush = (Brush)converter.ConvertFromString(swatch.ExemplarHue.Color.ToString());

                        Material materialota = new DiffuseMaterial(brush);
                        if (a.Children[1] is GeometryModel3D ota) ota.Material = materialota;
                    }
                }

                //color weight bar
                Material materialbar = new DiffuseMaterial(new SolidColorBrush(Colors.Silver));
                if (a.Children[2] is GeometryModel3D bar) bar.Material = materialbar;
                Model = a;

                //var text = new TextBlock { Text = "Testing" };
                //Overlay.SetPosition3D(text, new Point3D(0, 0, 10));
                //a.Children.Add(text);

                Xaxis = -90;
                Yaxis = 90;
                Zaxis = -30;

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
                OpenDialog(ex.Message);
            }
        }
        private void LoadCompass()
        {
            try
            {
                const string compassN = @"Models/compassN.png";
                const string compassS = @"Models/compassS.png";
                var compassFile = SkyServer.SouthernHemisphere ? compassS : compassN;
                var filePath = System.IO.Path.Combine(_directoryPath ?? throw new InvalidOperationException(), compassFile);
                var file = new Uri(filePath).LocalPath;
                Compass = MaterialHelper.CreateImageMaterial(file, 80);
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
                OpenDialog(ex.Message);
            }
        }
        private void Rotate()
        {
            switch (SkySettings.Mount)
            {
                case MountType.Simulator:
                    Yaxis = Math.Round(SkyServer.ActualAxisX, 3);
                    Xaxis = SkyServer.SouthernHemisphere ? Math.Round(SkyServer.ActualAxisY * -1.0, 3) : Math.Round(SkyServer.ActualAxisY - 180, 3);
                    break;
                case MountType.SkyWatcher:
                    Yaxis = Math.Round(SkyServer.ActualAxisX, 3);
                    Xaxis = Math.Round(SkyServer.ActualAxisY * -1.0, 3);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
                return _openDialogCommand ?? (_openDialogCommand = new RelayCommand(
                           param => OpenDialog(null)
                       ));
            }
        }
        private void OpenDialog(string msg, string caption = null)
        {
            if (msg != null) DialogMsg = msg;
            DialogCaption = caption ?? Application.Current.Resources["msgDialog"].ToString();
            DialogContent = new DialogOK();
            IsDialogOpen = true;

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

        private ICommand _clickOkDialogCommand;
        public ICommand ClickOkDialogCommand
        {
            get
            {
                return _clickOkDialogCommand ?? (_clickOkDialogCommand = new RelayCommand(
                           param => ClickOkDialog()
                       ));
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
                return _clickCancelDialogCommand ?? (_clickCancelDialogCommand = new RelayCommand(
                           param => ClickCancelDialog()
                       ));
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
                return _runMessageDialog ?? (_runMessageDialog = new RelayCommand(
                           param => ExecuteMessageDialog()
                       ));
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

    }

    public class Overlay : DependencyObject
    {
        /// <summary>
        /// The position 3 d property.
        /// </summary>
        public static readonly DependencyProperty Position3DProperty = DependencyProperty.RegisterAttached(
            "Position3D", typeof(Point3D), typeof(Overlay));

        /// <summary>
        /// The get position 3 d.
        /// </summary>
        /// <param name="obj">
        /// The obj.
        /// </param>
        /// <returns>
        /// </returns>
        public static Point3D GetPosition3D(DependencyObject obj)
        {
            return (Point3D)obj.GetValue(Position3DProperty);
        }

        /// <summary>
        /// The set position 3 d.
        /// </summary>
        /// <param name="obj">
        /// The obj.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        public static void SetPosition3D(DependencyObject obj, Point3D value)
        {
            obj.SetValue(Position3DProperty, value);
        }

        // Using a DependencyProperty as the backing store for Position3D.  This enables animation, styling, binding, etc...
    }
}
