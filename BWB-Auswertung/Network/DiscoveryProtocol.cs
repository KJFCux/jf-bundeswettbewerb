namespace BWB_Auswertung.Network
{
    internal static class DiscoveryProtocol
    {
        public const string HelloMagic = "BWB-AUSWERTUNG-HELLO/1";
    }

    internal class HelloMessage
    {
        public string Magic { get; set; } = DiscoveryProtocol.HelloMagic;
        public string Hostname { get; set; } = string.Empty;
        public int SyncPort { get; set; }
        public string Veranstaltungstitel { get; set; } = string.Empty;
        public string InstanzId { get; set; } = string.Empty;
    }
}
