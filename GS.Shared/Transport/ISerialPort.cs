using System;

namespace GS.Shared.Transport
{
    public interface ISerialPort : IDisposable
    {
        /// <summary>
        /// Read timeout in milliseconds
        /// </summary>
        int ReadTimeout { get; }

        bool IsOpen { get; }

        void Open();

        void Write(string data);

        string ReadExisting();

        void DiscardInBuffer();

        void DiscardOutBuffer();
    }
}