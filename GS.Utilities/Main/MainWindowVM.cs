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
using ASCOM.DriverAccess;
using GS.Shared;
using GS.Shared.Command;
using GS.Utilities.Controls.Dialogs;
using GS.Utilities.Helpers;
using MaterialDesignThemes.Wpf;
using Timer = System.Timers.Timer;

namespace GS.Utilities.Main
{
    internal class MainWindowVm : ObservableObject, IDisposable
    {
        private static readonly string DocDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static readonly string LogDir = Path.Combine(DocDir, "GSServer");

        private static readonly string CommonDir =
            Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);

        private const string GsDir = @"\ASCOM\Telescope\GSServer\";
        private static string _gsFilePath = CommonDir + GsDir + "GS.Server.exe";

        //serial test
        private static Timer _aTimer;
        private static readonly object TimerLock = new object();
        private SerialPort _serial;
        private const char EndChar = (char)13;
        private const string J1 = ":j1";
        private const string J2 = ":j2";
        private int _counter;
        private bool _axis;
        private const string Zoom = "Zoom Zoom...";
        private Stopwatch _startTime;

        public MainWindowVm()
        {
            try
            {
                using (new WaitCursor())
                {
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

        public List<string> Languages => Shared.Languages.SupportedLanguages;

        public string Lang
        {
            get => Properties.Utilities.Default.Language;
            set
            {
                Properties.Utilities.Default.Language = value;
                OnPropertyChanged();
                OpenDialog("Restart Needed");
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

        private static bool IsGsAppOpen(string name)
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
                var command = _clickCloseAppCommand;
                if (command != null)
                {
                    return command;
                }

                return (_clickCloseAppCommand = new RelayCommand(
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
                var command = _minimizeWindowCommand;
                if (command != null)
                {
                    return command;
                }

                return (_minimizeWindowCommand = new RelayCommand(
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
                var cmd = _clickConnectCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return (_clickConnectCmd = new RelayCommand(
                    param => ConnectCheck()
                ));
            }
        }
        private void ConnectCheck()
        {
            try
            {
                Connect = null;
                var msg = $"{Application.Current.Resources["utilNotFound"]}";
                var util = new ASCOM.Utilities.Chooser();
                var progId = util.Choose("ASCOM.GS.Sky.Telescope");
                if (progId != null)
                {
                    var t = new Telescope(progId) { Connected = true };
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
            _aTimer?.Stop();
            if (_aTimer != null) _aTimer.Elapsed -= OnTimedEvent;
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalMilliseconds < 100)
            {

            }
            _serial?.Close();
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            var hasLock = false;
            try
            {
                Monitor.TryEnter(TimerLock, ref hasLock);
                if (!hasLock) return;
                if (!_serial.IsOpen) return;
                var commandStr = new StringBuilder(20);
                commandStr.Append(_axis ? J1 : J2);
                _axis = !_axis;
                commandStr.Append(EndChar);
                _serial.Write(commandStr.ToString());
                _counter++;

                // var receivedData = IncomingData;
                var receivedData = ReceiveResponse();
                receivedData = receivedData?.Trim();
                receivedData = receivedData?.Replace("\0", string.Empty);
                if (string.IsNullOrEmpty(receivedData))
                {
                    Stop();
                    throw new Exception($"{Application.Current.Resources["utilTimeout"]} {_counter}");
                }

                var zoomtxt = string.Empty;
                if (Interval < 11.0 && ZoomCounter < 30)
                {
                    ZoomCounter++;
                    if (ZoomCounter < 12) zoomtxt = Zoom.Substring(0, ZoomCounter);
                }

                InvokeOnUiThread(
                    delegate
                    {
                        SerMsg =
                            $"{Application.Current.Resources["utilCounter"]} {_counter} {Application.Current.Resources["utilTimer"]} {_startTime.Elapsed:hh\\:mm\\:ss\\.fff}, {commandStr} {receivedData} {zoomtxt}";
                    });
                _aTimer.Interval = Interval;

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
                if (hasLock) Monitor.Exit(TimerLock);
            }
        }

        private static void InvokeOnUiThread(Action action, CancellationToken token = default)
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
            var startReading = false;

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalMilliseconds < _serial.ReadTimeout)
            {
                var data = _serial.ReadExisting();
                foreach (var byt in data)
                {
                    // this code order is important
                    if (byt == '=' || byt == '!' || byt == EndChar) startReading = true;
                    if ((byt == EndChar) && (mBuffer.Length == 0)) continue;
                    if (startReading) mBuffer.Append(byt);
                    if (byt != EndChar) continue;
                    if (!startReading) continue;
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
                var cmd = _clickSerialStartCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return (_clickSerialStartCmd = new RelayCommand(
                    param => SerialStart()
                ));
            }
        }
        private void SerialStart()
        {
            try
            {
                if (IsGsAppOpen("GS.Server"))
                {
                    var str = $"{Application.Current.Resources["utilCloseApps"]}" + Environment.NewLine;
                    OpenDialog(str);
                    return;
                }

                using (new WaitCursor())
                {
                    SerMsg = string.Empty;
                    _counter = 0;
                    ZoomCounter = 0;
                    _startTime = new Stopwatch();
                    _startTime.Start();

                    _serial = new SerialPort
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
                    _serial.Open();
                    //serial.DataReceived += DataReceived;

                    _aTimer = new Timer { Interval = Interval };
                    // Hook up the Elapsed event for the timer. 
                    _aTimer.Elapsed += OnTimedEvent;
                    // Have the timer fire repeated events (true is the default)
                    _aTimer.AutoReset = true;
                    // Start the timer
                    _aTimer.Enabled = true;
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
                var cmd = _clickSerialStopCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return (_clickSerialStopCmd = new RelayCommand(
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
                var cmd = _clickFileCheckCmd;
                if (cmd != null)
                {
                    return cmd;
                }

                return (_clickFileCheckCmd = new RelayCommand(
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
                            $"{Application.Current.Resources["utilProcessName"]}  {gsProcess.ProcessName} {Application.Current.Resources["utilId"]}  {gsProcess.Id}";
                    }
                }

                var progList = FileUtil.WhoIsLocking(_gsFilePath);
                if (progList.Count > 0)
                {
                    msg += $" { Application.Current.Resources["utilLocks"]} { progList.Count}";
                    var combindedString = string.Join(Environment.NewLine, progList);
                    OpenDialog(combindedString);
                }

                if (msg == string.Empty) { msg = $"{Application.Current.Resources["utilNothing"]}"; }

                FileLocked = msg;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                OpenDialog(ex.Message);
            }
        }

        [Conditional("DEBUG")]
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
                var command = _openDelDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return (_openDelDialogCommand = new RelayCommand(
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
                    if (IsGsAppOpen("GS.Server") || IsGsAppOpen("GS.ChartViewer"))
                    {
                        var str = $"{Application.Current.Resources["utilCloseApps"]}" + Environment.NewLine;
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
                var command = _clickDelAcceptDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return (_clickDelAcceptDialogCommand = new RelayCommand(
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
                        if (Directory.Exists(LogDir))
                        {
                            foreach (var file in Directory.EnumerateFiles(LogDir, "GSMonitorLog*.txt"))
                            {
                                File.Delete(file);
                                cntLogs++;
                            }

                            foreach (var file in Directory.EnumerateFiles(LogDir, "GSErrorLog*.txt"))
                            {
                                File.Delete(file);
                                cntLogs++;
                            }

                            foreach (var file in Directory.EnumerateFiles(LogDir, "GSSessionLog*.txt"))
                            {
                                File.Delete(file);
                                cntLogs++;

                            }

                            foreach (var file in Directory.EnumerateFiles(LogDir, "GSPulsesLog*.txt"))
                            {
                                File.Delete(file);
                                cntLogs++;
                            }
                        }

                        msg = $"{cntLogs} {Application.Current.Resources["utilDelLogFiles"]}" + Environment.NewLine;
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

                        msg += $"{cntSettings} {Application.Current.Resources["utilDelSettings"]}" + Environment.NewLine;
                        msg += $"{cntSettingsDir} {Application.Current.Resources["utilDelSettingsVer"]}" +
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
                var command = _cancelDelDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return (_cancelDelDialogCommand = new RelayCommand(
                    param => CancelDelDialog()));
            }
        }
        private void CancelDelDialog()
        {
            IsDelDialogOpen = false;
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

        private ICommand _cancelDialogCommand;
        public ICommand CancelDialogCommand
        {
            get
            {
                var command = _cancelDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return (_cancelDialogCommand = new RelayCommand(
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

        public void Dispose()
        {
            _serial?.Dispose();
            _aTimer?.Stop();
        }
    }
}
