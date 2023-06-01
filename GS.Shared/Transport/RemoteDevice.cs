using System;
using System.Net;
using System.Text;

namespace GS.Shared.Transport
{
    /// <summary>
    /// Represents a remote-addressable device that supports the SkyWatcher serial protocol,
    /// e.g. through the SkyWatcher WiFi adapter or a using a built-in WiFi, e.g. AZ GTI.
    /// </summary>
    public class RemoteDevice : IEquatable<RemoteDevice>
    {
        public RemoteDevice(int index, IPEndPoint endpoint)
        {
            Index = index;
            Endpoint = endpoint;
        }

        /// <summary>
        /// Unique index used in device discovery (see <see cref="SerialUdpPortDiscoveryService"/>).
        /// </summary>
        public int Index { get; }

        public IPEndPoint Endpoint { get; }

        public override bool Equals(object obj)
        {
            var other = obj as RemoteDevice;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 1009;
                hash = (hash * 9176) + Endpoint.GetHashCode();
                return hash;
            }
        }

        public bool Equals(RemoteDevice other) => Endpoint.Equals(other.Endpoint);

        public override string ToString()
            => new StringBuilder()
                .Append(Index)
                .Append(": ")
                .Append(Endpoint)
                .ToString();
    }
}