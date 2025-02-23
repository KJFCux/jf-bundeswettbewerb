using System;
using System.ComponentModel;

namespace LagerInsights.Models
{
    public enum Gender
    {
        W,
        M,
        D,
        N
    }

    public enum Status
    {
        [Description("Teilnehmer / 1. Geschwisterkind")]
        AGESCHWISTERKIND,
        [Description("2. Geschwisterkind")]
        BGESCHWISTERKIND,
        [Description("3. Geschwisterkind")]
        CGESCHWISTERKIND,
        [Description("Betreuer")]
        BETREUER,
        [Description("Mitarbeiter")]
        MITARBEITER
    }
    [Serializable]
    public class Person : INotifyPropertyChanged
    {


        public string Vorname { get; set; }
        public string Nachname { get; set; }

        public Gender Geschlecht { get; set; }

        public string Strasse { get; set; }
        public string Plz { get; set; }
        public string Ort { get; set; }
        public Status Status { get; set; }
        public string Essgewohnheiten { get; set; }
        public string Unvertraeglichkeiten { get; set; }



        private DateTime geburtsdatum;
        public DateTime Geburtsdatum
        {
            get { return geburtsdatum; }
            set
            {
                if (geburtsdatum != value)
                {
                    geburtsdatum = value;
                    OnPropertyChanged();

                }
            }
        }

        public int Alter
        {
            get
            {
                int age = Globals.VERANSTALTUNGSDATUM.Year - Geburtsdatum.Year;

                //Nur wenn noch nicht 10 soll das genaue Alter berechnet werden. Sonst der Jahrgang
                if (Geburtsdatum.Date > Globals.VERANSTALTUNGSDATUM.AddYears(-age))
                {
                    if (age == 10)
                    {
                        age--;
                    }
                }

                return age;
            }
        }
        public string StatusFriendlyName
        {
            get
            {
                switch (Status)
                {
                    case Status.AGESCHWISTERKIND: return "Teilnehmer / 1. Geschwisterkind";
                    case Status.BGESCHWISTERKIND: return "2. Geschwisterkind";
                    case Status.CGESCHWISTERKIND: return "3. Geschwisterkind";
                    case Status.BETREUER: return "Betreuer";
                    case Status.MITARBEITER: return "Mitarbeiter";

                    default: return "none";
                }
            }
        }

        public Person()
        {
            Geschlecht = Gender.N;
            Geburtsdatum = new DateTime(DateTime.Now.Year, 1, 1);
            Vorname = string.Empty;
            Nachname = string.Empty;
            Strasse = string.Empty;
            Plz = string.Empty;
            Ort = string.Empty;
            Status = Status.AGESCHWISTERKIND;
            Essgewohnheiten = string.Empty;
            Unvertraeglichkeiten = string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }
    }
}
