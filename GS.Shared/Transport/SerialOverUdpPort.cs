using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;

namespace GS.Shared.Transport
{
    public sealed class SerialOverUdpPort : UdpClient, ISerialPort
    {
        private readonly IPEndPoint _remoteEndpoint;
        private SendReceiveState _state;

        public SerialOverUdpPort(IPEndPoint remoteEndpoint, TimeSpan readTimeout)
        {
            _remoteEndpoint = remoteEndpoint;
            ReadTimeout = (int)readTimeout.TotalMilliseconds;
        }
        public int ReadTimeout { get; }
        public bool IsOpen => Client.Connected;
        public void DiscardInBuffer(){}
        public void DiscardOutBuffer(){}
        public void Open()
        {
            try
            {
                Connect(_remoteEndpoint);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Type = MonitorType.Error,
                    Category = MonitorCategory.Other,
                    Datetime = DateTime.UtcNow,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Device = MonitorDevice.Server,
                    Message = $"{_remoteEndpoint}|{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                throw;
            }
        }
        public string ReadExisting()
        {
            var a = _state?.Cts?.IsCancellationRequested == false && _state?.Received is string received
                ? received
                : string.Empty;
            //Debug.WriteLine("Read: " + _state?.Received);
            return a;
        }
        public void Write(string data)
        {
            try
            {
                var bytes = new byte[data.Length];
                for (var i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = (byte)data[i];
                }

                var newState = new SendReceiveState(ReadTimeout);
                var prevState = Interlocked.Exchange(ref _state, newState);
                prevState?.Cts?.Cancel(false);

                _ = BeginSend(bytes, bytes.Length, EndSendCb, newState);
                // Debug.WriteLine("Cmd: " + Encoding.Default.GetString(bytes));
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Type = MonitorType.Error,
                    Category = MonitorCategory.Other,
                    Datetime = DateTime.UtcNow,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Device = MonitorDevice.Server,
                    Message = $"|{_remoteEndpoint}|Error|{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                throw;
            }
        }
        private void EndSendCb(IAsyncResult result)
        {
            try
            {
                if (result.IsCompleted
                    && result.AsyncState is SendReceiveState state
                    && EndSend(result) > 0
                )
                {
                    _ = BeginReceive(EndReceiveCb, state);
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Type = MonitorType.Error,
                    Category = MonitorCategory.Other,
                    Datetime = DateTime.UtcNow,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Device = MonitorDevice.Server,
                    Message = $"SerialOverUdpPort|{_remoteEndpoint}|Send|{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }
        private void EndReceiveCb(IAsyncResult result)
        {
            try
            {
                if (result == null){return;}
                if (!result.IsCompleted){return;}
                IPEndPoint ep = null;
                var bytes = EndReceive(result, ref ep);
                if (!(bytes?.Length > 0)){return;}
                _state.Received = Encoding.Default.GetString(bytes);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Type = MonitorType.Error,
                    Category = MonitorCategory.Other,
                    Datetime = DateTime.UtcNow,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Device = MonitorDevice.Server,
                    Message = $"SerialOverUdpPort|{_remoteEndpoint}|Receive|{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                Client?.Close();
            }
        }
    }

    internal class SendReceiveState
    {
        public SendReceiveState(int timeoutMs)
        {
            Cts = new CancellationTokenSource(timeoutMs);
        }
        public string Received { get; set; }
        public CancellationTokenSource Cts { get; }
    }
}

