using System;
using System.IO.Ports;

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

    public sealed class GSSerialPort : SerialPort, ISerialPort
    {
        public GSSerialPort(
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
            StopBits = stopBits;
            DataBits = dataBits;
            Handshake = handshake;
            Parity = parity;
            DtrEnable = options.HasFlag(SerialOptions.DtrEnable);
            RtsEnable = options.HasFlag(SerialOptions.RtsEnable);
            DiscardNull = options.HasFlag(SerialOptions.DiscardNull);
        }
    }
}

