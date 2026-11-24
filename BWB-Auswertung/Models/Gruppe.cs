using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace BWB_Auswertung.Models
{
    [Serializable]
    public class Gruppe : INotifyPropertyChanged
    {
        public int? StartNr { get; set; }

        public required string Feuerwehr { get; set; }

        public required string GruppenName { get; set; }

        public string? Organisationseinheit { get; set; }

        public int? Platz { get; set; }
        public bool? OhneWertung { get; set; }

        public int? Losentscheid { get; set; }

        public required List<Person> Persons { get; set; }
        public int? WettbewerbsbahnATeil { get; set; }
        public int? WettbewerbsbahnBTeil { get; set; }
        public DateTime StartzeitATeil { get; set; }
        public DateTime StartzeitBTeil { get; set; }

        public string? UrlderAnmeldung { get; set; }

        /// <summary>
        /// Dauerhafte ID für Gruppen ohne Online-Anmeldung (z.B. manuell angelegt oder per Excel importiert).
        /// Wird mit der Gruppe gespeichert, damit die ID über Neustarts und SFTP-/LAN-Abgleiche stabil bleibt.
        /// </summary>
        public string? LokaleUID { get; set; }

        /// <summary>
        /// Eindeutige ID der Gruppe. Bei Online-Anmeldungen der Parameter "anmeldung" aus der URL,
        /// sonst die dauerhaft gespeicherte <see cref="LokaleUID"/>.
        /// Früher wurde hier bei jedem Zugriff eine neue Guid erzeugt. Dadurch wurden lokal angelegte
        /// Gruppen beim SFTP-Sync bei jedem Abgleich dupliziert.
        /// </summary>
        public string AnmeldungUID
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(UrlderAnmeldung)
                    && Uri.TryCreate(UrlderAnmeldung, UriKind.Absolute, out Uri? uri))
                {
                    string? aid = HttpUtility.ParseQueryString(uri.Query)["anmeldung"];
                    if (!string.IsNullOrWhiteSpace(aid))
                    {
                        return aid;
                    }
                }

                LokaleUID ??= Guid.NewGuid().ToString();
                return LokaleUID;
            }
        }

        public DateTime? TimeStampAnmeldung { get; set; }
        public DateTime? TimeStampAenderung { get; set; }

        //AUSWERTUNG
        public bool? DisqualifikationA { get; set; }
        public bool? DisqualifikationB { get; set; }

        // B-Teil
        private int? eindruckLauefer1;
        public int? EindruckLauefer1
        {
            get { return eindruckLauefer1; }
            set
            {
                if (eindruckLauefer1 != value)
                {
                    eindruckLauefer1 = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? eindruckLauefer2;
        public int? EindruckLauefer2
        {
            get { return eindruckLauefer2; }
            set
            {
                if (eindruckLauefer2 != value)
                {
                    eindruckLauefer2 = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? eindruckLauefer3;
        public int? EindruckLauefer3
        {
            get { return eindruckLauefer3; }
            set
            {
                if (eindruckLauefer3 != value)
                {
                    eindruckLauefer3 = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? eindruckLauefer4;
        public int? EindruckLauefer4
        {
            get { return eindruckLauefer4; }
            set
            {
                if (eindruckLauefer4 != value)
                {
                    eindruckLauefer4 = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? eindruckLauefer5;
        public int? EindruckLauefer5
        {
            get { return eindruckLauefer5; }
            set
            {
                if (eindruckLauefer5 != value)
                {
                    eindruckLauefer5 = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? eindruckLauefer6;
        public int? EindruckLauefer6
        {
            get { return eindruckLauefer6; }
            set
            {
                if (eindruckLauefer6 != value)
                {
                    eindruckLauefer6 = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? eindruckLauefer7;
        public int? EindruckLauefer7
        {
            get { return eindruckLauefer7; }
            set
            {
                if (eindruckLauefer7 != value)
                {
                    eindruckLauefer7 = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? eindruckLauefer8;
        public int? EindruckLauefer8
        {
            get { return eindruckLauefer8; }
            set
            {
                if (eindruckLauefer8 != value)
                {
                    eindruckLauefer8 = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? eindruckLauefer9;
        public int? EindruckLauefer9
        {
            get { return eindruckLauefer9; }
            set
            {
                if (eindruckLauefer9 != value)
                {
                    eindruckLauefer9 = value;
                    OnPropertyChanged();
                }
            }
        }



        private int? fehlerLauefer1;
        public int? FehlerLauefer1
        {
            get { return fehlerLauefer1; }
            set
            {
                if (fehlerLauefer1 != value)
                {
                    fehlerLauefer1 = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? fehlerLauefer2;
        public int? FehlerLauefer2
        {
            get { return fehlerLauefer2; }
            set
            {
                if (fehlerLauefer2 != value)
                {
                    fehlerLauefer2 = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? fehlerLauefer3;
        public int? FehlerLauefer3
        {
            get { return fehlerLauefer3; }
            set
            {
                if (fehlerLauefer3 != value)
                {
                    fehlerLauefer3 = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? fehlerLauefer4;
        public int? FehlerLauefer4
        {
            get { return fehlerLauefer4; }
            set
            {
                if (fehlerLauefer4 != value)
                {
                    fehlerLauefer4 = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? fehlerLauefer5;
        public int? FehlerLauefer5
        {
            get { return fehlerLauefer5; }
            set
            {
                if (fehlerLauefer5 != value)
                {
                    fehlerLauefer5 = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? fehlerLauefer6;
        public int? FehlerLauefer6
        {
            get { return fehlerLauefer6; }
            set
            {
                if (fehlerLauefer6 != value)
                {
                    fehlerLauefer6 = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? fehlerLauefer7;
        public int? FehlerLauefer7
        {
            get { return fehlerLauefer7; }
            set
            {
                if (fehlerLauefer7 != value)
                {
                    fehlerLauefer7 = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? fehlerLauefer8;
        public int? FehlerLauefer8
        {
            get { return fehlerLauefer8; }
            set
            {
                if (fehlerLauefer8 != value)
                {
                    fehlerLauefer8 = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? fehlerLauefer9;
        public int? FehlerLauefer9
        {
            get { return fehlerLauefer9; }
            set
            {
                if (fehlerLauefer9 != value)
                {
                    fehlerLauefer9 = value;
                    OnPropertyChanged();
                }
            }
        }

        //A-Teil
        private int? eindruckGfMe;
        public int? EindruckGfMe
        {
            get { return eindruckGfMe; }
            set
            {
                if (eindruckGfMe != value)
                {
                    eindruckGfMe = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? eindruckMa;
        public int? EindruckMa
        {
            get { return eindruckMa; }
            set
            {
                if (eindruckMa != value)
                {
                    eindruckMa = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? eindruckA;
        public int? EindruckA
        {
            get { return eindruckA; }
            set
            {
                if (eindruckA != value)
                {
                    eindruckA = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? eindruckW;
        public int? EindruckW
        {
            get { return eindruckW; }
            set
            {
                if (eindruckW != value)
                {
                    eindruckW = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? eindruckS;
        public int? EindruckS
        {
            get { return eindruckS; }
            set
            {
                if (eindruckS != value)
                {
                    eindruckS = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? fehlerGfMe;
        public int? FehlerGfMe
        {
            get { return fehlerGfMe; }
            set
            {
                if (fehlerGfMe != value)
                {
                    fehlerGfMe = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? fehlerMa;
        public int? FehlerMa
        {
            get { return fehlerMa; }
            set
            {
                if (fehlerMa != value)
                {
                    fehlerMa = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? fehlerA;
        public int? FehlerA
        {
            get { return fehlerA; }
            set
            {
                if (fehlerA != value)
                {
                    fehlerA = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? fehlerW;
        public int? FehlerW
        {
            get { return fehlerW; }
            set
            {
                if (fehlerW != value)
                {
                    fehlerW = value;
                    OnPropertyChanged();
                }
            }
        }

        private int? fehlerS;
        public int? FehlerS
        {
            get { return fehlerS; }
            set
            {
                if (fehlerS != value)
                {
                    fehlerS = value;
                    OnPropertyChanged();
                }
            }
        }

        //ZEITEN ANFANG
        public int? ZeitATeil1Minuten { get; set; }
        public int? ZeitATeil1Sekunden { get; set; }
        public double? ZeitATeil1 => ZeitInSekunden(ZeitATeil1Minuten, ZeitATeil1Sekunden);

        public int? ZeitATeil2Minuten { get; set; }
        public int? ZeitATeil2Sekunden { get; set; }
        public double? ZeitATeil2 => ZeitInSekunden(ZeitATeil2Minuten, ZeitATeil2Sekunden);

        public int? ZeitKnoten1 { get; set; }
        public int? ZeitKnoten2 { get; set; }

        public int? ZeitBTeil1Minuten { get; set; }
        public int? ZeitBTeil1Sekunden { get; set; }
        public double? ZeitBTeil1 => ZeitInSekunden(ZeitBTeil1Minuten, ZeitBTeil1Sekunden);

        public int? ZeitBTeil2Minuten { get; set; }
        public int? ZeitBTeil2Sekunden { get; set; }
        public double? ZeitBTeil2 => ZeitInSekunden(ZeitBTeil2Minuten, ZeitBTeil2Sekunden);

        // Wettbewerbsordnung 3.6 / 4.6 / 5.6: "Als Übungszeit bzw. Zeittakt gilt die Durchschnittszeit
        // der jeweiligen beiden Zeitnehmer. Die Sekunden werden kaufmännisch gerundet."
        public double DurchschnittszeitBTeil => DurchschnittszeitZeitnehmer(ZeitBTeil1, ZeitBTeil2);

        public double DurchschnittszeitATeil => DurchschnittszeitZeitnehmer(ZeitATeil1, ZeitATeil2);

        public double DurchschnittszeitKnotenATeil => DurchschnittszeitZeitnehmer(ZeitKnoten1, ZeitKnoten2);

        private static double? ZeitInSekunden(int? minuten, int? sekunden)
        {
            if (minuten == null && sekunden == null)
            {
                return null;
            }
            return new TimeSpan(0, minuten ?? 0, sekunden ?? 0).TotalSeconds;
        }

        /// <summary>
        /// Mittelwert der Zeitnehmer, kaufmännisch auf volle Sekunden gerundet.
        /// Nicht eingetragene Zeiten (leer oder 0) werden ignoriert, damit eine einzelne
        /// eingetragene Zeit nicht halbiert wird.
        /// </summary>
        internal static double DurchschnittszeitZeitnehmer(params double?[] zeiten)
        {
            var gueltig = zeiten.Where(z => z.HasValue && z.Value > 0).Select(z => z!.Value).ToList();
            if (gueltig.Count == 0)
            {
                return 0;
            }
            return Math.Round(gueltig.Average(), 0, MidpointRounding.AwayFromZero);
        }
        //ZEITEN ENDE
        public int GesamtAlter
        {
            get
            {
                int gesamtAlter = 0;
                for (int i = 0; i < 9; i++)
                {
                    gesamtAlter += Persons[i].Alter;
                }
                return gesamtAlter;
            }
        }
        public int GesamtAlterinTagen
        {
            get
            {
                int gesamtAlter = 0;
                for (int i = 0; i < 9; i++)
                {
                    long elapsedTicks = Globals.VERANSTALTUNGSDATUM.Ticks - Persons[i].Geburtsdatum.Ticks;
                    TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);

                    gesamtAlter += elapsedSpan.Days;
                }
                return gesamtAlter;
            }
        }
        public decimal FehlerATeil
        {
            get
            {
                var fehler = new int?[] { FehlerGfMe, FehlerMa, FehlerA, FehlerW, FehlerS };
                return fehler.Sum() ?? 0;
            }
        }

        public decimal FehlerBTeil
        {
            get
            {
                var fehler = new int?[] { FehlerLauefer1, FehlerLauefer2, FehlerLauefer3, FehlerLauefer4, FehlerLauefer5, FehlerLauefer6, FehlerLauefer7, FehlerLauefer8, FehlerLauefer9 };
                return fehler.Sum() ?? 0;
            }
        }
        public double ATeilGesamteindruck
        {
            get
            {
                List<int?> gesamteindruck = new List<int?> { EindruckGfMe, EindruckMa, EindruckA, EindruckW, EindruckS };
                double average = gesamteindruck.Average() ?? 0;
                return Math.Round(average, 2);
            }
        }
        public double BTeilGesamteindruck
        {
            get
            {
                List<int?> gesamteindruck = new List<int?> { EindruckLauefer1, EindruckLauefer2, EindruckLauefer3, EindruckLauefer4, EindruckLauefer5, EindruckLauefer6, EindruckLauefer7, EindruckLauefer8, EindruckLauefer9 };
                double average = gesamteindruck.Average() ?? 0;

                return Math.Round(average, 2);
            }
        }

        public double Gesamteindruck
        {
            get
            {
                List<int?> gesamteindruck = new List<int?> { EindruckGfMe, EindruckMa, EindruckA, EindruckW, EindruckS, EindruckLauefer1, EindruckLauefer2, EindruckLauefer3, EindruckLauefer4, EindruckLauefer5, EindruckLauefer6, EindruckLauefer7, EindruckLauefer8, EindruckLauefer9 };
                double average = gesamteindruck.Average() ?? 0;

                // Wettbewerbsordnung 2.4: "Dieser Wert mit einer Dezimalstelle wird als Minuspunkte verrechnet."
                return Math.Round(average, 1, MidpointRounding.AwayFromZero);
            }
        }

        public decimal PunkteATeil
        {
            get
            {
                //Wenn Disqualifizert den Bereich nicht werten
                bool disqualifikationABool = DisqualifikationA ?? false;
                bool ohneWertungBool = OhneWertung ?? false;

                //Ohne eingetragene Zeit noch nicht werten (wie im B-Teil). Sonst hatte eine Gruppe,
                //die noch gar nicht gestartet ist, volle 1000 Punkte und war z.B. "Bester A-Teil".
                if (disqualifikationABool || ohneWertungBool || DurchschnittszeitATeil <= 0)
                {
                    return 0m;
                }

                decimal punkte = 1000m;

                //Zeit die länger gebraucht wurde verrechnen. Plus Sekunden aber nicht
                double differenzATeil = (Globals.SECONDS_ATEIL - DurchschnittszeitATeil);
                if (differenzATeil <= 0)
                {
                    punkte += Convert.ToDecimal(differenzATeil);

                }
                //Knoten Zeit abziehen
                punkte -= Convert.ToDecimal(DurchschnittszeitKnotenATeil);

                //Fehlerpunkte abziehen
                punkte -= FehlerATeil;

                return punkte;
            }
        }
        public decimal PunkteBTeil
        {
            get
            {
                //Wenn Disqualifizert den Bereich nicht werten
                bool disqualifikationBBool = DisqualifikationB ?? false;
                bool ohneWertungBool = OhneWertung ?? false;

                if (disqualifikationBBool || ohneWertungBool || DurchschnittszeitBTeil <= 0)
                {
                    return 0m;
                }

                decimal punkte = 400m;

                //Zeit die gelaufen wurde verrechnen mit der Sollzeit
                punkte += Convert.ToDecimal(SollZeitBTeilInSekunden - DurchschnittszeitBTeil);

                //Fehlerpunkte abziehen
                punkte -= FehlerBTeil;

                return punkte;
            }
        }

        public double SollZeitBTeilInSekunden
        {
            get
            {
                //Gesamtalter nach den Werten in den Wettbewerbsrichtlinien der jeweiligen Sollzeit zugeordnet
                switch (GesamtAlter)
                {
                    case < 90:
                        return 0; //Gruppe zu jung
                    case <= 94:
                        return new TimeSpan(0, 2, 40).TotalSeconds;
                    case <= 103:
                        return new TimeSpan(0, 2, 35).TotalSeconds;
                    case <= 112:
                        return new TimeSpan(0, 2, 30).TotalSeconds;
                    case <= 121:
                        return new TimeSpan(0, 2, 25).TotalSeconds;
                    case <= 130:
                        return new TimeSpan(0, 2, 20).TotalSeconds;
                    case <= 139:
                        return new TimeSpan(0, 2, 15).TotalSeconds;
                    case <= 148:
                        return new TimeSpan(0, 2, 10).TotalSeconds;
                    case <= 157:
                        return new TimeSpan(0, 2, 5).TotalSeconds;
                    case <= 162:
                        return new TimeSpan(0, 2, 0).TotalSeconds;
                    default:
                        return 0; // Gruppe zu alt
                }
            }
        }
        private static readonly Regex NichtAlphanumerisch = new Regex("[^a-zA-Z0-9]", RegexOptions.Compiled);
        private static readonly Regex NichtAlphanumerischMitUmlauten = new Regex("[^a-zA-Z0-9öäüÄÜÖß ]", RegexOptions.Compiled);

        public string GruppennameOhneSonderzeichen
        {
            get
            {
                Dictionary<string, string> replacements = new Dictionary<string, string>
               {
                   { "ä", "ae" },
                   { "ö", "oe" },
                   { "ü", "ue" },
                   { "Ä", "Ae" },
                   { "Ö", "Oe" },
                   { "Ü", "Ue" },
                   { "ß", "ss" }
               };

                string result = GruppenName ?? string.Empty;
                foreach (var replacement in replacements)
                {
                    result = result.Replace(replacement.Key, replacement.Value);
                }

                return NichtAlphanumerisch.Replace(result, "");
            }
        }
        public string FeuerwehrOhneSonderzeichen
        {
            get
            {
                return NichtAlphanumerischMitUmlauten.Replace(Feuerwehr ?? string.Empty, "");
            }
        }

        public string SollZeitBTeilInMinutenString
        {
            get
            {
                TimeSpan span = new TimeSpan(0, 0, Convert.ToInt32(SollZeitBTeilInSekunden));
                return new DateTime(span.Ticks).ToString("mm:ss");
            }
        }

        public string SollZeitATeilInMinutenString
        {
            get
            {
                TimeSpan span = new TimeSpan(0, 0, Convert.ToInt32(Globals.SECONDS_ATEIL));
                return new DateTime(span.Ticks).ToString("mm:ss");
            }
        }

        public decimal GesamtPunkte
        {
            get
            {

                return PunkteATeil + PunkteBTeil - Convert.ToDecimal(Gesamteindruck);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged()
        {
            this.TimeStampAenderung = DateTime.Now;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }
    }
}
