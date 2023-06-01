using System;
using System.Net;
using System.Net.Sockets;

namespace GS.Shared.Transport
{
    public class SerialOverUdpPort : ISerialPort
    {
        private readonly UdpClient _udpClient;
        private readonly IPEndPoint _remoteEndpoint;

        public SerialOverUdpPort(IPEndPoint remoteEndpoint, TimeSpan readTimeout)
        {
            _udpClient = new UdpClient
            {
                DontFragment = true
            };
            _remoteEndpoint = remoteEndpoint;
            ReadTimeout = readTimeout;
        }

        public TimeSpan ReadTimeout { get; }

        public bool IsOpen => _udpClient.Client.Connected;

        public void DiscardInBuffer()
        {
            if (_udpClient.Available > 0)
            {
                _ = ReadExisting();
            }
        }

        public void DiscardOutBuffer()
        {
            // we can't control the underlying buffer
        }

        public void Dispose()
        {
            _udpClient.Close();
            GC.SuppressFinalize(this);
        }

        public void Open()
        {
            _udpClient.Connect(_remoteEndpoint);
            var timeoutMs = (int)ReadTimeout.TotalMilliseconds;
            _udpClient.Client.ReceiveTimeout = timeoutMs;
            _udpClient.Client.SendTimeout = timeoutMs;
        }

        public string ReadExisting()
        {
            IPEndPoint remoteEp = null;
            var bytes = _udpClient.Receive(ref remoteEp);
            var chars = new char[bytes.Length];
            for (var i = 0; i < bytes.Length; i++)
            {
                chars[i] = (char)bytes[i];
            }
            return new string(chars);
        }

        public void Write(string data)
        {
            var bytes = new byte[data.Length];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)data[i];
            }
            _ = _udpClient.Send(bytes, bytes.Length);
        }
    }
}

