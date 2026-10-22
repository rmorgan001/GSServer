using System;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace GS.Shared.Transport
{
    [Flags]
    public enum SerialOptions
    {
        None = 0,
        DtrEnable = 1,
        RtsEnable = 1 << 1,
        DiscardNull = 1 << 2
    }

    public sealed class GsSerialPort : SerialPort, ISerialPort
    {
        public GsSerialPort(
            string portName,
            int baudRate,
            TimeSpan readTimeout,
            Handshake handshake,
            Parity parity,
            StopBits stopBits,
            int dataBits,
            SerialOptions options
        )
        {
            Encoding = System.Text.Encoding.ASCII;
            PortName = portName;
            BaudRate = baudRate;
            ReadTimeout = (int)readTimeout.TotalMilliseconds;
            WriteTimeout = ReadTimeout;
            StopBits = stopBits;
            DataBits = dataBits;
            Handshake = handshake;
            Parity = parity;
            DtrEnable = options.HasFlag(SerialOptions.DtrEnable);
            RtsEnable = options.HasFlag(SerialOptions.RtsEnable);
            DiscardNull = options.HasFlag(SerialOptions.DiscardNull);
        }

        /// <summary>
        /// Writes to the port. On a recoverable failure the port is closed and re-opened once and the write retried.
        /// If recovery fails the original exception is thrown.
        /// </summary>
        public new void Write(string data)
        {
            try
            {
                base.Write(data);
                return;
            }
            catch (Exception ex) when (IsRecoverable(ex))
            {
                LogToMonitor(MonitorType.Warning, $"{PortName}|Write failed, attempting recovery|{ex.GetType().Name}|{ex.Message}");
                try
                {
                    Reopen();
                    base.Write(data);
                    LogToMonitor(MonitorType.Information, $"{PortName}|Write recovered after port reopen");
                    return;
                }
                catch (Exception recoveryEx)
                {
                    LogToMonitor(MonitorType.Error, $"{PortName}|Write recovery failed|{recoveryEx.GetType().Name}|{recoveryEx.Message}");
                }

                throw;
            }
        }

        // Argument errors and a disposed port will not be fixed by reopening
        private static bool IsRecoverable(Exception ex)
        {
            return !(ex is ObjectDisposedException)
                   && (ex is IOException
                       || ex is InvalidOperationException
                       || ex is TimeoutException
                       || ex is UnauthorizedAccessException);
        }

        /// <summary>
        /// Closes and reopens the serial port, then discards pending input and output data.
        /// </summary>
        private void Reopen()
        {
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                // a dead device can fail to close, the open below decides if recovery worked
                LogToMonitor(MonitorType.Warning, $"{PortName}|Close failed during recovery|{ex.GetType().Name}|{ex.Message}");
            }

            Thread.Sleep(100); // let the driver release the handle
            Open();
            DiscardInBuffer();
            DiscardOutBuffer();
        }

        private void LogToMonitor(MonitorType type, string message)
        {
            var monitorItem = new MonitorEntry
            {
                Type = type,
                Category = MonitorCategory.Other,
                Datetime = DateTime.UtcNow,
                Method = nameof(Write),
                Thread = Thread.CurrentThread.ManagedThreadId,
                Device = MonitorDevice.Server,
                Message = message
            };
            MonitorLog.LogToMonitor(monitorItem);
        }
    }
}

