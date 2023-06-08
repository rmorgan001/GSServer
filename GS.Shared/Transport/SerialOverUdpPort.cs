using GS.Principles;
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

        public SerialOverUdpPort(IPEndPoint remoteEndpoint, TimeSpan readTimeout)
        {
            DontFragment = true;
            _remoteEndpoint = remoteEndpoint;
            ReadTimeout = (int)readTimeout.TotalMilliseconds;
        }

        public int ReadTimeout { get; }

        public bool IsOpen => Client.Connected;

        public void DiscardInBuffer()
        {
            if (IsOpen && Available > 0)
            {
                _ = ReadExisting();
            }
        }

        public void DiscardOutBuffer()
        {
            // we can't control the underlying buffer
        }

        public void Open()
        {
            Connect(_remoteEndpoint);
            Client.ReceiveTimeout = ReadTimeout;
            Client.SendTimeout = ReadTimeout;
        }

        public string ReadExisting()
        {
            IPEndPoint remoteEp = null;
            try
            {
                var bytes = Receive(ref remoteEp);
                if (bytes?.Length > 0)
                {
                    var chars = new char[bytes.Length];
                    for (var i = 0; i < bytes.Length; i++)
                    {
                        chars[i] = (char)bytes[i];
                    }
                    return new string(chars);
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Telescope,
                    Category = MonitorCategory.Driver,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod()?.Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}|{ex.InnerException.Message}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                return string.Empty;
            }
        }

        public void Write(string data)
        {
            var bytes = new byte[data.Length];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)data[i];
            }
            _ = Send(bytes, bytes.Length);
        }
    }
}

