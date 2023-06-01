using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GS.Shared.Transport
{
    public class SerialUdpPortDiscoveryService : ISerialUdpPortDiscoveryService
    {
        private readonly byte[] DiscoverMsg = Encode(":e1\r");

        static byte[] Encode(string msg) => Encoding.ASCII.GetBytes(msg);
        static string Decode(byte[] msg) => msg != null ? Encoding.ASCII.GetString(msg).Replace("\0", "").Trim() : "";

        private readonly ConcurrentDictionary<IPAddress, Lazy<UdpClient>> _udpClients;
        private readonly ConcurrentDictionary<IPEndPoint, int> _dirtyDevices;
        private readonly ConcurrentDictionary<IPEndPoint, int> _activeDevices;
        private readonly ConcurrentDictionary<int, RemoteDevice> _allDevices;
        private readonly int _remotePort;

        private volatile int _deviceIndex = 0;
        private bool disposedValue;

        public SerialUdpPortDiscoveryService(int remotePort)
        {
            _udpClients = new ConcurrentDictionary<IPAddress, Lazy<UdpClient>>();
            _dirtyDevices = new ConcurrentDictionary<IPEndPoint, int>();
            _activeDevices = new ConcurrentDictionary<IPEndPoint, int>();
            _allDevices = new ConcurrentDictionary<int, RemoteDevice>();
            _remotePort = remotePort;
        }

        public IEnumerable<RemoteDevice> ActiveDevices
        {
            get
            {
                foreach (var activeDeviceId in _activeDevices.Values)
                {
                    if (_allDevices.TryGetValue(activeDeviceId, out var device))
                    {
                        yield return device;
                    }
                }
            }
        }

        public event EventHandler<DiscoveryEventArgs> DiscoveredDeviceEvent;

        public event EventHandler<DiscoveryEventArgs> RemovedDeviceEvent;

        /// <summary>
        /// Sends <c>:e1\r</c> to all WiFi broadcast addresses (no NAT traversal).
        /// <list type="number">
        ///   <item>Initializes UDP clients for broadcast, one per network interface</item>
        ///   <item>Removes all devices that where discovered previously but did not respond on last invocation of this method</item>
        ///   <item>Marks all currently active devices as dirty</item>
        ///   <item>Broadcasts and listens for responses</item>
        /// </list>
        /// </summary>
        public void Discover()
        {
            InitializeUdpClients();
            CleanupDirtyDevices();
            MarkPreviouslyActiveDevicesAsDirty();
            BroadcastDiscoverMessage();
        }

        void BroadcastDiscoverMessage()
        {
            var broadCastIP = new IPEndPoint(IPAddress.Broadcast, _remotePort);

            Parallel.ForEach(
                _udpClients,
                kv => kv.Value.Value.BeginSend(DiscoverMsg, DiscoverMsg.Length, broadCastIP, EndSendCb, kv.Key)
            );
        }

        /// <summary>
        /// Enumerates all WiFi interfaces that are up and creates <see cref="UdpClient"/>s for each.
        /// Also disposes of any clients that are bound to interfaces that are down.
        /// </summary>
        void InitializeUdpClients()
        {
            var networkIfaceIps = new HashSet<IPAddress>(
                from ni in NetworkInterface.GetAllNetworkInterfaces()
                where ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                    && ni.OperationalStatus == OperationalStatus.Up
                    && !ni.IsReceiveOnly
                from ip in ni.GetIPProperties().UnicastAddresses
                where ip.Address.AddressFamily == AddressFamily.InterNetwork
                select ip.Address
            );

            var needRemoving = new HashSet<IPAddress>(_udpClients.Keys);
            needRemoving.ExceptWith(networkIfaceIps);
            foreach (var toBeRemoved in needRemoving)
            {
                if (_udpClients.TryRemove(toBeRemoved, out var client) && client.IsValueCreated)
                {
                    client.Value.Dispose();
                }
            }

            foreach (var toAdd in networkIfaceIps)
            {
                _ = _udpClients.AddOrUpdate(
                    toAdd,
                    ip => new Lazy<UdpClient>(() => new UdpClient(new IPEndPoint(ip, 0))
                    {
                        EnableBroadcast = true,
                        DontFragment = true
                    }, LazyThreadSafetyMode.ExecutionAndPublication),
                    (_, existing) => existing
                );
            }
        }

        /// <summary>
        /// Removes all devices that are currently dirty (as they have not been fully discovered last run).
        /// </summary>
        void CleanupDirtyDevices()
        {
            foreach (var ep in _dirtyDevices.Keys)
            {
                if (_activeDevices.TryRemove(ep, out var deviceIndex) && _allDevices.TryRemove(deviceIndex, out var device))
                {
                    RemovedDeviceEvent?.Invoke(this, new DiscoveryEventArgs(device));
                }
            }
        }

        /// <summary>
        /// Move currently active devices to the dirty list, and only if they are rediscovered move them back.
        /// The remaining dirty devices will be cleaned up on <see cref="Dispose"/> or on <see cref="Discover"/>,
        /// whichever comes first.
        /// </summary>
        void MarkPreviouslyActiveDevicesAsDirty()
        {
            foreach (var ep in _activeDevices.Keys)
            {
                if (_activeDevices.TryRemove(ep, out var device))
                {
                    _dirtyDevices.AddOrUpdate(ep, device, (_, old) => device);
                }
            }
        }

        void EndSendCb(IAsyncResult sendRes)
        {
            var sender = sendRes.AsyncState as IPAddress;
            if (sendRes.IsCompleted && sender != null && _udpClients.TryGetValue(sender, out var updClient))
            {
                _ = updClient.Value.EndSend(sendRes);
                updClient.Value.BeginReceive(BeginReceiveEP1Cb, sender);
            }
        }

        void BeginReceiveEP1Cb(IAsyncResult receiveRes)
        {
            var sender = receiveRes.AsyncState as IPAddress;
            if (receiveRes.IsCompleted && sender != null && _udpClients.TryGetValue(sender, out var updClient))
            {
                IPEndPoint remoteEP = null;
                var response = Decode(updClient.Value.EndReceive(receiveRes, ref remoteEP));
                if (remoteEP != null && IsSuccessfulResponse(response))
                {
                    OnDeviceDiscovery(remoteEP);
                }
            }
        }

        void OnDeviceDiscovery(IPEndPoint remoteEP)
        {
            int deviceId;
            if (_dirtyDevices.TryRemove(remoteEP, out var dirtyDeviceId))
            {
                deviceId = _activeDevices.GetOrAdd(remoteEP, dirtyDeviceId);
            }
            else if (_activeDevices.TryGetValue(remoteEP, out var activeDeviceId))
            {
                deviceId = activeDeviceId;
            }
            else
            {
                deviceId = _activeDevices.GetOrAdd(remoteEP, _ => Interlocked.Increment(ref _deviceIndex));
            }

            var dsicoveredDevice = _allDevices.GetOrAdd(deviceId, new RemoteDevice(deviceId, remoteEP));

            DiscoveredDeviceEvent?.Invoke(this, new DiscoveryEventArgs(dsicoveredDevice));
        }

        static bool IsSuccessfulResponse(string response) => response?.Length > 2 && response[0] == '=';

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var kv in _udpClients)
                    {
                        if (_udpClients.TryRemove(kv.Key, out var client) && client.IsValueCreated)
                        {
                            client.Value.Close();
                        }
                    }
                }

                _dirtyDevices.Clear();
                _activeDevices.Clear();
                _allDevices.Clear();

                disposedValue =true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

