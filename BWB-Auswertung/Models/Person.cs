using System;
using System.ComponentModel;

namespace BWB_Auswertung.Models
{
    public enum Gender
    {
        W,
        M,
        D,
        N
    }
    [Serializable]
    public class Person : INotifyPropertyChanged
    {


        private string vorname;
        public string Vorname
        {
            get { return vorname; }
            set
            {
                if (vorname != value)
                {
                    vorname = value;
                    OnPropertyChanged();
                }
            }
        }

        private string nachname;
        public string Nachname
        {
            get { return nachname; }
            set
            {
                if (nachname != value)
                {
                    nachname = value;
                    OnPropertyChanged();
                }
            }
        }

        private Gender geschlecht;
        public Gender Geschlecht
        {
            get { return geschlecht; }
            set
            {
                if (geschlecht != value)
                {
                    geschlecht = value;
                    OnPropertyChanged();
                }
            }
        }


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

        public Person()
        {
            Vorname = string.Empty;
            Nachname = string.Empty;
            Geschlecht = Gender.N;
            Geburtsdatum = new DateTime(DateTime.Now.AddYears(-11).Year,1,1);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }
    }
}
