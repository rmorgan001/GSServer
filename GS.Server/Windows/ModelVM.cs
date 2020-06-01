/* Copyright(C) 2020  Rob Morgan (robert.morgan.e@gmail.com)

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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ASCOM.Utilities;
using GS.Principles;
using GS.Server.Controls.Dialogs;
using GS.Server.Helpers;
using GS.Server.SkyTelescope;
using GS.Shared;
using HelixToolkit.Wpf;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;

namespace GS.Server.Windows
{
    public class ModelVM : ObservableObject, IDisposable
    {
        #region Fields
        private readonly Util _util = new Util();
        private readonly string _directoryPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
        public static ModelVM _modelVM;
        #endregion

        public ModelVM()
        {
            try
            {
                using (new WaitCursor())
                {
                    var monitorItem = new MonitorEntry
                        { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Opening Model Window" };
                    MonitorLog.LogToMonitor(monitorItem);

                    SkyServer.StaticPropertyChanged += PropertyChangedSkyServer;
                    Settings.Settings.StaticPropertyChanged += PropertyChangedSettings;

                    Title = Application.Current.Resources["lbModel"].ToString();
                    ScreenEnabled = SkyServer.IsMountRunning;
                    ModelWinVisability = false;
                    TopMost = true;
                    _modelVM = this;

                    LoadImages();
                    Rotate();
                    LoadGEM();
                    LoadCompass();

                    RightAscension = "00h 00m 00s";
                    Declination = "00° 00m 00s";
                }

            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                throw;
            }
        }

        #region ViewModel

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
                     case "RightAscensionXform":
                         RightAscension = _util.HoursToHMS(SkyServer.RightAscensionXform, "h ", ":", "", 2);
                         Rotate();
                         break;
                     case "DeclinationXform":
                         Declination = _util.DegreesToDMS(SkyServer.DeclinationXform, "° ", ":", "", 2);
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
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, "Error");
            }
        }

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
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, "Error");
            }
        }

        public IList<string> ImageFiles;
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

        private void LoadImages()
        {
            if (!string.IsNullOrEmpty(ImageFile)) return;
            var random = new Random();
            ImageFiles = new List<string> { "M33.png", "Horsehead.png", "NGC6992.png", "Orion.png" };
            ImageFile = "../Resources/" + ImageFiles[random.Next(ImageFiles.Count)];
        }

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

        private string _rightAscension;
        public string RightAscension
        {
            get => _rightAscension;
            set
            {
                _rightAscension = value;
                OnPropertyChanged();
            }
        }

        private string _declination;
        public string Declination
        {
            get => _declination;
            set
            {
                _declination = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Viewport3D

        private bool _modelWinVisability;
        public bool ModelWinVisability
        {
            get => _modelWinVisability;
            set
            {
                if (_modelWinVisability == value) return;
                _modelWinVisability = value;
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
        public bool ModelOn
        {
            get => SkySettings.ModelOn;
            set
            {
                SkySettings.ModelOn = value;
                if (value)
                {
                    Rotate();
                    LoadGEM();
                    LoadCompass();
                }
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
                CameraVis = false;

                //LookDirection = _skyTelescopeVm.LookDirection;
                //UpDirection = _skyTelescopeVm.UpDirection;
                //Position = _skyTelescopeVm.Position;

                Xaxis = -90;
                Yaxis = 90;
                Zaxis = -30;

                const string gpModel = @"Models/GEM1.obj";
                var filePath = System.IO.Path.Combine(_directoryPath ?? throw new InvalidOperationException(), gpModel);
                var file = new Uri(filePath).LocalPath;
                var import = new ModelImporter();
                var color = Colors.Crimson;
                Material material = new DiffuseMaterial(new SolidColorBrush(color));
                import.DefaultMaterial = material;

                //color object
                var a = import.Load(file);
                Material materialweights = new DiffuseMaterial(new SolidColorBrush(Colors.Black));
                if (a.Children[0] is GeometryModel3D weights) weights.Material = materialweights;

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
                Material materialbar = new DiffuseMaterial(new SolidColorBrush(Colors.Silver));
                if (a.Children[2] is GeometryModel3D bar) bar.Material = materialbar;
                Model = a;
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
                OpenDialog(ex.Message, "Error");
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
                Compass = MaterialHelper.CreateImageMaterial(file, 100);
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
                OpenDialog(ex.Message, "Error");
            }
        }
        private void Rotate()
        {
            if (!ModelOn) return;

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

        #region Window Info

        private int _winHeight;
        public int WinHeight
        {
            get => _winHeight;
            set
            {
                if (_winHeight == value) return;
                _winHeight = value;
                OnPropertyChanged();
            }
        }

        private int _winWidth;
        public int WinWidth
        {
            get => _winWidth;
            set
            {
                if (_winWidth == value) return;
                _winWidth = value;
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

        private ICommand _openCloseWindowCmd;
        public ICommand OpenCloseWindowCmd
        {
            get
            {
                return _openCloseWindowCmd ?? (_openCloseWindowCmd = new RelayCommand(
                    param => CloseWindow()
                ));
            }
        }
        private void CloseWindow()
        {
            var win = Application.Current.Windows.OfType<ModelV>().FirstOrDefault();
            win?.Close();
        }

        private WindowState _windowstate;
        public WindowState Windowstate
        {
            get => _windowstate;
            set
            {
                _windowstate = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openModelWindowCmd;
        public ICommand OpenModelWindowCmd
        {
            get
            {
                return _openModelWindowCmd ?? (_openModelWindowCmd = new RelayCommand(param => OpenModelWindow()));
            }
        }
        private void OpenModelWindow()
        {
            try
            {
                //do nothing
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
                OpenDialog(ex.Message, "Error");
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

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            // GC.SuppressFinalize(this);
        }
        // NOTE: Leave out the finalizer altogether if this class doesn't
        // own unmanaged resources itself, but leave the other methods
        // exactly as they are.
        ~ModelVM()
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
