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
using GS.Shared.Command;
using HelixToolkit.Wpf;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;

namespace GS.Server.Windows
{
    public class ModelVM : ObservableObject, IDisposable
    {
        #region Fields
        private readonly Util _util = new Util();
        public static ModelVM _modelVM;
        #endregion

        public ModelVM()
        {
            try
            {
                using (new WaitCursor())
                {
                    var monitorItem = new MonitorEntry
                        { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = "Opening Model Window" };
                    MonitorLog.LogToMonitor(monitorItem);

                    SkyServer.StaticPropertyChanged += PropertyChangedSkyServer;
                    Settings.Settings.StaticPropertyChanged += PropertyChangedSettings;

                    Title = Application.Current.Resources["3dModel"].ToString();
                    ScreenEnabled = SkyServer.IsMountRunning;
                    ModelWinVisibility = false;
                    TopMost = true;
                    _modelVM = this;

                    LoadImages();
                    Rotate();
                    LoadGEM();

                    RightAscension = _util.HoursToHMS(SkyServer.RightAscensionXForm, "h ", ":", "", 2);
                    Declination = _util.DegreesToDMS(SkyServer.DeclinationXForm, "° ", ":", "", 2);
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
                     case "RightAscensionXForm":
                         RightAscension = _util.HoursToHMS(SkyServer.RightAscensionXForm, "h ", ":", "", 2);
                         Rotate();
                         break;
                     case "DeclinationXForm":
                         Declination = _util.DegreesToDMS(SkyServer.DeclinationXForm, "° ", ":", "", 2);
                         break;
                 }
             });
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
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
                    Device = MonitorDevice.Server,
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
        private double xAxisOffset;
        private double yAxisOffset;
        private double zAxisOffset;

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

        public int CameraIndex { get; set; }

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
                }
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
                if (CameraIndex == 1)
                {
                    LookDirection = Settings.Settings.ModelLookDirection1;
                    UpDirection = Settings.Settings.ModelUpDirection1;
                    Position = Settings.Settings.ModelPosition1;
                }
                else
                {
                    LookDirection = Settings.Settings.ModelLookDirection2;
                    UpDirection = Settings.Settings.ModelUpDirection2;
                    Position = Settings.Settings.ModelPosition2;
                }


                //offset for model to match start position
                xAxisOffset = 90;
                yAxisOffset = -90;
                zAxisOffset = 0;

                //start position
                XAxis = -90;
                YAxis = 90;
                ZAxis = Math.Round(Math.Abs(SkySettings.Latitude), 2);

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
                    Device = MonitorDevice.Server,
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
        private void Rotate()
        {
            var axes = Shared.Model3D.RotateModel(SkySettings.Mount.ToString(), SkyServer.ActualAxisX,
                SkyServer.ActualAxisY, SkyServer.SouthernHemisphere);

            YAxis = axes[0];
            XAxis = axes[1];
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
                //do nothing
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
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
                if (CameraIndex == 1)
                {
                    Settings.Settings.ModelLookDirection1 = new Vector3D(-2616, -3167, -1170);
                    Settings.Settings.ModelUpDirection1 = new Vector3D(.35, .43, .82);
                    Settings.Settings.ModelPosition1 = new Point3D(2523, 3000, 1379);
                }
                else
                {
                    Settings.Settings.ModelLookDirection2 = new Vector3D(-900, -1100, -400);
                    Settings.Settings.ModelUpDirection2 = new Vector3D(.35, .43, .82);
                    Settings.Settings.ModelPosition2 = new Point3D(900, 1100, 800);
                }
                LookDirection = new Vector3D(-900, -1100, -400);
                UpDirection = new Vector3D(.35, .43, .82);
                Position = new Point3D(900, 1100, 800);
                
                LoadGEM();
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
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
            var win = Application.Current.Windows.OfType<ModelV>().FirstOrDefault();
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
                Device = MonitorDevice.Server,
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
