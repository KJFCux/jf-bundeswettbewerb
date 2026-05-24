using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BWB_Auswertung.IO;
using BWB_Auswertung.Models;

namespace BWB_Auswertung.Network
{
    public class PeerDiscovery : IDisposable
    {
        private readonly int discoveryPort;
        private readonly int syncPort;
        private readonly string veranstaltungstitel;
        private readonly string instanzId = Guid.NewGuid().ToString("N");
        private readonly string hostname = Environment.MachineName;

        private UdpClient? sendClient;
        private UdpClient? receiveClient;
        private CancellationTokenSource? cts;
        private Task? broadcastTask;
        private Task? listenTask;

        public ObservableCollection<Peer> Peers { get; } = new ObservableCollection<Peer>();

        public PeerDiscovery(int discoveryPort, int syncPort, string veranstaltungstitel)
        {
            this.discoveryPort = discoveryPort;
            this.syncPort = syncPort;
            this.veranstaltungstitel = veranstaltungstitel ?? string.Empty;
        }

        public void Start()
        {
            if (cts != null) return;
            cts = new CancellationTokenSource();

            try
            {
                receiveClient = new UdpClient();
                receiveClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                receiveClient.Client.Bind(new IPEndPoint(IPAddress.Any, discoveryPort));
                receiveClient.EnableBroadcast = true;

                sendClient = new UdpClient();
                sendClient.EnableBroadcast = true;

                broadcastTask = Task.Run(() => BroadcastLoop(cts.Token));
                listenTask = Task.Run(() => ListenLoop(cts.Token));
            }
            catch (Exception ex)
            {
                LOGGING.Write($"PeerDiscovery.Start: {ex.Message}", nameof(PeerDiscovery), EventLogEntryType.Error);
            }
        }

        public void Stop()
        {
            try
            {
                cts?.Cancel();
                sendClient?.Close();
                receiveClient?.Close();
            }
            catch { /* ignore */ }
            finally
            {
                sendClient = null;
                receiveClient = null;
                cts = null;
            }
        }

        private async Task BroadcastLoop(CancellationToken token)
        {
            var hello = new HelloMessage
            {
                Hostname = hostname,
                SyncPort = syncPort,
                Veranstaltungstitel = veranstaltungstitel,
                InstanzId = instanzId
            };

            while (!token.IsCancellationRequested)
            {
                try
                {
                    string json = JsonSerializer.Serialize(hello);
                    byte[] data = Encoding.UTF8.GetBytes(json);

                    foreach (var addr in GetBroadcastAddresses())
                    {
                        try
                        {
                            await sendClient!.SendAsync(data, data.Length, new IPEndPoint(addr, discoveryPort)).ConfigureAwait(false);
                        }
                        catch { /* einzelne Schnittstelle ignorieren */ }
                    }
                }
                catch (Exception ex)
                {
                    LOGGING.Write($"PeerDiscovery.Broadcast: {ex.Message}", nameof(PeerDiscovery), EventLogEntryType.Warning);
                }

                try { await Task.Delay(3000, token).ConfigureAwait(false); }
                catch (TaskCanceledException) { break; }
            }
        }

        private async Task ListenLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult result = await receiveClient!.ReceiveAsync().ConfigureAwait(false);
                    string json = Encoding.UTF8.GetString(result.Buffer);
                    HelloMessage? msg = JsonSerializer.Deserialize<HelloMessage>(json);
                    if (msg == null || msg.Magic != DiscoveryProtocol.HelloMagic) continue;

                    //Eigene Pakete ignorieren
                    if (msg.InstanzId == instanzId) continue;

                    string ip = result.RemoteEndPoint.Address.ToString();
                    UpdatePeer(msg, ip);
                }
                catch (ObjectDisposedException) { break; }
                catch (SocketException) { break; }
                catch (Exception ex)
                {
                    LOGGING.Write($"PeerDiscovery.Listen: {ex.Message}", nameof(PeerDiscovery), EventLogEntryType.Warning);
                }
            }
        }

        private void UpdatePeer(HelloMessage msg, string ip)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var existing = Peers.FirstOrDefault(p => p.IpAddress == ip && p.SyncPort == msg.SyncPort);
                if (existing == null)
                {
                    Peers.Add(new Peer
                    {
                        Hostname = msg.Hostname,
                        IpAddress = ip,
                        SyncPort = msg.SyncPort,
                        Veranstaltungstitel = msg.Veranstaltungstitel,
                        LastSeen = DateTime.Now
                    });
                }
                else
                {
                    existing.Hostname = msg.Hostname;
                    existing.Veranstaltungstitel = msg.Veranstaltungstitel;
                    existing.LastSeen = DateTime.Now;
                }

                //Veraltete Peers (>15s) entfernen
                var stale = Peers.Where(p => (DateTime.Now - p.LastSeen).TotalSeconds > 15).ToList();
                foreach (var s in stale) Peers.Remove(s);
            });
        }

        private static IEnumerable<IPAddress> GetBroadcastAddresses()
        {
            var result = new List<IPAddress> { IPAddress.Broadcast };
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up) continue;
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                    foreach (var ua in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        if (ua.IPv4Mask == null) continue;
                        byte[] ip = ua.Address.GetAddressBytes();
                        byte[] mask = ua.IPv4Mask.GetAddressBytes();
                        if (ip.Length != 4 || mask.Length != 4) continue;
                        byte[] bc = new byte[4];
                        for (int i = 0; i < 4; i++) bc[i] = (byte)(ip[i] | ~mask[i]);
                        result.Add(new IPAddress(bc));
                    }
                }
            }
            catch { /* fallback bleibt 255.255.255.255 */ }
            return result.Distinct();
        }

        public void Dispose() => Stop();
    }
}
