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
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GS.Shared
{
    /// <summary>
    /// Handles output to logging files usign a blocking queue
    /// </summary>
    public static class MonitorQueue
    {
        #region Fields
        public static event PropertyChangedEventHandler StaticPropertyChanged;
        private static readonly BlockingCollection<MonitorEntry> _monitorBlockingCollection;
        private static readonly BlockingCollection<PulseEntry> _pulseBlockingCollection;
        private static int _errIndex;
        private static int _sessionIndex;
        private static int _monitorindex;
        private static readonly string _instanceFileName;
        private static readonly string _logPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static readonly string _monitorFile = Path.Combine(_logPath, "GSServer\\GSMonitorLog");
        private static readonly string _errorFile = Path.Combine(_logPath, "GSServer\\GSErrorLog");
        private static readonly string _sessionFile = Path.Combine(_logPath, "GSServer\\GSSessionLog");
        private static readonly SemaphoreSlim _lockFile = new SemaphoreSlim(1);
        #endregion

        #region Properties

        // UI indicator for warnings
        private static bool _warningState;
        public static bool WarningState
        {
            get => _warningState;
            set
            {
                if (_warningState == value) return;
                _warningState = value;
                OnStaticPropertyChanged();
                FlipOffWarningState();
            }
        }

        private static async void FlipOffWarningState()
        {
            if (!WarningState) return;
            await Task.Delay(100);
            WarningState = false;
        }

        /// <summary>
        /// Holds an entry and triggers the Monitor UI to pick up
        /// </summary>
        private static MonitorEntry _monitorEntry;
        public static MonitorEntry MonitorEntry
        {
            get => _monitorEntry;
            private set
            {
                _monitorEntry = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Holds an entry and triggers the Charts UI to pick up
        /// </summary>
        private static PulseEntry _pulseEntry;
        public static PulseEntry PulseEntry
        {
            get => _pulseEntry;
            private set
            {
                _pulseEntry = value;
                OnStaticPropertyChanged();
            }
        }

        /// <summary>
        /// Holds an entry and triggers the Charts UI pick up
        /// </summary>
        private static MonitorEntry _cmdjSentEntry;
        public static MonitorEntry CmdjSentEntry
        {
            get => _cmdjSentEntry;
            private set
            {
                _cmdjSentEntry = value;
                OnStaticPropertyChanged();
            }
        }

        #endregion

        static MonitorQueue()
        {
            _instanceFileName = $"{DateTime.Now:yyyy-dd-MM-HH}.txt";
            DeleteFiles("GSSessionLog", 7, _logPath);
            DeleteFiles("GSErrorLog", 7, _logPath);
            DeleteFiles("GSChartingLog", 7, _logPath);
            DeleteFiles("GSMonitorLog", 7, _logPath);


            _monitorBlockingCollection = new BlockingCollection<MonitorEntry>();
            Task.Factory.StartNew(() =>
            {
                foreach (var monitorentry in _monitorBlockingCollection.GetConsumingEnumerable())
                {
                    ProcessEntryQueueItem(monitorentry);
                }
            });

            _pulseBlockingCollection = new BlockingCollection<PulseEntry>();
            Task.Factory.StartNew(() =>
            {
                foreach (var pulseEntry in _pulseBlockingCollection.GetConsumingEnumerable())
                {
                    ProcessPulseQueueItem(pulseEntry);
                }
            });
        }

        #region Methods

        /// <summary>
        /// reset the count from the UI
        /// </summary>
        public static void ResetMonitorIndex()
        {
            _monitorindex = 0;
        }

        /// <summary>
        /// adds a monitor item to a blocking queue
        /// </summary>
        /// <param name="entry"></param>
        public static void AddEntry(MonitorEntry entry)
        {
            _monitorBlockingCollection.TryAdd(entry);
        }

        /// <summary>
        /// adds a pulse item to a blocking queue
        /// </summary>
        /// <param name="entry"></param>
        public static void AddPulse(PulseEntry entry)
        {
            _pulseBlockingCollection.TryAdd(entry);
        }

        /// <summary>
        /// trigger the property event for the UI to pick up the property
        /// </summary>
        /// <param name="propertyName"></param>
        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Process queue monitor items to the appropriate logs
        /// </summary>
        /// <param name="entry"></param>
        private static void ProcessEntryQueueItem(MonitorEntry entry)
        {
            switch (entry.Type)
            {
                // Output error log
                case MonitorType.Error:
                    WriteOutErrors(entry);
                    break;
                // Output session log
                case MonitorType.Warning:
                    WarningState = true;
                    WriteOutSession(entry);
                    break;
                case MonitorType.Information:
                    WriteOutSession(entry);
                    break;
            }

            // Output monitor window
            if (Settings.StartMonitor)
            {
                var foundDevice = MonitorLog.InDevices(entry.Device);
                var foundCategory = MonitorLog.InCategory(entry.Category);
                var foundType = MonitorLog.InTypes(entry.Type);

                // Check checkboxes for output to monitor window
                if (!foundDevice || !foundCategory || !foundType) return;
                ++_monitorindex;
                entry.Index = _monitorindex;
                MonitorEntry = entry;

                // Write out log if selected
                if (Settings.LogMonitor) WriteOutMonitor(entry);
            }

            // Output specific entries for charting
            if (MonitorLog.GetjEntries) ProcessChartItems(entry);
        }

        /// <summary>
        /// Process mount entries that need to be charted or logged
        /// </summary>
        /// <param name="entry"></param>
        private static void ProcessChartItems(MonitorEntry entry)
        {
            if (entry.Device != MonitorDevice.Telescope || entry.Category != MonitorCategory.Mount ||
                entry.Type != MonitorType.Data) return;
            switch (entry.Method)
            {
                case "ReceiveResponse": // skywatcher
                    if (entry.Message.Contains(":j1"))
                    {
                        var msg = entry.Message.Split(',');
                        if (msg.Length < 2) return;
                        // make sure it a valid mount response
                        msg[1] = msg[1].Trim();
                        if (!msg[1].Contains("=")) return;
                        // convert response
                        var msgval = Strings.StringToLong(msg[1]);
                        entry.Message = $"{msg[0].Trim()},{msg[1]},{msgval}";
                        //send to charting and log
                        CmdjSentEntry = entry;
                        //  WriteOutCmdj(entry);
                    }
                    break;
                case "AxesDegrees":  // from simulator
                    if (entry.Message.Contains("tracking"))
                    {
                        CmdjSentEntry = entry;
                        // WriteOutCmdj(entry);
                    }
                    break;
            }
        }

        /// <summary>
        /// Process queue pulse items to the appropriate logs
        /// </summary>
        /// <param name="entry"></param>
        private static void ProcessPulseQueueItem(PulseEntry entry)
        {
            if (!MonitorLog.GetPulses) return;
            PulseEntry = entry;
        }

        /// <summary>
        /// Writes out the monitor type Information to the session log
        /// </summary>
        /// <param name="entry"></param>
        private static void WriteOutSession(MonitorEntry entry)
        {
            try
            {
                if (!Settings.LogSession) return;
                ++_sessionIndex;
                FileWriteAsync(_sessionFile + _instanceFileName, $"{_sessionIndex},{entry.Datetime.ToLocalTime():yyyy:dd:MM:HH:mm:ss.fff},{entry.Device},{entry.Category},{entry.Type},{entry.Thread},{entry.Method},{entry.Message}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //throw;
            }
        }

        /// <summary>
        /// Writes all warnings and errors to an error file 
        /// </summary>
        /// <param name="entry"></param>
        private static void WriteOutErrors(MonitorEntry entry)
        {
            try
            {
                ++_errIndex;
                FileWriteAsync(_errorFile + _instanceFileName, $"{_errIndex},{entry.Datetime.ToLocalTime():yyyy:dd:MM:HH:mm:ss.fff},{entry.Device},{entry.Category},{entry.Type},{entry.Thread},{entry.Method},{entry.Message}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // throw;
            }
        }

        /// <summary>
        /// Writes out the monitor item to a log file
        /// </summary>
        /// <param name="entry"></param>
        private static void WriteOutMonitor(MonitorEntry entry)
        {
            try
            {
                if (!Settings.LogMonitor) return;
                FileWriteAsync(_monitorFile + _instanceFileName, $"{entry.Index},{entry.Datetime.ToLocalTime():yyyy:dd:MM:HH:mm:ss.fff},{entry.Device},{entry.Category},{entry.Type},{entry.Thread},{entry.Method},{entry.Message}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // throw;
            }
        }

        /// <summary>
        /// Send monitor entries to a file async
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="messaage"></param>
        /// <param name="append"></param>
        private static async void FileWriteAsync(string filePath, string messaage, bool append = true)
        {
            try
            {
                await _lockFile.WaitAsync();

                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException());
                using (var stream = new FileStream(filePath, append ? FileMode.Append : FileMode.Create,
                    FileAccess.Write, FileShare.None, 4096, true))
                using (var sw = new StreamWriter(stream))
                {
                    await sw.WriteLineAsync(messaage);
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

        /// <summary>
        /// Deletes files by name, how old, and dir path
        /// </summary>
        /// <param name="name"></param>
        /// <param name="daysold"></param>
        /// <param name="path"></param>
        private static void DeleteFiles(string name, int daysold, string path)
        {
            try
            {
                var files = Directory.GetFiles(path);
                foreach (var file in files)
                {
                    var fi = new FileInfo(file);
                    if (fi.Name.Contains(name) && fi.CreationTime < (DateTime.Now - new TimeSpan(daysold, 0, 0, 0))) fi.Delete();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //throw;
            }
        }

        #endregion
    }
}
