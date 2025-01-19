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
using GS.Principles;
using GS.Server.Helpers;
using GS.Server.Main;
using GS.Server.Phd;
using GS.Server.SkyTelescope;
using GS.Shared;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Geared;
using LiveCharts.Wpf;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GS.Server.Controls.Dialogs;
using GS.Server.Windows;
using GS.Shared.Command;
using Brush = System.Windows.Media.Brush;
using Color = System.Drawing.Color;


namespace GS.Server.Pulses
{
    public sealed class PulsesVM : ObservableObject, IPageVM, IDisposable
    {
        #region Fields
        public string TopName => "";
        public string BottomName => "Pulses";
        public int Uid => 6;

        private DispatcherTimer _xAxisTimer;
        private CancellationTokenSource _cts;
        private CancellationToken _ct;

        //PHD
        private GuiderImpl _phd;
        private CancellationTokenSource _ctsPhd;
        private CancellationToken _ctPhd;

        private const string BaseLogName = "Pulses";

        #endregion

        public PulsesVM()
        {
            try
            {
                using (new WaitCursor())
                {
                    var monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow,
                        Device = MonitorDevice.UI,
                        Category = MonitorCategory.Interface,
                        Type = MonitorType.Information,
                        Method = MethodBase.GetCurrentMethod()?.Name,
                        Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = "Loading PulsesVM"
                    };
                    MonitorLog.LogToMonitor(monitorItem);

                    // Pulse events
                    MonitorQueue.StaticPropertyChanged += PropertyChangedMonitor;
                    // Phd events
                    GuiderImpl.PropertyChanged += PropertyChangedGuiding;
                    SkySettings.StaticPropertyChanged += PropertyChangedSkySettings;

                    DecBacklashList = new List<int>(Enumerable.Range(0, 1001));
                    var extendedlist = new List<int>(Numbers.InclusiveIntRange(1000, 3000, 100));
                    DecBacklashList = DecBacklashList.Concat(extendedlist);

                    LoadDefaultSettings();
                    LoadPulsesDefaults();
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}|{ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        #region RaDur

        public GearedValues<PointModel> RaDur { get; set; }

        private string _raDurTitle;
        public string RaDurTitle
        {
            get => _raDurTitle;
            set
            {
                if (_raDurTitle == value) return;
                _raDurTitle = value;
                OnPropertyChanged();
            }
        }

        private bool _raDurToggle;
        public bool RaDurToggle
        {
            get => _raDurToggle;
            set
            {
                if (_raDurToggle == value) return;
                _raDurToggle = value;
                OnPropertyChanged();
            }
        }

        private bool _raDurInvert;
        public bool RaDurInvert
        {
            get => _raDurInvert;
            set
            {
                if (_raDurInvert == value) return;
                _raDurInvert = value;
                OnPropertyChanged();
            }
        }

        private string _raDurColor;
        public string RaDurColor
        {
            get => _raDurColor;
            set
            {
                if (_raDurColor == value) return;
                _raDurColor = value;
                OnPropertyChanged();
            }
        }

        private int _raDurPtSz;
        public int RaDurPtSz
        {
            get => _raDurPtSz;
            set
            {
                if (_raDurPtSz == value) return;
                _raDurPtSz = value;
                OnPropertyChanged();
            }
        }

        private ChartSeriesType _raDurSeries;
        public ChartSeriesType RaDurSeries
        {
            get => _raDurSeries;
            set
            {
                if (_raDurSeries == value) return;
                _raDurSeries = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region RaRej

        public GearedValues<PointModel> RaRej { get; set; }

        private string _raRejTitle;
        public string RaRejTitle
        {
            get => _raRejTitle;
            set
            {
                if (_raRejTitle == value) return;
                _raRejTitle = value;
                OnPropertyChanged();
            }
        }

        private bool _raRejToggle;
        public bool RaRejToggle
        {
            get => _raRejToggle;
            set
            {
                if (_raRejToggle == value) return;
                _raRejToggle = value;
                OnPropertyChanged();
            }
        }

        private bool _raRejInvert;
        public bool RaRejInvert
        {
            get => _raRejInvert;
            set
            {
                if (_raRejInvert == value) return;
                _raRejInvert = value;
                OnPropertyChanged();
            }
        }

        private string _raRejColor;
        public string RaRejColor
        {
            get => _raRejColor;
            set
            {
                if (_raRejColor == value) return;
                _raRejColor = value;
                OnPropertyChanged();
            }
        }

        private int _raRejPtSz;
        public int RaRejPtSz
        {
            get => _raRejPtSz;
            set
            {
                if (_raRejPtSz == value) return;
                _raRejPtSz = value;
                OnPropertyChanged();
            }
        }

        private ChartSeriesType _raRejSeries;
        public ChartSeriesType RaRejSeries
        {
            get => _raRejSeries;
            set
            {
                if (_raRejSeries == value) return;
                _raRejSeries = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region DecDur

        public GearedValues<PointModel> DecDur { get; set; }

        private string _decDurTitle;
        public string DecDurTitle
        {
            get => _decDurTitle;
            set
            {
                if (_decDurTitle == value) return;
                _decDurTitle = value;
                OnPropertyChanged();
            }
        }

        private bool _decDurToggle;
        public bool DecDurToggle
        {
            get => _decDurToggle;
            set
            {
                if (_decDurToggle == value) return;
                _decDurToggle = value;
                OnPropertyChanged();
            }
        }

        private bool _decDurInvert;
        public bool DecDurInvert
        {
            get => _decDurInvert;
            set
            {
                if (_decDurInvert == value) return;
                _decDurInvert = value;
                OnPropertyChanged();
            }
        }

        private string _decDurColor;
        public string DecDurColor
        {
            get => _decDurColor;
            set
            {
                if (_decDurColor == value) return;
                _decDurColor = value;
                OnPropertyChanged();
            }
        }

        private int _decDurPtSz;
        public int DecDurPtSz
        {
            get => _decDurPtSz;
            set
            {
                if (_decDurPtSz == value) return;
                _decDurPtSz = value;
                OnPropertyChanged();
            }
        }

        private ChartSeriesType _decDurSeries;
        public ChartSeriesType DecDurSeries
        {
            get => _decDurSeries;
            set
            {
                if (_decDurSeries == value) return;
                _decDurSeries = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region DecRej
        public GearedValues<PointModel> DecRej { get; set; }

        private string _decRejTitle;
        public string DecRejTitle
        {
            get => _decRejTitle;
            set
            {
                if (_decRejTitle == value) return;
                _decRejTitle = value;
                OnPropertyChanged();
            }
        }

        private bool _decRejToggle;
        public bool DecRejToggle
        {
            get => _decRejToggle;
            set
            {
                if (_decRejToggle == value) return;
                _decRejToggle = value;
                OnPropertyChanged();
            }
        }

        private bool _decRejInvert;
        public bool DecRejInvert
        {
            get => _decRejInvert;
            set
            {
                if (_decRejInvert == value) return;
                _decRejInvert = value;
                OnPropertyChanged();
            }
        }

        private string _decRejColor;
        public string DecRejColor
        {
            get => _decRejColor;
            set
            {
                if (_decRejColor == value) return;
                _decRejColor = value;
                OnPropertyChanged();
            }
        }

        private int _decRejPtSz;
        public int DecRejPtSz
        {
            get => _decRejPtSz;
            set
            {
                if (_decRejPtSz == value) return;
                _decRejPtSz = value;
                OnPropertyChanged();
            }
        }

        private ChartSeriesType _decRejSeries;
        public ChartSeriesType DecRejSeries
        {
            get => _decRejSeries;
            set
            {
                if (_decRejSeries == value) return;
                _decRejSeries = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        private ICommand _clickPulsesStartCmd;
        public ICommand ClickPulsesStartCmd
        {
            get
            {
                var cmd = _clickPulsesStartCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _clickPulsesStartCmd = new RelayCommand(param => PulsesStartCmd());
            }
            set => _clickPulsesStartCmd = value;
        }
        private void PulsesStartCmd()
        {
            try
            {
                using (new WaitCursor())
                {
                    IsPulsing = !IsPulsing;
                    if (IsPulsing)
                    {
                        StartPulses();
                        if (RaPhdToggle || DecPhdToggle) ClickPhdConnectCmd.Execute(null);
                        return;
                    }
                    PhdClose();

                }
            }
            catch (Exception ex)
            {
                PhdClose();
                IsPulsing = false;
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}|{ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickPulsesClearCmd;
        public ICommand ClickPulsesClearCmd
        {
            get
            {
                var cmd = _clickPulsesClearCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _clickPulsesClearCmd = new RelayCommand(param => PulsesClearCmd());
            }
            set => _clickPulsesClearCmd = value;
        }
        private void PulsesClearCmd()
        {
            try
            {
                using (new WaitCursor())
                {
                    ClearPulses();
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}|{ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickPulsesSizeCmd;
        public ICommand ClickPulsesSizeCmd
        {
            get
            {
                var cmd = _clickPulsesSizeCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _clickPulsesSizeCmd = new RelayCommand(param => PulsesSizeCmd());
            }
            set => _clickPulsesSizeCmd = value;
        }
        private void PulsesSizeCmd()
        {
            try
            {
                ResizeAxes();
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickPulsesPauseCmd;
        public ICommand ClickPulsesPauseCmd
        {
            get
            {
                var cmd = _clickPulsesPauseCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _clickPulsesPauseCmd = new RelayCommand(param => PulsesPauseCmd());
            }
            set => _clickPulsesPauseCmd = value;
        }
        private void PulsesPauseCmd()
        {
            try
            {
                if (!IsPulsing) return;
                if (_xAxisTimer.IsEnabled)
                {
                    _xAxisTimer.Stop();
                    PauseBadgeContent = Application.Current.Resources["pulOn"].ToString();
                }
                else
                {
                    _xAxisTimer.Start();
                    PauseBadgeContent = "";
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickPulsesSeriesCmd;
        public ICommand ClickPulsesSeriesCmd
        {
            get
            {
                var cmd = _clickPulsesSeriesCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _clickPulsesSeriesCmd = new RelayCommand(PulsesSeriesCmd);
            }
            set => _clickPulsesSeriesCmd = value;
        }
        private void PulsesSeriesCmd(object param)
        {
            try
            {
                var cnt = PulsesCollection.Count;
                var title = (TitleItem)param;

                for (var i = 0; i < cnt; i++)
                {
                    var a = PulsesCollection[i];
                    if (a.Title != title.TitleName) continue;
                    var type = a.GetType();
                    if (type == typeof(GColumnSeries))
                    {
                        if (a is GColumnSeries series) series.Visibility = a.IsSeriesVisible ? Visibility.Collapsed : Visibility.Visible;
                    }
                    if (type == typeof(GLineSeries))
                    {
                        if (a is GLineSeries series) series.Visibility = a.IsSeriesVisible ? Visibility.Collapsed : Visibility.Visible;
                    }
                    if (type == typeof(GStepLineSeries))
                    {
                        if (a is GStepLineSeries series) series.Visibility = a.IsSeriesVisible ? Visibility.Collapsed : Visibility.Visible;
                    }
                    if (type == typeof(GScatterSeries))
                    {
                        if (a is GScatterSeries series) series.Visibility = a.IsSeriesVisible ? Visibility.Collapsed : Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickPhdConnectCmd;
        public ICommand ClickPhdConnectCmd
        {
            get
            {
                var cmd = _clickPhdConnectCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _clickPhdConnectCmd = new RelayCommand(async param => await PhdConnectCmd());
            }
            set => _clickPhdConnectCmd = value;
        }
        private async Task PhdConnectCmd()
        {
            try
            {
                if (!IsPulsing)
                {
                    OpenDialog("Start pulsing first");
                    return;
                }
                if (_phd == null)
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    await PhdAsync();
                }
                else
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    PhdClose();
                }
            }
            catch (Exception ex)
            {
                _phd = null;
                PhdBadgeContent = "";

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

                OpenDialog(ex.Message);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private ICommand _clickPulsesZoomCmd;
        public ICommand ClickPulsesZoomCmd
        {
            get
            {
                var cmd = _clickPulsesZoomCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _clickPulsesZoomCmd = new RelayCommand(PulsesZoomCmd);
            }
            set => _clickPulsesZoomCmd = value;
        }
        private void PulsesZoomCmd(object param)
        {
            try
            {
                Zoom = (string)param;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickUpZoomCmd;
        public ICommand ClickUpZoomCmd
        {
            get
            {
                var cmd = _clickUpZoomCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _clickUpZoomCmd = new RelayCommand(param => UpZoomCmd());
            }
            set => _clickUpZoomCmd = value;
        }
        private void UpZoomCmd()
        {
            try
            {
                DoYMinMaxCalc = false;
                double step;
                switch (Zoom)
                {
                    case "X":
                        step = Math.Abs(AxisXMax - AxisXMin) / 100 * 10.0;
                        AxisXMax -= step;
                        AxisXMin += step;

                        break;
                    case "Y":
                        step = Math.Abs(AxisYMax - AxisYMin) / 100 * 10.0;
                        AxisYMax -= step;
                        AxisYMin += step;
                        break;
                    case "Xy":
                        //X
                        step = Math.Abs(AxisXMax - AxisXMin) / 100 * 10.0;
                        AxisXMax -= step;
                        AxisXMin += step;
                        //Y
                        step = Math.Abs(AxisYMax - AxisYMin) / 100 * 10.0;
                        AxisYMax -= step;
                        AxisYMin += step;
                        break;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickDownZoomCmd;
        public ICommand ClickDownZoomCmd
        {
            get
            {
                var cmd = _clickDownZoomCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _clickDownZoomCmd = new RelayCommand(param => DownZoomCmd());
            }
            set => _clickDownZoomCmd = value;
        }
        private void DownZoomCmd()
        {
            try
            {
                DoYMinMaxCalc = false;
                double step;
                switch (Zoom)
                {
                    case "X":
                        step = Math.Abs(AxisXMax - AxisXMin) / 100 * 10.0;
                        AxisXMax += step;
                        AxisXMin -= step;
                        break;
                    case "Y":
                        step = Math.Abs(AxisYMax - AxisYMin) / 100 * 10.0;
                        AxisYMax += step;
                        AxisYMin -= step;
                        break;
                    case "Xy":
                        //X
                        step = Math.Abs(AxisXMax - AxisXMin) / 100 * 10.0;
                        AxisXMax += step;
                        AxisXMin -= step;
                        //Y
                        step = Math.Abs(AxisYMax - AxisYMin) / 100 * 10.0;
                        AxisYMax += step;
                        AxisYMin -= step;
                        break;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _rangeChangedCmd;
        public ICommand RangeChangedCmd
        {
            get
            {
                var cmd = _rangeChangedCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _rangeChangedCmd = new RelayCommand(RangeChanged);
            }
            set => _rangeChangedCmd = value;
        }
        private void RangeChanged(object param)
        {
            try
            {
                DoYMinMaxCalc = false;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        #endregion

        #region ViewModel 
        private void PropertyChangedMonitor(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ThreadContext.InvokeOnUiThread(
                    delegate
                    {
                        switch (e.PropertyName)
                        {
                            case "PulseEntry":
                                ProcessPulse(MonitorQueue.PulseEntry);
                                break;
                            case "CmdjSentEntry":
                                break;
                        }
                    }, _ct);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }
        private void PropertyChangedGuiding(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ThreadContext.BeginInvokeOnUiThread(
                    delegate
                    {
                        switch (e.PropertyName)
                        {
                            case "PhdGuideStep":
                                ProcessPhd(_phd?.PhdGuideStep);
                                break;
                        }
                    });
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

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
                ThreadContext.BeginInvokeOnUiThread(
             delegate
             {
                 switch (e.PropertyName)
                 {
                     case "DecBacklash":
                         DecBacklash = SkySettings.DecBacklash;
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
        private void XAxisTimer_Tick(object sender, EventArgs e)
        {
            SetXAxisLimits(HiResDateTime.UtcNow.ToLocalTime());
        }
        
        public IEnumerable<int> DecBacklashList { get; }

        public int DecBacklash
        {
            get => SkySettings.DecBacklash;
            set
            {
                if (SkySettings.DecBacklash == value) return;
                SkySettings.DecBacklash = value;
                OnPropertyChanged();
            }
        }

        private void CalcAxisYMinMax(double value)
        {
            if (!DoYMinMaxCalc) { return; }

            if (value > AxisYMax || double.IsNaN(AxisYMax))
            {
                AxisYMax = value;
            }

            if (value < AxisYMin || double.IsNaN(AxisYMin))
            {
                AxisYMin = value;
            }

            var dist = Math.Abs(AxisYMax - AxisYMin) * 1.0;
            var per = Math.Abs(dist) / 100 * 10.0;

            if (value + per > AxisYMax)
            {
                AxisYMax = value + per;
            }

            if (value - per < AxisYMin)
            {
                AxisYMin = value - per;
            }
        }

        private bool DoYMinMaxCalc { get; set; }
        #endregion

        #region Pulses Settings
        public List<string> ColorsList { get; set; }
        public IList<int> PointSizes { get; set; }

        private BindingList<TitleItem> _titleItems;
        public BindingList<TitleItem> TitleItems
        {
            get => _titleItems;
            set
            {
                if (_titleItems == value) return;
                _titleItems = value;
                OnPropertyChanged();
            }
        }

        private CartesianMapper<PointModel> _mapper;
        public CartesianMapper<PointModel> Mapper
        {
            get => _mapper;
            set
            {
                if (_mapper == value) return;
                _mapper = value;
                OnPropertyChanged();
            }
        }

        private TimeSpan _animationsSpeed;
        public TimeSpan AnimationsSpeed
        {
            get => _animationsSpeed;
            set
            {
                if (_animationsSpeed == value) return;
                _animationsSpeed = value;
                OnPropertyChanged();
            }
        }
        public IList<int> AnimationTimes { get; set; }

        private int _animationTime;
        public int AnimationTime
        {
            get => _animationTime;
            set
            {
                if (_animationTime == value) return;
                _animationTime = value;
                AnimationsSpeed = TimeSpan.FromMilliseconds(value * 100);
                OnPropertyChanged();
            }
        }

        private bool _animationsDisabled;
        public bool AnimationsDisabled
        {
            get => _animationsDisabled;
            set
            {
                if (_animationsDisabled == value) return;
                _animationsDisabled = value;
                OnPropertyChanged();
            }
        }

        private long _axisXUnit;
        public long AxisXUnit
        {
            get => _axisXUnit;
            set
            {
                if (_axisXUnit == value) return;
                _axisXUnit = value;
                OnPropertyChanged();
            }
        }

        private double _axisXMax;
        public double AxisXMax
        {
            get => _axisXMax;
            set
            {
                if (Math.Abs(_axisXMax - value) < 0) return;
                _axisXMax = value;
                OnPropertyChanged();
            }
        }

        private double _axisXMin;
        public double AxisXMin
        {
            get => _axisXMin;
            set
            {
                if (Math.Abs(_axisXMin - value) < 0) return;
                _axisXMin = value;
                OnPropertyChanged();
            }
        }

        public IList<int> AxisMinSecondsRange { get; set; }

        private int _axisXSeconds;
        public int AxisXSeconds
        {
            get => _axisXSeconds;
            set
            {
                if (_axisXSeconds == value) return;
                _axisXSeconds = value;
                OnPropertyChanged();
            }
        }

        private double _axisYUnit;
        public double AxisYUnit
        {
            get => _axisYUnit;
            set
            {
                if (Math.Abs(_axisYUnit - value) < 0) return;
                _axisYUnit = value;
                OnPropertyChanged();
            }
        }

        private double _axisYMax;
        public double AxisYMax
        {
            get => _axisYMax;
            set
            {
                if (Math.Abs(_axisYMax - value) < 0) return;
                _axisYMax = value;
                OnPropertyChanged();
            }
        }

        private double _axisYMin;
        public double AxisYMin
        {
            get => _axisYMin;
            set
            {
                if (Math.Abs(_axisYMin - value) < 0) return;
                _axisYMin = value;
                OnPropertyChanged();
            }
        }

        private Quality _chartQuality;
        public Quality ChartQuality
        {
            get => _chartQuality;
            set
            {
                if (_chartQuality == value) return;
                _chartQuality = value;
                ChartsQuality(value);
                OnPropertyChanged();
            }
        }

        private Func<double, string> _formatterX;
        public Func<double, string> FormatterX
        {
            get => _formatterX;
            set
            {
                if (_formatterX == value) return;
                _formatterX = value;
                OnPropertyChanged();
            }
        }

        private Func<double, string> _formatterY;
        public Func<double, string> FormatterY
        {
            get => _formatterY;
            set
            {
                if (_formatterY == value) return;
                _formatterY = value;
                OnPropertyChanged();
            }
        }

        public IList<double> MaxPointsRange { get; set; }

        private double _maxPoints;
        public double MaxPoints
        {
            get => _maxPoints;
            set
            {
                _maxPoints = value;
                OnPropertyChanged();
            }
        }

        private bool _isPulsing;
        public bool IsPulsing
        {
            get => _isPulsing;
            set
            {
                if (_isPulsing == value) return;
                _isPulsing = value;
                if (value)
                {
                    LogStart();
                    _xAxisTimer.Start();
                    _cts = new CancellationTokenSource();
                    _ct = _cts.Token;
                    SkyServer.MonitorPulse = true;
                    StartBadgeContent = Application.Current.Resources["pulOn"].ToString();
                    PauseBadgeContent = "";
                }
                else
                {
                    SkyServer.MonitorPulse = false;
                    MonitorLog.GetPulses = false;
                    MonitorLog.GetJEntries = false;
                    _xAxisTimer.Stop();
                    _cts?.Cancel();
                    _cts = null;
                    StartBadgeContent = "";
                    PauseBadgeContent = "";
                }
                OnPropertyChanged();
            }
        }

        public IList<int> Smoothness { get; set; }

        private int _lineSmoothness;
        public int LineSmoothness
        {
            get => _lineSmoothness;
            set
            {
                if (_lineSmoothness == value) return;
                _lineSmoothness = value;
                OnPropertyChanged();
            }
        }

        public SeriesCollection _pulsesCollection;
        public SeriesCollection PulsesCollection
        {
            get => _pulsesCollection;
            set
            {
                if (_pulsesCollection == value) return;
                _pulsesCollection = value;
                OnPropertyChanged();
            }
        }

        private string _startBadgeContent;
        public string StartBadgeContent
        {
            get => _startBadgeContent;
            set
            {
                if (_startBadgeContent == value) return;
                _startBadgeContent = value;
                OnPropertyChanged();
            }
        }

        private string _pauseBadgeContent;
        public string PauseBadgeContent
        {
            get => _pauseBadgeContent;
            set
            {
                if (_pauseBadgeContent == value) return;
                _pauseBadgeContent = value;
                OnPropertyChanged();
            }
        }

        private ChartScale _scale;
        public ChartScale Scale
        {
            get => _scale;
            set
            {
                if (_scale == value) return;
                _scale = value;
                OnPropertyChanged();
            }
        }

        public List<ChartScale> ScaleList { get; set; }

        private double _raStepsPerSecond;
        public double RaStepsPerSecond
        {
            get => _raStepsPerSecond;
            set
            {
                if (Math.Abs(_raStepsPerSecond - value) < 0) return;
                _raStepsPerSecond = value;
                OnPropertyChanged();
            }
        }

        private double _decStepsPerSecond;
        public double DecStepsPerSecond
        {
            get => _decStepsPerSecond;
            set
            {
                if (Math.Abs(_decStepsPerSecond - value) < 0) return;
                _decStepsPerSecond = value;
                OnPropertyChanged();
            }
        }

        private string _zoom;
        public string Zoom
        {
            get => _zoom;
            set
            {
                if (_zoom == value) return;
                _zoom = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Pulses Methods
        private void LoadPulsesDefaults()
        {
            TitleItems = new BindingList<TitleItem>();

            if (RaDur == null) RaDur = new GearedValues<PointModel>();
            if (DecDur == null) DecDur = new GearedValues<PointModel>();
            if (RaRej == null) RaRej = new GearedValues<PointModel>();
            if (DecRej == null) DecRej = new GearedValues<PointModel>();
            if (RaPhd == null) RaPhd = new GearedValues<PointModel>();
            if (DecPhd == null) DecPhd = new GearedValues<PointModel>();

            ChartQuality = Quality.Low;

            Mapper = Mappers.Xy<PointModel>()
                .X(model => model.DateTime.Ticks)   //use DateTime.Ticks as X
                .Y(model => model.Value)
                .Fill(model => model.Fill)
                .Stroke(model => model.Stroke);

            //lets save the mapper globally.
            Charting.For<PointModel>(Mapper);

            // X axis second timer
            _xAxisTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _xAxisTimer.Tick += XAxisTimer_Tick;

            //set how to display the Labels
            FormatterX = value => new DateTime((long)value).ToString("HH:mm:ss");
            FormatterY = value => value.ToString("N2");

            AxisXUnit = TimeSpan.FromSeconds(1).Ticks; //AxisXUnit = 10000000
            AxisYUnit = .5;
            AxisYMax = 3;
            AxisYMin = -3;
            AxisXSeconds = 40;
            SetXAxisLimits(HiResDateTime.UtcNow.ToLocalTime());
            Zoom = "Y";
        }
        private void LoadDefaultSettings()
        {
            // combo selections
            ColorsList = new List<string>();
            foreach (KnownColor colorValue in Enum.GetValues(typeof(KnownColor)))
            {
                var color = Color.FromKnownColor(colorValue);
                if (!ColorsList.Contains(color.Name) && !color.IsSystemColor)
                { ColorsList.Add(color.Name); }
            }

            MaxPointsRange = new List<double>(Numbers.InclusiveRange(500, 10000, 500));
            AxisMinSecondsRange = new List<int>(Enumerable.Range(10, 111));
            AnimationTimes = new List<int>(Enumerable.Range(0, 10));
            Smoothness = new List<int>(Enumerable.Range(0, 10));
            PointSizes = new List<int>(Enumerable.Range(0, 20));
            ScaleList = new List<ChartScale>() { ChartScale.ArcSecs, ChartScale.Milliseconds, ChartScale.Steps };

            AnimationTime = 0;
            AnimationsDisabled = true;
            LineSmoothness = 0;
            MaxPoints = 3000;
            Scale = ChartScale.ArcSecs;

            PhdHostText = "LocalHost";

            RaDurTitle = "Ra Duration";
            RaDurSeries = ChartSeriesType.GColumnSeries;
            RaDurPtSz = 0;
            RaDurToggle = true;
            RaDurInvert = false;
            RaDurColor = "CadetBlue";

            RaRejTitle = "Ra Rejections";
            RaRejSeries = ChartSeriesType.GScatterSeries;
            RaRejPtSz = 0;
            RaRejToggle = true;
            RaRejInvert = false;
            RaRejColor = "Lime";

            RaPhdTitle = "Ra PHD";
            RaPhdSeries = ChartSeriesType.GLineSeries;
            RaPhdPtSz = 0;
            RaPhdToggle = false;
            RaPhdInvert = false;
            RaPhdColor = "Green";

            DecDurTitle = "Dec Duration";
            DecDurSeries = ChartSeriesType.GColumnSeries;
            DecDurPtSz = 0;
            DecDurToggle = true;
            DecDurInvert = false;
            DecDurColor = "IndianRed";

            DecRejTitle = "Dec Rejections";
            DecRejSeries = ChartSeriesType.GScatterSeries;
            DecRejPtSz = 0;
            DecRejToggle = true;
            DecRejInvert = false;
            DecRejColor = "Gold";

            DecPhdTitle = "Dec PHD";
            DecPhdSeries = ChartSeriesType.GLineSeries;
            DecPhdPtSz = 0;
            DecPhdToggle = false;
            DecPhdInvert = false;
            DecPhdColor = "Red";
        }
        private void StartPulses()
        {
            try
            {
                ClearPulses();
                ResizeAxes();
                ChartsQuality(ChartQuality);

                MonitorLog.GetPulses = true;
                SkyServer.MonitorPulse = true;

                RaStepsPerSecond = SkyServer.StepsPerRevolution[0] / 360.0 / 3600;
                DecStepsPerSecond = SkyServer.StepsPerRevolution[1] / 360.0 / 3600;

                PulsesCollection = new SeriesCollection();
                TitleItems.Clear();

                if (RaDurToggle)
                {
                    var titleItem = CreateSeries(RaDurColor, RaDurTitle, RaDurSeries, RaDur, ChartValueSet.Values1, RaDurPtSz, 0);
                    if (titleItem != null) TitleItems.Add(titleItem);
                }
                if (RaRejToggle)
                {
                    var titleItem = CreateSeries(RaRejColor, RaRejTitle, RaRejSeries, RaRej, ChartValueSet.Values3, RaRejPtSz, 0);
                    if (titleItem != null) TitleItems.Add(titleItem);
                }
                if (RaPhdToggle)
                {
                    RaPhd = new GearedValues<PointModel>();
                    var titleItem = CreateSeries(RaPhdColor, RaPhdTitle, RaPhdSeries, RaPhd, ChartValueSet.Values5, RaPhdPtSz, 0);
                    if (titleItem != null) TitleItems.Add(titleItem);
                }
                if (DecDurToggle)
                {
                    var titleItem = CreateSeries(DecDurColor, DecDurTitle, DecDurSeries, DecDur, ChartValueSet.Values2, DecDurPtSz, 0);
                    if (titleItem != null) TitleItems.Add(titleItem);
                }
                if (DecRejToggle)
                {
                    var titleItem = CreateSeries(DecRejColor, DecRejTitle, DecRejSeries, DecRej, ChartValueSet.Values4, DecRejPtSz, 0);
                    if (titleItem != null) TitleItems.Add(titleItem);
                }
                if (DecPhdToggle)
                {
                    DecPhd = new GearedValues<PointModel>();
                    var titleItem = CreateSeries(DecPhdColor, DecPhdTitle, DecPhdSeries, DecPhd, ChartValueSet.Values6, DecPhdPtSz, 0);
                    if (titleItem != null) TitleItems.Add(titleItem);
                }
            }
            catch (Exception ex)
            {
                IsPulsing = false;

                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.UI,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $" {ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }
        private void ProcessPulse(PulseEntry entry)
        {
            if (entry == null) return;

            var point = new PointModel { DateTime = entry.StartTime.ToLocalTime(), Value = 0 };
            var duration = Math.Abs(entry.Duration);
            var arcsecs = (duration / 1000.0) * Math.Abs(entry.Rate) * 3600;
            double value;
            SolidColorBrush pointcolor;

            switch (Scale)
            {
                case ChartScale.Milliseconds:
                    value = duration;
                    break;
                case ChartScale.ArcSecs:
                    value = arcsecs;
                    break;
                case ChartScale.Steps:
                    value = entry.Axis == 0 ? (int)(arcsecs * RaStepsPerSecond) : (int)(arcsecs * DecStepsPerSecond);
                    break;
                default:
                    return;
            }
            value = entry.Rate < 0 ? -value : +value;
            point.Value = (int)(value * 1000) / 1000.00;
            point.Value *= -1;

            switch (entry.Axis)
            {
                case 0:
                    if (entry.Rejected)
                    {
                        pointcolor = ToBrush(Color.FromName(RaRejColor)) as SolidColorBrush;
                        point.Stroke = pointcolor;
                        point.Fill = pointcolor;
                        point.Set = ChartValueSet.Values3;
                        if (RaRejInvert) point.Value *= -1;
                        CalcAxisYMinMax(point.Value);
                        RaRej.Add(point);
                        break;
                    }

                    pointcolor = ToBrush(Color.FromName(RaDurColor)) as SolidColorBrush;
                    point.Stroke = pointcolor;
                    point.Fill = pointcolor;
                    point.Set = ChartValueSet.Values1;
                    if (RaDurInvert) point.Value *= -1;
                    CalcAxisYMinMax(point.Value);
                    RaDur.Add(point);
                    break;
                case 1:
                    if (entry.Rejected)
                    {
                        pointcolor = ToBrush(Color.FromName(DecRejColor)) as SolidColorBrush;
                        point.Stroke = pointcolor;
                        point.Fill = pointcolor;
                        point.Set = ChartValueSet.Values4;
                        if (DecRejInvert) point.Value *= -1;
                        CalcAxisYMinMax(point.Value);
                        DecRej.Add(point);
                        break;
                    }

                    pointcolor = ToBrush(Color.FromName(DecDurColor)) as SolidColorBrush;
                    point.Stroke = pointcolor;
                    point.Fill = pointcolor;
                    point.Set = ChartValueSet.Values2;
                    if (DecDurInvert) point.Value *= -1;
                    CalcAxisYMinMax(point.Value);
                    DecDur.Add(point);
                    break;
            }

            ChartLogging.LogPoint(BaseLogName, ChartType.Pulses, point);

            if (RaDur.Count > MaxPoints) RaDur.RemoveAt(0);
            if (DecDur.Count > MaxPoints) DecDur.RemoveAt(0);
            if (RaRej.Count > MaxPoints) RaRej.RemoveAt(0);
            if (DecRej.Count > MaxPoints) DecRej.RemoveAt(0);
        }
        private void ClearPulses()
        {
            RaDur?.Clear();
            RaRej?.Clear();
            DecDur?.Clear();
            DecRej?.Clear();
            RaPhd?.Clear();
            DecPhd?.Clear();
        }
        private void ChartsQuality(Quality chartQuality)
        {
            RaDur.WithQuality(chartQuality);
            DecDur.WithQuality(chartQuality);
            RaRej.WithQuality(chartQuality);
            DecRej.WithQuality(chartQuality);
            RaPhd.WithQuality(chartQuality);
            DecPhd.WithQuality(chartQuality);
        }
        private void ResizeAxes()
        {
            AxisYMax = double.NaN;
            AxisYMin = double.NaN;
            DoYMinMaxCalc = true;
        }
        private void SetXAxisLimits(DateTime now)
        {
            AxisXMax = now.Ticks + AxisXUnit * 2;
            AxisXMin = now.Ticks - AxisXUnit * AxisXSeconds;
        }
        public static Brush ToBrush(Color color)
        {
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }
        private static GColumnSeries NewGColumnSeries(string title, IChartValues values, ChartValueSet set, int pointSize, Brush color, int scaleAt)
        {
            var series = new GColumnSeries
            {
                Fill = color,
                MaxColumnWidth = 8,
                MinWidth = 1,
                PointGeometry = DefaultGeometries.Diamond,
                Stroke = color,
                StrokeThickness = pointSize,
                ScalesYAt = scaleAt,
                Title = title,
                Values = values
            };
            LogGColumnSeries(series, set);
            return series;
        }
        private GLineSeries NewGLineSeries(string title, IChartValues values, ChartValueSet set, int pointSize, Brush color, int scaleAt)
        {
            var series = new GLineSeries
            {
                Fill = ToBrush(Color.Transparent),
                LineSmoothness = LineSmoothness,
                MinWidth = 1,
                Stroke = color,
                StrokeThickness = 1,
                PointGeometry = DefaultGeometries.Diamond,
                PointGeometrySize = pointSize,
                PointForeground = color,
                ScalesYAt = scaleAt,
                Title = title,
                Values = values
            };

            LogGLineSeries(series, set);
            return series;
        }
        private static GScatterSeries NewGScatterSeries(string title, IChartValues values, ChartValueSet set, int pointSize, Brush color, int scaleAt)
        {
            var series = new GScatterSeries
            {
                Fill = color,
                MinWidth = 1,
                Stroke = color,
                StrokeThickness = pointSize,
                PointGeometry = DefaultGeometries.Diamond,
                ScalesYAt = scaleAt,
                Title = title,
                Values = values
            };

            LogGScatterSeries(series, set);
            return series;
        }
        private static GStepLineSeries NewGStepLineSeries(string title, IChartValues values, ChartValueSet set, int pointSize, Brush color, int scaleAt)
        {
            var series = new GStepLineSeries()
            {
                Fill = ToBrush(Color.Transparent),
                MinWidth = 1,
                Stroke = color,
                StrokeThickness = 1,
                PointGeometry = DefaultGeometries.Diamond,
                PointGeometrySize = pointSize,
                PointForeground = color,
                ScalesYAt = scaleAt,
                Title = title,
                Values = values
            };

            LogGStepLineSeries(series, set);
            return series;
        }
        private TitleItem CreateSeries(string serColor, string title, ChartSeriesType seriesType, IChartValues values, ChartValueSet seriesSet, int pointSize, int scaleAt)
        {
            var brush = ToBrush(Color.FromName(serColor));
            var titleItem = new TitleItem { TitleName = title, Fill = brush };
            switch (seriesType)
            {
                case ChartSeriesType.GLineSeries:
                    var lSeries = NewGLineSeries(title, values, seriesSet, pointSize, brush, scaleAt);
                    PulsesCollection.Add(lSeries);
                    return titleItem;
                case ChartSeriesType.GColumnSeries:
                    var cSeries = NewGColumnSeries(title, values, seriesSet, pointSize, brush, scaleAt);
                    PulsesCollection.Add(cSeries);
                    return titleItem;
                case ChartSeriesType.GStepLineSeries:
                    var sSeries = NewGStepLineSeries(title, values, seriesSet, pointSize, brush, scaleAt);
                    PulsesCollection.Add(sSeries);
                    return titleItem;
                case ChartSeriesType.GScatterSeries:
                    var scSeries = NewGScatterSeries(title, values, seriesSet, pointSize, brush, scaleAt);
                    PulsesCollection.Add(scSeries);
                    return titleItem;
            }

            return null;
        }

        #endregion

        #region RaPHD

        public GearedValues<PointModel> RaPhd { get; set; }

        private string _raPhdTitle;
        public string RaPhdTitle
        {
            get => _raPhdTitle;
            set
            {
                if (_raPhdTitle == value) return;
                _raPhdTitle = value;
                OnPropertyChanged();
            }
        }

        private bool _raPhdToggle;
        public bool RaPhdToggle
        {
            get => _raPhdToggle;
            set
            {
                if (_raPhdToggle == value) return;
                _raPhdToggle = value;
                OnPropertyChanged();
            }
        }

        private bool _raPhdInvert;
        public bool RaPhdInvert
        {
            get => _raPhdInvert;
            set
            {
                if (_raPhdInvert == value) return;
                _raPhdInvert = value;
                OnPropertyChanged();
            }
        }

        private string _raPhdColor;
        public string RaPhdColor
        {
            get => _raPhdColor;
            set
            {
                if (_raPhdColor == value) return;
                _raPhdColor = value;
                OnPropertyChanged();
            }
        }

        private int _raPhdPtSz;
        public int RaPhdPtSz
        {
            get => _raPhdPtSz;
            set
            {
                if (_raPhdPtSz == value) return;
                _raPhdPtSz = value;
                OnPropertyChanged();
            }
        }

        private ChartSeriesType _raPhdSeries;
        public ChartSeriesType RaPhdSeries
        {
            get => _raPhdSeries;
            set
            {
                if (_raPhdSeries == value) return;
                _raPhdSeries = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region DecPHD

        public GearedValues<PointModel> DecPhd { get; set; }

        private string _decPhdTitle;
        public string DecPhdTitle
        {
            get => _decPhdTitle;
            set
            {
                if (_decPhdTitle == value) return;
                _decPhdTitle = value;
                OnPropertyChanged();
            }
        }

        private bool _decPhdToggle;
        public bool DecPhdToggle
        {
            get => _decPhdToggle;
            set
            {
                if (_decPhdToggle == value) return;
                _decPhdToggle = value;
                OnPropertyChanged();
            }
        }

        private bool _decPhdInvert;
        public bool DecPhdInvert
        {
            get => _decPhdInvert;
            set
            {
                if (_decPhdInvert == value) return;
                _decPhdInvert = value;
                OnPropertyChanged();
            }
        }

        private string _decPhdColor;
        public string DecPhdColor
        {
            get => _decPhdColor;
            set
            {
                if (_decPhdColor == value) return;
                _decPhdColor = value;
                OnPropertyChanged();
            }
        }

        private int _decPhdPtSz;
        public int DecPhdPtSz
        {
            get => _decPhdPtSz;
            set
            {
                if (_decPhdPtSz == value) return;
                _decPhdPtSz = value;
                OnPropertyChanged();
            }
        }

        private ChartSeriesType _decPhdSeries;
        public ChartSeriesType DecPhdSeries
        {
            get => _decPhdSeries;
            set
            {
                if (_decPhdSeries == value) return;
                _decPhdSeries = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region PHD Settings

        private double PhdPixelScale { get; set; }
        private bool IsPhdRunning { get; set; }

        private string _phdHostText;
        public string PhdHostText
        {
            get => _phdHostText;
            set
            {
                if (_phdHostText == value) return;
                _phdHostText = value;
                OnPropertyChanged();
            }
        }

        private string _phdBadgeContent;
        public string PhdBadgeContent
        {
            get => _phdBadgeContent;
            set
            {
                if (_phdBadgeContent == value) return;
                _phdBadgeContent = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region PHD Methods

        private async Task PhdAsync()
        {
            try
            {
                _ctsPhd = null;
                _ctsPhd = new CancellationTokenSource();
                _ctPhd = _ctsPhd.Token;

                _phd = new GuiderImpl(PhdHostText, 1, _ctsPhd);
                _phd.Connect();
                PhdBadgeContent = _phd.IsConnected() ? Application.Current.Resources["pulOn"].ToString() : "";
                Mouse.OverrideCursor = null;

                IsPhdRunning = true;
                var task = Task.Run(() =>
                {
                    while (IsPhdRunning)
                    {
                        if (_ctPhd.IsCancellationRequested)
                        {
                            IsPhdRunning = false;
                        }
                        else
                        {
                            _phd.DoWork();
                        }
                    }
                }, _ctPhd);
                await task;
            }
            catch (GuiderException ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                switch (ex.ErrorCode)
                {
                    case Phd.ErrorCode.LostConnection:
                    case Phd.ErrorCode.NewConnection:
                    case Phd.ErrorCode.Disconnected:
                    case Phd.ErrorCode.GuidingError:
                    case Phd.ErrorCode.NoResponse:
                        OpenDialog(ex.Message);
                        break;
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Server, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog(ex.Message);
            }
            finally
            {
                PhdClose();
            }
        }
        private void PhdClose()
        {
            _ctsPhd?.Cancel();
            _ctsPhd = null;
            _phd?.Close();
            _phd = null;
            PhdBadgeContent = "";
        }
        private bool PhdConnected()
        {
            if (_phd == null)
            {
                PhdBadgeContent = "";
                return false;
            }
            var con = _phd.IsConnected();
            PhdBadgeContent = con ? Application.Current.Resources["pulOn"].ToString() : "";
            return con;
        }
        private void ProcessPhd(GuideStep entry)
        {
            if (entry == null) return;
            if (!PhdConnected()) return;

            try
            {
                if (Math.Abs(_phd.LastPixelScale) <= 0.0) PhdPixelScale = _phd.PixelScale();
                SolidColorBrush pointcolor;
                if (RaPhdToggle)
                {
                    //var val = LocalPixelScale * entry.RADistanceRaw;
                    var point = new PointModel { DateTime = entry.LocalTimeStamp, Value = 0 };
                    var arcsecs = (PhdPixelScale * entry.RADistanceRaw) / Math.Cos(SkyServer.Declination / 3600);
                    double val;
                    switch (Scale)
                    {
                        case ChartScale.Milliseconds:
                            val = arcsecs / 3600 / Math.Abs(SkyServer.GuideRateRa) * 1000;
                            break;
                        case ChartScale.ArcSecs:
                            val = arcsecs;
                            break;
                        case ChartScale.Steps:
                            val = (int)(arcsecs * RaStepsPerSecond);
                            break;
                        default:
                            return;
                    }

                    val *= -1;
                    if (RaPhdInvert) val *= -1;
                    val = ((int)(val * 1000)) / 1000.00; // set to 2 decimal points for chart speed
                    point.Value = val;
                    pointcolor = ToBrush(Color.FromName(RaPhdColor)) as SolidColorBrush;
                    point.Stroke = pointcolor;
                    point.Fill = pointcolor;
                    point.Set = ChartValueSet.Values5;
                    CalcAxisYMinMax(point.Value);
                    RaPhd.Add(point);
                    ChartLogging.LogPoint(BaseLogName, ChartType.Pulses, point);
                    if (RaPhd.Count > MaxPoints) RaPhd.RemoveAt(0);
                }

                if (!DecPhdToggle) return;
                {
                    var point = new PointModel { DateTime = entry.LocalTimeStamp, Value = 0 };
                    var arcsecs = PhdPixelScale * entry.DecDistanceRaw;
                    double val;
                    switch (Scale)
                    {
                        case ChartScale.Milliseconds:
                            val = arcsecs / 3600 / Math.Abs(SkyServer.GuideRateDec) * 1000;
                            break;
                        case ChartScale.ArcSecs:
                            val = arcsecs;
                            break;
                        case ChartScale.Steps:
                            val = (int)(arcsecs * DecStepsPerSecond);
                            break;
                        default:
                            return;
                    }
                    if (DecDurInvert) val *= -1;
                    val = ((int)(val * 1000)) / 1000.00; // set to 2 decimal points for chart speed
                    point.Value = val;
                    pointcolor = ToBrush(Color.FromName(DecPhdColor)) as SolidColorBrush;
                    point.Stroke = pointcolor;
                    point.Fill = pointcolor;
                    point.Set = ChartValueSet.Values6;
                    CalcAxisYMinMax(point.Value);
                    DecPhd.Add(point);
                    ChartLogging.LogPoint(BaseLogName, ChartType.Pulses, point);
                    if (DecPhd.Count > MaxPoints) DecPhd.RemoveAt(0);
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.UI, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }

        }

        #endregion

        #region Logging

        private void LogStart()
        {
            ChartLogging.LogStart(BaseLogName, ChartType.Pulses);
            ChartLogging.LogData(BaseLogName, ChartType.Pulses, "GSVersion", $"{Assembly.GetExecutingAssembly().GetName().Version}");
            ChartLogging.LogData(BaseLogName, ChartType.Pulses, "MountName", $"{SkyServer.MountName}");
            ChartLogging.LogData(BaseLogName, ChartType.Pulses, "MountVersion", $"{SkyServer.MountVersion}");
            ChartLogging.LogData(BaseLogName, ChartType.Pulses, "RaStepsPerSecond", $"{RaStepsPerSecond}");
            ChartLogging.LogData(BaseLogName, ChartType.Pulses, "DecStepsPerSecond", $"{DecStepsPerSecond}");
            ChartLogging.LogData(BaseLogName, ChartType.Pulses, "AnimationTime", $"{AnimationTime}");
            ChartLogging.LogData(BaseLogName, ChartType.Pulses, "LineSmoothness", $"{LineSmoothness}");
            ChartLogging.LogData(BaseLogName, ChartType.Pulses, "Scale", $"{Scale}");
            ChartLogging.LogData(BaseLogName, ChartType.Pulses, "GuideRateDec", $"{SkyServer.GuideRateDec}");
            ChartLogging.LogData(BaseLogName, ChartType.Pulses, "GuideRateRa", $"{SkyServer.GuideRateRa}");
        }

        private static void LogGColumnSeries(GColumnSeries series, ChartValueSet set)
        {
            if (series == null) return;
            var str = $"{set}|{series.Title}|{series.Stroke}|{series.StrokeThickness}|{series.Fill}|{series.MinWidth}|{series.ScalesYAt}|{series.MaxColumnWidth},";
            if (!string.IsNullOrEmpty(str)) ChartLogging.LogSeries(BaseLogName, ChartType.Pulses, "GColumnSeries", str);
        }
        private static void LogGLineSeries(LineSeries series, ChartValueSet set)
        {
            if (series == null) return;
            var str = $"{set}|{series.Title}|{series.Stroke}|{series.StrokeThickness}|{series.Fill}|{series.MinWidth}|{series.ScalesYAt}|{series.PointGeometrySize}|{series.LineSmoothness}";
            if (!string.IsNullOrEmpty(str)) ChartLogging.LogSeries(BaseLogName, ChartType.Pulses, "GLineSeries", str);
        }
        private static void LogGStepLineSeries(GStepLineSeries series, ChartValueSet set)
        {
            if (series == null) return;
            var str = $"{set}|{series.Title}|{series.Stroke}|{series.StrokeThickness}|{series.Fill}|{series.MinWidth}|{series.ScalesYAt}|{series.PointGeometrySize}";
            if (!string.IsNullOrEmpty(str)) ChartLogging.LogSeries(BaseLogName, ChartType.Pulses, "GStepLineSeries", str);
        }
        private static void LogGScatterSeries(Series series, ChartValueSet set)
        {
            if (series == null) return;
            var str = $"{set}|{series.Title}|{series.Stroke}|{series.StrokeThickness}|{series.Fill}|{series.MinWidth}|{series.ScalesYAt}";
            if (!string.IsNullOrEmpty(str)) ChartLogging.LogSeries(BaseLogName, ChartType.Pulses, "GScatterSeries", str);
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

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            // GC.SuppressFinalize(this);
        }
        // NOTE: Leave out the finalizer altogether if this class doesn't
        // own unmanaged resources itself, but leave the other methods
        // exactly as they are.
        ~PulsesVM()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        // The bulk of the clean-up code is implemented in Dispose(bool)
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts?.Dispose();
                _phd?.Dispose();
                _ctsPhd?.Dispose();
                RaDur?.Dispose();
                RaRej?.Dispose();
                DecDur?.Dispose();
                DecRej?.Dispose();
                RaPhd?.Dispose();
                DecPhd?.Dispose();
                _xAxisTimer?.Stop();
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
