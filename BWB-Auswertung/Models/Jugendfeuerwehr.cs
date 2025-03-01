using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace LagerInsights.Models
{
    [Serializable]
    [XmlRoot("Jugendfeuerwehr")]
    public class Jugendfeuerwehr : INotifyPropertyChanged
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

        public int AnzahlTeilnehmer => Persons.Count;

        public int Anzahl1Geschwisterkind
        {
            get
            {
                return Persons.Count(p => p.Status == Status.AGESCHWISTERKIND);
            }
        }
        public int Anzahl2Geschwisterkind
        {
            get
            {
                return Persons.Count(p => p.Status == Status.BGESCHWISTERKIND);
            }
        }
        public int Anzahl3Geschwisterkind
        {
            get
            {
                return Persons.Count(p => p.Status == Status.CGESCHWISTERKIND);
            }
        }
        public int AnzahlBetreuer
        {
            get
            {
                return Persons.Count(p => p.Status == Status.BETREUER);
            }
        }
        public int AnzahlMitarbeiter
        {
            get
            {
                return Persons.Count(p => p.Status == Status.MITARBEITER);
            }
        }

        public decimal ZuBezahlenderBetrag
        {
            get
            {
                return (Teilnehmerbeitrag ?? 0 * (Anzahl1Geschwisterkind + AnzahlBetreuer + AnzahlMitarbeiter)) + ((Teilnehmerbeitrag ?? 0 * Anzahl2Geschwisterkind) * 0.75m) + ((Teilnehmerbeitrag ?? 0 * Anzahl3Geschwisterkind) * 0.5m);
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
