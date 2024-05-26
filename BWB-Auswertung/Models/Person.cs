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


        public string Vorname { get; set; }
        public string Nachname { get; set; }
        public Gender Geschlecht { get; set; }


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
            Geburtsdatum = new DateTime(DateTime.Now.Year,1,1);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }
    }
}
