/* Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Input;
using GS.LogView.Helpers;
using GS.Shared;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Geared;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Color = System.Drawing.Color;

namespace GS.LogView
{
    internal class MainWindowVM : ObservableObject, IDisposable
    {
        //Index
        public MainWindowVM()
        {
            DataItems = new ObservableCollection<string>();
            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Settings.StaticPropertyChanged += PropertyChangedSettings;
            Settings.Load();
            LoadDefaults();
        }

        #region Window Items and View Commands

        /// <summary>
        /// Property changes from the server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertyChangedSettings(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                switch (e.PropertyName)
                {
                    case "ThirdColor":
                        ThirdColor = Settings.ThirdColor;
                        break;
                    case "FourthColor":
                        FourthColor = Settings.FourthColor;
                        break;
                    case "RaColor":
                        RaColor = Settings.RaColor;
                        break;
                    case "DecColor":
                        DecColor = Settings.DecColor;
                        break;
                    case "RaLine":
                        RaLine = Settings.RaLine;
                        break;
                    case "RaBar":
                        RaBar = Settings.RaBar;
                        break;
                    case "RaStep":
                        RaStep = Settings.RaStep;
                        break;
                    case "DecLine":
                        DecLine = Settings.DecLine;
                        break;
                    case "DecBar":
                        DecBar = Settings.DecBar;
                        break;
                    case "DecStep":
                        DecStep = Settings.DecStep;
                        break;
                    case "ThirdLine":
                        ThirdLine = Settings.ThirdLine;
                        break;
                    case "ThirdBar":
                        ThirdBar = Settings.ThirdBar;
                        break;
                    case "ThirdStep":
                        ThirdStep = Settings.ThirdStep;
                        break;
                    case "FourthLine":
                        FourthLine = Settings.FourthLine;
                        break;
                    case "FourthBar":
                        FourthBar = Settings.FourthBar;
                        break;
                    case "FourthStep":
                        FourthStep = Settings.FourthStep;
                        break;
                    case "DisableAnimations":
                        DisableAnimations = Settings.DisableAnimations;
                        break;
                    case "LineSmoothness":
                        LineSmoothness = Settings.LineSmoothness;
                        break;
                    case "LineSize":
                        LineSize = Settings.LineSize;
                        break;
                    case "PointSize":
                        PointSize = Settings.PointSize;
                        break;
                }
            }
            catch (Exception ex)
            {
                OpenDialog(ex.Message);
            }
        }

        public void Dispose()
        {
            RaValues?.Dispose();
            DecValues?.Dispose();
            ThirdValues?.Dispose();
            FourthValues?.Dispose();
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

        private void CloseLogView()
        {
            if (Application.Current.MainWindow != null) Application.Current.MainWindow.Close();
        }

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
                    LoadFile(filename);
                    ParseLog();
                    if (IndexItems.Count <= 0) return;
                    SelectedItem = IndexItems.First();
                    LoadChartFromIndex(SelectedItem.Index);
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
            Properties.LogView.Default.WindowState = WindowState.Minimized;
        }

        private ICommand _clickChartZoomResetCommand;
        public ICommand ClickChartZoomResetCommand
        {
            get
            {
                return _clickChartZoomResetCommand ?? (_clickChartZoomResetCommand = new RelayCommand(param => ChartZoomResetCommand()));
            }
            set => _clickChartZoomResetCommand = value;
        }
        private void ChartZoomResetCommand()
        {
            try
            {
                AxisYmax -= double.NaN;
                AxisYmin += double.NaN;

            }
            catch (Exception ex)
            {

                OpenDialog(ex.Message);
            }
        }

        private ICommand _mouseDataHoverCommand;
        public ICommand MouseDataHoverCommand => _mouseDataHoverCommand ?? (_mouseDataHoverCommand = new RelayCommand(DataHoverCommand));
        private void DataHoverCommand(object chartPoint)
        {
            DisplayLogLine((ChartPoint) chartPoint);
        }

        #endregion

        #region Index

        /// <summary>
        /// container for each item in the log
        /// </summary>
        private List<LogItem> LogItems { get; set; }

        /// <summary>
        /// Index items creates list of charts for the listview
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
        /// Determines which chart to load from the listview
        /// </summary>
        private IndexItem _selectedItem;
        public IndexItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                using (new WaitCursor())
                {
                    if (value == null) return;
                    _selectedItem = value;
                    LoadChartFromIndex(value.Index);
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region View Property Settings

        /// <summary>
        /// List of chart info to be displayed in the ItemsControl
        /// </summary>
        public ObservableCollection<string> DataItems { get; }

        /// <summary>
        /// list of avaiable colors
        /// </summary>
        public List<string> ColorsList { get; set; }

        /// <summary>
        /// Store for selected color
        /// </summary>
        public string ThirdColor
        {
            get => Settings.ThirdColor;
            set
            {
                Settings.ThirdColor = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Store for selected color
        /// </summary>
        public string FourthColor
        {
            get => Settings.FourthColor;
            set
            {
                Settings.FourthColor = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Store for selected color
        /// </summary>
        public string RaColor
        {
            get => Settings.RaColor;
            set
            {
                Settings.RaColor = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Store for selected color
        /// </summary>
        public string DecColor
        {
            get => Settings.DecColor;
            set
            {
                Settings.DecColor = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// inverts each plotted item for ra
        /// </summary>
        private bool _invertRa;
        public bool InvertRa
        {
            get => _invertRa;
            set
            {
                if (_invertRa == value) return;
                _invertRa = value;
                foreach (var item in RaValues)
                {
                    item.Value = item.Value * -1;
                }
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// inverts each plotted item for dec
        /// </summary>
        private bool _invertDec;
        public bool InvertDec
        {
            get => _invertDec;
            set
            {
                if (_invertDec == value) return;
                _invertDec = value;
                foreach (var item in DecValues)
                {
                    item.Value = item.Value * -1;
                }
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// inverts each plotted item for ra
        /// </summary>
        private bool _invertThird;
        public bool InvertThird
        {
            get => _invertThird;
            set
            {
                if (_invertThird == value) return;
                _invertThird = value;
                foreach (var item in ThirdValues)
                {
                    item.Value = item.Value * -1;
                }
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// inverts each plotted item for dec
        /// </summary>
        private bool _invertFourth;
        public bool InvertFourth
        {
            get => _invertFourth;
            set
            {
                if (_invertFourth == value) return;
                _invertFourth = value;
                foreach (var item in FourthValues)
                {
                    item.Value = item.Value * -1;
                }
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        public bool RaLine
        {
            get => Settings.RaLine;
            set
            {
                Settings.RaLine = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        public bool RaBar
        {
            get => Settings.RaBar;
            set
            {
                Settings.RaBar = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        public bool RaStep
        {
            get => Settings.RaStep;
            set
            {
                Settings.RaStep = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        public bool DecLine
        {
            get => Settings.DecLine;
            set
            {
                Settings.DecLine = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        public bool DecBar
        {
            get => Settings.DecBar;
            set
            {
                Settings.DecBar = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        public bool DecStep
        {
            get => Settings.DecStep;
            set
            {
                Settings.DecStep = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        public bool ThirdLine
        {
            get => Settings.ThirdLine;
            set
            {
                Settings.ThirdLine = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        public bool ThirdBar
        {
            get => Settings.ThirdBar;
            set
            {
                Settings.ThirdBar = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        public bool ThirdStep
        {
            get => Settings.ThirdStep;
            set
            {
                Settings.ThirdStep = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        public bool FourthLine
        {
            get => Settings.FourthLine;
            set
            {
                Settings.FourthLine = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        public bool FourthBar
        {
            get => Settings.FourthBar;
            set
            {
                Settings.FourthBar = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// visability check
        /// </summary>
        public bool FourthStep
        {
            get => Settings.FourthStep;
            set
            {
                Settings.FourthStep = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Chart Animations check
        /// </summary>
        public bool DisableAnimations
        {
            get => Settings.DisableAnimations;
            set
            {
                Settings.DisableAnimations = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Time taken to plot an animated plot
        /// </summary>
        public IList<int> AnimationTimes { get; set; }
        public int AnimationTime
        {
            get => Settings.AnimationTime;
            set
            {
                Settings.AnimationTime = value;
                AnimationsSpeed = TimeSpan.FromMilliseconds(value * 100);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Smoothness from one plot to the next
        /// </summary>
        public IList<int> Smoothness { get; set; }
        public int LineSmoothness
        {
            get => Settings.LineSmoothness;
            set
            {
                Settings.LineSmoothness = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Plotted line size
        /// </summary>
        public IList<double> LineSizes { get; set; }
        public double LineSize
        {
            get => Settings.LineSize;
            set
            {
                Settings.LineSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Plotted point size
        /// </summary>
        public IList<int> PointSizes { get; set; }
        public int PointSize
        {
            get => Settings.PointSize;
            set
            {
                Settings.PointSize = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Chart Properties and Data

        private TimeSpan _animationsSpeed;
        public TimeSpan AnimationsSpeed
        {
            get => _animationsSpeed;
            set
            {
                _animationsSpeed = value;
                OnPropertyChanged();
            }
        }

        private DateTime _starDate;
        public DateTime StartDate
        {
            get => _starDate;
            set
            {
                _starDate = value;
                OnPropertyChanged();
            }
        }

        private double _starDateTicks;
        public double StartDateTicks
        {
            get => _starDateTicks;
            set
            {
                _starDateTicks = value;
                OnPropertyChanged();
            }
        }

        private DateTime _endDate;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged();
            }
        }

        private double _endDateTicks;
        public double EndDateTicks
        {
            get => _endDateTicks;
            set
            {
                _endDateTicks = value;
                OnPropertyChanged();
            }
        }
        
        private string _rangetxt;
        public string RangeTxt
        {
            get => _rangetxt;
            set
            {
                _rangetxt = value;
                OnPropertyChanged();
            }
        }

        private double _axisYmax;
        public double AxisYmax
        {
            get => _axisYmax;
            set
            {
                _axisYmax = value;
                OnPropertyChanged();
            }
        }

        private double _axisYmin;
        public double AxisYmin
        {
            get => _axisYmin;
            set
            {
                _axisYmin = value;
                OnPropertyChanged();
            }
        }

        private bool _raRowHeight;
        public bool RaRowHeight
        {
            get => _raRowHeight;
            set
            {
                if (value == _raRowHeight) return;
                _raRowHeight = value;
                OnPropertyChanged();
            }
        }

        private bool _decRowHeight;
        public bool DecRowHeight
        {
            get => _decRowHeight;
            set
            {
                if (value == _decRowHeight) return;
                _decRowHeight = value;
                OnPropertyChanged();
            }
        }

        private string _chartName;
        public string ChartName
        {
            get => _chartName;
            set
            {
                if (_chartName == value) return;
                _chartName = value;
                OnPropertyChanged();
            }
        }

        private string _strRaLineOne;
        public string StrRaLineOne
        {
            get => _strRaLineOne;
            private set
            {
                if (_strRaLineOne == value) return;
                _strRaLineOne = value;
                OnPropertyChanged();
            }
        }

        private string _strDecLineOne;
        public string StrDecLineOne
        {
            get => _strDecLineOne;
            set
            {
                if (_strDecLineOne == value) return;
                _strDecLineOne = value;
                OnPropertyChanged();
            }
        }

        private string _logLineTextBlock;
        public string LogLineTextBlock
        {
            get => _logLineTextBlock;
            private set
            {
                if (_logLineTextBlock == value) return;
                _logLineTextBlock = value;
                OnPropertyChanged();
            }
        }

        private double _from;
        public double From
        {
            get { return _from; }
            set
            {
                _from = value;
                OnPropertyChanged();
            }
        }

        private double _to;
        public double To
        {
            get { return _to; }
            set
            {
                _to = value;
                OnPropertyChanged();
            }
        }

        private Func<double, string> _xformatter;
        public Func<double, string> XFormatter
        {
            get { return _xformatter; }
            set
            {
                _xformatter = value;
                OnPropertyChanged();
            }
        }

        private Func<double, string> _yformatter;
        public Func<double, string> YFormatter
        {
            get { return _yformatter; }
            set
            {
                _yformatter = value;
                OnPropertyChanged();
            }
        }

        public long AxisXstep { get; set; }
        public long AxisXunit { get; set; }

        public double AxisYunit { get; set; }

        private GearedValues<DateTimePoint> _thirdValues;
        public GearedValues<DateTimePoint> ThirdValues
        {
            get => _thirdValues;
            set
            {
                _thirdValues = value;
                OnPropertyChanged();
            }
        }

        private GearedValues<DateTimePoint> _fourthValues;
        public GearedValues<DateTimePoint> FourthValues
        {
            get => _fourthValues;
            set
            {
                _fourthValues = value;
                OnPropertyChanged();
            }
        }

        private GearedValues<DateTimePoint> _raValues;
        public GearedValues<DateTimePoint> RaValues 
        {
            get => _raValues;
            set
            {
                _raValues = value;
                OnPropertyChanged();
            }
        }

        private GearedValues<DateTimePoint> _decValues;
        public GearedValues<DateTimePoint> DecValues
        {
            get => _decValues;
            set
            {
                _decValues = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Chart defaults
        /// </summary>
        private void LoadDefaults()
        {
            From = Principles.HiResDateTime.UtcNow.Ticks - TimeSpan.FromSeconds(5).Ticks;
            To = Principles.HiResDateTime.UtcNow.Ticks + TimeSpan.FromSeconds(5).Ticks;
            XFormatter = x => new DateTime((long)x).ToString("d");
            StartDateTicks = From;
            EndDateTicks = To;

            // combo selections
            ColorsList = new List<string>();
            foreach (KnownColor colorValue in Enum.GetValues(typeof(KnownColor)))
            {
                var color = Color.FromKnownColor(colorValue);
                if (!ColorsList.Contains(color.Name) && !color.IsSystemColor)
                { ColorsList.Add(color.Name); }
            }

            RaValues = new GearedValues<DateTimePoint>().WithQuality(Quality.Highest);
            DecValues = new GearedValues<DateTimePoint>().WithQuality(Quality.Highest);
            ThirdValues = new GearedValues<DateTimePoint>().WithQuality(Quality.Highest);
            FourthValues = new GearedValues<DateTimePoint>().WithQuality(Quality.Highest);

            AxisXstep = TimeSpan.FromSeconds(1).Ticks;
            AxisXunit = TimeSpan.FromSeconds(1).Ticks; //AxisXunit = 10000000
            AxisYunit = .5;
            AxisYmax = 3;
            AxisYmin = -3;
            DisableAnimations = true;
            AnimationTimes = new List<int>(Enumerable.Range(0, 11));
            Smoothness = new List<int>(Enumerable.Range(0, 11));
            LineSizes = new List<double>(Numbers.InclusiveRange(.5,5));
            PointSizes = new List<int>(Enumerable.Range(0, 21));
            LineSize = 1.5;
            ClearCharts();
        }

        /// <summary>
        /// Gets the filename from the filedialog
        /// </summary>
        /// <returns></returns>
        private static string GetFileName()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt",
                Multiselect = false, 
            };
            return openFileDialog.ShowDialog() != true ? null : openFileDialog.FileName;
        }

        /// <summary>
        /// Load the log file into LogItems 
        /// </summary>
        /// <param name="filename"></param>
        private void LoadFile(string filename)
        {
            ClearCharts();
            const int BufferSize = 128;
            var linecount = 0;
            LogItems = null;
            var logitem = new List<LogItem>();
            using (var fileStream = File.OpenRead(filename))
               
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
            {
                string readline;
                while ((readline = streamReader.ReadLine()) != null)
                {
                    var recBad = false;
                    var item = new LogItem { Line = readline };
                    try
                    {
                        if (readline.Length <= 0 && linecount > 100000) continue;

                        var line = readline.Split('\t');
                        if (line.Length < 2){ continue; }

                        var res = Enum.TryParse(line[0].Trim(), out ChartItemCode a);
                        if (res) { item.ItemCode = a; } else { continue; }
                    }
                    catch (IndexOutOfRangeException)
                    {
                        recBad = true;
                    }

                    if (recBad) continue;
                    logitem.Add(item);
                    linecount++;
                }
            }

            if (logitem.Count <= 0)
            {
                OpenDialog($"No valid records found");
            }
            else
            {
                LogItems = logitem;
            }
        }

        /// <summary>
        /// Parse and create the index items from loaded log
        /// </summary>
        private void ParseLog()
        {
            if (LogItems == null) return;
            if (LogItems.Count <= 0 ) return;
            var index = 0;
            IndexItems = null;
            var indexItems = new List<IndexItem>();
            foreach (var logitem in LogItems)
            {
                switch (logitem.ItemCode)
                {
                    case ChartItemCode.Start:
                        var item = new IndexItem { ItemCode = logitem.ItemCode };
                        try
                        {
                            var line = logitem.Line.Split('\t');
                            if (line.Length >= 1) item.Data = line[1].Trim();
                        }
                        catch (Exception)
                        {
                            break;
                        }
                        index++;
                        item.Index = index;
                        indexItems.Add(item);
                        break;
                    case ChartItemCode.Stop:
                        try
                        {
                            logitem.Index = index;
                            var line = logitem.Line.Split('\t');
                            if (line.Length >= 1) logitem.Data = line[1].Trim();
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                        break;
                    case ChartItemCode.Data:
                        logitem.Index = index;
                        try
                        {
                            logitem.Index = index;
                            var line = logitem.Line.Split('\t');
                            if (line.Length >= 1) logitem.Data = line[1].Trim();
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                        break;
                    case ChartItemCode.RaValue:
                    case ChartItemCode.DecValue:
                    case ChartItemCode.ThirdValue:
                    case ChartItemCode.FourthValue:
                        try
                        {
                            logitem.Index = index;
                            var line = logitem.Line.Split('\t');
                            if (line.Length >= 2)
                            {
                                var res = DateTime.TryParseExact(line[1].Trim(), "yyyy:dd:MM:HH:mm:ss.fff", null, System.Globalization.DateTimeStyles.None, out var a);
                                if (res) { logitem.X = a; } else { continue; }
                                res = double.TryParse(line[2].Trim(), out var b);
                                if (res) { logitem.Y = b; }
                            }
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                        break;
                }
            }

            if (indexItems.Count <= 0)
            {
                OpenDialog("No valid indexes found");
            }
            else
            {
                var removables = new List<IndexItem>();
                foreach (var indexitem in indexItems)
                {
                    var findfirst = LogItems.Find(x => x.Index == indexitem.Index && x.X > new DateTime(2000, 1, 1));
                    if (findfirst != null) indexitem.StartTime = findfirst.X;
                    var findlast = LogItems.FindLast(x => x.Index == indexitem.Index && x.X > new DateTime(2000,1,1));
                    if (findlast != null) indexitem.EndTime = findlast.X;
                    if (findfirst == null || findlast == null)
                    {
                        removables.Add(indexitem);
                        continue;
                    }
                    indexitem.TimeLength = indexitem.EndTime - indexitem.StartTime;
                }

                // remove any indexes that don't have data
                if (removables.Count > 0)
                {
                    foreach (var ritem in removables)
                    {
                        indexItems.Remove(ritem);
                    }
                }

                IndexItems = indexItems;
            }
        }

        /// <summary>
        /// Loads the chart from an individual index
        /// </summary>
        /// <param name="index"></param>
        private void LoadChartFromIndex(int index)
        {
            ClearCharts();
            if (IndexItems == null || LogItems == null) return;
            if (IndexItems.Count <= 0 || LogItems.Count <= 0) return;
            
            // pull index record
            var indexItem = IndexItems.Find(x => x.Index == index);
            if (indexItem == null) return;
            LoadData(indexItem);
        }

        /// <summary>
        /// Load data lines from an individual index log
        /// </summary>
        /// <param name="indexItem"></param>
        private void LoadData(IndexItem indexItem)
        {
            if (LogItems == null) return;
            
            // pull log records
            var logitems = LogItems.FindAll(x => x.Index == indexItem.Index);
            if (logitems.Count == 0) return;

            From = indexItem.StartTime.Ticks - TimeSpan.FromSeconds(5).Ticks;
            To = indexItem.EndTime.Ticks + TimeSpan.FromSeconds(5).Ticks;
            XFormatter = x => new DateTime((long)x).ToString("d");
            YFormatter = value => value.ToString("N2");

            StartDate = indexItem.StartTime;
            StartDateTicks = indexItem.StartTime.Ticks;
            EndDate = indexItem.EndTime;
            EndDateTicks = indexItem.EndTime.Ticks;

            foreach (var item in logitems)
            {
                switch (item.ItemCode)
                {
                    case ChartItemCode.Data:
                        DataItems.Add(item.Data);
                        break;
                    case ChartItemCode.RaValue:
                        RaValues.Add(new DateTimePoint { DateTime = item.X, Value = item.Y });
                        break;
                    case ChartItemCode.DecValue:
                        DecValues.Add(new DateTimePoint { DateTime = item.X, Value = item.Y });
                        break;
                    case ChartItemCode.ThirdValue:
                        ThirdValues.Add(new DateTimePoint { DateTime = item.X, Value = item.Y });
                        break;
                    case ChartItemCode.FourthValue:
                        FourthValues.Add(new DateTimePoint { DateTime = item.X, Value = item.Y });
                        break;
                }
            }
        }

        /// <summary>
        /// Reset chart data
        /// </summary>
        private void ClearCharts()
        {
            RaValues?.Clear();
            DecValues?.Clear();
            ThirdValues?.Clear();
            FourthValues?.Clear();

            StrRaLineOne = string.Empty;
            RangeTxt = string.Empty;
            if(DataItems.Count > 0) DataItems.Clear();
            LogLineTextBlock = string.Empty;
        }

        /// <summary>
        /// Puts together the info for a plotted point
        /// </summary>
        /// <param name="chartPoint"></param>
        private void DisplayLogLine(ChartPoint chartPoint)
        {
            try
            {
                var dtp = (DateTimePoint) chartPoint.Instance;
                var findfirst = LogItems.Find(x => x.X == dtp.DateTime && (Math.Abs(x.Y).Equals(Math.Abs(dtp.Value))));
                if(findfirst != null) LogLineTextBlock = findfirst.Line = findfirst.Line.Replace("\t", " | "); 
            }
            catch (Exception ex)
            {
                OpenDialog($"{ex.Message}");
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
    }

    /// <summary>
    /// Contains properties for each chart found and used in the list view
    /// </summary>
    internal class IndexItem
    {
        public int Index { get; set; }
        public ChartItemCode ItemCode { get; set; }
        public string Data { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TimeLength { get; set; }
    }

    /// <summary>
    /// Raw log item information
    /// </summary>
    internal class LogItem
    {
        public int Index { get; set; }
        public ChartItemCode ItemCode { get; set; }
        public DateTime X { get; set; }
        public double Y { get; set; }
        public string Data { get; set; }
        public string Line { get; set; }
    }
}
