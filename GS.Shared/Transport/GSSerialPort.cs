using System;
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

    public class GSSerialPort : ISerialPort
    {
        private readonly SerialPort _serialPort;

        public GSSerialPort(
            string portName,
            int baudRate,
            TimeSpan readTmeout,
            Handshake handshake,
            Parity parity,
            StopBits stopBits,
            int dataBits,
            SerialOptions options
        )
        {
            _serialPort = new SerialPort
            {
                Encoding = System.Text.Encoding.ASCII,
                PortName = portName,
                BaudRate = baudRate,
                ReadTimeout = (int)(ReadTimeout = readTmeout).TotalMilliseconds,
                StopBits = stopBits,
                DataBits = dataBits,
                DtrEnable = options.HasFlag(SerialOptions.DtrEnable),
                RtsEnable = options.HasFlag(SerialOptions.RtsEnable),
                Handshake = handshake,
                Parity = parity,
                DiscardNull = options.HasFlag(SerialOptions.DiscardNull),
            };
        }

        public bool IsOpen => _serialPort.IsOpen;

        public TimeSpan ReadTimeout { get; }

        public void Open() => _serialPort.Open();

        public void Dispose()
        {
            _serialPort.Close();
            GC.SuppressFinalize(this);
        }

        public string ReadExisting() => _serialPort.ReadExisting();

        public void Write(string data) => _serialPort.Write(data);

        public void DiscardInBuffer() => _serialPort.DiscardInBuffer();

        public void DiscardOutBuffer() => _serialPort.DiscardOutBuffer();
    }
}

