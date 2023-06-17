using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace GS.Shared.Transport
{
    /// <summary>
    /// General discovery process:
    /// <list type="bullet">
    ///   <item>Discovers all COM serial ports.</item>
    ///   <item>Sends <c>:e1\r</c> to all WiFi broadcast addresses (no NAT traversal).</item>
    /// </list>
    /// </summary>
    public sealed class DiscoveryService : IDiscoveryService
    {
        const int DiscoveryIntervalMs = 2000;

        private readonly byte[] DiscoverMsg = Encode(":e1\r");

        static byte[] Encode(string msg) => Encoding.ASCII.GetBytes(msg);
        static string Decode(byte[] msg) => msg != null ? Encoding.ASCII.GetString(msg).Replace("\0", "").Trim() : "";

        private readonly ConcurrentDictionary<IPAddress, Lazy<UdpClient>> _udpClients;
        private readonly DispatcherTimer _timer;
        private readonly int _remotePort;
        private readonly TimeSpan _broadcastTimeout;

        private CancellationTokenSource _cts;
        private bool disposedValue;

        public DiscoveryService(int remotePort = Device.DefaultPort, int discoveryIntervalMs = DiscoveryIntervalMs)
        {
            _udpClients = new ConcurrentDictionary<IPAddress, Lazy<UdpClient>>();
            _remotePort = remotePort;
            DiscoveryInterval = TimeSpan.FromMilliseconds(discoveryIntervalMs);
            _broadcastTimeout =  TimeSpan.FromMilliseconds(Math.Max(discoveryIntervalMs - 200, 200));
            _timer = new DispatcherTimer
            {
                Interval = DiscoveryInterval
            };
            _timer.Tick += TimerTick;
        }

        private void TimerTick(object sender, EventArgs e) => Discover();

        public event EventHandler<DiscoveryEventArgs> DiscoveredDeviceEvent;

        //public event EventHandler<DiscoveryEventArgs> RemovedDeviceEvent;

        public TimeSpan DiscoveryInterval { get; }

        public bool Wifi { get; set; }

        /// <inheritdoc/>
        public void StartAutoDiscovery()
        {
            if (_timer.IsEnabled)
            {
                return;
            }
            _timer.Start();
        }

        /// <inheritdoc/>
        public void StopAutoDiscovery()
        {
            _cts?.Cancel();
            _timer.Stop();

            var monitorItem = new MonitorEntry
            {
                Type = MonitorType.Information,
                Category = MonitorCategory.Server,
                Datetime = DateTime.UtcNow,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Device = MonitorDevice.Server,
                Message = "AutoDiscovery|Stop"
            };
            MonitorLog.LogToMonitor(monitorItem);
        }

        /// <summary>
        /// Step by step discovery process:
        /// <list type="number">
        ///   <item>Initializes UDP clients for broadcast, one per network interface</item>
        ///   <item>Discovers all COM serial ports (synchronously)</item>
        ///   <item>Broadcasts to all active network interfaces and listens for responses</item>
        /// </list>
        /// </summary>
        private void Discover()
        {
            var monitorItem = new MonitorEntry
            {
                Type = MonitorType.Information,
                Category = MonitorCategory.Server,
                Datetime = DateTime.UtcNow,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Device = MonitorDevice.Server,
                Message = "Discovery|Started"
            };
            MonitorLog.LogToMonitor(monitorItem);

            if (Wifi)
            {
                InitializeUdpClients();
                //DiscoverSerialDevices();
                BroadcastDiscoverMessage();
            }
            DiscoverSerialDevices();

        }

        private void DiscoverSerialDevices()
        {
            var allPorts = SerialPort.GetPortNames();
            var portNumbers = new SortedSet<long>();
            foreach (var port in allPorts)
            {
                if (string.IsNullOrEmpty(port)) continue;
                var portNumber = Strings.GetNumberFromString(port);
                if (portNumber >= 1)
                {
                    portNumbers.Add(portNumber.Value);
                }
            }

            var serialDevices = portNumbers.Select(portNumber => new Device(portNumber)).ToList();

            DiscoveredDeviceEvent?.Invoke(this, new DiscoveryEventArgs(serialDevices, isSynchronous: true));
        }

        private void BroadcastDiscoverMessage()
        {
            var oldCts = Interlocked.Exchange(ref _cts, new CancellationTokenSource(_broadcastTimeout));
            oldCts?.Cancel();

            var broadCastIP = new IPEndPoint(IPAddress.Broadcast, _remotePort);

            Parallel.ForEach(
                _udpClients,
                kv => kv.Value.Value.BeginSend(DiscoverMsg, DiscoverMsg.Length, broadCastIP, EndSendCb, new DiscoveryState(kv.Key, _cts))
            );
        }

        /// <summary>
        /// Enumerates all WiFi interfaces that are up and creates <see cref="UdpClient"/>s for each.
        /// Also disposes of any clients that are bound to interfaces that are down.
        /// </summary>
        private void InitializeUdpClients()
        {
            var networkIfaceIps = new SortedSet<IPAddress>(
                from ni in NetworkInterface.GetAllNetworkInterfaces()
                where ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                    && ni.OperationalStatus == OperationalStatus.Up
                    && !ni.IsReceiveOnly
                from ip in ni.GetIPProperties().UnicastAddresses
                where ip.Address.AddressFamily == AddressFamily.InterNetwork
                select ip.Address
            );

            var monitorItem = new MonitorEntry
            {
                Type = MonitorType.Data,
                Category = MonitorCategory.Server,
                Datetime = DateTime.UtcNow,
                Method = MethodBase.GetCurrentMethod()?.Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Device = MonitorDevice.Server,
                Message = $"Discovery|Network Interfaces|{string.Join(",", networkIfaceIps)}"
            };
            MonitorLog.LogToMonitor(monitorItem);

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

        private void EndSendCb(IAsyncResult sendRes)
        {
            var state = sendRes.AsyncState as DiscoveryState;
            var sender = state?.InterfaceAddress;
            if (sendRes.IsCompleted && sender != null && !state.Cts.IsCancellationRequested && _udpClients.TryGetValue(sender, out var updClient))
            {
                _ = updClient.Value.EndSend(sendRes);
                updClient.Value.BeginReceive(BeginReceiveEP1Cb, state);
            }
        }

        private void BeginReceiveEP1Cb(IAsyncResult receiveRes)
        {
            var state = receiveRes.AsyncState as DiscoveryState;
            var sender = state?.InterfaceAddress;
            if (receiveRes.IsCompleted && sender != null && !state.Cts.IsCancellationRequested && _udpClients.TryGetValue(sender, out var updClient))
            {
                IPEndPoint remoteEP = null;
                var response = Decode(updClient.Value.EndReceive(receiveRes, ref remoteEP));
                if (remoteEP != null && IsSuccessfulResponse(response))
                {
                    OnUdpDeviceDiscovery(remoteEP);
                }
            }
        }

        private void OnUdpDeviceDiscovery(IPEndPoint remoteEP)
        {
            var deviceIndex = GetDeviceIndex(remoteEP);

            DiscoveredDeviceEvent?.Invoke(this, new DiscoveryEventArgs(new[] { new Device(deviceIndex, remoteEP) }));
        }

        private static bool IsSuccessfulResponse(string response) => response?.Length > 2 && response[0] == '=';

        private void Dispose(bool disposing)
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

                _cts?.Cancel();
                _timer.Stop();

                disposedValue =true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            // ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor
            GC.SuppressFinalize(this);
        }

        private static long GetDeviceIndex(IPEndPoint endpoint)
        {
            var id = 0xFF & (long)endpoint.AddressFamily;
            id <<= 32 + 16;
            id |= (uint)(0xff & endpoint.Port);
            id <<= 32;
            if (endpoint.AddressFamily == AddressFamily.InterNetwork)
            {
                id |= (uint)BitConverter.ToInt32(endpoint.Address.GetAddressBytes(), 0);
            }
            else
            {
                throw new NotSupportedException($"Address family {endpoint.AddressFamily} is not supported");
            }

            return -Math.Abs(id);
        }
    }

    class DiscoveryState
    {
        public DiscoveryState(IPAddress interfaceAddress, CancellationTokenSource cts)
        {
            InterfaceAddress = interfaceAddress;
            Cts = cts;
        }

        public IPAddress InterfaceAddress { get; }

        public CancellationTokenSource Cts { get; }
    }
}

