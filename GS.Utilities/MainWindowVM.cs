using ASCOM.DriverAccess;
using GS.Shared;
using GS.Utilities.Dialogs;
using GS.Utilities.Helpers;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using Timer = System.Timers.Timer;

namespace GS.Utilities
{
    internal class MainWindowVM : ObservableObject, IDisposable
    {
        private static readonly string _docDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static readonly string _logDir = Path.Combine(_docDir, "GSServer");

        private static readonly string _commonDir =
            Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
        private static readonly string _gsDir = "\\ASCOM\\Telescope\\GSServer\\";
        private static string _gsFilePath = _commonDir + _gsDir + "GS.Server.exe";
        // private static string _gsTelescopeFilePath = _commonDir + _gsDir + "Drivers\\ASCOM.GS.Sky.Telescope.dll";

        //serial test
        private static Timer aTimer;
        private static readonly object _timerLock = new object();
        private SerialPort serial;
        private const char _endChar = (char)13;
        private const string j1 = ":j1";
        private const string j2 = ":j2";
        private int counter;
        private bool axis;
        private const string zoom = "Zoom Zoom...";
        private Stopwatch _startTime;

        public MainWindowVM()
        {
            try
            {
                using (new WaitCursor())
                {
                    Languages.SetLanguageDictionary(false, LanguageApp.GSUtilities);

                    Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    LoadDefaults();
                }
            }
            catch (Exception ex)
            {
                OpenDialog(ex.Message);
            }
        }

        #region Settings

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

        #endregion

        #region Methods

        private void Exit()
        {
            using (new WaitCursor())
            {
                if (Application.Current.MainWindow != null) Application.Current.MainWindow.Close();
            }
        }

        private void LoadDefaults()
        {
            ScreenEnabled = true;

            //Speed Test
            BaudRates = new List<int>() { 9600, 19200, 38400, 57600, 115200 };
            ComPortList = new List<int>(Enumerable.Range(0, 20));
            IntervalList = new List<double>(InclusiveRange(100, 500, 50));
            Interval = 300.0;
            ComPort = 1;
            BaudRate = 9600;
        }

        private static bool IsGSAppOpen(string name)
        {
            return Process.GetProcesses().Any(clsProcess => clsProcess.ProcessName.Contains(name));
        }



        #endregion

        #region Commands

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
            Exit();
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
            Properties.Utilities.Default.WindowState = WindowState.Minimized;
        }

        #endregion

        #region Connect

        private string _connect;
        public string Connect
        {
            get => _connect;
            set
            {
                if (_connect == value) return;
                _connect = value;
                OnPropertyChanged();
            }
        }

        private ICommand _clickConnectCmd;
        public ICommand ClickConnectCmd
        {
            get
            {
                return _clickConnectCmd ?? (_clickConnectCmd = new RelayCommand(
                           param => ConnectCheck()
                       ));
            }
        }
        private void ConnectCheck()
        {
            try
            {
                Connect = null;
                var msg = "Not found";
                var util = new ASCOM.Utilities.Chooser();
                var progID = util.Choose("ASCOM.GS.Sky.Telescope");
                if (progID != null)
                {
                    var t = new Telescope(progID) { Connected = true };
                    msg = $"v{t.DriverVersion}, {t.DriverInfo}";
                    t.Connected = false;
                    t.Dispose();
                }
                Connect = msg;
                util.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                OpenDialog(ex.Message);
            }

        }

        #endregion

        #region Serial Test

        public IList<int> ComPortList { get; set; }

        private int _comPort;
        public int ComPort
        {
            get => _comPort;
            set
            {
                if (_comPort == value) { return; }
                _comPort = value;
                OnPropertyChanged();
            }
        }

        public IList<int> BaudRates { get; set; }

        private int _baudRate;
        public int BaudRate
        {
            get => _baudRate;
            set
            {
                if (_baudRate == value) { return; }
                _baudRate = value;
                OnPropertyChanged();
            }
        }

        public IList<double> IntervalList { get; set; }
        private double _interval;
        public double Interval
        {
            get => _interval;
            set
            {
                _interval = value;
                ZoomCounter = 0;
                OnPropertyChanged();
            }
        }

        private string _serMsg;
        public string SerMsg
        {
            get => _serMsg;
            set
            {
                if (_serMsg == value) { return; }
                _serMsg = value.Replace("\r", "");
                OnPropertyChanged();
            }
        }

        private int _zoomCounter;
        public int ZoomCounter
        {
            get => _zoomCounter;
            set
            {
                if (_zoomCounter == value) { return; }
                _zoomCounter = value;
                OnPropertyChanged();
            }
        }

        private void Stop()
        {
            _startTime?.Stop();
            aTimer?.Stop();
            if (aTimer != null) aTimer.Elapsed -= OnTimedEvent;
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalMilliseconds < 100)
            {

            }
            serial?.Close();
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            var hasLock = false;
            try
            {
                Monitor.TryEnter(_timerLock, ref hasLock);
                if (!hasLock) return;
                if (!serial.IsOpen) return;
                var commandStr = new StringBuilder(20);
                commandStr.Append(axis ? j1 : j2);
                axis = !axis;
                commandStr.Append(_endChar);
                serial.Write(commandStr.ToString());
                counter++;

                // var receivedData = IncomingData;
                var receivedData = ReceiveResponse();
                receivedData = receivedData?.Trim();
                receivedData = receivedData?.Replace("\0", string.Empty);
                if (string.IsNullOrEmpty(receivedData))
                {
                    Stop();
                    throw new Exception($"{Application.Current.Resources["tbUtilTimeout"]} {counter}");
                }

                var zoomtxt = string.Empty;
                if (Interval < 11.0 && ZoomCounter < 30)
                {
                    ZoomCounter++;
                    if (ZoomCounter < 12) zoomtxt = zoom.Substring(0, ZoomCounter);
                }

                InvokeOnUiThread(
                    delegate
                    {
                        SerMsg =
                            $"{Application.Current.Resources["tbUtilCounter"]} {counter} {Application.Current.Resources["tbUtilTimer"]} {_startTime.Elapsed:hh\\:mm\\:ss\\.fff}, {commandStr} {receivedData} {zoomtxt}";
                    });
                aTimer.Interval = Interval;

            }
            catch (Exception ex)
            {
                Stop();
                InvokeOnUiThread(
                    delegate
                    {
                        OpenDialog(ex.Message);
                    });
            }
            finally
            {
                if (hasLock) Monitor.Exit(_timerLock);
            }
        }

        private static void InvokeOnUiThread(Action action, CancellationToken token = default(CancellationToken))
        {
            if (Application.Current == null) return;
            if (Application.Current.Dispatcher != null && Application.Current.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                if (token.IsCancellationRequested) return;
                if (Application.Current.Dispatcher != null) Application.Current.Dispatcher.Invoke(action);
            }
        }

        private string ReceiveResponse()
        {
            // format "::e1\r=020883\r"
            var mBuffer = new StringBuilder(15);
            var StartReading = false;

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalMilliseconds < serial.ReadTimeout)
            {
                var data = serial.ReadExisting();
                foreach (var byt in data)
                {
                    // this code order is important
                    if (byt == '=' || byt == '!' || byt == _endChar) StartReading = true;
                    if (StartReading) mBuffer.Append(byt);
                    if (byt != _endChar) continue;
                    if (!StartReading) continue;
                    return mBuffer.ToString();
                }
                Thread.Sleep(1);
            }
            return null;
        }

        private static IEnumerable<double> InclusiveRange(double start, double end, double step = .1, int round = 1)
        {
            while (start <= end)
            {
                yield return start;
                start += step;
                start = Math.Round(start, round);
            }
        }

        private ICommand _clickSerialStartCmd;
        public ICommand ClickSerialStartCmd
        {
            get
            {
                return _clickSerialStartCmd ?? (_clickSerialStartCmd = new RelayCommand(
                           param => SerialStart()
                       ));
            }
        }
        private void SerialStart()
        {
            try
            {
                if (IsGSAppOpen("GS.Server"))
                {
                    var str = $"{Application.Current.Resources["msgDelCloseApps"]}" + Environment.NewLine;
                    OpenDialog(str);
                    return;
                }

                using (new WaitCursor())
                {
                    SerMsg = string.Empty;
                    counter = 0;
                    ZoomCounter = 0;
                    _startTime = new Stopwatch();
                    _startTime.Start();

                    serial = new SerialPort
                    {
                        PortName = $"COM{ComPort}",
                        BaudRate = BaudRate,
                        ReadTimeout = 1000,
                        StopBits = StopBits.One,
                        DataBits = 8,
                        DtrEnable = false,
                        RtsEnable = false,
                        Handshake = Handshake.None,
                        Parity = Parity.None,
                        DiscardNull = true,
                    };
                    serial.Open();
                    //serial.DataReceived += DataReceived;

                    aTimer = new Timer { Interval = Interval };
                    // Hook up the Elapsed event for the timer. 
                    aTimer.Elapsed += OnTimedEvent;
                    // Have the timer fire repeated events (true is the default)
                    aTimer.AutoReset = true;
                    // Start the timer
                    aTimer.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                OpenDialog(ex.Message);
            }
        }

        private ICommand _clickSerialStopCmd;
        public ICommand ClickSerialStopCmd
        {
            get
            {
                return _clickSerialStopCmd ?? (_clickSerialStopCmd = new RelayCommand(
                           param => SerialStop()
                       ));
            }
        }
        private void SerialStop()
        {
            try
            {
                using (new WaitCursor())
                {
                    Stop();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                OpenDialog(ex.Message);
            }
        }
        #endregion

        #region FileLocks

        private string _fileLocked;
        public string FileLocked
        {
            get => _fileLocked;
            set
            {
                if (_fileLocked == value) return;
                _fileLocked = value;
                OnPropertyChanged();
            }
        }

        private ICommand _clickFileCheckCmd;
        public ICommand ClickFileCheckCommand
        {
            get
            {
                return _clickFileCheckCmd ?? (_clickFileCheckCmd = new RelayCommand(
                           param => FileCheck()
                       ));
            }
        }
        private void FileCheck()
        {
            try
            {
                var msg = string.Empty;
                FileLocked = msg;
                using (new WaitCursor())
                {
                    DebugSetFilePath();

                    foreach (var process in Process.GetProcesses())
                    {
                        if (process.ProcessName != "GS.Server") continue;
                        var gsProcess = process;
                        msg =
                            $"{Application.Current.Resources["tbUtilProcessName"]}  {gsProcess.ProcessName} {Application.Current.Resources["tbUtilId"]}  {gsProcess.Id}";
                        //var gsModules = FileUtil.CollectModules(pro);
                        // Console.WriteLine("Process: {0} ID: {1}", theprocess.ProcessName, theprocess.Id);
                    }
                }

                var progList = FileUtil.WhoIsLocking(_gsFilePath);
                if (progList.Count > 0)
                {
                    msg += $" { Application.Current.Resources["tbUtilLocks"]} { progList.Count}";
                    var combindedString = string.Join(Environment.NewLine, progList);
                    OpenDialog(combindedString);
                }

                if (msg == string.Empty) { msg = $"{Application.Current.Resources["tbUtilNothing"]}"; }

                FileLocked = msg;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                OpenDialog(ex.Message);
            }
        }

        [ConditionalAttribute("DEBUG")]
        private void DebugSetFilePath()
        {
            _gsFilePath = "C:\\Users\\Rob\\source\\repos\\GSSolution\\Builds\\Debug\\GS.Server.exe";
        }

        #endregion

        #region DelFiles

        private bool _delLogFiles;
        public bool DelLogFiles
        {
            get => _delLogFiles;
            set
            {
                if (_delLogFiles == value) return;
                _delLogFiles = value;
                OnPropertyChanged();
            }
        }

        private bool _delSettings;
        public bool DelSettings
        {
            get => _delSettings;
            set
            {
                if (_delSettings == value) return;
                _delSettings = value;
                OnPropertyChanged();
            }
        }

        private string _delDialogMsg;
        public string DelDialogMsg
        {
            get => _delDialogMsg;
            set
            {
                if (_delDialogMsg == value) return;
                _delDialogMsg = value;
                OnPropertyChanged();
            }
        }

        private bool _isDelDialogOpen;
        public bool IsDelDialogOpen
        {
            get => _isDelDialogOpen;
            set
            {
                if (_isDelDialogOpen == value) return;
                _isDelDialogOpen = value;
                ScreenEnabled = !value;
                OnPropertyChanged();
            }
        }

        private object _delDialogContent;
        public object DelDialogContent
        {
            get => _delDialogContent;
            set
            {
                if (_delDialogContent == value) return;
                _delDialogContent = value;
                OnPropertyChanged();
            }
        }

        private ICommand _openDelDialogCommand;
        public ICommand OpenDelDialogCommand
        {
            get
            {
                return _openDelDialogCommand ?? (_openDelDialogCommand = new RelayCommand(
                           param => OpenDelDialog(null)
                       ));
            }
        }
        private void OpenDelDialog(string msg)
        {
            using (new WaitCursor())
            {
                if (!DelSettings && !DelLogFiles) return;

                if (DelSettings || DelLogFiles)
                {
                    if (IsGSAppOpen("GS.Server") || IsGSAppOpen("GS.ChartViewer"))
                    {
                        var str = $"{Application.Current.Resources["msgDelCloseApps"]}" + Environment.NewLine;
                        OpenDialog(str);
                        return;
                    }
                }

                if (msg != null) DelDialogMsg = msg;
                DelDialogContent = new DelFilesDialog();
                IsDelDialogOpen = true;
            }
        }

        private ICommand _clickDelAcceptDialogCommand;
        public ICommand ClickAcceptDelDialogCommand
        {
            get
            {
                return _clickDelAcceptDialogCommand ?? (_clickDelAcceptDialogCommand = new RelayCommand(
                           param => ClickDelDialog()
                       ));
            }
        }
        private void ClickDelDialog()
        {
            try
            {
                using (new WaitCursor())
                {
                    var msg = string.Empty;
                    var cntLogs = 0;
                    if (DelLogFiles)
                    {
                        if (Directory.Exists(_logDir))
                        {
                            foreach (var file in Directory.EnumerateFiles(_logDir, "GSMonitorLog*.txt"))
                            {
                                File.Delete(file);
                                cntLogs++;
                            }

                            foreach (var file in Directory.EnumerateFiles(_logDir, "GSErrorLog*.txt"))
                            {
                                File.Delete(file);
                                cntLogs++;
                            }

                            foreach (var file in Directory.EnumerateFiles(_logDir, "GSSessionLog*.txt"))
                            {
                                File.Delete(file);
                                cntLogs++;

                            }

                            foreach (var file in Directory.EnumerateFiles(_logDir, "GSPulsesLog*.txt"))
                            {
                                File.Delete(file);
                                cntLogs++;
                            }
                        }

                        msg = $"{cntLogs} {Application.Current.Resources["msgDelLogFiles"]}" + Environment.NewLine;
                    }

                    var cntSettings = 0;
                    var cntSettingsDir = 0;
                    if (DelSettings)
                    {
                        var config =
                            ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                        var configDirPath = Path.GetFullPath(Path.Combine(config.FilePath, @"..\..\..\"));

                        if (Directory.Exists(configDirPath))
                        {
                            foreach (var directory in Directory.GetDirectories(configDirPath,
                                "GS.ChartViewer.exe_StrongName*"))
                            {
                                cntSettings += Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                                    .Length;
                                cntSettingsDir += Directory.GetDirectories(directory).Length;
                                Directory.Delete(directory, true); //if explorer folder is open Dir Not Empty error
                            }

                            foreach (var directory in Directory.GetDirectories(configDirPath,
                                "GS.LogView.exe_StrongName*"))
                            {
                                cntSettings += Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                                    .Length;
                                cntSettingsDir += Directory.GetDirectories(directory).Length;
                                Directory.Delete(directory, true); //if explorer folder is open Dir Not Empty error
                            }

                            foreach (var directory in Directory.GetDirectories(configDirPath,
                                "GS.Server.exe_StrongName*"))
                            {
                                cntSettings += Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Length;
                                cntSettingsDir += Directory.GetDirectories(directory).Length;
                                Directory.Delete(directory, true); //if explorer folder is open Dir Not Empty error
                            }

                            foreach (var directory in Directory.GetDirectories(configDirPath,
                                "GS.Utilities.exe_StrongName*"))
                            {
                                cntSettings += Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Length;
                                cntSettingsDir += Directory.GetDirectories(directory).Length;
                                Directory.Delete(directory, true); //if explorer folder is open Dir Not Empty error
                            }
                        }

                        msg += $"{cntSettings} {Application.Current.Resources["msgDelSettings"]}" + Environment.NewLine;
                        msg += $"{cntSettingsDir} {Application.Current.Resources["msgDelSettingsVer"]}" +
                               Environment.NewLine;
                    }

                    IsDelDialogOpen = false;

                    if (DelSettings || DelLogFiles)
                    {
                        OpenDialog(msg);
                        DelSettings = false;
                        DelLogFiles = false;
                    }

                }
            }
            catch (Exception ex)
            {
                IsDelDialogOpen = false;
                OpenDialog(ex.Message);
            }

        }

        private ICommand _cancelDelDialogCommand;
        public ICommand CancelDelDialogCommand
        {
            get
            {
                return _cancelDelDialogCommand ?? (_cancelDelDialogCommand = new RelayCommand(
                           param => CancelDelDialog()));
            }
        }
        private void CancelDelDialog()
        {
            IsDelDialogOpen = false;
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
                ScreenEnabled = !value;
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

        public void Dispose()
        {
            serial?.Dispose();
            aTimer?.Stop();
        }
    }
}
