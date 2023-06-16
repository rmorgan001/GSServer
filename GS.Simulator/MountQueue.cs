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
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GS.Simulator
{
    public static class MountQueue
    {
        #region Fields

        private static BlockingCollection<IMountCommand> _commandBlockingCollection;
        private static ConcurrentDictionary<long, IMountCommand> _resultsDictionary;
        private static Actions _actions;
        private static CancellationTokenSource _cts;
        public static event PropertyChangedEventHandler StaticPropertyChanged;

        #endregion

        #region properties

        public static bool IsRunning { get; private set; }
        private static long _id;
        public static long NewId => Interlocked.Increment(ref _id);

        private static bool _isPulseGuidingDec;
        /// <summary>
        /// status for Dec Pulse
        /// </summary>
        public static bool IsPulseGuidingDec
        {
            get => _isPulseGuidingDec;
            set
            {
                _isPulseGuidingDec = value;
                OnStaticPropertyChanged();
            }
        }

        private static bool _isPulseGuidingRa;
        /// <summary>
        /// status for Dec Pulse
        /// </summary>
        public static bool IsPulseGuidingRa
        {
            get => _isPulseGuidingRa;
            set
            {
                _isPulseGuidingRa = value;
                OnStaticPropertyChanged();
            }
        }

        private static double[] _steps;
        /// <summary>
        /// current micro steps, used to update SkyServer and UI
        /// </summary>
        public static double[] Steps
        {
            get => _steps;
            set
            {
                _steps = value;
                OnStaticPropertyChanged();
            }
        }
        #endregion

        #region Queues

        /// <summary>
        /// Add a command to the blocking queue
        /// </summary>
        /// <param name="command"></param>
        public static void AddCommand(IMountCommand command)
        {
            if (!IsRunning) return;
            CleanResults(20, 120);
            _commandBlockingCollection.TryAdd(command);
        }

        /// <summary>
        /// Cleans up the results dictionary
        /// </summary>
        /// <param name="count"></param>
        /// <param name="seconds"></param>
        private static void CleanResults(int count, int seconds)
        {
            if (!IsRunning) return;
            if (_resultsDictionary.IsEmpty) return;
            var recordscount = _resultsDictionary.Count;
            if (recordscount == 0) return;
            if (count == 0 && seconds == 0)
            {
                _resultsDictionary.Clear();
                return;
            }

            if (recordscount < count) return;
            var now = HiResDateTime.UtcNow;
            foreach (var result in _resultsDictionary)
            {
                if (result.Value.CreatedUtc.AddSeconds(seconds) >= now) continue;
                _resultsDictionary.TryRemove(result.Key, out _);
            }
        }

        /// <summary>
        /// Mount data results
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static IMountCommand GetCommandResult(IMountCommand command)
        {
            if (!IsRunning || _cts.IsCancellationRequested)
            {
                var e = new MountException(ErrorCode.ErrQueueFailed, "Queue not running");
                command.Exception = e;
                command.Successful = false;
                return command;
            }
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalMilliseconds < 22000)
            {
                if (_resultsDictionary == null) break;
                var success = _resultsDictionary.TryRemove(command.Id, out var result);
                if (success) return result;
                Thread.Sleep(1);
            }
            var ex = new MountException(ErrorCode.ErrQueueFailed, $"Queue Read Timeout {command.Id}, {command}");
            command.Exception = ex;
            command.Successful = false;
            return command;
        }

        /// <summary>
        /// Process command queue
        /// </summary>
        /// <param name="command"></param>
        private static void ProcessCommandQueue(IMountCommand command)
        {
            try
            {
                if (!IsRunning || _cts.IsCancellationRequested || !Actions.IsConnected) return;
                command.Execute(_actions);
                if (command.Id > 0)
                {
                    _resultsDictionary.TryAdd(command.Id, command);
                }
            }
            catch (Exception e)
            {
                command.Exception = e;
                command.Successful = false;
            }
        }

        public static void Start()
        {
            Stop();
            if (_cts == null) _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            _actions = new Actions();
            _actions.InitializeAxes();
            _resultsDictionary = new ConcurrentDictionary<long, IMountCommand>();
            _commandBlockingCollection = new BlockingCollection<IMountCommand>();
            IsRunning = true;

            Task.Factory.StartNew(() =>
            {
                while (!ct.IsCancellationRequested)
                {
                    foreach (var command in _commandBlockingCollection.GetConsumingEnumerable())
                    {
                        ProcessCommandQueue(command);
                    }
                }
            }, ct);

            //var task = Task.Run(() =>
            //{
            //    while (!ct.IsCancellationRequested)
            //    {
            //        foreach (var command in _commandBlockingCollection.GetConsumingEnumerable())
            //        {
            //            ProcessCommandQueue(command);
            //        }
            //    }
            //}, ct);
            //await task;
            //task.Wait(ct);

            IsRunning = true;
        }

        public static void Stop()
        {
            _actions?.Shutdown();
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            IsRunning = false;
            _resultsDictionary = null;
            _commandBlockingCollection = null;
        }

        #endregion

        /// <summary>
        /// called from the setter property.  property name is not required
        /// </summary>
        /// <param name="propertyName"></param>
        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }
    }
}
