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
using GS.Principles;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GS.Shared
{
    public static class PulsesLogging
    {
        private static readonly BlockingCollection<PulsesLogItem> _chartBlockingCollection;
        private static readonly string _instanceFileName;
        private static readonly string _logPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static readonly string _chartingFile = Path.Combine(_logPath, "GSServer\\GSPulsesLog");
        private static readonly SemaphoreSlim _lockFile = new SemaphoreSlim(1);

        static PulsesLogging()
        {
            try
            {
                _instanceFileName = $"{DateTime.Now:yyyy-dd-MM}.txt";
                DeleteFiles("GSPulsesLog", 7, _logPath);

                _chartBlockingCollection = new BlockingCollection<PulsesLogItem>();
                Task.Factory.StartNew(() =>
                {
                    foreach (var logitem in _chartBlockingCollection.GetConsumingEnumerable())
                    {
                        ProcessChartQueueItem(logitem);
                    }
                });

                IsRunning = true;
            }
            catch (Exception ex)
            {
                IsRunning = false;
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $" {ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        private static bool IsRunning { get; set; }

        /// <summary>
        /// adds a item to a blocking queue
        /// </summary>
        /// <param name="logitem"></param>
        private static void AddEntry(PulsesLogItem logitem)
        {
            _chartBlockingCollection.TryAdd(logitem);
        }

        /// <summary>
        /// Process item from the blocking queue
        /// </summary>
        /// <param name="logitem"></param>
        private static void ProcessChartQueueItem(PulsesLogItem logitem)
        {
            try
            {
                if (logitem.Message != string.Empty) FileWriteAsync(_chartingFile + _instanceFileName, logitem);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $" {ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                IsRunning = false;
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
            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                var fi = new FileInfo(file);
                if (fi.Name.Contains(name) && fi.CreationTime < (DateTime.Now - new TimeSpan(daysold, 0, 0, 0))) fi.Delete();
            }
        }

        /// <summary>
        /// Send entries to a file async
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="logitem"></param>
        /// <param name="append"></param>
        private static async void FileWriteAsync(string filePath, PulsesLogItem logitem, bool append = true)
        {
            await _lockFile.WaitAsync();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException());
                using (var stream = new FileStream(filePath, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                using (var sw = new StreamWriter(stream))
                {
                    var str = $"{(int)logitem.PulseLogCode},{logitem.Message}";
                    await sw.WriteLineAsync(str);
                }
            }
            finally
            {
                _lockFile.Release();
            }
        }

        public static void LogStart(ChartType type)
        {
            if (!IsRunning) return;
            var str = $"{HiResDateTime.UtcNow.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff},{ChartLogCode.Start},{type}";
            var pulsesLogItem = new PulsesLogItem { PulseLogCode = ChartLogCode.Start, Message = str };
            AddEntry(pulsesLogItem);
        }

        public static void LogInfo(string value)
        {
            if (!IsRunning) return;
            var str = $"{HiResDateTime.UtcNow.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff},{value}";
            var pulsesLogItem = new PulsesLogItem { PulseLogCode = ChartLogCode.Info, Message = str };
            AddEntry(pulsesLogItem);
        }

        public static void LogData(string key, string value)
        {
            if (!IsRunning) return;
            var str = $"{HiResDateTime.UtcNow.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff},{key},{value}";
            var pulsesLogItem = new PulsesLogItem { PulseLogCode = ChartLogCode.Data, Message = str };
            AddEntry(pulsesLogItem);
        }

        public static void LogPoint(PointModel point)
        {
            if (!IsRunning) return;
            var str = $"{point.DateTime.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff},{point.Value},{point.Set}";
            var pulsesLogItem = new PulsesLogItem { PulseLogCode = ChartLogCode.Point, Message = str };
            AddEntry(pulsesLogItem);
        }

        public static void LogSeries(string series, string message)
        {
            if (!IsRunning) return;
            var str = $"{HiResDateTime.UtcNow.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff},{series},{message}";
            var pulsesLogItem = new PulsesLogItem { PulseLogCode = ChartLogCode.Series, Message = str };
            AddEntry(pulsesLogItem);
        }
    }

    public class PointModel
    {
        public DateTime DateTime { get; set; }
        public double Value { get; set; }
        public SolidColorBrush Fill { get; set; }
        public SolidColorBrush Stroke { get; set; }
        public ChartValueSet Set { get; set; }
    }
    public class TitleItem
    {
        public string TitleName { get; set; }
        public Brush Fill { get; set; }
        public ChartValueSet ValueSet { get; set; }
    }
    public enum ChartValueSet
    {
        Values1 = 1, //RaDur
        Values2 = 2, //DecDur
        Values3 = 3, //RaRej
        Values4 = 4, //DecRej
        Values5 = 5, //RaPhd
        Values6 = 6 //DecPhd
    }
    public enum ChartSeriesType
    {
        GLineSeries = 1,
        GColumnSeries = 2,
        GStepLineSeries = 3,
        GScatterSeries = 4
    }
    public enum ChartScale
    {
        Milliseconds = 1,
        Arcsecs = 2,
        Steps = 3,
        Unknown = 4
    }
    public enum ChartType
    {
        Pulses = 1
    }

    public class PulsesLogItem
    {
        public ChartLogCode PulseLogCode { get; set; }
        public string Message { get; set; }
    }

    public enum ChartLogCode
    {
        Start = 1,
        Info = 2,
        Data = 3,
        Point = 4,
        Series = 5
    }


}
