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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using GS.Server.Controls.Dialogs;
using GS.Server.Windows;
using GS.Shared.Command;
using ASCOM.DeviceInterface;
using Vector = System.Windows.Vector;

namespace GS.Server.Model3D
{
    public class Model3Dvm : ObservableObject, IPageVM, IDisposable
    {
        #region fields
        public string TopName => "";
        public string BottomName => "3D";
        public int Uid => 4;
        private bool _disposed;
        private readonly Util _util = new Util();
        #endregion

        public Model3Dvm()
        {
            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Ui, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = " Loading Model3D" };
            MonitorLog.LogToMonitor(monitorItem);

            SkyServer.StaticPropertyChanged += PropertyChangedSkyServer;
            Settings.Settings.StaticPropertyChanged += PropertyChangedSettings;
            SkySettings.StaticPropertyChanged += PropertyChangedSkySettings;

            LookDirection = Settings.Settings.ModelLookDirection1;
            UpDirection = Settings.Settings.ModelUpDirection1;
            Position = Settings.Settings.ModelPosition1;

            LoadTopBar();
            LoadTelescopeModel();
            Rotate();
            SetPierSideIndicator();

            FactorList = new List<int>(Enumerable.Range(1, 21));

            ActualAxisX = "--.--";
            ActualAxisY = "--.--";
            CameraVis = false;
            RaAxisVis = false;
            DecAxisVis = false;
            RaVis = true;
            HaVis = false;
            DecVis = true;
            AzVis = true;
            AltVis = true;
            RaAxisVis = false;
            DecAxisVis = false;
            SideVis = false;
            TopVis = true;
            ScreenEnabled = SkyServer.IsMountRunning;
            ModelWinVisibility = true;
            ModelType = Settings.Settings.ModelType;
            Interval = SkySettings.DisplayInterval;
            ModelFactor = Settings.Settings.ModelIntFactor;

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
                if (!IsCurrentViewModel()){return;}
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
                         var ra = _util.HoursToHMS(SkyServer.RightAscensionXForm, "h ", ":", "", 2);
                         RightAscension = _raInDegrees ? _util.DegreesToDMS(_util.HMSToDegrees(ra), "° ", ":", "", 2) : ra;
                         //RightAscension = _util.HoursToHMS(SkyServer.RightAscensionXForm, "h ", ":", "", 2);
                         break;
                     case "Rotate3DModel":
                         Rotate();
                         SetPierSideIndicator();
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
                         ActualAxisX = $"{Numbers.TruncateD(SkyServer.ActualAxisX, 3):F3}";
                         break;
                     case "ActualAxisY":
                         if (!DecAxisVis) return;
                         ActualAxisY = $"{Numbers.TruncateD(SkyServer.ActualAxisY, 3):F3}";
                         break;
                     case "Steps":
                         RawAxisX = $"{SkyServer.Steps[0],10:+00000000;-00000000; 00000000}";
                         RawAxisY = $"{SkyServer.Steps[1],10:+00000000;-00000000; 00000000}";
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
                if (!IsCurrentViewModel()) { return; }
                ThreadContext.BeginInvokeOnUiThread(
                    delegate
                    {
                        switch (e.PropertyName)
                        {
                            case "AccentColor":
                            case "ModelType":
                                ModelType = Settings.Settings.ModelType;
                                LoadTelescopeModel();
                                break;
                            case "ModelIntFactor":
                                ModelFactor = Settings.Settings.ModelIntFactor;
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
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message);
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
                if (!IsCurrentViewModel() && e.PropertyName != "AlignmentMode") { return; }
                ThreadContext.BeginInvokeOnUiThread(
             delegate
             {
                 switch (e.PropertyName)
                 {
                     case "AlignmentMode":
                         ModelType = Settings.Settings.ModelType;
                         OpenResetView();
                         break;
                     case "DisplayInterval":
                         Interval = SkySettings.DisplayInterval;
                         break;
                     case "PolarMode":
                         switch (SkyServer.PolarMode)
                         {
                             case PolarMode.Left:
                                 ModelReflect = -1;
                                 break;
                             case PolarMode.Right:
                                 ModelReflect = 1;
                                 break;
                         }
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
                var modelVm = ModelVm.Model1Vm;
                modelVm.WinHeight = 220;
                modelVm.WinWidth = 250;
                modelVm.Position = Position;
                modelVm.LookDirection = LookDirection;
                modelVm.UpDirection = UpDirection;
                modelVm.ModelReflect = ModelReflect;
                modelVm.ImageFile = ImageFile;
                modelVm.CameraIndex = 1;
                bWin.Show();
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
                if (Numbers.IsNaNVector3D(LookDirection) || Numbers.Is0Vector3D(LookDirection))
                {
                    Settings.Settings.ModelLookDirection2 = new Vector3D(-900, -1100, -400);
                }

                if (Numbers.IsNaNVector3D(UpDirection) || Numbers.Is0Vector3D(UpDirection))
                {
                    Settings.Settings.ModelUpDirection2 = new Vector3D(.35, .43, .82);
                }

                if (Numbers.IsNaNPoint3D(Position) || Numbers.Is0Point3D(Position))
                {
                    Settings.Settings.ModelPosition2 = new Point3D(900, 1100, 800);
                }

                LoadTelescopeModel();
                Rotate();
                SetPierSideIndicator();
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

        private ICommand _saveModelViewCmd;
        public ICommand SaveModelViewCmd
        {
            get
            {
                var cmd = _saveModelViewCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _saveModelViewCmd = new RelayCommand(param => SaveModelView());
            }
        }
        private void SaveModelView()
        {
            try
            {
                Settings.Settings.ModelLookDirection1 = LookDirection;
                Settings.Settings.ModelUpDirection1 = UpDirection;
                Settings.Settings.ModelPosition1 = Position;
                Settings.Settings.Save();
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

        /// <summary>
        /// Checks Selected Tab
        /// </summary>
        /// <returns></returns>
        private bool IsCurrentViewModel()
        {
            if (SkyServer.SelectedTab?.Uid != 4) { return false; }
            ScreenEnabled = SkyServer.IsMountRunning;
            return true;
        }
        #endregion

        #region Viewport3D
        private Vector3 _axisModelOffsets;

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

        private bool _haVis;
        public bool HaVis
        {
            get => _haVis;
            set
            {
                if (_haVis == value) return;
                _haVis = value;
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
                //if (_actualAxisX == value) return;
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
                //if (_actualAxisY == value) return;
                _actualAxisY = value;
                OnPropertyChanged();
            }
        }

        private string _rawAxisX;
        public string RawAxisX
        {
            get => _rawAxisX;
            private set
            {
                //if (_actualAxisX == value) return;
                _rawAxisX = value;
                OnPropertyChanged();
            }
        }

        private string _rawAxisY;
        public string RawAxisY
        {
            get => _rawAxisY;
            private set
            {
                //if (_actualAxisY == value) return;
                _rawAxisY = value;
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
                XAxisOffset = value + _axisModelOffsets.X;
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
                YAxisOffset = value + _axisModelOffsets.Y;
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
                ZAxisOffset = _axisModelOffsets.Z - value;
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

        private double _yAxisCentre;

        public double YAxisCentre
        {
            get => _yAxisCentre;
            set
            {
                _yAxisCentre = value;
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

        private double _modelReflect;
        public double ModelReflect
        {
            get => _modelReflect;
            set
            {
                if (_modelReflect == value) return;
                _modelReflect = value;
                OnPropertyChanged();
            }
        }

        public IList<int> FactorList { get; }

        private int _modelFactor;
        public int ModelFactor
        {
            get => _modelFactor;
            set
            {
                if (_modelFactor == value) {return;}
                _modelFactor = value;
                Settings.Settings.ModelIntFactor = value;
                OnPropertyChanged();
                IntervalTotal = Interval * value;
            }
        }

        private int _interval;
        public int Interval
        {
            get => SkySettings.DisplayInterval;
            set
            {
                if (value == _interval) {return;}
                _interval = value;
                OnPropertyChanged();
                _interval = value;
                IntervalTotal = value * ModelFactor;
            }
        }

        private double _intervalTotal;
        public double IntervalTotal
        {
            get => _intervalTotal;
            set
            {
                _intervalTotal = value;
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

        private double _beamWidth;
        public double BeamWidth
        {
            get => _beamWidth;
            set
            {
                _beamWidth = value;
                OnPropertyChanged();
            }
        }

        private double _beamHalfWidth;
        public double BeamHalfWidth
        {
            get => _beamHalfWidth;
            set
            {
                _beamHalfWidth = value;
                OnPropertyChanged();
            }
        }

        private double _modelPierHeight;
        public double ModelPierHeight
        {
            get => _modelPierHeight;
            set
            {
                _modelPierHeight = value;
                OnPropertyChanged();
            }
        }

        private double _modelPierPivot;
        public double ModelPierPivot
        {
            get => _modelPierPivot;
            set
            {
                _modelPierPivot = value;
                OnPropertyChanged();
            }
        }

        private double _upperBeamLength;
        public double UpperBeamLength
        {
            get => _upperBeamLength;
            set
            {
                _upperBeamLength = value;
                OnPropertyChanged();
            }
        }

        private Point3D _upperBeamMidPoint;
        public Point3D UpperBeamMidPoint
        {
            get => _upperBeamMidPoint;
            set
            {
                _upperBeamMidPoint = value;
                OnPropertyChanged();
            }
        }

        private double _upperBeamAngle;
        public double UpperBeamAngle
        {
            get => _upperBeamAngle;
            set
            {
                _upperBeamAngle = value;
                OnPropertyChanged();
            }
        }

        private double _lowerBeamLength;
        public double LowerBeamLength
        {
            get => _lowerBeamLength;
            set
            {
                _lowerBeamLength = value;
                OnPropertyChanged();
            }
        }

        private Point3D _lowerBeamMidPoint;
        public Point3D LowerBeamMidPoint
        {
            get => _lowerBeamMidPoint;
            set
            {
                _lowerBeamMidPoint = value;
                OnPropertyChanged();
            }
        }

        private double _lowerBeamAngle;
        public double LowerBeamAngle
        {
            get => _lowerBeamAngle;
            set
            {
                _lowerBeamAngle = value;
                OnPropertyChanged();
            }
        }

        private double _pierSideIndicatorStartAngle;
        public double PierSideIndicatorStartAngle
        {
            get => _pierSideIndicatorStartAngle;
            set
            {
                _pierSideIndicatorStartAngle = value;
                OnPropertyChanged();
            }
        }

        private double _pierSideIndicatorEndAngle;
        public double PierSideIndicatorEndAngle
        {
            get => _pierSideIndicatorEndAngle;
            set
            {
                _pierSideIndicatorEndAngle = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void LoadTelescopeModel()
        {
            try
            {
                CameraVis = false;

                //camera direction
                LookDirection = Settings.Settings.ModelLookDirection1;
                UpDirection = Settings.Settings.ModelUpDirection1;
                Position = Settings.Settings.ModelPosition1;
                //offset for model to match start position
                _axisModelOffsets = SkySettings.AxisModelOffsets;
                //start position
                XAxis = -90;
                YAxis = 90;
                ZAxis = 90;
                YAxisCentre = 0;
                switch (SkyServer.PolarMode)
                {
                    case PolarMode.Left:
                        ModelReflect = -1;
                        break;
                    case PolarMode.Right:
                        ModelReflect = 1;
                        break;
                }

                switch (SkySettings.AlignmentMode)
                {
                    case AlignmentModes.algAltAz:
                        break;
                    case AlignmentModes.algPolar:
                        //start position
                        ZAxis = Math.Round(Math.Abs(SkySettings.Latitude), 2);
                        break;
                    case AlignmentModes.algGermanPolar:
                    default:
                        //start position
                        ZAxis = Math.Round(Math.Abs(SkySettings.Latitude), 2);
                        YAxisCentre = Settings.Settings.YAxisCentre;
                        break;
                }

                //load compass, pier model and telescope model
                Compass = MaterialHelper.CreateImageMaterial(Shared.Model3D.GetCompassFile(SkyServer.SouthernHemisphere, SkySettings.AlignmentMode == AlignmentModes.algAltAz), 100);
                LoadPierModel();

                var import = new ModelImporter();
                var altAz = (SkySettings.AlignmentMode == AlignmentModes.algAltAz) ? "AltAz" : String.Empty;
                var model = import.Load(Shared.Model3D.GetModelFile(Settings.Settings.ModelType, altAz));

                // set up OTA color
                var accentColor = Settings.Settings.AccentColor;
                Material materialota = null;
                if (!string.IsNullOrEmpty(accentColor))
                {
                    var swatches = new SwatchesProvider().Swatches;
                    foreach (var swatch in swatches)
                    {
                        if (swatch.Name != accentColor) continue;
                        var converter = new BrushConverter();
                        var accentbrush = (Brush)converter.ConvertFromString(swatch.ExemplarHue.Color.ToString());
                        materialota = MaterialHelper.CreateMaterial(accentbrush);
                    }
                }

                // iterate over objects in model: 0 is primary OTA, 1 is weights or secondary OTA, 2 is bar
                for (int i = 0; i < model.Children.Count; i++)
                    switch (i)
                    {
                        case 0:
                            //color primary OTA
                            if (!(materialota is null) && (model.Children[0] is GeometryModel3D otaP))
                                otaP.Material = materialota;
                            break;
                        case 1:
                            //color weights or secondary ota (not for German Polar)
                            if (SkySettings.AlignmentMode == AlignmentModes.algGermanPolar)
                            {
                                var materialweights = MaterialHelper.CreateMaterial(new SolidColorBrush(Color.FromRgb(64, 64, 64)));
                                if (model.Children[1] is GeometryModel3D weights) { weights.Material = materialweights; }
                            }
                            else
                            {
                                //color secondary OTA
                                if (!(materialota is null) && (model.Children[1] is GeometryModel3D otaS))
                                    otaS.Material = materialota;
                            }
                            break;
                        case 2:
                            //color bar
                            var materialbar = MaterialHelper.CreateMaterial(Brushes.Gainsboro);
                            if (model.Children[2] is GeometryModel3D bar) { bar.Material = materialbar; }
                            break;
                        default:
                            break;
                    }
                Model = model;
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

        /// <summary>
        /// 
        /// </summary>
        private void LoadPierModel()
        {
            // Default vertical pier only
            ModelPierHeight = Settings.Settings.ModelPierHeight;
            ModelPierPivot = 0;

            BeamWidth = Settings.Settings.ModelBeamWidth;
            BeamHalfWidth = BeamWidth / 2;
            LowerBeamLength = ModelPierHeight;
            LowerBeamMidPoint = new Point3D(0, LowerBeamLength / 2, BeamHalfWidth);
            LowerBeamAngle = 90.0;

            UpperBeamLength = 0;
            UpperBeamMidPoint = new Point3D(0, 0, ModelPierHeight);
            UpperBeamAngle = 90.0;

            switch (SkySettings.AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    // Simple vertical pier
                    break;
                case AlignmentModes.algPolar:
                    // Latitude canted two beam pier
                    Vector Pier = new Vector(0.0, ModelPierHeight - BeamWidth);
                    // Upper beam
                    UpperBeamLength = 0.80 * ModelPierHeight;
                    // Absolute value for both hemispheres
                    var latitudeDeg = Math.Abs(SkySettings.Latitude);
                    var latitudeRad = SkyServer.DegToRad(latitudeDeg);
                    Vector UpperBeam = new Vector(Math.Cos(latitudeRad), -Math.Sin(latitudeRad));
                    UpperBeam = (UpperBeamLength - BeamWidth) * UpperBeam;
                    UpperBeamAngle = latitudeDeg - 90.0;
                    UpperBeamMidPoint = new Point3D(0, 0, ModelPierHeight - UpperBeamLength / 2);
                    // Lower beam
                    Vector LowerBeam = Pier + UpperBeam;
                    LowerBeamLength = LowerBeam.Length;
                    LowerBeamAngle = 90.0 - Vector.AngleBetween(LowerBeam, Pier);
                    LowerBeamMidPoint = new Point3D(0, LowerBeamLength / 2, BeamHalfWidth);
                    break;
                case AlignmentModes.algGermanPolar:
                    // Vertical pier with 90 degree extension
                    UpperBeamLength = Settings.Settings.ModelPierPivot;
                    UpperBeamMidPoint = new Point3D(0, 0, ModelPierHeight - UpperBeamLength / 2);
                    ModelPierPivot = -UpperBeamLength;
                    break;
            }
        }

        private void Rotate()
        {
            var axes = Shared.Model3D.RotateModel(SkySettings.Mount.ToString(), SkyServer.ActualAxisX,
               SkyServer.ActualAxisY, SkyServer.SouthernHemisphere, SkySettings.AlignmentMode, (int) SkyServer.PolarMode);

            YAxis = axes[0];
            XAxis = axes[1];
        }

        /// <summary>
        /// Set start and end angles for pier side indicator graphic
        /// The coordinate system is offset from North by 90 degrees
        /// and the angle increases clockwise
        /// </summary>
        private void SetPierSideIndicator()
        {
            var pierSide = SkyServer.IsSideOfPier;

            PierSideIndicatorStartAngle = 0.0;
            PierSideIndicatorEndAngle = 0.0;

            switch (SkySettings.AlignmentMode)
            {
                case AlignmentModes.algAltAz:
                    switch (pierSide)
                    {
                        case PierSide.pierEast:
                            PierSideIndicatorStartAngle = -SkySettings.AxisLimitX - 90.0;
                            PierSideIndicatorEndAngle = -90.0;
                            break;
                        case PierSide.pierWest:
                            PierSideIndicatorStartAngle = +SkySettings.AxisLimitX - 90.0;
                            PierSideIndicatorEndAngle = -90.0;
                            break;
                        default:
                            break;
                    }
                    break;
                case AlignmentModes.algPolar:
                    switch (pierSide)
                    {
                        case PierSide.pierEast:
                            PierSideIndicatorEndAngle = -SkySettings.AxisLimitX + 90.0;
                            PierSideIndicatorStartAngle = SkySettings.AxisLimitX + 90.0;
                            break;
                        case PierSide.pierWest:
                            PierSideIndicatorEndAngle = SkySettings.AxisLimitX - 90.0;
                            PierSideIndicatorStartAngle = -SkySettings.AxisLimitX - 90.0;
                            break;
                        default:
                            break;
                    }
                    break;
                case AlignmentModes.algGermanPolar:
                    switch (AdjustPierSide(pierSide))
                    {
                        case PierSide.pierEast:
                            PierSideIndicatorStartAngle = 90.0;
                            PierSideIndicatorEndAngle = -90.0;
                            break;
                        case PierSide.pierWest:
                            PierSideIndicatorStartAngle = 90.0;
                            PierSideIndicatorEndAngle = 270.0;
                            break;
                        default:
                            PierSideIndicatorStartAngle = 0.0;
                            PierSideIndicatorEndAngle = 0.0;
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// Southern hemisphere pier side is reversed
        /// </summary>
        /// <param name="pierSide"></param>
        /// <returns></returns>
        private PierSide AdjustPierSide(PierSide pierSide)
        {
            if (SkyServer.SouthernHemisphere)
            {
                switch (pierSide)
                {
                    case PierSide.pierEast:
                        return PierSide.pierWest;
                    case PierSide.pierWest:
                        return PierSide.pierEast;
                    default:
                        return PierSide.pierUnknown;
                }
            }
            else
            {
                return pierSide;
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
                // if (value == _altitude) return;
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
                // if (value == _azimuth) return;
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

        private bool _raInDegrees;

        private ICommand _raDoubleClickCommand;

        public ICommand RaDoubleClickCommand
        {
            get
            {
                var command = _raDoubleClickCommand;
                if (command != null)
                {
                    return command;
                }

                return _raDoubleClickCommand = new RelayCommand(
                    ClickRaDoubleClickCommand
                );
            }
        }

        private void ClickRaDoubleClickCommand(object parameter)
        {
            try
            {
                _raInDegrees = !_raInDegrees;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Ui,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message);
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
                Device = MonitorDevice.Ui,
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

        ~Model3Dvm()
        {
            Settings.Settings.ModelLookDirection1 = LookDirection;
            Settings.Settings.ModelUpDirection1 = UpDirection;
            Settings.Settings.ModelPosition1 = Position;
            Settings.Settings.Save();
        }
        #endregion
    }
}
