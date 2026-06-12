using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BWB_Auswertung.Models;

namespace BWB_Auswertung.Services
{
    public enum AutoStartReihenfolge
    {
        [Description("Aktuelle Sortierung")]
        AktuelleSortierung,
        [Description("Feuerwehr, dann Gruppenname")]
        FeuerwehrDannGruppenname,
        [Description("Zufall")]
        Zufall
    }

    public enum Teil
    {
        A,
        B
    }

    public class AutoStartResult
    {
        public int ZugewieseneA { get; set; }
        public int ZugewieseneB { get; set; }
        public List<string> Warnungen { get; set; } = new List<string>();
    }

    public static class StartzeitenService
    {
        private const int MAX_SLOT_ITERATIONEN = 1000;

        public static AutoStartResult AutoAssign(
            IList<Gruppe> gruppen,
            Settings einstellungen,
            AutoStartReihenfolge reihenfolge)
        {
            var result = new AutoStartResult();

            int minAbstandMin = Math.Max(0, einstellungen.MinAbstandCrossTeilMinuten);
            TimeSpan minAbstand = TimeSpan.FromMinutes(minAbstandMin);

            //Mittagspause auf das Veranstaltungsdatum normieren. Dauer 0 = keine Pause.
            DateTime pauseStart = default;
            DateTime pauseEnde = default;
            int pauseDauerMin = Math.Max(0, einstellungen.MittagspauseDauerMinuten);
            if (pauseDauerMin > 0)
            {
                pauseStart = einstellungen.Veranstaltungsdatum.Date.Add(einstellungen.MittagspauseStart.TimeOfDay);
                pauseEnde = pauseStart.AddMinutes(pauseDauerMin);
            }

            AssignTeil(gruppen, einstellungen, reihenfolge, Teil.A, minAbstand, pauseStart, pauseEnde, result);
            AssignTeil(gruppen, einstellungen, reihenfolge, Teil.B, minAbstand, pauseStart, pauseEnde, result);

            return result;
        }

        private static void AssignTeil(
            IList<Gruppe> gruppen,
            Settings einstellungen,
            AutoStartReihenfolge reihenfolge,
            Teil teil,
            TimeSpan minAbstand,
            DateTime pauseStart,
            DateTime pauseEnde,
            AutoStartResult result)
        {
            int anzahlBahnen = teil == Teil.A ? einstellungen.AnzahlBahnenATeil : einstellungen.AnzahlBahnenBTeil;
            int intervallMin = teil == Teil.A ? einstellungen.StartintervallATeilMinuten : einstellungen.StartintervallBTeilMinuten;
            DateTime beginn = teil == Teil.A ? einstellungen.StartBeginnATeil : einstellungen.StartBeginnBTeil;

            //Datum auf Veranstaltungsdatum normieren, nur Uhrzeit aus den Einstellungen verwenden
            beginn = einstellungen.Veranstaltungsdatum.Date.Add(beginn.TimeOfDay);

            if (anzahlBahnen < 1 || intervallMin < 1)
            {
                result.Warnungen.Add($"{TeilName(teil)}: Anzahl Bahnen oder Intervall ungültig (mindestens 1).");
                return;
            }

            var intervall = TimeSpan.FromMinutes(intervallMin);

            //Bereits belegte Slots erfassen: pro Bahn die belegten Startzeiten
            var besetzteSlots = new Dictionary<int, HashSet<DateTime>>();
            for (int b = 1; b <= anzahlBahnen; b++)
                besetzteSlots[b] = new HashSet<DateTime>();

            foreach (var g in gruppen)
            {
                if (HatStartzeit(g, teil) && HatBahn(g, teil))
                {
                    int bahn = GetBahn(g, teil) ?? 0;
                    if (bahn >= 1 && bahn <= anzahlBahnen)
                    {
                        besetzteSlots[bahn].Add(GetStartzeit(g, teil));
                    }
                }
            }

            //Kandidaten = Gruppen, die für diesen Teil noch keine Startzeit haben
            IEnumerable<Gruppe> kandidaten = gruppen.Where(g => !HatStartzeit(g, teil));
            switch (reihenfolge)
            {
                case AutoStartReihenfolge.FeuerwehrDannGruppenname:
                    kandidaten = kandidaten.OrderBy(g => g.Feuerwehr).ThenBy(g => g.GruppenName);
                    break;
                case AutoStartReihenfolge.Zufall:
                    kandidaten = kandidaten.OrderBy(_ => Guid.NewGuid());
                    break;
                default:
                    kandidaten = kandidaten.ToList();
                    break;
            }

            foreach (var g in kandidaten.ToList())
            {
                if (TryFindSlot(g, gruppen, teil, beginn, intervall, anzahlBahnen, minAbstand, pauseStart, pauseEnde, besetzteSlots,
                                out DateTime zeit, out int bahn))
                {
                    SetStartzeit(g, teil, zeit);
                    if (!HatBahn(g, teil))
                    {
                        SetBahn(g, teil, bahn);
                    }
                    besetzteSlots[bahn].Add(zeit);

                    if (teil == Teil.A) result.ZugewieseneA++;
                    else result.ZugewieseneB++;
                }
                else
                {
                    result.Warnungen.Add(
                        $"{TeilName(teil)}: Kein freier Slot für '{g.GruppenName}' ({g.Feuerwehr}) gefunden.");
                }
            }
        }

        private static bool TryFindSlot(
            Gruppe gruppe,
            IList<Gruppe> alleGruppen,
            Teil teil,
            DateTime beginn,
            TimeSpan intervall,
            int anzahlBahnen,
            TimeSpan minAbstand,
            DateTime pauseStart,
            DateTime pauseEnde,
            Dictionary<int, HashSet<DateTime>> besetzteSlots,
            out DateTime zeit,
            out int bahn)
        {
            //Wenn die Gruppe bereits eine Bahn hat, nur diese Bahn nutzen
            int? fixeBahn = HatBahn(gruppe, teil) ? GetBahn(gruppe, teil) : null;

            for (int i = 0; i < MAX_SLOT_ITERATIONEN; i++)
            {
                DateTime kandidatZeit = beginn + TimeSpan.FromTicks(intervall.Ticks * i);

                //Während der Mittagspause werden keine Startzeiten vergeben.
                if (pauseEnde > pauseStart && kandidatZeit >= pauseStart && kandidatZeit < pauseEnde)
                    continue;

                IEnumerable<int> bahnen = fixeBahn.HasValue
                    ? new[] { fixeBahn.Value }
                    : Enumerable.Range(1, anzahlBahnen);

                foreach (int kandidatBahn in bahnen)
                {
                    if (kandidatBahn < 1 || kandidatBahn > anzahlBahnen) continue;
                    if (besetzteSlots[kandidatBahn].Contains(kandidatZeit)) continue;
                    if (!CrossTeilOk(gruppe, alleGruppen, teil, kandidatZeit, minAbstand)) continue;

                    zeit = kandidatZeit;
                    bahn = kandidatBahn;
                    return true;
                }
            }

            zeit = default;
            bahn = 0;
            return false;
        }

        /// <summary>
        /// Prüft den konfigurierten Mindestabstand: Die Gruppe selbst und alle Gruppen derselben Feuerwehr
        /// dürfen mit ihrem jeweils anderen Teil nicht innerhalb von minAbstand zur geprüften Zeit liegen.
        /// </summary>
        private static bool CrossTeilOk(Gruppe gruppe, IList<Gruppe> alleGruppen, Teil teil, DateTime kandidatZeit, TimeSpan minAbstand)
        {
            if (minAbstand <= TimeSpan.Zero) return true;
            Teil andererTeil = teil == Teil.A ? Teil.B : Teil.A;

            foreach (var other in alleGruppen)
            {
                bool sameFeuerwehr = !string.IsNullOrWhiteSpace(gruppe.Feuerwehr)
                                     && string.Equals(gruppe.Feuerwehr, other.Feuerwehr, StringComparison.OrdinalIgnoreCase);
                bool sameGruppe = ReferenceEquals(gruppe, other);

                if (!sameFeuerwehr && !sameGruppe) continue;

                if (HatStartzeit(other, andererTeil))
                {
                    DateTime otherZeit = GetStartzeit(other, andererTeil);
                    if ((kandidatZeit - otherZeit).Duration() < minAbstand)
                        return false;
                }
            }
            return true;
        }

        private static bool HatStartzeit(Gruppe g, Teil teil)
        {
            DateTime z = GetStartzeit(g, teil);
            return z != default;
        }

        private static bool HatBahn(Gruppe g, Teil teil)
        {
            int? b = GetBahn(g, teil);
            return b.HasValue && b.Value > 0;
        }

        private static DateTime GetStartzeit(Gruppe g, Teil teil)
            => teil == Teil.A ? g.StartzeitATeil : g.StartzeitBTeil;

        private static void SetStartzeit(Gruppe g, Teil teil, DateTime zeit)
        {
            if (teil == Teil.A) g.StartzeitATeil = zeit;
            else g.StartzeitBTeil = zeit;
        }

        private static int? GetBahn(Gruppe g, Teil teil)
            => teil == Teil.A ? g.WettbewerbsbahnATeil : g.WettbewerbsbahnBTeil;

        private static void SetBahn(Gruppe g, Teil teil, int bahn)
        {
            if (teil == Teil.A) g.WettbewerbsbahnATeil = bahn;
            else g.WettbewerbsbahnBTeil = bahn;
        }

        private static string TeilName(Teil t) => t == Teil.A ? "A-Teil" : "B-Teil";
    }
}
