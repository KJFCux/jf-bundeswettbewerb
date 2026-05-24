using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BWB_Auswertung.IO;
using BWB_Auswertung.Models;

namespace BWB_Auswertung.Network
{
    public class SyncServer : IDisposable
    {
        private readonly int port;
        private readonly Func<IEnumerable<Gruppe>> gruppenProvider;
        private readonly Func<string> veranstaltungstitelProvider;
        private TcpListener? listener;
        private CancellationTokenSource? cts;

        public SyncServer(int port, Func<IEnumerable<Gruppe>> gruppenProvider, Func<string> veranstaltungstitelProvider)
        {
            this.port = port;
            this.gruppenProvider = gruppenProvider;
            this.veranstaltungstitelProvider = veranstaltungstitelProvider;
        }

        public void Start()
        {
            if (listener != null) return;
            try
            {
                cts = new CancellationTokenSource();
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                _ = Task.Run(() => AcceptLoop(cts.Token));
            }
            catch (Exception ex)
            {
                LOGGING.Write($"SyncServer.Start: {ex.Message}", nameof(SyncServer), EventLogEntryType.Error);
            }
        }

        public void Stop()
        {
            try
            {
                cts?.Cancel();
                listener?.Stop();
            }
            catch { /* ignore */ }
            finally
            {
                listener = null;
                cts = null;
            }
        }

        private async Task AcceptLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await listener!.AcceptTcpClientAsync().ConfigureAwait(false);
                    _ = Task.Run(() => HandleClient(client));
                }
                catch (ObjectDisposedException) { break; }
                catch (SocketException) { break; }
                catch (Exception ex)
                {
                    LOGGING.Write($"SyncServer.Accept: {ex.Message}", nameof(SyncServer), EventLogEntryType.Warning);
                }
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            NetworkStream? stream = null;
            SyncResponse response = new SyncResponse
            {
                Hostname = Environment.MachineName,
                Zeitpunkt = DateTime.Now
            };

            try
            {
                client.ReceiveTimeout = 30000;
                client.SendTimeout = 30000;
                stream = client.GetStream();

                try
                {
                    response.Veranstaltungstitel = veranstaltungstitelProvider() ?? string.Empty;
                }
                catch { response.Veranstaltungstitel = string.Empty; }

                //Anfrage lesen (Fehler hier nicht sicher beantwortbar)
                string requestJson;
                try
                {
                    requestJson = await SyncProtocol.ReadFrameAsync(stream).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LOGGING.Write($"SyncServer ReadRequest: {ex}", nameof(SyncServer), EventLogEntryType.Warning);
                    return;
                }

                SyncRequest? request = null;
                try
                {
                    request = JsonSerializer.Deserialize<SyncRequest>(requestJson, SyncProtocol.JsonOptions);
                }
                catch (Exception ex)
                {
                    response.Fehler = $"Anfrage-Deserialisierung fehlgeschlagen: {ex.GetType().Name}: {ex.Message}";
                    LOGGING.Write($"SyncServer DeserializeRequest: {ex}", nameof(SyncServer), EventLogEntryType.Error);
                }

                if (response.Fehler == null && (request == null || request.Command != "GET_GRUPPEN"))
                {
                    response.Fehler = $"Unbekannter Befehl: {request?.Command ?? "(null)"}";
                }

                if (response.Fehler == null)
                {
                    try
                    {
                        Gruppe[] snapshot = gruppenProvider().ToArray();
                        //Test-Serialisieren jeder Gruppe einzeln, um defekte Gruppen zu identifizieren
                        var ok = new List<Gruppe>(snapshot.Length);
                        var fehlerListe = new List<string>();
                        foreach (var g in snapshot)
                        {
                            try
                            {
                                _ = JsonSerializer.Serialize(g, SyncProtocol.JsonOptions);
                                ok.Add(g);
                            }
                            catch (Exception exG)
                            {
                                fehlerListe.Add($"{g.Feuerwehr}/{g.GruppenName}: {exG.GetType().Name}: {exG.Message}");
                            }
                        }
                        response.Gruppen = ok.ToArray();
                        if (fehlerListe.Count > 0)
                        {
                            response.Fehler = "Einzelne Gruppen konnten nicht serialisiert werden:\n" + string.Join("\n", fehlerListe);
                            LOGGING.Write($"SyncServer per-group failures: {response.Fehler}", nameof(SyncServer), EventLogEntryType.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        response.Gruppen = Array.Empty<Gruppe>();
                        response.Fehler = $"Snapshot-Erstellung fehlgeschlagen: {ex.GetType().Name}: {ex.Message}";
                        LOGGING.Write($"SyncServer Snapshot: {ex}", nameof(SyncServer), EventLogEntryType.Error);
                    }
                }

                string responseJson;
                try
                {
                    responseJson = JsonSerializer.Serialize(response, SyncProtocol.JsonOptions);
                }
                catch (Exception ex)
                {
                    LOGGING.Write($"SyncServer SerializeResponse: {ex}", nameof(SyncServer), EventLogEntryType.Error);
                    var fallback = new SyncResponse
                    {
                        Hostname = response.Hostname,
                        Veranstaltungstitel = response.Veranstaltungstitel,
                        Zeitpunkt = response.Zeitpunkt,
                        Fehler = $"JSON-Serialisierung fehlgeschlagen: {ex.GetType().Name}: {ex.Message}"
                    };
                    responseJson = JsonSerializer.Serialize(fallback, SyncProtocol.JsonOptions);
                }

                try
                {
                    await SyncProtocol.WriteFrameAsync(stream, responseJson).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LOGGING.Write($"SyncServer WriteResponse: {ex}", nameof(SyncServer), EventLogEntryType.Warning);
                }
            }
            catch (Exception ex)
            {
                LOGGING.Write($"SyncServer.HandleClient: {ex}", nameof(SyncServer), EventLogEntryType.Warning);
            }
            finally
            {
                try { stream?.Dispose(); } catch { /* ignore */ }
                try { client.Close(); } catch { /* ignore */ }
            }
        }

        public void Dispose() => Stop();
    }
}
