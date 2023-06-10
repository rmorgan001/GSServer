using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
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

        public void DiscardInBuffer()
        {
            if (IsOpen)
            {
                var localState = _state;
                if (localState != null)
                {
                    localState.Received = null;
                }
            }
        }

        public void DiscardOutBuffer()
        {
            _state?.Cts?.Cancel(false);
        }

        public void Open() => Connect(_remoteEndpoint);

        public string ReadExisting() => _state?.Cts?.IsCancellationRequested == false && _state?.Received is string received ? received : string.Empty;

        public void Write(string data)
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
        }

        void EndSendCb(IAsyncResult result)
        {
            try
            {
                if (result.IsCompleted
                    && result.AsyncState is SendReceiveState state
                    && !state.Cts.IsCancellationRequested
                    && EndSend(result) > 0
                )
                {
                    BeginReceive(EndReceiveCb, state);
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Type = MonitorType.Error,
                    Category = MonitorCategory.Driver,
                    Datetime = DateTime.UtcNow,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Device = MonitorDevice.Server,
                    Message = $"SerialOverUdpPort|{_remoteEndpoint}|Send|{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }

        void EndReceiveCb(IAsyncResult result)
        {
            try
            {
                if (result.IsCompleted && result.AsyncState is SendReceiveState state && !state.Cts.IsCancellationRequested)
                {
                    IPEndPoint ep = null;
                    var bytes = EndReceive(result, ref ep);
                    if (bytes?.Length > 0)
                    {
                        var chars = new char[bytes.Length];
                        for (var i = 0; i < bytes.Length; i++)
                        {
                            chars[i] = (char)bytes[i];
                        }

                        state.Received = new string(chars);
                    }
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Type = MonitorType.Error,
                    Category = MonitorCategory.Driver,
                    Datetime = DateTime.UtcNow,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Device = MonitorDevice.Server,
                    Message = $"SerialOverUdpPort|{_remoteEndpoint}|Receive|{ex.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
            }
        }
    }

    class SendReceiveState
    {
        public SendReceiveState(int timeoutMS)
        {
            Cts = new CancellationTokenSource(timeoutMS);
        }

        public string Received { get; set; }

        public CancellationTokenSource Cts { get; }
    }
}

