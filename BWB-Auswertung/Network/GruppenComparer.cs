using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using BWB_Auswertung.Models;

namespace BWB_Auswertung.Network
{
    public enum VergleichStatus
    {
        Identisch,
        Abweichend,
        NurLokal,
        NurPartner
    }

    public class PropertyDifferenz
    {
        public string Property { get; set; } = string.Empty;
        public string LokalWert { get; set; } = string.Empty;
        public string PartnerWert { get; set; } = string.Empty;
    }

    public class GruppenVergleich
    {
        public string MatchKey { get; set; } = string.Empty;
        public string Feuerwehr { get; set; } = string.Empty;
        public string GruppenName { get; set; } = string.Empty;
        public int? StartNr { get; set; }
        public Gruppe? Lokal { get; set; }
        public Gruppe? Partner { get; set; }
        public VergleichStatus Status { get; set; }
        public List<PropertyDifferenz> Differenzen { get; set; } = new List<PropertyDifferenz>();

        public string StatusAnzeige => Status switch
        {
            VergleichStatus.Identisch => "Identisch",
            VergleichStatus.Abweichend => $"Abweichend ({Differenzen.Count})",
            VergleichStatus.NurLokal => "Nur lokal",
            VergleichStatus.NurPartner => "Nur Partner",
            _ => string.Empty
        };
    }

    public static class GruppenComparer
    {
        //Score-relevante Properties (Eindrücke, Fehler, Zeiten, Knoten, Disqualifikation, Losentscheid, OhneWertung)
        private static readonly string[] VergleichsProperties = new[]
        {
            //A-Teil Eindruck
            nameof(Gruppe.EindruckGfMe), nameof(Gruppe.EindruckMa), nameof(Gruppe.EindruckA),
            nameof(Gruppe.EindruckW), nameof(Gruppe.EindruckS),
            //A-Teil Fehler
            nameof(Gruppe.FehlerGfMe), nameof(Gruppe.FehlerMa), nameof(Gruppe.FehlerA),
            nameof(Gruppe.FehlerW), nameof(Gruppe.FehlerS),
            //B-Teil Eindruck
            nameof(Gruppe.EindruckLauefer1), nameof(Gruppe.EindruckLauefer2), nameof(Gruppe.EindruckLauefer3),
            nameof(Gruppe.EindruckLauefer4), nameof(Gruppe.EindruckLauefer5), nameof(Gruppe.EindruckLauefer6),
            nameof(Gruppe.EindruckLauefer7), nameof(Gruppe.EindruckLauefer8), nameof(Gruppe.EindruckLauefer9),
            //B-Teil Fehler
            nameof(Gruppe.FehlerLauefer1), nameof(Gruppe.FehlerLauefer2), nameof(Gruppe.FehlerLauefer3),
            nameof(Gruppe.FehlerLauefer4), nameof(Gruppe.FehlerLauefer5), nameof(Gruppe.FehlerLauefer6),
            nameof(Gruppe.FehlerLauefer7), nameof(Gruppe.FehlerLauefer8), nameof(Gruppe.FehlerLauefer9),
            //Zeiten
            nameof(Gruppe.ZeitATeil1Minuten), nameof(Gruppe.ZeitATeil1Sekunden),
            nameof(Gruppe.ZeitATeil2Minuten), nameof(Gruppe.ZeitATeil2Sekunden),
            nameof(Gruppe.ZeitBTeil1Minuten), nameof(Gruppe.ZeitBTeil1Sekunden),
            nameof(Gruppe.ZeitBTeil2Minuten), nameof(Gruppe.ZeitBTeil2Sekunden),
            nameof(Gruppe.ZeitKnoten1), nameof(Gruppe.ZeitKnoten2),
            //Sonstiges Wertungsrelevantes
            nameof(Gruppe.DisqualifikationA), nameof(Gruppe.DisqualifikationB),
            nameof(Gruppe.OhneWertung), nameof(Gruppe.Losentscheid)
        };

        public static List<GruppenVergleich> Compare(IEnumerable<Gruppe> lokal, IEnumerable<Gruppe> partner)
        {
            var lokalList = lokal?.ToList() ?? new List<Gruppe>();
            var partnerList = partner?.ToList() ?? new List<Gruppe>();

            var lokalDict = new Dictionary<string, Gruppe>(StringComparer.OrdinalIgnoreCase);
            foreach (var g in lokalList)
            {
                string key = BuildMatchKey(g);
                if (!lokalDict.ContainsKey(key)) lokalDict[key] = g;
            }

            var partnerDict = new Dictionary<string, Gruppe>(StringComparer.OrdinalIgnoreCase);
            foreach (var g in partnerList)
            {
                string key = BuildMatchKey(g);
                if (!partnerDict.ContainsKey(key)) partnerDict[key] = g;
            }

            var ergebnis = new List<GruppenVergleich>();
            var alleKeys = new HashSet<string>(lokalDict.Keys, StringComparer.OrdinalIgnoreCase);
            foreach (var k in partnerDict.Keys) alleKeys.Add(k);

            foreach (var key in alleKeys)
            {
                lokalDict.TryGetValue(key, out Gruppe? l);
                partnerDict.TryGetValue(key, out Gruppe? p);

                var v = new GruppenVergleich
                {
                    MatchKey = key,
                    Lokal = l,
                    Partner = p,
                    Feuerwehr = l?.Feuerwehr ?? p?.Feuerwehr ?? string.Empty,
                    GruppenName = l?.GruppenName ?? p?.GruppenName ?? string.Empty,
                    StartNr = l?.StartNr ?? p?.StartNr
                };

                if (l == null)
                {
                    v.Status = VergleichStatus.NurPartner;
                }
                else if (p == null)
                {
                    v.Status = VergleichStatus.NurLokal;
                }
                else
                {
                    v.Differenzen = DiffProperties(l, p);
                    v.Status = v.Differenzen.Count == 0 ? VergleichStatus.Identisch : VergleichStatus.Abweichend;
                }

                ergebnis.Add(v);
            }

            return ergebnis
                .OrderBy(e => e.StartNr ?? int.MaxValue)
                .ThenBy(e => e.Feuerwehr)
                .ThenBy(e => e.GruppenName)
                .ToList();
        }

        public static string BuildMatchKey(Gruppe g)
        {
            //1. UrlderAnmeldung -> anmeldung-Param
            if (!string.IsNullOrWhiteSpace(g.UrlderAnmeldung))
            {
                try
                {
                    var uri = new Uri(g.UrlderAnmeldung);
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    string? aid = query["anmeldung"];
                    if (!string.IsNullOrWhiteSpace(aid)) return $"AID:{aid}";
                }
                catch { /* fallthrough */ }
            }

            //2. Feuerwehr + GruppenName (normalisiert)
            string fw = (g.Feuerwehr ?? string.Empty).Trim().ToLowerInvariant();
            string gn = (g.GruppenName ?? string.Empty).Trim().ToLowerInvariant();
            return $"FG:{fw}|{gn}";
        }

        private static List<PropertyDifferenz> DiffProperties(Gruppe a, Gruppe b)
        {
            var diffs = new List<PropertyDifferenz>();
            Type t = typeof(Gruppe);
            foreach (var name in VergleichsProperties)
            {
                PropertyInfo? pi = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (pi == null) continue;
                object? va = pi.GetValue(a);
                object? vb = pi.GetValue(b);
                if (!object.Equals(va, vb))
                {
                    diffs.Add(new PropertyDifferenz
                    {
                        Property = name,
                        LokalWert = Format(va),
                        PartnerWert = Format(vb)
                    });
                }
            }
            return diffs;
        }

        private static string Format(object? v) => v?.ToString() ?? "—";
    }
}
