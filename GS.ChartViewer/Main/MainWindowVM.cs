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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GS.ChartViewer.Controls.Dialogs;
using GS.ChartViewer.Helpers;
using GS.Principles;
using GS.Shared;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Geared;
using LiveCharts.Wpf;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Brush = System.Windows.Media.Brush;
using Color = System.Drawing.Color;

namespace GS.ChartViewer.Main
{
    internal class MainWindowVM : ObservableObject, IDisposable
    {
        #region Fields

        private double _maxPoint;
        private double _minPoint;

        #endregion

        public MainWindowVM()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            LoadDefaults();
        }

        #region Window

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
            WindowStates = WindowState.Minimized;
        }

        private ICommand _maximizeWindowCommand;
        public ICommand MaximizeWindowCommand
        {
            get
            {
                return _maximizeWindowCommand ?? (_maximizeWindowCommand = new RelayCommand(
                    param => MaximizeWindow()
                ));
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
                return _normalWindowCommand ?? (_normalWindowCommand = new RelayCommand(
                    param => NormalWindow()
                ));
            }
        }
        private void NormalWindow()
        {
            WindowStates = WindowState.Normal;
        }

        public WindowState WindowStates
        {
            get => Properties.ChartViewer.Default.WindowState;
            set
            {
                Properties.ChartViewer.Default.WindowState = value;
                OnPropertyChanged();
            }
        }


        #endregion

        #region Commands

        private ICommand _clickOpenFileCommand;
        public ICommand ClickOpenFileCommand
        {
            get
            {
                return _clickOpenFileCommand ?? (_clickOpenFileCommand = new RelayCommand(
                           param => OpenFileCommand()
                       ));
            }
        }
        private void OpenFileCommand()
        {
            try
            {
                var filename = GetFileName();
                if (filename == null) return;
                if (!File.Exists(filename))
                {
                    OpenDialog("Invalid File Name");
                    return;
                }
                using (new WaitCursor())
                {
                    var loaded = LoadFile(filename);
                    if (!loaded) return;
                    var index = BuildIndex();
                    if (!index) return;
                    SelectedIndex = IndexItems.First();
                }
            }
            catch (Exception ex)
            {
                OpenDialog($"{ex.Message}");
            }
        }

        private ICommand _clickCloseAppCommand;
        public ICommand ClickCloseAppCommand
        {
            get
            {
                return _clickCloseAppCommand ?? (_clickCloseAppCommand = new RelayCommand(
                           param => CloseApp()
                       ));
            }
        }
        private void CloseApp()
        {
            CloseLogView();
        }

        private ICommand _clickChartsZoomCmd;
        public ICommand ClickChartZoomCmd
        {
            get => _clickChartsZoomCmd ?? (_clickChartsZoomCmd = new RelayCommand(ChartZoomCmd));
            set => _clickChartsZoomCmd = value;
        }
        private void ChartZoomCmd(object param)
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

        private ICommand _clickChartSeriesCmd;
        public ICommand ClickChartSeriesCmd
        {
            get => _clickChartSeriesCmd ?? (_clickChartSeriesCmd = new RelayCommand(ChartSeriesCmd));
            set => _clickChartSeriesCmd = value;
        }
        private void ChartSeriesCmd(object param)
        {
            try
            {
                var cnt = ChartCollection.Count;
                if (cnt == 0) return;
                var title = (TitleItem)param;
                if (title == null) return;

                for (var i = 0; i < cnt; i++)
                {
                    var a = ChartCollection[i];
                    if (a == null) continue;
                    if (a.Title != title.TitleName) continue;
                    var type = a.GetType();
                    if (type == null) continue;
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

                    switch (title.ValueSet)
                    {
                        case ChartValueSet.Values1:
                            Chart1Toggle = !Chart1Toggle;
                            break;
                        case ChartValueSet.Values2:
                            Chart2Toggle = !Chart2Toggle;
                            break;
                        case ChartValueSet.Values3:
                            Chart3Toggle = !Chart3Toggle;
                            break;
                        case ChartValueSet.Values4:
                            Chart4Toggle = !Chart4Toggle;
                            break;
                        case ChartValueSet.Values5:
                            Chart5Toggle = !Chart5Toggle;
                            break;
                        case ChartValueSet.Values6:
                            Chart6Toggle = !Chart6Toggle;
                            break;

                    }
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {ex.Message},  {ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickChartSizeCmd;
        public ICommand ClickChartSizeCmd
        {
            get
            {
                return _clickChartSizeCmd ?? (_clickChartSizeCmd = new RelayCommand(param => ChartSizeCmd()));
            }
            set => _clickChartSizeCmd = value;
        }
        private void ChartSizeCmd()
        {
            try
            {
                if (SelectedIndex == null) return;
                SetXAxisLimits(SelectedIndex.StartTime, SelectedIndex.StartTime + TimeSpan.FromMinutes(1));
                ResizeAxes(true, true);
            }
            catch (Exception ex)
            {
                OpenDialog(ex.Message);
            }
        }


        #endregion

        #region Methods

        private void CloseLogView()
        {
            if (Application.Current.MainWindow != null) Application.Current.MainWindow.Close();
        }
        private static string GetFileName()
        {
            var openFileDialog = new OpenFileDialog
            {
                FileName = "*ChartLog*",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Multiselect = false,
            };
            return openFileDialog.ShowDialog() != true ? null : openFileDialog.FileName;
        }
        private bool LoadFile(string filename)
        {
            bool loaded;
            const int BufferSize = 128;
            LineCount = 0;
            BadLineCount = 0;
            var chartType = ChartType.Plot;
            var endTime = string.Empty;
            var loadedLogs = new List<List<string>>();
            List<string> chartlog = null;
            using (var fileStream = File.OpenRead(filename))

            {
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
                {
                    string readline;
                    while ((readline = streamReader.ReadLine()) != null)
                    {
                        var recBad = true;
                        try
                        {
                            if (readline.Length <= 0) continue;

                            var line = readline.Split(',');
                            if (line.Length < 2) continue;
                            endTime = line[2].Trim();
                            var result = Enum.TryParse(line[0].Trim(), out ChartType type);
                            var result1 = Enum.TryParse(line[1].Trim(), out ChartLogCode code);
                            if (result && result1)
                            {
                                switch (type)
                                {
                                    case ChartType.Pulses:
                                    case ChartType.Plot:
                                        chartType = type;
                                        break;
                                    default:
                                        continue;
                                }

                                switch (code)
                                {
                                    case ChartLogCode.Start:
                                        if (chartlog == null)
                                        {
                                            chartlog = new List<string> { readline };
                                        }
                                        else
                                        {
                                            if (chartlog.Count > 1) loadedLogs.Add(chartlog);
                                            chartlog = new List<string> { readline };
                                        }
                                        break;
                                    case ChartLogCode.Info:
                                        chartlog?.Add(readline);
                                        break;
                                    case ChartLogCode.Data:
                                        chartlog?.Add(readline);
                                        break;
                                    case ChartLogCode.Point:
                                        chartlog?.Add(readline);
                                        break;
                                    case ChartLogCode.Series:
                                        chartlog?.Add(readline);
                                        break;
                                    default:
                                        BadLineCount++;
                                        continue;
                                }
                                recBad = false;
                                LineCount++;
                            }
                        }
                        catch (IndexOutOfRangeException)
                        {
                            BadLineCount++;
                            continue;
                        }
                        catch (Exception ex)
                        {
                            OpenDialog(ex.Message);
                            return false;
                        }

                        if (recBad) BadLineCount++;
                    }
                    // check for log with no stop or end of file
                    if (chartlog?.Count > 0) loadedLogs.Add(chartlog);
                }
            }

            if (LineCount <= 0)
            {
                OpenDialog($"{Application.Current.Resources["chtNoValidRecords"]}");
                loaded = false;
            }
            else
            {
                chartlog?.Add($"{chartType},{ChartLogCode.Data},{endTime},LineCount,{LineCount}");
                chartlog?.Add($"{chartType},{ChartLogCode.Data},{endTime},BadLineCount,{BadLineCount}");
                loaded = true;
                Logs = loadedLogs;
                IndexItems = null;
            }

            return loaded;
        }
        private bool BuildIndex()
        {
            var index = new List<IndexItem>();
            var recno = 0;

            foreach (var list in Logs)
            {
                try
                {
                    var firstline = list.First().Split(',');
                    if (firstline.Length < 4) continue;
                    var type = firstline[4];
                    var result =  Enum.TryParse(firstline[0].Trim(), out ChartType ctype);
                    if (!result) continue;
                    var lastline = list.Last().Split(',');
                    if (firstline.Length < 2) continue;
                    var pass = DateTime.TryParseExact(firstline[2].Trim(), "yyyy-MM-dd HH:mm:ss.fff", null, System.Globalization.DateTimeStyles.None, out var startTime);
                    if (!pass) continue;
                    pass = DateTime.TryParseExact(lastline[2].Trim(), "yyyy-MM-dd HH:mm:ss.fff", null, System.Globalization.DateTimeStyles.None, out var endTime);
                    if (!pass) continue;

                    recno++;
                    var indexItem = new IndexItem
                    {
                        RecNo = recno,
                        StartTime = startTime,
                        EndTime = endTime,
                        TimeLength = endTime - startTime,
                        Type = type,
                        ChartType = ctype
                    };
                    index.Add(indexItem);
                }
                catch (Exception ex)
                {
                    OpenDialog(ex.Message);
                    return false;
                }
            }

            if (index.Count <= 0) return false;
            IndexItems = index;
            return true;
        }
        private void LoadDefaults()
        {
            ChartCollection = new SeriesCollection();
            DataKeys = new BindingList<DataKey>();
            SelectedLog = new List<string>();
            TitleItems = new BindingList<TitleItem>();
            LogText = new BindingList<string>();

            if (Chart1Values == null) Chart1Values = new GearedValues<PointModel>();
            if (Chart2Values == null) Chart2Values = new GearedValues<PointModel>();
            if (Chart3Values == null) Chart3Values = new GearedValues<PointModel>();
            if (Chart4Values == null) Chart4Values = new GearedValues<PointModel>();
            if (Chart5Values == null) Chart5Values = new GearedValues<PointModel>();
            if (Chart6Values == null) Chart6Values = new GearedValues<PointModel>();

            ChartQuality = Quality.Low;

            Mapper = Mappers.Xy<PointModel>()
                .X(model => model.DateTime.Ticks) //use DateTime.Ticks as X
                .Y(model => model.Value)
                .Fill(model => model.Fill)
                .Stroke(model => model.Stroke);

            //lets save the mapper globally.
            Charting.For<PointModel>(Mapper);

            StartDateTicks = HiResDateTime.UtcNow.Ticks - TimeSpan.FromSeconds(5).Ticks;
            EndDateTicks = HiResDateTime.UtcNow.Ticks + TimeSpan.FromSeconds(5).Ticks;

            //set how to display the Labels
            FormatterX = value => new DateTime((long)value).ToString("HH:mm:ss");
            FormatterY = value => value.ToString("N2");

            AxisXUnit = TimeSpan.FromSeconds(1).Ticks; //AxisX unit = 10000000
            AxisYUnit = .5;
            AxisYMax = 3;
            AxisYMin = -3;
            //AxisXSeconds = 40;
            SetXAxisLimits(HiResDateTime.UtcNow, HiResDateTime.UtcNow);
            Zoom = "Xy";
            DisableAnimations = true;
            AnimationsSpeed = TimeSpan.Zero;
            LogTextVis = false;
        }
        private void LoadLogByIndex(IndexItem indexItem)
        {
            using (new WaitCursor())
            {
                if (indexItem == null) return;
                var index = indexItem.RecNo - 1;
                if (index > Logs.Count) return;
                var log = Logs[index];

                ClearChart();
                ChartsQuality(ChartQuality);

                foreach (var linearray in log.Select(line => line.Split(',')))
                {
                    var result = Enum.TryParse<ChartLogCode>(linearray[1], true, out var code);
                    if (!result) continue;
                    var result1 = Enum.TryParse<ChartType>(linearray[0], true, out var type);
                    if (!result1) continue;
                    if (indexItem.ChartType != type) continue;
                    LoadLogLine(code, linearray);
                    LogText.Add(string.Join(",", linearray));
                }

                StartDateTicks = indexItem.StartTime.Ticks;
                EndDateTicks = indexItem.EndTime.Ticks;
                SetXAxisLimits(indexItem.StartTime, indexItem.StartTime + TimeSpan.FromMinutes(1));
                //ResizeY();
                ResizeAxes(true, true);
            }
        }
        private void LoadLogLine(ChartLogCode code, string[] lineArray)
        {
            bool result;
            switch (code)
            {
                case ChartLogCode.Start:
                    break;
                case ChartLogCode.Info:
                    break;
                case ChartLogCode.Data:
                    if (lineArray.Length < 3) break;
                    var pair = new DataKey { Key = lineArray[3], Value = lineArray[4] };
                    DataKeys.Add(pair);
                    if (pair.Key == "Scale")
                    {
                        result = Enum.TryParse<ChartScale>(pair.Value, true, out var scale);
                        Scale = result ? scale : ChartScale.Unknown;
                    }

                    break;
                case ChartLogCode.Point:
                    if (lineArray.Length < 3) break;
                    result = DateTime.TryParseExact(lineArray[2].Trim(), "yyyy-MM-dd HH:mm:ss.fff", null, System.Globalization.DateTimeStyles.None, out var datetime);
                    if (!result) break;
                    result = double.TryParse(lineArray[3].Trim(), out var value);
                    if (!result) break;
                    result = Enum.TryParse<ChartValueSet>(lineArray[4], true, out var valueSet);
                    if (!result) break;
                    var pointModel = new PointModel { DateTime = datetime, Value = value };
                    switch (valueSet)
                    {
                        case ChartValueSet.Values1:
                            Chart1Values.Add(pointModel);
                            break;
                        case ChartValueSet.Values2:
                            Chart2Values.Add(pointModel);
                            break;
                        case ChartValueSet.Values3:
                            Chart3Values.Add(pointModel);
                            break;
                        case ChartValueSet.Values4:
                            Chart4Values.Add(pointModel);
                            break;
                        case ChartValueSet.Values5:
                            Chart5Values.Add(pointModel);
                            break;
                        case ChartValueSet.Values6:
                            Chart6Values.Add(pointModel);
                            break;
                    }

                    if (pointModel.Value > _maxPoint) _maxPoint = pointModel.Value;
                    if (pointModel.Value < _minPoint) _minPoint = pointModel.Value;
                    break;
                case ChartLogCode.Series:
                    if (lineArray.Length < 9) break;
                    result = Enum.TryParse<ChartSeriesType>(lineArray[3], true, out var chartSeriesType);
                    if (!result) break;
                    result = Enum.TryParse<ChartValueSet>(lineArray[4], true, out var chartValueSet);
                    if (!result) break;
                    IChartValues values;
                    switch (chartValueSet)
                    {
                        case ChartValueSet.Values1:
                            values = Chart1Values;
                            Chart1Toggle = true;
                            break;
                        case ChartValueSet.Values2:
                            values = Chart2Values;
                            Chart2Toggle = true;
                            break;
                        case ChartValueSet.Values3:
                            values = Chart3Values;
                            Chart3Toggle = true;
                            break;
                        case ChartValueSet.Values4:
                            values = Chart4Values;
                            Chart4Toggle = true;
                            break;
                        case ChartValueSet.Values5:
                            values = Chart5Values;
                            Chart5Toggle = true;
                            break;
                        case ChartValueSet.Values6:
                            values = Chart6Values;
                            Chart6Toggle = true;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    if (values == null) break;
                    int pointsize;
                    int scaleat;
                    var brush = new SolidColorBrush();
                    var title = string.Empty;

                    switch (chartSeriesType)
                    {
                        case ChartSeriesType.GLineSeries:
                            int.TryParse(lineArray[7], out pointsize);
                            int.TryParse(lineArray[10], out scaleat);
                            title = lineArray[5];
                            brush = (SolidColorBrush)(new BrushConverter().ConvertFrom(lineArray[6]));
                            ChartColor(chartValueSet, brush);
                            var gLineSeries = NewGLineSeries(title, values, pointsize, brush, scaleat);
                            ChartCollection.Add(gLineSeries);
                            break;
                        case ChartSeriesType.GColumnSeries:
                            int.TryParse(lineArray[7], out pointsize);
                            int.TryParse(lineArray[10], out scaleat);
                            title = lineArray[5];
                            brush = (SolidColorBrush)(new BrushConverter().ConvertFrom(lineArray[6]));
                            ChartColor(chartValueSet, brush);
                            var gColumnSeries = NewGColumnSeries(title, values, pointsize, brush, scaleat);
                            ChartCollection.Add(gColumnSeries);
                            break;
                        case ChartSeriesType.GStepLineSeries:
                            int.TryParse(lineArray[7], out pointsize);
                            int.TryParse(lineArray[8], out scaleat);
                            title = lineArray[5];
                            brush = (SolidColorBrush)(new BrushConverter().ConvertFrom(lineArray[6]));
                            ChartColor(chartValueSet, brush);
                            var gStepLineSeries = NewGStepLineSeries(title, values, pointsize, brush, scaleat);
                            ChartCollection.Add(gStepLineSeries);
                            break;
                        case ChartSeriesType.GScatterSeries:
                            int.TryParse(lineArray[7], out pointsize);
                            int.TryParse(lineArray[10], out scaleat);
                            title = lineArray[5];
                            brush = (SolidColorBrush)(new BrushConverter().ConvertFrom(lineArray[6]));
                            ChartColor(chartValueSet, brush);
                            var gScatterSeries = NewGScatterSeries(title, values, pointsize, brush, scaleat);
                            ChartCollection.Add(gScatterSeries);
                            break;
                    }

                    if (!string.IsNullOrEmpty(title))
                    {
                        var titleItem = new TitleItem { TitleName = title, Fill = brush, ValueSet = chartValueSet };
                        TitleItems.Add(titleItem);
                    }

                    break;
            }
        }
        private void ClearChart()
        {
            ChartCollection?.Clear();
            TitleItems?.Clear();
            DataKeys.Clear();
            LogText?.Clear();
            SelectedLog?.Clear();
            Chart1Values?.Clear();
            Chart2Values?.Clear();
            Chart3Values?.Clear();
            Chart4Values?.Clear();
            Chart5Values?.Clear();
            Chart6Values?.Clear();
            _maxPoint = 0;
            _minPoint = 0;

            Chart1Toggle = false;
            Chart2Toggle = false;
            Chart3Toggle = false;
            Chart4Toggle = false;
            Chart5Toggle = false;
            Chart6Toggle = false;
        }
        private void ChartColor(ChartValueSet valueSet, Brush col)
        {
            switch (valueSet)
            {
                case ChartValueSet.Values1:
                    Chart1Color = col;
                    break;
                case ChartValueSet.Values2:
                    Chart2Color = col;
                    break;
                case ChartValueSet.Values3:
                    Chart3Color = col;
                    break;
                case ChartValueSet.Values4:
                    Chart4Color = col;
                    break;
                case ChartValueSet.Values5:
                    Chart5Color = col;
                    break;
                case ChartValueSet.Values6:
                    Chart6Color = col;
                    break;
            }
        }
        private void ChartsQuality(Quality chartQuality)
        {
            Chart1Values.WithQuality(chartQuality);
            Chart2Values.WithQuality(chartQuality);
            Chart3Values.WithQuality(chartQuality);
            Chart4Values.WithQuality(chartQuality);
            Chart5Values.WithQuality(chartQuality);
            Chart6Values.WithQuality(chartQuality);
        }

       private void ResizeAxes(bool x, bool y)
        {
            if(y){ AxisYMax -= double.NaN;}
            if(x){ AxisYMin += double.NaN;}
        }
        private void SetXAxisLimits(DateTime start, DateTime end)
        {
            AxisXMin = start.Ticks - TimeSpan.FromSeconds(5).Ticks;
            AxisXMax = end.Ticks + TimeSpan.FromSeconds(5).Ticks;
        }
        private static Brush ToBrush(Color color)
        {
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }
        private static GColumnSeries NewGColumnSeries(string title, IChartValues values, int pointSize, Brush color, int scaleAt)
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
            return series;
        }
        private GLineSeries NewGLineSeries(string title, IChartValues values, int pointSize, Brush color, int scaleAt)
        {
            var series = new GLineSeries
            {
                Fill = ToBrush(Color.Transparent),
                LineSmoothness = LineSmoothness,
                MinWidth = 1,
                Stroke = color,
                StrokeThickness = 1,
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = pointSize,
                PointForeground = color,
                ScalesYAt = scaleAt,
                Title = title,
                Values = values
            };
            return series;
        }
        private static GScatterSeries NewGScatterSeries(string title, IChartValues values, int pointSize, Brush color, int scaleAt)
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

            return series;
        }
        private static GStepLineSeries NewGStepLineSeries(string title, IChartValues values, int pointSize, Brush color, int scaleAt)
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
            return series;
        }

        #endregion

        #region Chart Properties

        /// <summary>
        /// Index items creates list of charts for the list view
        /// </summary>
        private List<IndexItem> _indexItems;
        public List<IndexItem> IndexItems
        {
            get => _indexItems;
            set
            {
                _indexItems = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Determines which chart to load from the list view
        /// </summary>
        private IndexItem _selectedIndex;
        public IndexItem SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value;
                LoadLogByIndex(value);
                OnPropertyChanged();
            }
        }

        private BindingList<DataKey> _dataKeys;
        public BindingList<DataKey> DataKeys
        {
            get => _dataKeys;
            set
            {
                if (_dataKeys == value) return;
                _dataKeys = value;
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

        private long _starDateTicks;
        public long StartDateTicks
        {
            get => _starDateTicks;
            set
            {
                if (Math.Abs(_starDateTicks - value) < 0) return;
                _starDateTicks = value;
                OnPropertyChanged();
            }
        }

        private long _endDateTicks;
        public long EndDateTicks
        {
            get => _endDateTicks;
            set
            {
                if (Math.Abs(_endDateTicks - value) < 0) return;
                _endDateTicks = value;
                OnPropertyChanged();
            }
        }
        public void Dispose()
        {

        }
        private string _version;
        public string Version
        {
            get => _version;
            set
            {
                _version = value;
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

        private string _rangeTxt;
        public string RangeTxt
        {
            get => _rangeTxt;
            set
            {
                _rangeTxt = value;
                OnPropertyChanged();
            }
        }

        private int _lineCount;
        public int LineCount
        {
            get => _lineCount;
            set
            {
                if (_lineCount == value) return;
                _lineCount = value;
                OnPropertyChanged();
            }
        }

        private BindingList<string> _logText;
        public BindingList<string> LogText
        {
            get => _logText;
            set
            {
                if (_logText == value) return;
                _logText = value;
                OnPropertyChanged();
            }
        }

        private bool _logTextVis;
        public bool LogTextVis
        {
            get => _logTextVis;
            set
            {
                if (_logTextVis == value) return;
                _logTextVis = value;
                OnPropertyChanged();
            }
        }

        private int BadLineCount { get; set; }

        private bool _disableAnimations;
        public bool DisableAnimations
        {
            get => _disableAnimations;
            private set
            {
                if (_disableAnimations == value) return;
                _disableAnimations = value;
                OnPropertyChanged();
            }
        }

        private List<List<string>> Logs = new List<List<string>>();

        private List<string> _selectedLog;

        public List<string> SelectedLog
        {
            get => _selectedLog;
            set
            {
                if (_selectedLog == value) return;
                _selectedLog = value;
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

        private SeriesCollection _chartCollection;
        public SeriesCollection ChartCollection
        {
            get => _chartCollection;
            set
            {
                _chartCollection = value;
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

        #region Chart1

        public GearedValues<PointModel> Chart1Values { get; set; }

        private bool _chart1Toggle;
        public bool Chart1Toggle
        {
            get => _chart1Toggle;
            set
            {
                if (_chart1Toggle == value) return;
                _chart1Toggle = value;
                OnPropertyChanged();
            }
        }

        private Brush _chart1Color;
        public Brush Chart1Color
        {
            get => _chart1Color;
            set
            {
                if (_chart1Color == value) return;
                _chart1Color = value;
                OnPropertyChanged();
            }
        }


        #endregion

        #region Chart2

        public GearedValues<PointModel> Chart2Values { get; set; }

        private bool _chart2Toggle;
        public bool Chart2Toggle
        {
            get => _chart2Toggle;
            set
            {
                if (_chart2Toggle == value) return;
                _chart2Toggle = value;
                OnPropertyChanged();
            }
        }

        private Brush _chart2Color;
        public Brush Chart2Color
        {
            get => _chart2Color;
            set
            {
                if (_chart2Color == value) return;
                _chart2Color = value;
                OnPropertyChanged();
            }
        }


        #endregion

        #region Chart3

        public GearedValues<PointModel> Chart3Values { get; set; }


        private bool _chart3Toggle;
        public bool Chart3Toggle
        {
            get => _chart3Toggle;
            set
            {
                if (_chart3Toggle == value) return;
                _chart3Toggle = value;
                OnPropertyChanged();
            }
        }

        private Brush _chart3Color;
        public Brush Chart3Color
        {
            get => _chart3Color;
            set
            {
                if (_chart3Color == value) return;
                _chart3Color = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Chart4

        public GearedValues<PointModel> Chart4Values { get; set; }

        private bool _chart4Toggle;
        public bool Chart4Toggle
        {
            get => _chart4Toggle;
            set
            {
                if (_chart4Toggle == value) return;
                _chart4Toggle = value;
                OnPropertyChanged();
            }
        }

        private Brush _chart4Color;
        public Brush Chart4Color
        {
            get => _chart4Color;
            set
            {
                if (_chart4Color == value) return;
                _chart4Color = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Chart5

        public GearedValues<PointModel> Chart5Values { get; set; }

        private bool _chart5Toggle;
        public bool Chart5Toggle
        {
            get => _chart5Toggle;
            set
            {
                if (_chart5Toggle == value) return;
                _chart5Toggle = value;
                OnPropertyChanged();
            }
        }

        private Brush _chart5Color;
        public Brush Chart5Color
        {
            get => _chart5Color;
            set
            {
                if (_chart5Color == value) return;
                _chart5Color = value;
                OnPropertyChanged();
            }
        }


        #endregion

        #region Chart6

        public GearedValues<PointModel> Chart6Values { get; set; }

        private bool _chart6Toggle;
        public bool Chart6Toggle
        {
            get => _chart6Toggle;
            set
            {
                if (_chart6Toggle == value) return;
                _chart6Toggle = value;
                OnPropertyChanged();
            }
        }

        private Brush _chart6Color;
        public Brush Chart6Color
        {
            get => _chart6Color;
            set
            {
                if (_chart6Color == value) return;
                _chart6Color = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Dialog  

        private string _DialogMsg;
        public string DialogMsg
        {
            get => _DialogMsg;
            set
            {
                if (_DialogMsg == value) return;
                _DialogMsg = value;
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
        private void OpenDialog(string msg)
        {
            if (msg != null) DialogMsg = msg;
            DialogContent = new Dialog();
            IsDialogOpen = true;
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

        private ICommand _cancelDialogCommand;
        public ICommand CancelDialogCommand
        {
            get
            {
                return _cancelDialogCommand ?? (_cancelDialogCommand = new RelayCommand(
                           param => CancelDialog()
                       ));
            }
        }
        private void CancelDialog()
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
    }

    internal class IndexItem
    {
        public int RecNo { get; set; }
        public string Type { get; set; }
        public ChartType ChartType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TimeLength { get; set; }
    }
    internal class DataKey
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
