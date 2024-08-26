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
using ASCOM.Utilities;
using GS.Server.Controls.Dialogs;
using GS.Server.Windows;
using GS.Shared.Command;

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
        private readonly double _polarisRa = 2.53019444;
        private readonly double _polarisDec = 89.264167;
        //private const double _octansRa = 21.4233333;
        private const double _octansRa = 21.1463333335;
        //private const double _octansDec = -88.9563888900;
        private const double _octansDec = 0;
        private double _poleRa;
        private double _poleDec;

        #endregion

        public PoleLocatorVM()
        {
            try
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = " Loading PoleLocatorVM" };
                MonitorLog.LogToMonitor(monitorItem);

                // setup property events to monitor
                SkySettings.StaticPropertyChanged += PropertyChangedSkySettings;

                MirrorFlip = true;
                CenterX = 200;
                CenterY = 200;
                GridAngle = 0;
                Epoch = TransOptions.J2000;
                StarCenter = $"{CenterX},{30 + YearPosition()}";

                //ConvertRaDec();
                MainVM();
                Update();
                SetHemisphere();
                SetLst();
                SetDegrees();

                _timer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(1)};
                _timer.Tick += Timer_Tick;
                _timer.Start();


                ////apparent
                //var a = TransformCoords(_polarisRa, _polarisDec, "j2000", "apparent");
                //var b = _util.HoursToHMS(a.X);
                //var C = _util.DegreesToDMS(a.Y);
                
                ////topocentric
                //var d = TransformCoords(_polarisRa, _polarisDec, "j2000", "topocentric");
                //var e = _util.HoursToHMS(d.X);
                //var f = _util.DegreesToDMS(d.Y);

                ////J2000
                //var g = _polarisRa;
                //var h = _polarisDec;
                //var i = _util.HoursToHMS(g);
                //var j = _util.DegreesToDMS(h);

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
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        #region Methods


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
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                SkyServer.AlertState = true;
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
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
                if (_mainWindowVM.CurrentPageViewModel?.Uid != 5) return;

                Update();
                SetHemisphere();
                SetLst();
                SetDegrees();
            }
            catch (Exception ex)
            {
                _timer.Tick -= Timer_Tick;

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }

        }

        /// <summary>
        /// Reference to main to see which models are loaded
        /// </summary>
        private void MainVM()
        {
            if (_mainWindowVM == null) _mainWindowVM = MainWindowVM._mainWindowVm;
        }

        private Vector TransformCoords(double ra, double dec, string from, string to)
        {
            var a = Transforms.ConvertRaDec(ra, dec, SkySettings.Latitude, SkySettings.Longitude,
                SkySettings.Elevation, from, to);
            return a;
        }

        /// <summary>
        /// Calculates all the positions for both hemisphere
        /// </summary>
        private void SetDegrees()
        {
            var ha = Coordinate.Ra2Ha24(_poleRa, Lst);
            Ha12 = ha; // Coordinate.Ra2Ha12(_poleRa, Lst);
            var deg = Range.Range360(ha * 15.0);

            if (SkyServer.SouthernHemisphere)
            {
                HaDeg = Range.Range360(deg);
                HaFlipDeg = Range.Range360(deg - 180.0);
                GridAngle = MirrorFlip ? (int)Range.Range360(HaFlipDeg + 100) : (int)Range.Range360(HaDeg + 100);
            }
            else
            {
                HaDeg = 360 - deg;
                HaFlipDeg = Range.Range360(180.0 - deg);
                PolePosition = MirrorFlip ? HaFlipDeg : HaDeg;
            }

        }

        /// <summary>
        /// Sets up hemisphere information 
        /// </summary>
        private void SetHemisphere()
        {
            var shemi = SkyServer.SouthernHemisphere;
            NorthernHemisphere = !shemi;
            SouthernHemisphere = shemi;

            var tmpRa = shemi ? _octansRa : _polarisRa;
            var tmpDec = shemi ? _octansDec : _polarisDec;

            var a = new Vector(0, 0);
            switch (Epoch)
            {
                case TransOptions.Apparent:
                    //apparent
                    a = TransformCoords(tmpRa, tmpDec, "j2000", "apparent");
                    break;
                case TransOptions.Topocentric:
                    a = TransformCoords(tmpRa, tmpDec, "j2000", "topocentric");
                    break;
                case TransOptions.J2000:
                    a.X = tmpRa;
                    a.Y = tmpDec;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _poleRa = a.X;
            _poleDec = a.Y;
            Ra = _util.HoursToHMS(_poleRa,"h ", ":","",2);
            Dec = _util.DegreesToDMS(_poleDec, "° ", ":", "", 2);
        }

        /// <summary>
        /// generates local sidereal time for calculations
        /// </summary>
        private void SetLst()
        {
            UTCNow = HiResDateTime.UtcNow;
            //var gsjd = JDate.Ole2Jd(UTCNow.Add(SkySettings.UTCDateOffset));
            var gsjd = JDate.Ole2Jd(UTCNow);
            Lst = Time.Lst(JDate.Epoch2000Days(), gsjd, false, SkySettings.Longitude);
            LST = _util.HoursToHMS(Lst);
        }

        /// <summary>
        /// Calculates position of polaris based on year.
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

        private string _ra;
        public string Ra
        {
            get => _ra;
            set
            {
                if (value == _ra) return;
                _ra = value;
                OnPropertyChanged();
            }
        }

        private string _dec;
        public string Dec
        {
            get => _dec;
            set
            {
                if (value == _dec) return;
                _dec = value;
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
        public double Lst
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

        private double _polePosition;
        public double PolePosition
        {
            get => _polePosition;
            set
            {
                _polePosition = value;
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

        private TransOptions _epoch;

        public TransOptions Epoch
        {
            get => _epoch;
            set
            {
                _epoch = value;
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

    public enum TransOptions
    {
        Apparent,
        Topocentric,
        J2000
    }
}
