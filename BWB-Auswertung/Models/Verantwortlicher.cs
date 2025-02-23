using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagerInsights.Models
{

    [Serializable]
    public class Verantwortlicher : INotifyPropertyChanged
    {


        public string Vorname { get; set; }
        public string Nachname { get; set; }

        public Gender Geschlecht { get; set; }

        public string Strasse { get; set; }
        public string Plz { get; set; }
        public string Ort { get; set; }
        public string Funktion { get; set; }
        public string Telefon { get; set; }
        public string Email { get; set; }

        public string FullName
        {
            get
            {
                return $"{Vorname} {Nachname}";
            }
        }
        public Verantwortlicher()
        {
            Vorname = string.Empty;
            Nachname = string.Empty;
            Geschlecht = Gender.N;
            Strasse = string.Empty;
            Plz = string.Empty;
            Ort = string.Empty;
            Funktion = string.Empty;
            Telefon = string.Empty;
            Email = string.Empty;
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }
    }
}