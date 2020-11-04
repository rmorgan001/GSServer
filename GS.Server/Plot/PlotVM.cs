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
using GS.Shared;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GS.Server.Controls.Dialogs;
using GS.Server.SkyTelescope;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Geared;
using LiveCharts.Wpf;
using Brush = System.Windows.Media.Brush;
using Color = System.Drawing.Color;

namespace GS.Server.Plot
{
    public class PlotVM : ObservableObject, IPageVM, IDisposable
    {
        #region Fields
        public string TopName => "Plot";
        public string BottomName => "Plot";
        public int Uid => 8;

        private DispatcherTimer _xAxisTimer;
        private CancellationTokenSource _cts;
        private CancellationToken _ct;
        private double raStepsPerSecond;
        private double decStepsPerSecond;
        private const string BaseLogName = "Plot";
        
        #endregion
        
        public PlotVM()
        {
            var monitorItem = new MonitorEntry
            { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = " Loading PlotVM" };
            MonitorLog.LogToMonitor(monitorItem);

            // setup property events to monitor
            MonitorQueue.StaticPropertyChanged += PropertyChangedMonitor;

            LoadDefaults();
        }

        #region ViewModel

        /// <summary>
        /// Property changes from the monitor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertyChangedMonitor(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ThreadContext.InvokeOnUiThread(
                    delegate
                    {
                        switch (e.PropertyName)
                        {
                            case "CmdjSentEntry":
                                ProcessValues1(MonitorQueue.CmdjSentEntry);
                                break;
                            case "Cmdj2SentEntry":
                                ProcessValues2(MonitorQueue.Cmdj2SentEntry);
                                break;
                        }
                    }, _ct);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        /// <summary>
        /// updates x axis time
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XAxisTimer_Tick(object sender, EventArgs e)
        {
            SetXAxisLimits(HiResDateTime.UtcNow.ToLocalTime());
        }

        /// <summary>
        /// Process ra data
        /// </summary>
        /// <param name="entry"></param>
        private void ProcessValues1(MonitorEntry entry)
        {
            var date = entry.Datetime.ToLocalTime();
            var point = new PointModel {DateTime = date, Value = 0, Set = ChartValueSet.Values1};
            var msg = entry.Message.Split(',');
            double.TryParse(msg[2], out var steps);

            if (IsZeroBased)
            {
                var zero = Conversions.Deg2ArcSec(90.0) * raStepsPerSecond;
                steps -= zero;
            }

            switch (Scale)
            {
                case ChartScale.Degrees:
                    point.Value = Conversions.ArcSec2Deg(steps / raStepsPerSecond);
                    break;
                case ChartScale.Arcsecs:
                    point.Value = steps / raStepsPerSecond;
                    break;
                case ChartScale.Steps:
                    point.Value = steps;
                    break;
                default:
                    return;
            }
            if (IsLogging) ChartLogging.LogPoint(BaseLogName,ChartType.Plot, point);

            Values1.Add(point);
            if (Values1.Count > MaxPoints) Values1.RemoveAt(0);

            var item = TitleItems.FirstOrDefault(x => x.TitleName == Values1Title);
            if (item == null) return;
            item.Value = point.Value;
        }

        /// <summary>
        /// process dec data
        /// </summary>
        /// <param name="entry"></param>
        private void ProcessValues2(MonitorEntry entry)
        {
            var date = entry.Datetime.ToLocalTime();
            var point = new PointModel { DateTime = date, Value = 0, Set = ChartValueSet.Values2};
            var msg = entry.Message.Split(',');
            double.TryParse(msg[2], out var steps);

            if (IsZeroBased)
            {
                var zero = Conversions.Deg2ArcSec(90) * decStepsPerSecond;
                steps -= zero;
            }

            switch (Scale)
            {
                case ChartScale.Degrees:
                    point.Value = Conversions.ArcSec2Deg(steps / decStepsPerSecond);
                    break;
                case ChartScale.Arcsecs:
                    point.Value = steps / decStepsPerSecond;
                    break;
                case ChartScale.Steps:
                    point.Value = steps;
                    break;
                default:
                    return;
            }

            if (IsLogging) ChartLogging.LogPoint(BaseLogName,ChartType.Plot, point);

            Values2.Add(point);
            if (Values2.Count > MaxPoints) Values2.RemoveAt(0);

            var item = TitleItems.FirstOrDefault(x => x.TitleName == Values2Title);
            if (item == null) return;
            item.Value = point.Value;
        }

        /// <summary>
        /// default data
        /// </summary>
        private void LoadDefaults()
        {
            IsZeroBased = false;
            TitleItems = new BindingList<TitleItem>();

            if (Values1 == null) Values1 = new GearedValues<PointModel>();
            if (Values2 == null) Values2 = new GearedValues<PointModel>();

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

            MaxPointsRange = new List<double>(Numbers.InclusiveRange(1000, 50000, 1000));
            MaxPoints = 5000;

            FormatterX = value => new DateTime((long)value).ToString("HH:mm:ss");
            FormatterY = value => value.ToString("N2");

            AxisXUnit = TimeSpan.FromSeconds(1).Ticks; //AxisXUnit = 10000000
            AxisYUnit = .5;
            AxisYMax = 3;
            AxisYMin = -3;
            AxisXSeconds = 40;
            SetXAxisLimits(HiResDateTime.UtcNow.ToLocalTime());
            Zoom = "Xy";

            Scale = ChartScale.Steps;

            Values1Title = "Ra";
            Values1Series = ChartSeriesType.GLineSeries;
            Values1PtSz = 1;
            Values1Toggle = true;
            Values1Invert = false;
            Values1Color = "CadetBlue";

            Values2Title = "Dec";
            Values2Series = ChartSeriesType.GLineSeries;
            Values2PtSz = 1;
            Values2Toggle = true;
            Values2Invert = false;
            Values2Color = "IndianRed";
        }

        #endregion

        #region Plot

        private bool _isLogging;
        public bool IsLogging
        {
            get => _isLogging;
            set
            {
                if (_isLogging == value) return;
                _isLogging = value;
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

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning == value) return;
                _isRunning = value;
                if (value)
                {
                    LogStart();
                    _xAxisTimer.Start();
                    _cts = new CancellationTokenSource();
                    _ct = _cts.Token;
                    StartBadgeContent = Application.Current.Resources["pltOn"].ToString();
                    PauseBadgeContent = "";
                }
                else
                {
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

        private bool _isZeroBased;
        public bool IsZeroBased
        {
            get => _isZeroBased;
            set
            {
                if (_isZeroBased == value) return;
                _isZeroBased = value;
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

        public SeriesCollection _valuesCollection;
        public SeriesCollection ValuesCollection
        {
            get => _valuesCollection;
            set
            {
                if (_valuesCollection == value) return;
                _valuesCollection = value;
                OnPropertyChanged();
            }
        }

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

        public List<ChartScale> ScaleList { get; set; }

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
        
        private void StartChart()
        {
            ClearValues();
            ResizeAxes();
            ChartsQuality(ChartQuality);
            TitleItems.Clear();

            raStepsPerSecond = Conversions.StepPerArcSec(SkyServer.StepsPerRevolution[0]);
            decStepsPerSecond = Conversions.StepPerArcSec(SkyServer.StepsPerRevolution[1]);

            MonitorLog.GetJEntries = true;

            ValuesCollection = new SeriesCollection();

            if (Values1Toggle)
            {
                var titleItem = CreateSeries(Values1Color, Values1Title, Values1Series, Values1, ChartValueSet.Values1, Values1PtSz, 0);
                if (titleItem != null) TitleItems.Add(titleItem);
            }

            if (!Values2Toggle) return;
            {
                var titleItem = CreateSeries(Values2Color, Values2Title, Values2Series, Values2, ChartValueSet.Values2, Values2PtSz, 0);
                if (titleItem != null) TitleItems.Add(titleItem);
            }
        }

        private void ChartsQuality(Quality chartQuality)
        {
            Values1.WithQuality(chartQuality);
            Values2.WithQuality(chartQuality);
        }

        private void SetXAxisLimits(DateTime now)
        {
            AxisXMax = now.Ticks + AxisXUnit * 2;
            AxisXMin = now.Ticks - AxisXUnit * AxisXSeconds;
        }

        private GColumnSeries NewGColumnSeries(string title, IChartValues values, ChartValueSet set, double pointsize, Brush color, int scaleat)
        {
            var series = new GColumnSeries
            {
                Fill = color,
                MaxColumnWidth = 8,
                MinWidth = 1,
                PointGeometry = DefaultGeometries.Diamond,
                Stroke = color,
                StrokeThickness = pointsize,
                ScalesYAt = scaleat,
                Title = title,
                Values = values
            };

            LogGColumnSeries(series, set);
            return series;
        }
        private GLineSeries NewGLineSeries(string title, IChartValues values, ChartValueSet set, double pointSize, Brush color, int scaleAt)
        {
            var col = new GSColors();
            var series = new GLineSeries
            {
                Fill = col.ToBrush(Color.Transparent),
                LineSmoothness = 0,
                MinWidth = 1,
                Stroke = color,
                StrokeThickness = pointSize,
                PointForeground = color,
                PointGeometry = DefaultGeometries.Cross,
                PointGeometrySize = 10,
                ScalesYAt = scaleAt,
                Title = title,
                Values = values
            };

            LogGLineSeries(series, set);
            return series;
        }
        private GScatterSeries NewGScatterSeries(string title, IChartValues values, ChartValueSet set, double pointSize, Brush color, int scaleAt)
        {
            var series = new GScatterSeries
            {
                Fill = color,
                MinWidth = 1,
                Stroke = color,
                StrokeThickness = pointSize,
                ScalesYAt = scaleAt,
                Title = title,
                Values = values
            };
            
            LogGScatterSeries(series, set);
            return series;
        }
        private GStepLineSeries NewGStepLineSeries(string title, IChartValues values, ChartValueSet set, double pointSize, Brush color, int scaleAt)
        {
            var col = new GSColors();
            var series = new GStepLineSeries()
            {
                Fill = col.ToBrush(Color.Transparent),
                MinWidth = 1,
                Stroke = color,
                StrokeThickness = pointSize,
                PointForeground = color,
                ScalesYAt = scaleAt,
                Title = title,
                Values = values
            };

            LogGStepLineSeries(series, set);
            return series;
        }
        private TitleItem CreateSeries(string serColor, string title, ChartSeriesType seriesType, IChartValues values, ChartValueSet seriesSet, double pointSize, int scaleAt)
        {
            var col = new GSColors();
            var brush = col.ToBrush(Color.FromName(serColor));
            var titleItem = new TitleItem { TitleName = title, Fill = brush };
            switch (seriesType)
            {
                case ChartSeriesType.GLineSeries:
                    var lSeries = NewGLineSeries(title, values, seriesSet, pointSize, brush, scaleAt);
                    ValuesCollection.Add(lSeries);
                    return titleItem;
                case ChartSeriesType.GColumnSeries:
                    var cSeries = NewGColumnSeries(title, values, seriesSet, pointSize, brush, scaleAt);
                    ValuesCollection.Add(cSeries);
                    return titleItem;
                case ChartSeriesType.GStepLineSeries:
                    var sSeries = NewGStepLineSeries(title, values, seriesSet, pointSize, brush, scaleAt);
                    ValuesCollection.Add(sSeries);
                    return titleItem;
                case ChartSeriesType.GScatterSeries:
                    var scSeries = NewGScatterSeries(title, values, seriesSet, pointSize, brush, scaleAt);
                    ValuesCollection.Add(scSeries);
                    return titleItem;
            }

            return null;
        }
        private void ResizeAxes()
        {
            AxisYMax -= double.NaN;
            AxisYMin += double.NaN;
        }
        private void ClearValues()
        {
            Values1?.Clear();
            Values2?.Clear();
        }

        #endregion

        #region Values1
        public GearedValues<PointModel> Values1 { get; set; }

        private string _values1Title;
        public string Values1Title
        {
            get => _values1Title;
            set
            {
                if (_values1Title == value) return;
                _values1Title = value;
                OnPropertyChanged();
            }
        }

        private bool _values1Toggle;
        public bool Values1Toggle
        {
            get => _values1Toggle;
            set
            {
                if (_values1Toggle == value) return;
                _values1Toggle = value;
                OnPropertyChanged();
            }
        }

        private bool _values1Invert;
        public bool Values1Invert
        {
            get => _values1Invert;
            set
            {
                if (_values1Invert == value) return;
                _values1Invert = value;
                OnPropertyChanged();
            }
        }

        private string _values1Color;
        public string Values1Color
        {
            get => _values1Color;
            set
            {
                if (_values1Color == value) return;
                _values1Color = value;
                OnPropertyChanged();
            }
        }

        private double _values1PtSz;
        public double Values1PtSz
        {
            get => _values1PtSz;
            set
            {
                _values1PtSz = value;
                OnPropertyChanged();
            }
        }

        private double _value1value;
        public double Value1Value
        {
            get => _value1value;
            set
            {
                _value1value = value;
                OnPropertyChanged();
            }
        }

        private ChartSeriesType _values1Series;
        public ChartSeriesType Values1Series
        {
            get => _values1Series;
            set
            {
                if (_values1Series == value) return;
                _values1Series = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Values2
        public GearedValues<PointModel> Values2 { get; set; }

        private string _values2Title;
        public string Values2Title
        {
            get => _values2Title;
            set
            {
                if (_values2Title == value) return;
                _values2Title = value;
                OnPropertyChanged();
            }
        }

        private bool _values2Toggle;
        public bool Values2Toggle
        {
            get => _values2Toggle;
            set
            {
                if (_values2Toggle == value) return;
                _values2Toggle = value;
                OnPropertyChanged();
            }
        }

        private bool _values2Invert;
        public bool Values2Invert
        {
            get => _values2Invert;
            set
            {
                if (_values2Invert == value) return;
                _values2Invert = value;
                OnPropertyChanged();
            }
        }

        private string _values2Color;
        public string Values2Color
        {
            get => _values2Color;
            set
            {
                if (_values2Color == value) return;
                _values2Color = value;
                OnPropertyChanged();
            }
        }

        private double _values2PtSz;
        public double Values2PtSz
        {
            get => _values2PtSz;
            set
            {
                _values2PtSz = value;
                OnPropertyChanged();
            }
        }

        private ChartSeriesType _values2Series;
        public ChartSeriesType Values2Series
        {
            get => _values2Series;
            set
            {
                if (_values2Series == value) return;
                _values2Series = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        private ICommand _clickPlotStartCmd;
        public ICommand ClickPlotStartCmd
        {
            get
            {
                var cmd = _clickPlotStartCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _clickPlotStartCmd = new RelayCommand(param => PlotStartCmd());
            }
            set => _clickPlotStartCmd = value;
        }
        private void PlotStartCmd()
        {
            try
            {
                using (new WaitCursor())
                {
                    IsRunning = !IsRunning;
                    if (IsRunning)
                    {
                        StartChart();
                    }
                }
            }
            catch (Exception ex)
            {

                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {ex.Message}, {ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickPlotClearCmd;
        public ICommand ClickPlotClearCmd
        {
            get
            {
                var cmd = _clickPlotClearCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _clickPlotClearCmd = new RelayCommand(param => PlotClearCmd());
            }
            set => _clickPlotClearCmd = value;
        }
        private void PlotClearCmd()
        {
            try
            {
                using (new WaitCursor())
                {
                    ClearValues();
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {ex.Message}, {ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickPlotSizeCmd;
        public ICommand ClickPlotSizeCmd
        {
            get
            {
                var cmd = _clickPlotSizeCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _clickPlotSizeCmd = new RelayCommand(param => PlotSizeCmd());
            }
            set => _clickPlotSizeCmd = value;
        }
        private void PlotSizeCmd()
        {
            try
            {
                ResizeAxes();
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickPlotPauseCmd;
        public ICommand ClickPlotPauseCmd
        {
            get
            {
                var cmd = _clickPlotPauseCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _clickPlotPauseCmd = new RelayCommand(param => PlotPauseCmd());
            }
            set => _clickPlotPauseCmd = value;
        }
        private void PlotPauseCmd()
        {
            try
            {
                if (!IsRunning) return;
                if (_xAxisTimer.IsEnabled)
                {
                    _xAxisTimer.Stop();
                    PauseBadgeContent = Application.Current.Resources["pltOn"].ToString();
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
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickPlotZoomCmd;
        public ICommand ClickPlotZoomCmd
        {
            get
            {
                var cmd = _clickPlotZoomCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _clickPlotZoomCmd = new RelayCommand(PlotZoomCmd);
            }
            set => _clickPlotZoomCmd = value;
        }
        private void PlotZoomCmd(object param)
        {
            try
            {
                Zoom = (string)param;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }
        
        private ICommand _clickPlotSeriesCmd;
        public ICommand ClickPlotSeriesCmd
        {
            get
            {
                var cmd = _clickPlotSeriesCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _clickPlotSeriesCmd = new RelayCommand(PlotSeriesCmd);
            }
            set => _clickPlotSeriesCmd = value;
        }
        private void PlotSeriesCmd(object param)
        {
            try
            {
                var cnt = ValuesCollection.Count;
                var title = (TitleItem)param;

                for (var i = 0; i < cnt; i++)
                {
                    var a = ValuesCollection[i];
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
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        #endregion

        #region Logging

        private void LogStart()
        {
            if (!IsLogging) return;
            ChartLogging.LogStart(BaseLogName, ChartType.Plot);
            ChartLogging.LogData(BaseLogName, ChartType.Plot, "GSVersion", $"{Assembly.GetExecutingAssembly().GetName().Version}");
            ChartLogging.LogData(BaseLogName, ChartType.Plot, "MountName", $"{SkyServer.MountName}");
            ChartLogging.LogData(BaseLogName, ChartType.Plot, "MountVersion", $"{SkyServer.MountVersion}");
            ChartLogging.LogData(BaseLogName, ChartType.Plot, "RaStepsPerSecond", $"{SkyServer.StepsPerRevolution[0] / 360.0 / 3600}");
            ChartLogging.LogData(BaseLogName, ChartType.Plot, "DecStepsPerSecond", $"{SkyServer.StepsPerRevolution[1] / 360.0 / 3600}");
            ChartLogging.LogData(BaseLogName, ChartType.Plot, "Scale", $"{Scale}");
            ChartLogging.LogData(BaseLogName, ChartType.Plot, "GuideRateDec", $"{SkyServer.GuideRateDec}");
            ChartLogging.LogData(BaseLogName, ChartType.Plot, "GuideRateRa", $"{SkyServer.GuideRateRa}");
            ChartLogging.LogData(BaseLogName, ChartType.Plot, "IsZeroBased", $"{IsZeroBased}");
            ChartLogging.LogData(BaseLogName, ChartType.Plot, "Quality", $"{ChartQuality}");
            ChartLogging.LogData(BaseLogName, ChartType.Plot, "MaxPoints", $"{MaxPoints}");
        }

        private void LogGColumnSeries(GColumnSeries series, ChartValueSet set)
        {
            if (!IsLogging) return;
            if (series == null) return;
            var str = $"{set},{series.Title},{series.Stroke},{series.StrokeThickness},{series.Fill},{series.MinWidth},{series.ScalesYAt},{series.MaxColumnWidth},";
            if (!string.IsNullOrEmpty(str)) ChartLogging.LogSeries(BaseLogName, ChartType.Plot, "GColumnSeries", str);
        }
        private void LogGLineSeries(LineSeries series, ChartValueSet set)
        {
            if (!IsLogging) return;
            if (series == null) return;
            var str = $"{set},{series.Title},{series.Stroke},{series.StrokeThickness},{series.Fill},{series.MinWidth},{series.ScalesYAt},{series.PointGeometrySize},{series.LineSmoothness}";
            if (!string.IsNullOrEmpty(str)) ChartLogging.LogSeries(BaseLogName, ChartType.Plot, "GLineSeries", str);
        }
        private void LogGStepLineSeries(GStepLineSeries series, ChartValueSet set)
        {
            if (!IsLogging) return;
            if (series == null) return;
            var str = $"{set},{series.Title},{series.Stroke},{series.StrokeThickness},{series.Fill},{series.MinWidth},{series.ScalesYAt},{series.PointGeometrySize}";
            if (!string.IsNullOrEmpty(str)) ChartLogging.LogSeries(BaseLogName, ChartType.Plot, "GStepLineSeries", str);
        }
        private void LogGScatterSeries(Series series, ChartValueSet set)
        {
            if (!IsLogging) return;
            if (series == null) return;
            var str = $"{set},{series.Title},{series.Stroke},{series.StrokeThickness},{series.Fill},{series.MinWidth},{series.ScalesYAt}";
            if (!string.IsNullOrEmpty(str)) ChartLogging.LogSeries(BaseLogName, ChartType.Plot, "GScatterSeries", str);
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
            if (msg != null) DialogMsg = msg;
            DialogCaption = caption ?? Application.Current.Resources["diaDialog"].ToString();
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
        ~PlotVM()
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
                Values1?.Dispose();
                Values2?.Dispose();
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

    public enum ChartScale
    {
        Arcsecs = 1,
        Degrees = 2,
        Steps = 3,
    }
    public class TitleItem: ObservableObject
    {
        private double _value;
        public string TitleName { get; set; }
        public Brush Fill { get; set; }
        public double Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged();
            }
        }
        public ChartValueSet ValueSet { get; set; }
    }
}
