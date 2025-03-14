using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace LagerInsights.Models;

public enum Gender
{
    [Description("Frau")] W,
    [Description("Herr")] M,
    [Description("Sonstiges")] D,
    [Description("Herr/Frau/Sonstiges")] N
}

public enum Status
{
    [Description("Teilnehmer / 1. Geschwisterkind")] [XmlEnum("1Geschwister")]
    AGESCHWISTERKIND,

    [Description("2. Geschwisterkind")] [XmlEnum("2Geschwister")]
    BGESCHWISTERKIND,

    [Description("3. Geschwisterkind")] [XmlEnum("4Geschwister")]
    CGESCHWISTERKIND,

    [Description("Betreuer")] [XmlEnum("Betreuer")]
    BETREUER,

    [Description("Mitarbeiter")] [XmlEnum("Mitarbeiter")]
    MITARBEITER
}

[Serializable]
public class Person : INotifyPropertyChanged
{
    private DateTime geburtsdatum;

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


    public string Vorname { get; set; }

    [XmlElement("Name")] public string Nachname { get; set; }

    public Gender Geschlecht { get; set; }

    public string Strasse { get; set; }

    [XmlElement("PLZ")] public string Plz { get; set; }

    public string Ort { get; set; }
    public Status Status { get; set; }
    public string Essgewohnheiten { get; set; }
    public string Unvertraeglichkeiten { get; set; }

    public DateTime Geburtsdatum
    {
        get => geburtsdatum;
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
            var age = Globals.VERANSTALTUNGSDATUM.Year - Geburtsdatum.Year;

            //Nur wenn noch nicht 10 soll das genaue Alter berechnet werden. Sonst der Jahrgang
            if (Geburtsdatum.Date > Globals.VERANSTALTUNGSDATUM.AddYears(-age))
                if (age == 10)
                    age--;

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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }
}