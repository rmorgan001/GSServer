using System;
using System.Net;
using System.Net.Sockets;
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

        public string ReadExisting() => _state?.Received ?? string.Empty;

        public void Write(string data)
        {
            var bytes = new byte[data.Length];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)data[i];
            }

            var newState = new SendReceiveState();
            var prevState = Interlocked.Exchange(ref _state, newState);
            prevState?.Cts?.Cancel(false);

            _ = BeginSend(bytes, bytes.Length, EndSendCb, newState);
        }

        void EndSendCb(IAsyncResult result)
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

        void EndReceiveCb(IAsyncResult result)
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
    }

    class SendReceiveState
    {
        public string Received { get; set; }

        public CancellationTokenSource Cts { get; } = new CancellationTokenSource();
    }
}

