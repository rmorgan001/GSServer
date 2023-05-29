using System;

namespace GS.Shared.Transport
{
    public interface ISerialPort : IDisposable
    {
        TimeSpan ReadTimeout { get; }

        bool IsOpen { get; }

        void Open();

        void Write(string data);

        string ReadExisting();

        void DiscardInBuffer();

        void DiscardOutBuffer();
    }
}