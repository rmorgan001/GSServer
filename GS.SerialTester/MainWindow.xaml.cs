/* Copyright(C) 2019  Rob Morgan (robert.morgan.e@gmail.com)

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
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows;
using Timer = System.Timers.Timer;

namespace GS.SerialTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow : INotifyPropertyChanged
    {
        private static Timer aTimer;
        private SerialPort serial;
        private static readonly object _timerLock = new object();
        private const char _endChar = (char)13;
        private int counter;
        private bool axis;
        private const string j1 = ":j1";
        private const string j2 = ":j2";
        private string zoom = "Zoom Zoom...";
        private int zoomcounter;

        private Stopwatch _startTime;
        //private string IncomingData;

        public IList<double> IntervalList { get; }
        private double _interval;
        public double Interval
        {
            get => _interval;
            set
            {
                _interval = value;
                zoomcounter = 0;
                OnPropertyChanged();
            }
        }

        public MainWindow()
        {

            InitializeComponent();

            IntervalList = new List<double>(InclusiveRange(10, 1000, 10));
            Interval = 300.0;
            DataContext = this;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                mytextblock.Clear();
                counter = 0;
                zoomcounter = 0;
                _startTime = new Stopwatch();
                _startTime.Start();

                serial = new SerialPort
                {
                    PortName = $"COM{port.Text}",
                    BaudRate = 9600,
                    ReadTimeout = 5000,
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Stop();
            }

        }

        private void ButtonBase1_OnClick(object sender, RoutedEventArgs e)
        {
            Stop();
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
                var receivedData = RecieveResponse();
                receivedData = receivedData?.Trim();
                receivedData = receivedData?.Replace("\0", string.Empty);
                if (string.IsNullOrEmpty(receivedData))
                {
                    Stop();
                    throw new Exception($"Timeout Occured Event # {counter}");
                }

                var zoomtxt = string.Empty;
                if (Interval < 11.0 && zoomcounter < 30)
                {
                    zoomcounter++;
                    if (zoomcounter < 12)zoomtxt = zoom.Substring(0, zoomcounter);
                }
                
                InvokeOnUiThread(
                    delegate
                    {
                        mytextblock.Text =
                            $"Counter: {counter} Timer: {_startTime.Elapsed:hh\\:mm\\:ss\\.fff}, Cmd {commandStr} Data: {receivedData} {zoomtxt}";
                    });
                aTimer.Interval = Interval;
            }
            catch (Exception ex)
            {
                Stop();
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (hasLock) Monitor.Exit(_timerLock);
            }
        }

        private static void InvokeOnUiThread(Action action, CancellationToken token = default(CancellationToken))
        {
            if (Application.Current == null) return;
            if (Application.Current.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                if (token.IsCancellationRequested) return;
                Application.Current.Dispatcher.Invoke(action);
            }
        }

        /*
        private StringBuilder serialBuffer = new StringBuilder();
        private const string terminationSequence = "\r";
        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                //Trace.TraceInformation("Start Event");
                var data = serial.ReadExisting();
                serialBuffer.Append(data);
                var bufferString = serialBuffer.ToString();
                int index;
                do
                {
                    //  Trace.TraceInformation($"Start do: bufferString - {bufferString}");
                    index = bufferString.IndexOf(terminationSequence, StringComparison.Ordinal);
                    if (index <= -1) continue;
                    IncomingData = bufferString.Substring(0, index);
                    // Trace.TraceInformation("IncomingData {0}", IncomingData);
                    bufferString = bufferString.Remove(0, index + terminationSequence.Length);

                } while (index > -1);
                serialBuffer = new StringBuilder(bufferString);
               // Trace.TraceInformation("End Event");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }
        */


        /// <summary>
        /// Read serial port buffer - skywatcher original source
        /// </summary>
        /// <returns></returns>
        private string RecieveResponse()
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

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            Stop();
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

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
