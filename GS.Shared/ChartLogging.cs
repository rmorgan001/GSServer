/* Copyright(C) 2019-2022 Rob Morgan (robert.morgan.e@gmail.com)

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
    public static class ChartLogging
    {
        private static readonly BlockingCollection<ChartLogItem> _chartBlockingCollection;
        private static readonly string _instanceFileName;
        private static readonly string _logPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static readonly string _fileLocation = Path.Combine(_logPath, "GSServer\\");
        private static readonly string _fileNameAddOn = "ChartLog";
        private static readonly SemaphoreSlim _lockFile = new SemaphoreSlim(1);

        static ChartLogging()
        {
            try
            {
                _instanceFileName = $"{DateTime.Now:yyyy-dd-MM}.txt";
                DeleteFiles(_fileNameAddOn, 7, _logPath);

                _chartBlockingCollection = new BlockingCollection<ChartLogItem>();
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
                    Method = MethodBase.GetCurrentMethod()?.Name,
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
        /// <param name="logItem"></param>
        private static void AddEntry(ChartLogItem logItem)
        {
            _chartBlockingCollection.TryAdd(logItem);
        }

        /// <summary>
        /// Process item from the blocking queue
        /// </summary>
        /// <param name="logItem"></param>
        private static void ProcessChartQueueItem(ChartLogItem logItem)
        {
            try
            {
                if (logItem.Message != string.Empty) FileWriteAsync(_fileLocation + "GS" + logItem.LogBaseName + _fileNameAddOn + _instanceFileName, logItem);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                IsRunning = false;
            }
        }

        /// <summary>
        /// Deletes files by name, how old, and dir path
        /// </summary>
        /// <param name="name"></param>
        /// <param name="daySold"></param>
        /// <param name="path"></param>
        private static void DeleteFiles(string name, int daySold, string path)
        {
            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                var fi = new FileInfo(file);
                if (fi.Name.Contains(name) && fi.CreationTime < (DateTime.Now - new TimeSpan(daySold, 0, 0, 0))) fi.Delete();
            }
        }

        /// <summary>
        /// Send entries to a file async
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="logItem"></param>
        /// <param name="append"></param>
        private static async void FileWriteAsync(string filePath, ChartLogItem logItem, bool append = true)
        {
            await _lockFile.WaitAsync();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException());
                using (var stream = new FileStream(filePath, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                using (var sw = new StreamWriter(stream))
                {
                    var str = $"{(int)logItem.ChartType}|{(int)logItem.LogCode}|{logItem.Message}";
                    await sw.WriteLineAsync(str);
                }
            }
            catch(IOException ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Server,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $" {ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
            finally
            {
                _lockFile.Release();
            }
        }

        public static void LogStart(string basename, ChartType type)
        {
            if (!IsRunning) return;
            var str = $"{HiResDateTime.UtcNow.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}|{ChartLogCode.Start}|{type}";
            var chartsLogItem = new ChartLogItem { LogBaseName = basename, ChartType = type , LogCode = ChartLogCode.Start, Message = str };
            AddEntry(chartsLogItem);
        }

        //public static void LogInfo(string basename, ChartType type, string value)
        //{
        //    if (!IsRunning) return;
        //    var str = $"{HiResDateTime.UtcNow.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}|{value}";
        //    var chartsLogItem = new ChartLogItem { LogBaseName = basename, ChartType = type, LogCode = ChartLogCode.Info, Message = str };
        //    AddEntry(chartsLogItem);
        //}

        public static void LogData(string basename, ChartType type, string key, string value)
        {
            if (!IsRunning) return;
            var str = $"{HiResDateTime.UtcNow.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}|{key}|{value}";
            var chartsLogItem = new ChartLogItem { LogBaseName = basename, ChartType = type, LogCode = ChartLogCode.Data, Message = str };
            AddEntry(chartsLogItem);
        }

        public static void LogPoint(string basename, ChartType type, PointModel point)
        {
            if (!IsRunning) return;
            var str = $"{point.DateTime.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}|{point.Value}|{point.Set}";
            var chartsLogItem = new ChartLogItem { LogBaseName = basename, ChartType = type, LogCode = ChartLogCode.Point, Message = str };
            AddEntry(chartsLogItem);
        }

        public static void LogSeries(string basename, ChartType type, string series, string message)
        {
            if (!IsRunning) return;
            var str = $"{HiResDateTime.UtcNow.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}|{series}|{message}";
            var chartsLogItem = new ChartLogItem { LogBaseName = basename, ChartType = type, LogCode = ChartLogCode.Series, Message = str };
            AddEntry(chartsLogItem);
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
        ArcSecs = 2,
        Steps = 3,
        Unknown = 4
    }
    public enum ChartType
    {
        Pulses = 1,
        Plot = 2,
    }
    public class ChartLogItem
    {
        public ChartType ChartType { get; set; }
        public ChartLogCode LogCode { get; set; }
        public string Message { get; set; }
        public string LogBaseName { get; set; }
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
