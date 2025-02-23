using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace LagerInsights.Models
{
    [Serializable]
    public class Gruppe : INotifyPropertyChanged
    {
        public int? LagerNr { get; set; }

        public required string Feuerwehr { get; set; }

        public string? Organisationseinheit { get; set; }

        public required List<Person> Persons { get; set; }

        public required Verantwortlicher Verantwortlicher { get; set; }

        public string? UrlderAnmeldung { get; set; }

        public DateTime? TimeStampAnmeldung { get; set; }
        public DateTime? TimeStampAenderung { get; set; }
        public decimal? GezahlterBeitrag { get; set; }
        public decimal? Teilnehmerbeitrag { get; set; }


        public string FeuerwehrOhneSonderzeichen
        {
            get
            {
                Regex rgx = new Regex("[^a-zA-Z0-9öäüÄÜÖß ]");
                return rgx.Replace(Feuerwehr, "");
            }
        }

        public int anzahlTeilnehmer => Persons.Count;

        public int anzahl1Geschwisterkind
        {
            get
            {
                return Persons.Count(p => p.Status == Status.AGESCHWISTERKIND);
            }
        }
        public int anzahl2Geschwisterkind
        {
            get
            {
                return Persons.Count(p => p.Status == Status.BGESCHWISTERKIND);
            }
        }
        public int anzahl3Geschwisterkind
        {
            get
            {
                return Persons.Count(p => p.Status == Status.CGESCHWISTERKIND);
            }
        }
        public int anzahlBetreuer
        {
            get
            {
                return Persons.Count(p => p.Status == Status.BETREUER);
            }
        }
        public int anzahlMitarbeiter
        {
            get
            {
                return Persons.Count(p => p.Status == Status.MITARBEITER);
            }
        }

        public decimal zuBezahlenderBetrag
        {
            get
            {

                return (Teilnehmerbeitrag ?? 0 * (anzahl1Geschwisterkind + anzahlBetreuer + anzahlMitarbeiter)) + ((Teilnehmerbeitrag ?? 0 * anzahl2Geschwisterkind) * 0.75m) + ((Teilnehmerbeitrag ?? 0 * anzahl3Geschwisterkind) * 0.5m);

                return Persons.Count(p => p.Status == Status.MITARBEITER);
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
