using System;
using System.Net;

namespace GS.Shared.Transport
{
    /// <summary>
    /// Represents a device that supports the SkyWatcher serial protocol via a transport mechanism,
    /// currently supported is COM serial port (AKA EQMOD cable) and SkyWatcher WiFi, e.g. through the SkyWatcher WiFi adapter or
    /// a using a built-in WiFi, e.g. AZ GTI.
    /// </summary>
    public class Device : IEquatable<Device>
    {
        internal const int DefaultPort = 11880;

        public Device(long index, IPEndPoint endpoint = null)
        {
            if (endpoint == null && index <= 0)
            {
                throw new ArgumentException("Index must be positive for COM devices", nameof(index));
            }

            Index = index;
            Endpoint = endpoint;
            DiscoverTimeMs = Environment.TickCount;
        }

        /// <summary>
        /// Unique index (-1 or lower) used in device discovery (see <see cref="DiscoveryService.Discover"/>).
        /// Will be the COM port number (1 or greater) if this is a serial device.
        /// </summary>
        public long Index { get; }

        /// <summary>
        /// IP Address and port if device is a UDP device, <see langword="null"/> otherwise.
        /// </summary>
        public IPEndPoint Endpoint { get; }

        public long DiscoverTimeMs { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as Device;
            return Equals(other);
        }

        public override int GetHashCode() => Index.GetHashCode();

        public bool Equals(Device other) => Index == other?.Index;

        public string Name => ToString();

        public override string ToString()
        {
            if (Index > 0)
            {
                return $"COM{Index:d}";
            }

            if (Endpoint != null)
            {
                return Endpoint.Port == DefaultPort ? Endpoint.Address.ToString() : Endpoint.ToString();
            }
            return $"{Index} <INVALID>";
        }
    }
}