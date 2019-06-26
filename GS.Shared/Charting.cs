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
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GS.Shared
{
    public class Charting
    {
        #region Fields

        private readonly BlockingCollection<ChartEntry> _chartBlockingCollection;
        private readonly string _instanceFileName;
        private static readonly string _logPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private readonly string _chartingFile = Path.Combine(_logPath, "GSServer\\GSChartingLog");
        private static readonly SemaphoreSlim _lockFile = new SemaphoreSlim(1);

        #endregion

        public Charting()
        {
            _instanceFileName = $"{DateTime.Now:yyyy-dd-MM}.txt";
            DeleteFiles("GSChartingLog", 7, _logPath);

            _chartBlockingCollection = new BlockingCollection<ChartEntry>();
            Task.Factory.StartNew(() =>
            {
                foreach (var item in _chartBlockingCollection.GetConsumingEnumerable())
                {
                    ProcessChartQueueItem(item);
                }
            });
        }

        #region Methods

        /// <summary>
        /// adds a monitor item to a blocking queue
        /// </summary>
        /// <param name="entry"></param>
        public void AddEntry(ChartEntry entry)
        {
            _chartBlockingCollection.TryAdd(entry);
        }

        private void ProcessChartQueueItem(ChartEntry entry)
        {
           // if (!LogCharting) return;           
            var message = string.Empty;
            switch (entry.ItemCode)
            {
                case ChartItemCode.Start:
                case ChartItemCode.Stop:
                case ChartItemCode.Data:
                    message = $"{(int)entry.ItemCode}\t{entry.Data}";
                    break;
                case ChartItemCode.RaValue:
                case ChartItemCode.DecValue:
                case ChartItemCode.ThirdValue:
                case ChartItemCode.FourthValue:
                    message = $"{(int)entry.ItemCode}\t{entry.X:yyyy:dd:MM:HH:mm:ss.fff}\t{entry.Y}";
                    break;
            }
           if(message != string.Empty) FileWriteAsync(_chartingFile + _instanceFileName, message);
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
        /// <param name="message"></param>
        /// <param name="append"></param>
        private static async void FileWriteAsync(string filePath, string message, bool append = true)
        {
            await _lockFile.WaitAsync();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException());
                using (var stream = new FileStream(filePath, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                using (var sw = new StreamWriter(stream))
                {
                    await sw.WriteLineAsync(message);
                }
            }
            finally
            {
                _lockFile.Release();
            }
        }

        #endregion
    }

    #region enums

    /// <summary>
    /// Used to identify line types in the charting log
    /// </summary>
    public enum ChartItemCode
    {
        Start = 0,
        Stop = 1,
        Data = 2,
        RaValue = 3,
        DecValue = 4,
        ThirdValue = 5,
        FourthValue = 6
    }

    #endregion

    /// <summary>
    /// individual Monitor Item
    /// </summary>
    public class ChartEntry
    {
        public ChartItemCode ItemCode { get; set; }
        public  DateTime X { get; set; }
        public double Y { get; set; }
        public string Data { get; set; }
    }
}
