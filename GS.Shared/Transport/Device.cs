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

        public Device(int index, IPEndPoint endpoint = null)
        {
            if (endpoint == null && index <= 0)
            {
                throw new ArgumentException("Index must be positive for COM devices", nameof(index));
            }

            Index = index;
            Endpoint = endpoint;
        }

        /// <summary>
        /// Unique index (-1 or lower) used in device discovery (see <see cref="DiscoveryService.Discover"/>).
        /// Will be the COM port number (1 or greater) if this is a serial device.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// IP Address and port if device is a UDP device, <see langword="null"/> otherwise.
        /// </summary>
        public IPEndPoint Endpoint { get; }

        public override bool Equals(object obj)
        {
            var other = obj as Device;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 1009;
                if (Index > 0)
                {
                    hash = (hash * 9176) + Index.GetHashCode();
                }
                else if (Endpoint != null)
                {
                    hash = (hash * 9176) + Endpoint.GetHashCode();
                }
                return hash;
            }
        }

        public bool Equals(Device other) => Index > 0 ? Index == other.Index : Endpoint.Equals(other.Endpoint);

        public override string ToString()
        {
            if (Index > 0)
            {
                return string.Format("COM{0:d}", Index);
            }
            else if (Endpoint != null)
            {
                if (Endpoint.Port == DefaultPort)
                {
                    return Endpoint.Address.ToString();
                }
                else
                {
                    return Endpoint.ToString();
                }
            }
            else
            {
                return $"{Index} <INVALID>";
            }
        }
    }
}