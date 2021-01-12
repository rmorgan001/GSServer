/* Copyright(C) 2019-2021  Rob Morgan (robert.morgan.e@gmail.com)

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
using ASCOM.DeviceInterface;
using GS.Principles;
using GS.Server.Controls.Dialogs;
using GS.Server.Helpers;
using GS.Server.Main;
using GS.Server.SkyTelescope;
using GS.Shared;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Geared;
using LiveCharts.Wpf;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Timer = System.Timers.Timer;

namespace GS.Server.Pec
{
    public class PecVM : ObservableObject, IPageVM, IDisposable
    {
        #region Fields
        public string TopName => "PEC";
        public string BottomName => "PEC";
        public int Uid => 7;

        private const double _siderealDaySeconds = 86164.1;
        private const double _secondsOfArc = 1296000.0;
        private const string _tab = "~"; //"\t";

        private readonly SkyTelescopeVM _skyTelescopeVM;
        private Timer _timer;
        private readonly object _timerLock = new object();
        private static readonly SemaphoreSlim _lockFile = new SemaphoreSlim(1);
        private static readonly string _logPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.Create);
        private PecTrainingDefinition PecTrainDef;
        private PecLogData PrevCapture;
        private CancellationTokenSource _cts;
        private CancellationToken _ct;
        private int? _prevBin;
        private List<double> _prevBinFactors = new List<double>();
        private int _binCounter;
        private DateTime _normalizeStarTime;
        private double _normalizePosition;

        #endregion

        public PecVM()
        {
            try
            {
                using (new WaitCursor())
                {
                    var monitorItem = new MonitorEntry
                    {
                        Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server,
                        Category = MonitorCategory.Interface, Type = MonitorType.Information,
                        Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId,
                        Message = " Loading PecVM"
                    };
                    MonitorLog.LogToMonitor(monitorItem);

                    if (_skyTelescopeVM == null) _skyTelescopeVM = SkyTelescopeVM._skyTelescopeVM;
                    // setup property events to monitor
                    SkyServer.StaticPropertyChanged += PropertyChangedSkyServer;
                    SkySettings.StaticPropertyChanged += PropertyChangedSkySettings;

                    Range20 = new List<int>(Enumerable.Range(1, 20));
                    IsTrainingRunning = false;
                    IsMountRunning = SkyServer.IsMountRunning;

                    // settings
                    Cycles = 5;
                    ApplyMode = PecMode.PecWorm;
                    Debug = true;
                    AutoApply = false;
                    MergeType = PecMergeType.Replace;

                    LoadPlotDefaults();
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Interface,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message},{ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        #region View Model Items

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
                         IsMountRunning = SkyServer.IsMountRunning;
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
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
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
                     case "PecWormFile":
                         PecWormFileName = SkySettings.PecWormFile;
                         break;
                     case "Pec360File":
                         Pec360FileName = SkySettings.Pec360File;
                         break;
                     case "PecMode":
                         PecMode = SkySettings.PecMode;
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
                OpenDialog(ex.Message, $"{Application.Current.Resources["exError"]}");
            }
        }

        #endregion

        #region Pec

        private string _trainingBadge;
        public string TrainingBadge
        {
            get => _trainingBadge;
            set
            {
                if (_trainingBadge == value) return;
                _trainingBadge = value;
                OnPropertyChanged();
            }
        }

        public string PecWormFileName
        {
            get => SkySettings.PecWormFile;
            set
            {
                SkySettings.PecWormFile = value;
                OnPropertyChanged();
            }
        }

        public string Pec360FileName
        {
            get => SkySettings.Pec360File;
            set
            {
                SkySettings.Pec360File = value;
                OnPropertyChanged();
            }
        }

        private bool _debug;
        public bool Debug
        {
            get => _debug;
            set
            {
                if (_debug == value) return;
                _debug = value;
                OnPropertyChanged();
            }
        }

        private bool _isMountRunning;
        public bool IsMountRunning
        {
            get => _isMountRunning;
            set
            {
                if (_isMountRunning == value) return;
                _isMountRunning = value;
                OnPropertyChanged();
            }
        }

        private bool _isTrainingRunning;
        public bool IsTrainingRunning
        {
            get => _isTrainingRunning;
            set
            {
                _isTrainingRunning = value;
                OnPropertyChanged();
            }
        }

        private PecMergeType _mergeType;
        public PecMergeType MergeType
        {
            get => _mergeType;
            set
            {
                if (_mergeType == value) return;
                _mergeType = value;
                OnPropertyChanged();
            }
        }

        private bool _autoApply;
        public bool AutoApply
        {
            get => _autoApply;
            set
            {
                if (_autoApply == value) return;
                _autoApply = value;
                OnPropertyChanged();
            }
        }

        public PecMode PecMode
        {
            get => SkySettings.PecMode;
            set
            {
                SkySettings.PecMode = value;
                OnPropertyChanged();
            }
        }

        private PecMode _applyMode;
        public PecMode ApplyMode
        {
            get => _applyMode;
            set
            {
                if (_applyMode == value){return;}
                _applyMode = value;
                OnPropertyChanged();
            }
        }

        private bool _apply;
        public bool Apply
        {
            get => _apply;
            set
            {
                if (_apply == value) return;
                _apply = value;
                OnPropertyChanged();
            }
        }

        private bool _progressBarEnabled;
        public bool ProgressBarEnabled
        {
            get => _progressBarEnabled;
            set
            {
                if (_progressBarEnabled == value) return;
                _progressBarEnabled = value;
                OnPropertyChanged();
            }
        }

        private double _progressBarValue;
        public double ProgressBarValue
        {
            get => _progressBarValue;
            set
            {
                if (Math.Abs(_progressBarValue - value) < 0.0) return;
                _progressBarValue = value;
                OnPropertyChanged();
            }
        }

        private TimeSpan _indexTimeSpan;
        public TimeSpan IndexTimeSpan
        {
            get => _indexTimeSpan;
            set{
                _indexTimeSpan = value;
                OnPropertyChanged();
            }
        }

        public IList<int> Range20 { get; }

        private int _cycles;
        public int Cycles
        {
            get => _cycles;
            set
            {
                if (_cycles == value) return;
                _cycles = value;
                OnPropertyChanged();
            }
        }

        private ICommand _clickPecImportCmd;
        public ICommand ClickPecImportCmd
        {
            get
            {
                var cmd = _clickPecImportCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return _clickPecImportCmd = new RelayCommand(ClickPecImport);

            }
        }
        private void ClickPecImport(object parameter)
        {
            try
            {
                using (new WaitCursor())
                {
                    var key = parameter.ToString().Trim();
                    var filename = string.Empty;
                    switch (key)
                    {
                        case "Worm":
                            filename = GetFileName(PecFileType.GSPecWorm.ToString(), _logPath);
                            break;
                        case "360":
                            filename = GetFileName(PecFileType.GSPec360.ToString(), _logPath);
                            break;
                    }
                    if (filename == null) return;
                    if (!File.Exists(filename))
                    {
                        OpenDialog("Invalid File Name");
                        return;
                    }
                    SkyServer.LoadPecFile(filename);
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

        private ICommand _clickStartPecTrainingCmd;
        public ICommand ClickStartPecTrainingCmd
        {
            get
            {
                var cmd = _clickStartPecTrainingCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return (_clickStartPecTrainingCmd = new RelayCommand(
                    param => ClickStartPecTraining()
                ));
            }
        }
        private void ClickStartPecTraining()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (IsTrainingRunning)
                    {
                        StopTrainingTimer();
                        ProgressBarValue = 0;
                        PecTrainDef = null;
                        PrevCapture = null;
                        IndexTimeSpan = new TimeSpan(0);
                    }
                    else
                    {
                        StartTrainingTimer();
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

        private ICommand _clickPecApplyCmd;
        public ICommand ClickPecApplyCmd
        {
            get
            {
                var cmd = _clickPecApplyCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return (_clickPecApplyCmd = new RelayCommand(
                    param => ClickPecApply()
                ));
            }
        }
        private void ClickPecApply()
        {
            try
            {
                using (new WaitCursor())
                {
                    if (PecTrainDef == null){return;}

                    switch (ApplyMode)
                    {
                        case PecMode.PecWorm:
                            ApplyWorm();
                            Apply = false;
                            break;
                        case PecMode.Pec360:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
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

        private void ApplyWorm()
        {
            //cleanup bins
            PecTrainDef.Bins = SkyServer.CleanUpBins(PecTrainDef.Bins);
            if (PecTrainDef.Bins == null) { return; }

            //merge cycles
            var bins = MergeBins(PecTrainDef.Bins, PecTrainDef.BinCount, PecTrainDef.Cycles);
            if (bins == null) { return; }

            //create new local master list
            var mBins = SkyServer.MakeWormMaster(bins);

            //merge or replace the server pec master list
            SkyServer.UpdateWormMaster(mBins, MergeType);

            //output file
            SavePecFile(PecMode.PecWorm);
        }

        private static void Apply360()
        {

        }

        /// <summary>
        /// Merge bin cycles into 100 bins
        /// </summary>
        /// <returns></returns>
        private static List<PecBinData> MergeBins(IReadOnlyList<PecBinData> bins, int binCount, int cycles)
        {
            var validBins = new List<PecBinData>();

            for (var i = 0; i <= binCount - 1; i++)
            {
                var factors = new List<double>();
                var b = bins[i];
                if (b == null){continue;}

                factors.Add(b.BinFactor);
                for (var j = 1; j < cycles; j++)
                {
                    var bn = i + j * binCount;
                    if (bins.ElementAtOrDefault(bn) == null) continue;
                    var b2 = bins[bn];
                    factors.Add(b2.BinFactor);
                }
                var bd = new PecBinData { BinFactor = factors.Average(), BinNumber = b.BinNumber, BinUpdates = b.BinUpdates + 1};
                validBins.Add(bd);
            }

            return validBins;
        }

        private void SavePecFile(PecMode pecMode)
        {
            
            PecFileType fileType;
            string filePath;
            switch (pecMode)
            {
                case PecMode.PecWorm:
                    if (SkyServer.PecWormMaster == null) { return; }
                    fileType = PecFileType.GSPecWorm;
                    filePath = GetNextFileName(fileType + "_");
                    PecWormFileName = filePath;
                    break;
                case PecMode.Pec360:
                    if (SkyServer.Pec360Master == null) { return; }
                    fileType = PecFileType.GSPec360;
                    filePath = GetNextFileName(fileType + "_");
                    Pec360FileName = filePath;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            PecTrainDef.FileName = filePath;

            var message = $"#FileType={fileType}" + Environment.NewLine;
            message += $"#StartTime={PecTrainDef.StartTime:yyyy:MM:dd:HH:mm:ss.fff}" + Environment.NewLine;
            message += $"#StartPosition={PecTrainDef.StartPosition}" + Environment.NewLine;
            message += $"#EndTime={PecTrainDef.EndTime:yyyy:MM:dd:HH:mm:ss.fff}" + Environment.NewLine;
            message += $"#EndPosition={PecTrainDef.EndPosition}" + Environment.NewLine;
            message += $"#Index={PecTrainDef.Index}" + Environment.NewLine;
            message += $"#Cycles={PecTrainDef.Cycles}" + Environment.NewLine;
            message += $"#WormPeriod={PecTrainDef.WormPeriod}" + Environment.NewLine;
            message += $"#WormTeeth={PecTrainDef.WormTeeth}" + Environment.NewLine;
            message += $"#WormSteps={PecTrainDef.WormSteps}" + Environment.NewLine;
            message += $"#TrackingRate={PecTrainDef.TrackingRate}" + Environment.NewLine;
            message += $"#PositionOffset={PecTrainDef.PositionOffset}" + Environment.NewLine;
            message += $"#Ra={PecTrainDef.Ra}" + Environment.NewLine;
            message += $"#Dec={PecTrainDef.Dec}" + Environment.NewLine;
            message += $"#BinCount={PecTrainDef.BinCount}" + Environment.NewLine;
            message += $"#BinSteps={PecTrainDef.BinSteps}" + Environment.NewLine;
            message += $"#BinTime={PecTrainDef.BinTime}" + Environment.NewLine;
            message += $"#StepsPerSec={PecTrainDef.StepsPerSec}" + Environment.NewLine;
            message += $"#StepsPerRev={PecTrainDef.StepsPerRev}" + Environment.NewLine;
            message += $"#InvertCapture={PecTrainDef.InvertCapture}" + Environment.NewLine;
            message += $"#FileName={PecTrainDef.FileName}" + Environment.NewLine;
            message += $"#BinNumber{_tab}Factor{_tab}Updates" + Environment.NewLine;



            switch (pecMode)
            {
                case PecMode.PecWorm:
                    foreach (var pecBins in SkyServer.PecWormMaster)
                    {
                        message += $"{pecBins.Key}{_tab}{pecBins.Value.Item1}{_tab}{pecBins.Value.Item2}" + Environment.NewLine;
                    }
                    break;
                case PecMode.Pec360:
                    foreach (var pecBins in SkyServer.Pec360Master)
                    {
                        message += $"{pecBins.Key}{_tab}{pecBins.Value.Item1}{_tab}{pecBins.Value.Item2}" + Environment.NewLine;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            FileWriteAsync(filePath, message);
        }
        
        private static string GetFileName(string name, string dir)
        {
            var openFileDialog = new OpenFileDialog
            {
                FileName = $"{name}_*",
                InitialDirectory = dir,
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Multiselect = false,
            };
            return openFileDialog.ShowDialog() != true ? null : openFileDialog.FileName;
        }

        /// <summary>
        /// Creates training definition and event to capture the data 
        /// </summary>
        private void StartTrainingTimer()
        {
            try
            {
                if (IsTrainingRunning) return;
                if (!SkyServer.IsMountRunning){throw new Exception("Mount not running");}
                if(!SkyServer.Tracking) { throw new Exception("Tracking must be on and the mount guiding"); }

                // turn off all pec
                SkyServer.PPecOn = false;
                SkyServer.PecOn = false;
                
                StopTrainingTimer();
                TrainingBadge = "On";
                ProgressBarValue = 0;
                ProgressBarEnabled = true;
                Apply = false;

                PecTrainDef = null;
                PrevCapture = null;

                PecTrainDef = new PecTrainingDefinition
                {
                    Index = (int)Math.Ceiling(_siderealDaySeconds / SkyServer.WormTeethCount[0]) * Cycles,
                    Cycles = Cycles,
                    //StartTime = HiResDateTime.UtcNow,
                    //StartPosition = GetSteps(0),
                    PositionOffset = 0,
                    Ra = SkyServer.RightAscension,
                    Dec = SkyServer.Declination,
                    TrackingRate = SkyServer.CurrentTrackingRate() * 3600,
                    BinCount = SkyServer.PecBinCount,
                    BinSteps = SkyServer.PecBinSteps,
                    BinTime = _siderealDaySeconds / SkyServer.WormTeethCount[0] / SkyServer.PecBinCount,
                    WormPeriod = _siderealDaySeconds / SkyServer.WormTeethCount[0],
                    WormTeeth = SkyServer.WormTeethCount[0],
                    WormSteps = SkyServer.StepsPerRevolution[0] / (SkyServer.WormTeethCount[0] * 1.0),
                    StepsPerRev = SkyServer.StepsPerRevolution[0],
                    StepsPerSec = SkyServer.StepsPerRevolution[0] / 360.0 / 3600,
                    InvertCapture = false,
                    Log = new List<PecLogData>(),
                    Bins = new List<PecBinData>()
                };

                _binCounter = PecTrainDef.BinCount* PecTrainDef.Cycles;

                if (_timer == null)
                {
                    _timer = new Timer(1000) {Enabled = true};
                    _timer.Elapsed += TrainingPecEvent;
                }
                
                IsTrainingRunning = true;

            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                    { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                ThreadContext.BeginInvokeOnUiThread(delegate { OpenDialog(ex.Message); });
            }
        }
        
        /// <summary>
        /// Stop the training event
        /// </summary>
        private void StopTrainingTimer()
        {
            if (_timer != null) { _timer.Elapsed -= TrainingPecEvent; }
            _timer?.Stop();
            _timer = null;

            TrainingBadge = "";
            ProgressBarEnabled = false;
            IsTrainingRunning = false;
        }
        
        /// <summary>
        /// Gets the step count and timestamp
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        private static Tuple<double, DateTime> GetSteps(int axis)
        {
            var count = 0;
            while (true)
            {
                var (item1, item2) = SkyServer.GetRawStepsDt(axis);
                if (item1.HasValue)
                {
                    return new Tuple<double, DateTime>(Convert.ToDouble(item1), item2);
                }
                count++;
                if (count < 10) continue;
                return new Tuple<double, DateTime>(0, HiResDateTime.UtcNow);
            }
        }
        
        /// <summary>
        /// Get the next file name based on sequence numbers
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string GetNextFileName(string name)
        {
            for (var i = 1; i < 10000; i++)
            {
                var file = Path.Combine(_logPath, $"GSServer\\{name}{i}.txt");
                if (!File.Exists(file))
                    return file;
            }
            return Path.Combine(_logPath, $"GSServer\\{name}{DateTime.Now:yyyyMMddHHmmssfff}.txt");
        }
        
        /// <summary>
        /// Saves the debug file
        /// </summary>
        private void SaveDebugData()
        {
            if (!Debug) return;
            if (PecTrainDef == null) return;
            var filePath = GetNextFileName(PecFileType.GSPecDebug + "_");

            var message = $"#FileType={PecFileType.GSPecDebug}" + Environment.NewLine;
            message += $"#StartTime={PecTrainDef.StartTime:yyyy:MM:dd:HH:mm:ss.fff}" + Environment.NewLine;
            message += $"#StartPosition={PecTrainDef.StartPosition}" + Environment.NewLine;
            message += $"#EndTime={PecTrainDef.EndTime:yyyy:MM:dd:HH:mm:ss.fff}" + Environment.NewLine;
            message += $"#EndPosition={PecTrainDef.EndPosition}" + Environment.NewLine;
            message += $"#Index={PecTrainDef.Index}" + Environment.NewLine;
            message += $"#Cycles={PecTrainDef.Cycles}" + Environment.NewLine;
            message += $"#WormPeriod={PecTrainDef.WormPeriod}" + Environment.NewLine;
            message += $"#WormTeeth={PecTrainDef.WormTeeth}" + Environment.NewLine;
            message += $"#WormSteps={PecTrainDef.WormSteps}" + Environment.NewLine;
            message += $"#TrackingRate={PecTrainDef.TrackingRate}" + Environment.NewLine;
            message += $"#PositionOffset={PecTrainDef.PositionOffset}" + Environment.NewLine;
            message += $"#Ra={PecTrainDef.Ra}" + Environment.NewLine;
            message += $"#Dec={PecTrainDef.Dec}" + Environment.NewLine;
            message += $"#BinCount={PecTrainDef.BinCount}" + Environment.NewLine;
            message += $"#BinSteps={PecTrainDef.BinSteps}" + Environment.NewLine;
            message += $"#BinTime={PecTrainDef.BinTime}" + Environment.NewLine;
            message += $"#StepsPerSec={PecTrainDef.StepsPerSec}" + Environment.NewLine;
            message += $"#StepsPerRev={PecTrainDef.StepsPerRev}" + Environment.NewLine;
            message += $"#InvertCapture={PecTrainDef.InvertCapture}" + Environment.NewLine;
            message += $"#FileName={filePath}" + Environment.NewLine;
            message += $"#Index{_tab}TimeStamp{_tab}Position{_tab}DeltaSteps{_tab}DeltaTime{_tab}RateEstimate{_tab}Normalized{_tab}BinNumber{_tab}BinFactor{_tab}BinEstimate{_tab}Status" + Environment.NewLine;

            foreach (var capData in PecTrainDef.Log)
            {
                message += $"{capData.Index}{_tab}{capData.TimeStamp:yyyy:MM:dd:HH:mm:ss.fff}{_tab}{capData.Position}{_tab}{capData.DeltaSteps}{_tab}{capData.DeltaTime.TotalSeconds}{_tab}{capData.RateEstimate}{_tab}{capData.Normalized}{_tab}{capData.BinNumber}{_tab}{capData.BinFactor}{_tab}{capData.BinEstimate}{_tab}{capData.Status}" + Environment.NewLine;
            }
            FileWriteAsync(filePath, message);
        }

        /// <summary>
        /// Assigns a status for a factor number
        /// </summary>
        /// <param name="factor"></param>
        /// <returns></returns>
        private static PecStatus GetStatus(double factor)
        {
            var x = Math.Abs(factor - 1);
            if (x > 0 && x <= .01)
            {
                return PecStatus.Good;
            }

            if (x > .01 && x <= .02)
            {
                return PecStatus.Ok;
            }

            if (x > .02 && x <= .03)
            {
                return PecStatus.Warning;
            }

            if (x > .03 && x <= .04)
            {
                return PecStatus.NotSoGood;
            }

            if (x > .04)
            {
                return PecStatus.Bad;
            }

            return PecStatus.Bad;
        }

        private double ConvertBadFactor(PecStatus status, double factor)
        {
            switch (status)
            {
                case PecStatus.Good:
                    break;
                case PecStatus.Ok:
                    break;
                case PecStatus.Warning:
                    break;
                case PecStatus.NotSoGood:
                    break;
                case PecStatus.Bad:
                    var x = Math.Abs(factor - 1);
                    if (x > .04){ x = .04;}
                    return x + 1;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
            return factor;
        }

        /// <summary>
        /// Event to capture training data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrainingPecEvent(object sender, EventArgs e)
        {
            var hasLock = false;
            try
            {
                // Stops the overrun of previous event not ended before next one starts
                Monitor.TryEnter(_timerLock, ref hasLock);
                if (!hasLock) { return; }
                if (PecTrainDef == null) { return; }
                if (SkyServer.Tracking == false || SkyServer.IsSlewing  || SkySettings.TrackingRate != DriveRates.driveSidereal) throw new Exception("Movement Error");
                IsTrainingRunning = true;

                if (PecTrainDef.Log.Count <= 0) // new capture
                {
                    var newCapture = new PecLogData
                    {
                        Index = 0,
                        TimeStamp = HiResDateTime.UtcNow,
                        Position = Range.RangeDouble(GetSteps(0).Item1, PecTrainDef.StepsPerRev) + PecTrainDef.PositionOffset,
                        DeltaSteps = 0,
                        DeltaTime = new TimeSpan(0),
                        Normalized = 0,
                        RateEstimate = 0,
                        BinNumber = 0,
                        BinFactor = 0,
                        BinEstimate = 0
                    };
                    // store start in definition and for plot
                    PecTrainDef.StartTime = newCapture.TimeStamp;
                    _normalizeStarTime = newCapture.TimeStamp;
                    PecTrainDef.StartPosition = newCapture.Position;
                    _normalizePosition = newCapture.Position;

                    PecTrainDef.Log.Add(newCapture);
                    PrevCapture = newCapture;

                    _prevBin = null;
                    _prevBinFactors = null;
                    return;
                }

                if (PrevCapture == null) { throw new Exception("Missing Start Data"); }

                //get mount data
                var (item1, item2) = GetSteps(0);
                var newLogEntry = new PecLogData // new data capture
                {
                    Index = PecTrainDef.Log.Count,
                    Position = Range.RangeDouble(item1, PecTrainDef.StepsPerRev) + PecTrainDef.PositionOffset,
                    TimeStamp = item2
                };

                //calculations
                newLogEntry.DeltaSteps = Math.Abs(newLogEntry.Position - PrevCapture.Position);
                newLogEntry.DeltaTime = newLogEntry.TimeStamp - PrevCapture.TimeStamp;
                newLogEntry.RateEstimate = (newLogEntry.DeltaSteps / newLogEntry.DeltaTime.TotalSeconds) * (_secondsOfArc / SkyServer.StepsPerRevolution[0]);
                newLogEntry.Normalized = Math.Abs((_normalizeStarTime - newLogEntry.TimeStamp).TotalSeconds * PecTrainDef.TrackingRate) - Math.Abs((_normalizePosition - newLogEntry.Position) / PecTrainDef.StepsPerSec);
                newLogEntry.BinEstimate = newLogEntry.DeltaTime.TotalMilliseconds / newLogEntry.DeltaSteps *  PecTrainDef.BinSteps / 1000;
                newLogEntry.BinFactor = newLogEntry.BinEstimate / PecTrainDef.BinTime;
                newLogEntry.BinNumber =  (newLogEntry.Position + PecTrainDef.PositionOffset) / PecTrainDef.BinSteps;

                //Create bin data
                var newbin = (int) newLogEntry.BinNumber;
                if (_prevBinFactors == null) { _prevBinFactors = new List<double>(); }
                if ( _prevBin != newbin && _prevBin != null)
                {
                    // if no factors are found use 1 for the bin factor
                    var binFactor = _prevBinFactors.Count == 0 ? 1 : _prevBinFactors.Average();
                    var binData = new PecBinData {BinNumber = (int)_prevBin, BinFactor = binFactor, BinUpdates = _prevBinFactors.Count };
                    // bin factor over limit check
                    var b = GetStatus(binFactor);
                    if (b == PecStatus.Bad)
                    {
                        var x = ConvertBadFactor(b, binFactor);
                        binData.BinFactor = x;
                    }
                    PecTrainDef.Bins.Add(binData);
                    _binCounter -= 1;
                    _prevBinFactors.Clear();
                }

                // Status check and set bad entries to max limit
                newLogEntry.Status = GetStatus(newLogEntry.BinFactor);
                //if (newLogEntry.Status != PecStatus.Bad){_prevBinFactors?.Add(newLogEntry.BinFactor);}
                if (newLogEntry.Status == PecStatus.Bad)
                {
                    var x = ConvertBadFactor(newLogEntry.Status, newLogEntry.BinFactor);
                    _prevBinFactors?.Add(x);
                }
                else{_prevBinFactors?.Add(newLogEntry.BinFactor);}
                
                // Add to log data
                PrevCapture = newLogEntry;
                PecTrainDef.Log.Add(newLogEntry);

                // Add to plot and update UI chart
                var binNum = newLogEntry.BinNumber;
                if (SkyServer.SouthernHemisphere){binNum  = -binNum;}
                var point = new ObservablePoint {X = binNum, Y = newLogEntry.Normalized};

                //setup plot
                var startPlot = _prevBin == null;
                if (_prevBin != null){if (Math.Abs(binNum - (double) _prevBin) > 1000){startPlot = true;}}
                if (startPlot)
                {
                    _normalizePosition = newLogEntry.Position;
                    _normalizeStarTime = newLogEntry.TimeStamp;
                    var startp = (int)((newLogEntry.Position + PecTrainDef.PositionOffset) / PecTrainDef.BinSteps);
                    var indexCount = PecTrainDef.BinCount * PecTrainDef.Cycles;
                    if (SkyServer.SouthernHemisphere) indexCount = -indexCount;
                    ThreadContext.InvokeOnUiThread(delegate
                    {
                        if (startPlot) { NewPlot(startp, startp + indexCount); }
                    }, _ct);
                }
                else
                {
                     // UI plot
                    ThreadContext.InvokeOnUiThread(delegate
                    {
                        Values1.Add(point);
                    }, _ct); 
                }
                
                // progress bar and index counter
                var max = (PecTrainDef.BinCount * PecTrainDef.Cycles * 1.0);
                ProgressBarValue = ((max - _binCounter) / max) * 100.0 ;
                var index = PecTrainDef.Index - PecTrainDef.Log.Count;
                IndexTimeSpan = TimeSpan.FromSeconds(index);

                _prevBin = newbin;

                if (_binCounter >= 0) return;
                PecTrainDef.EndTime = PrevCapture.TimeStamp;
                PecTrainDef.EndPosition = PrevCapture.Position;
                StopTrainingTimer();
                SaveDebugData();
                Apply = true;
                IndexTimeSpan = new TimeSpan(0);

                if (!AutoApply) return;
                Apply = false;
                switch (ApplyMode)
                {
                    case PecMode.PecWorm:
                        ApplyWorm();
                        SkyServer.PecOn = true;
                        break;
                    case PecMode.Pec360:
                        Apply360();
                        SkyServer.PecOn = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception ex)
            {
                StopTrainingTimer();
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Interface, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $" {ex.Message}" };
                MonitorLog.LogToMonitor(monitorItem);

                ThreadContext.BeginInvokeOnUiThread(delegate { OpenDialog(ex.Message); });
            }
            finally
            {
                if (hasLock) { Monitor.Exit(_timerLock); }
            }
        }

        /// <summary>
        /// Send entries to a file async
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="message"></param>
        /// <param name="append"></param>
        private static async void FileWriteAsync(string filePath, string message, bool append = true)
        {
            try
            {
                await _lockFile.WaitAsync();

                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException());
                using (var stream = new FileStream(filePath, append ? FileMode.Append : FileMode.Create,
                    FileAccess.Write, FileShare.None, 4096, true))
                using (var sw = new StreamWriter(stream))
                {
                    await sw.WriteLineAsync(message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //throw;
            }
            finally
            {
                _lockFile.Release();
            }
        }

        #endregion

        #region Plot

        private SeriesCollection _valuesCollection;
        public SeriesCollection ValuesCollection
        {
            get => _valuesCollection;
            private set
            {
                if (_valuesCollection == value) return;
                _valuesCollection = value;
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

        private GearedValues<ObservablePoint> Values1 { get; set; }

        private void NewPlot(int startX, int endX)
        {
            ClearValues();
            ResizeAxes();
            ChartsQuality(Quality.Highest);
            _cts = new CancellationTokenSource();
            _ct = _cts.Token;
            SetXAxisLimits(startX, endX);
            ValuesCollection = new SeriesCollection();
            AddSeries();
        }

        private void ResizeAxes()
        {
            AxisYMax -= double.NaN;
            AxisYMin += double.NaN;
        }

        private void ClearValues()
        {
            Values1?.Clear();
            _cts?.Cancel();
            _cts = null;
        }

        private void AddSeries()
        {
            var col = new GSColors();
            var series = new GLineSeries
            {
                Fill = col.ToBrush(Color.Transparent),
                LineSmoothness = 0,
                MinWidth = 1,
                Stroke = col.ToBrush(Color.IndianRed),
                StrokeThickness = 1,
                PointForeground = col.ToBrush(Color.IndianRed),
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = 1,
                ScalesYAt = 0,
                Title = "Bins",
                Values = Values1
            };
            ValuesCollection.Add(series);
        }

        private void SetXAxisLimits(int start, int end)
        {
            if (end < start)
            {
                AxisXMin = -start;
                AxisXMax = -end;
                return;
            }
            AxisXMin = start;
            AxisXMax = end;
        }

        private void LoadPlotDefaults()
        {
            if (ValuesCollection == null) ValuesCollection = new SeriesCollection();
            if (Values1 == null) Values1 = new GearedValues<ObservablePoint>();

            FormatterY = value => value.ToString("N1");
            FormatterX = x => Math.Abs(x).ToString("N0");

            AxisYMax = 3;
            AxisYMin = -3;
            SetXAxisLimits(0, 1);
        }

        private void ChartsQuality(Quality chartQuality)
        {
            Values1.WithQuality(chartQuality);
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

                return (_openDialogCommand = new RelayCommand(
                    param => OpenDialog(null)
                ));
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

                return (_clickOkDialogCommand = new RelayCommand(
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
                var command = _clickCancelDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return (_clickCancelDialogCommand = new RelayCommand(
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
                var dialog = _runMessageDialog;
                if (dialog != null)
                {
                    return dialog;
                }

                return (_runMessageDialog = new RelayCommand(
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

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            // GC.SuppressFinalize(this);
        }
        // NOTE: Leave out the finalizer altogether if this class doesn't
        // own unmanaged resources itself, but leave the other methods
        // exactly as they are.
        ~PecVM()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        // The bulk of the clean-up code is implemented in Dispose(bool)
        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            _skyTelescopeVM?.Dispose();
            _timer?.Dispose();
            _cts?.Dispose();
            _lockFile?.Dispose();
        }
        #endregion
    }

    public class PecTrainingDefinition
    {
        public PecFileType FileType { get; set; }
        public int Index { get; set; }
        public int Cycles { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double StartPosition { get; set; }
        public double EndPosition { get; set; }
        public double PositionOffset { get; set; }
        public double Ra { get; set; }
        public double Dec { get; set; }
        public double TrackingRate { get; set; }
        public int BinCount { get; set; }
        public double BinSteps { get; set; }
        public double BinTime { get; set; }
        public double WormPeriod { get; set; }
        public int WormTeeth { get; set; }
        public double WormSteps { get; set; }
        public double StepsPerSec { get; set; }
        public double StepsPerRev { get; set; }
        public string FileName { get; set; }
        public bool InvertCapture { get; set; }
        public List<PecLogData> Log { get; set; }
        public List<PecBinData> Bins { get; set; }
    }
    public class PecLogData
    { 
        public int Index { get; set; }
        public DateTime TimeStamp { get; set; }
        public double Position { get; set; }
        public double DeltaSteps { get; set; }
        public TimeSpan DeltaTime { get; set; }
        public double Normalized { get; set; }
        public double RateEstimate { get; set; }
        public double BinNumber { get; set; }
        public double BinEstimate { get; set; }
        public double BinFactor { get; set; }
        public PecStatus Status { get; set; }
    }
    public class PecBinData
    {
        public int BinNumber { get; set; }
        public double BinFactor { get; set; }
        public int BinUpdates { get; set; }
    }
    public enum PecStatus
    {
        Good = 0,
        Ok = 1,
        Warning = 2,
        NotSoGood = 3,
        Bad = 4
    }
    public enum PecMergeType
    {
        Replace = 0,
        Merge = 1,
    }
}
