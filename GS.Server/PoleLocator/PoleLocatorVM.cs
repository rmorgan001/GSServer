/* Copyright(C) 2019-2020  Rob Morgan (robert.morgan.e@gmail.com)

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
using GS.Principles;
using GS.Server.Helpers;
using GS.Server.Main;
using GS.Server.SkyTelescope;
using GS.Shared;
using MaterialDesignThemes.Wpf;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
//using ASCOM.Astrometry.Transform;
using ASCOM.Utilities;
using GS.Server.Controls.Dialogs;

namespace GS.Server.PoleLocator
{
    public class PoleLocatorVM : ObservableObject, IPageVM, IDisposable
    {
        #region Fields

        public string TopName => "Pole";
        public string BottomName => "Locator";
        public int Uid => 5;

        private MainWindowVM _mainWindowVM;
        private readonly Util _util = new Util();
        private readonly DispatcherTimer _timer;
        private double _polaris = 2.53019444;
        private const double _octans = 21.4233333;
        private double _poleRa;

        #endregion

        public PoleLocatorVM()
        {
            try
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = " Loading PoleLocatorVM" };
                MonitorLog.LogToMonitor(monitorItem);

                // setup property events to monitor
                SkySettings.StaticPropertyChanged += PropertyChangedSkySettings;

                MirrorFlip = true;
                CenterX = 200;
                CenterY = 200;
                GridAngle = 0;
                StarCenter = $"{CenterX},{30 + YearPosition()}";

                //ConvertRaDec();
                MainVM();
                Update();
                SetHemi();
                SetLst();
                SetDegrees();

                _timer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(1)};
                _timer.Tick += Timer_Tick;
                _timer.Start();
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

                OpenDialog(ex.Message, "Error");
            }
        }

        #region Methods

        //private void ConvertRaDec()
        //{
        //    var xform = new Transform();
        //    xform.SiteElevation = SkySettings.Elevation;
        //    xform.SiteLatitude = SkySettings.Latitude;
        //    xform.SiteLongitude = SkySettings.Longitude;
        //    xform.Refraction = SkySettings.Refraction;
        //    xform.SiteTemperature = SkySettings.Temperature;
        //    xform.SetTopocentric(2.53019444, 89.26417);
        //    //new Vector(xform.RATopocentric, xform.DECTopocentric);

        //    _polaris = xform.RATopocentric;
        //}

        /// <summary>
        /// Property changes from the server
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
                     case "Longitude":
                     case "Latitude":
                         Update();
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
                OpenDialog(ex.Message, "Error");
            }
        }

        /// <summary>
        /// pulls latest lat long 
        /// </summary>
        private void Update()
        {
            Long = _util.DegreesToDMS(SkySettings.Longitude, "° ", ":", "", 2);
            Lat = _util.DegreesToDMS(SkySettings.Latitude, "° ", ":", "", 2);
        }

        /// <summary>
        /// Dispatch timer to update screen every second
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                // don't run if on a different viewmodel
                if (_mainWindowVM == null) return;
                if (_mainWindowVM.CurrentPageViewModel.Uid != 5) return;

                Update();
                SetHemi();
                SetLst();
                SetDegrees();
            }
            catch (Exception ex)
            {
                _timer.Tick -= Timer_Tick;

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

                OpenDialog(ex.Message, "Error");
            }

        }

        /// <summary>
        /// Reference to main to see which models are loaded
        /// </summary>
        private void MainVM()
        {
            if (_mainWindowVM == null) _mainWindowVM = MainWindowVM._mainWindowVm;
        }

        /// <summary>
        /// Caclulates all the positions for both hemis
        /// </summary>
        private void SetDegrees()
        {
            var ha = Coordinate.Ra2Ha24(_poleRa, lst);
            Ha12 = Coordinate.Ra2Ha12(_poleRa, lst);
            var deg = Range.Range360(ha * 15);

            if (SkyServer.SouthernHemisphere)
            {
                HaDeg = Range.Range360(deg);
                HaFlipDeg = Range.Range360(deg - 180);
                GridAngle = MirrorFlip ? (int)Range.Range360(HaFlipDeg + 100) : (int)Range.Range360(HaDeg + 100);
            }
            else
            {
                HaDeg = 360 - deg;
                HaFlipDeg = Range.Range360(180 - deg);
                PolePosition = MirrorFlip ? HaFlipDeg : HaDeg;
            }

        }

        /// <summary>
        /// Sets up hemi information 
        /// </summary>
        private void SetHemi()
        {
            var shemi = SkyServer.SouthernHemisphere;
            NorthernHemisphere = !shemi;
            SouthernHemisphere = shemi;

            _poleRa = shemi ? _octans : _polaris;
        }

        /// <summary>
        /// generates local sidereal time for calculations
        /// </summary>
        private void SetLst()
        {
            UTCNow = HiResDateTime.UtcNow;
            var gsjd = JDate.Ole2Jd(UTCNow.Add(SkySettings.UTCDateOffset));
            lst = Time.Lst(JDate.Epoch2000Days(), gsjd, false, SkySettings.Longitude);
            LST = _util.HoursToHMS(lst);
        }

        /// <summary>
        /// Caclulates position of polaris based on year.
        /// </summary>
        /// <returns></returns>
        private double YearPosition()
        {
            var yr = DateTime.Now.Year - 2022;
            return yr;
        }

        #endregion

        #region Properties

        private bool _mirrorFlip;
        public bool MirrorFlip
        {
            get => _mirrorFlip;
            set
            {
                if (_mirrorFlip == value) return;
                _mirrorFlip = value;
                OnPropertyChanged();
            }
        }
        
        private string _lat;

        public string Lat
        {
            get => _lat;
            set
            {
                if (value == _lat) return;
                _lat = value;
                OnPropertyChanged();
            }
        }

        private string _long;

        public string Long
        {
            get => _long;
            set
            {
                if (value == _long) return;
                _long = value;
                OnPropertyChanged();
            }
        }

        private string _lST;

        public string LST
        {
            get => _lST;
            set
            {
                if (value == _lST) return;
                _lST = value;
                OnPropertyChanged();
            }
        }

        private double _lst;
        public double lst
        {
            get => _lst;
            set
            {
                _lst = value;
                OnPropertyChanged();
            }
        }

        private double _haDeg;
        public double HaDeg
        {
            get => _haDeg;
            set
            {
                _haDeg = value;
                OnPropertyChanged();
            }
        }

        private double _ha12;
        public double Ha12
        {
            get => _ha12;
            set
            {
                _ha12 = value;
                Ha12Str = _util.HoursToHMS(value);
            }
        }

        private string _ha12Str;
        public string Ha12Str
        {
            get => _ha12Str;
            set
            {
                _ha12Str = value;
                OnPropertyChanged();
            }
        }

        private double _haFlipDeg;
        public double HaFlipDeg
        {
            get => _haFlipDeg;
            set
            {
                _haFlipDeg = value;
                OnPropertyChanged();
            }
        }

        private double _polePostion;
        public double PolePosition
        {
            get => _polePostion;
            set
            {
                _polePostion = value;
                OnPropertyChanged();
            }
        }

        private bool _southernHemisphere;
        public bool SouthernHemisphere
        {
            get => _southernHemisphere;
            set
            {
                if (_southernHemisphere == value) return;
                _southernHemisphere = value;
                OnPropertyChanged();
            }
        }

        private bool _northernHemisphere;
        public bool NorthernHemisphere
        {
            get => _northernHemisphere;
            set
            {
                if (_northernHemisphere == value) return;
                _northernHemisphere = value;
                OnPropertyChanged();
            }
        }

        private DateTime _utcNow;

        public DateTime UTCNow
        {
            get => _utcNow;
            set
            {
                _utcNow = value;
                OnPropertyChanged();
            }
        }
        
        private string _centerXY;
        public string CenterXY
        {
            get => _centerXY;
            set
            {
                _centerXY = value;
                OnPropertyChanged();
            }
        }

        private double _centerX;
        public double CenterX
        {
            get => _centerX;
            set
            {
                _centerX = value;
                CenterXY = $"{value},{CenterY}";
                OnPropertyChanged();
            }
        }

        private double _centerY;
        public double CenterY
        {
            get => _centerY;
            set
            {
                _centerY = value;
                CenterXY = $"{CenterX},{value}";
                OnPropertyChanged();
            }
        }

        private string _starCenter;
        public string StarCenter
        {
            get => _starCenter;
            set
            {
                _starCenter = value;
                OnPropertyChanged();
            }
        }

        private int _gridAngle;
        public int GridAngle
        {
            get => _gridAngle;
            set
            {
                _gridAngle = value;
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
        ~PoleLocatorVM()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        // The bulk of the clean-up code is implemented in Dispose(bool)
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                SkySettings.StaticPropertyChanged -= PropertyChangedSkySettings;
                _timer.Tick -= Timer_Tick;
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
