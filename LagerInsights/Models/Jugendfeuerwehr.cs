using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace LagerInsights.Models;

[Serializable]
[XmlRoot("Jugendfeuerwehr")]
public class Jugendfeuerwehr : INotifyPropertyChanged
{
    private decimal? teilnehmerbeitrag;
    public int? LagerNr { get; set; }

    public required string Feuerwehr { get; set; }

    public string? Organisationseinheit { get; set; }

    public required List<Person> Persons { get; set; }

    public required Verantwortlicher Verantwortlicher { get; set; }

    public string? UrlderAnmeldung { get; set; }

    public DateTime? TimeStampAnmeldung { get; set; }
    public DateTime? TimeStampAenderung { get; set; }

    private decimal? gezahlterBeitrag;

    [XmlIgnore]
    public decimal? GezahlterBeitrag
    {
        get => gezahlterBeitrag;
        set
        {
            if (gezahlterBeitrag != value)
            {
                gezahlterBeitrag = value;
                TimeStampGezahlterBeitrag = DateTime.Now;
                OnPropertyChanged();
            }
        }
    }
    [XmlElement("GezahlterBeitrag")]
    public string GezahlterBeitragRaw
    {
        get => GezahlterBeitrag?.ToString(System.Globalization.CultureInfo.InvariantCulture);
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                GezahlterBeitrag = null;
            else if (decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
                GezahlterBeitrag = result;
            else
                GezahlterBeitrag = null;
        }
    }

    [XmlIgnore]
    public DateTime? TimeStampGezahlterBeitrag { get; set; } //Änderungszeitpunkt des gezahlten Beitrages zum Abgleich mit den Versionen

    [XmlElement("TimeStampGezahlterBeitrag")]
    public string TimeStampGezahlterBeitragRaw
    {
        get => TimeStampGezahlterBeitrag?.ToString("o");
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                TimeStampGezahlterBeitrag = null;
            else if (DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var result))
                TimeStampGezahlterBeitrag = result;
            else
                TimeStampGezahlterBeitrag = null;
        }
    }



    private bool? einverstaendniserklaerung;


    [XmlElement("Einverstaendniserklaerung")]
    public string EinverstaendniserklaerungRaw
    {
        get => Einverstaendniserklaerung?.ToString().ToLower();
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                Einverstaendniserklaerung = null;
            else if (bool.TryParse(value, out var result))
                Einverstaendniserklaerung = result;
            else
                Einverstaendniserklaerung = null;
        }
    }

    [XmlIgnore]
    public bool? Einverstaendniserklaerung
    {
        get => einverstaendniserklaerung;
        set
        {
            if (einverstaendniserklaerung != value)
            {
                einverstaendniserklaerung = value;
                TimeStampEinverstaendniserklaerung = DateTime.Now;
                OnPropertyChanged();
            }
        }
    }


    [XmlIgnore]
    public DateTime? TimeStampEinverstaendniserklaerung { get; set; } //Änderungszeitpunkt der Einverständniserklärung zum Abgleich mit den Versionen

    [XmlElement("TimeStampEinverstaendniserklaerung")]
    public string TimeStampEinverstaendniserklaerungRaw
    {
        get => TimeStampEinverstaendniserklaerung?.ToString("o");
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                TimeStampEinverstaendniserklaerung = null;
            else if (DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var result))
                TimeStampEinverstaendniserklaerung = result;
            else
                TimeStampEinverstaendniserklaerung = null;
        }
    }



    private string? zeltdorf;

    public string? Zeltdorf
    {
        get => zeltdorf;
        set
        {
            if (zeltdorf != value)
            {
                zeltdorf = value;
                TimeStampZeltdorf = DateTime.Now;
                OnPropertyChanged();
            }
        }
    }
    [XmlIgnore]
    public DateTime? TimeStampZeltdorf { get; set; } //Änderungszeitpunkt des Zeltdorfes zum Abgleich mit den Versionen

    [XmlIgnore]
    public decimal? Teilnehmerbeitrag
    {
        get => teilnehmerbeitrag;
        set
        {
            if (teilnehmerbeitrag != value)
            {
                teilnehmerbeitrag = value;
                OnPropertyChanged();
            }
        }
    }
    [XmlElement("Teilnehmerbeitrag")]
    public string TeilnehmerbeitragRaw
    {
        get => Teilnehmerbeitrag?.ToString(System.Globalization.CultureInfo.InvariantCulture);
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                Teilnehmerbeitrag = null;
            else if (decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
                Teilnehmerbeitrag = result;
            else
                Teilnehmerbeitrag = null;
        }
    }

    [XmlElement("TimeStampZeltdorf")]
    public string TimeStampZeltdorfRaw
    {
        get => TimeStampZeltdorf?.ToString("o");
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                TimeStampZeltdorf = null;
            else if (DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var result))
                TimeStampZeltdorf = result;
            else
                TimeStampZeltdorf = null;
        }
    }

    public string FeuerwehrOhneSonderzeichen
    {
        get
        {
            var rgx = new Regex("[^a-zA-Z0-9öäüÄÜÖß ]");
            return rgx.Replace(Feuerwehr, "");
        }
    }

    public int AnzahlTeilnehmer => Persons.Count;

    public int Anzahl1Geschwisterkind
    {
        get { return Persons.Count(p => p.Status == Status.AGESCHWISTERKIND); }
    }

    public int Anzahl2Geschwisterkind
    {
        get { return Persons.Count(p => p.Status == Status.BGESCHWISTERKIND); }
    }

    public int Anzahl3Geschwisterkind
    {
        get { return Persons.Count(p => p.Status == Status.CGESCHWISTERKIND); }
    }

    public int AnzahlBetreuer
    {
        get { return Persons.Count(p => p.Status == Status.BETREUER); }
    }

    public int AnzahlMitarbeiter
    {
        get { return Persons.Count(p => p.Status == Status.MITARBEITER); }
    }

    public decimal ZuBezahlenderBetrag
    {
        get
        {
            var beitrag = Teilnehmerbeitrag.HasValue ? Teilnehmerbeitrag.Value : 200;
            var normaleTeilnehmer =
                beitrag * (Anzahl1Geschwisterkind + AnzahlBetreuer + AnzahlMitarbeiter);
            var zweiGeschwister = beitrag * 0.75m * Anzahl2Geschwisterkind;
            var dreiGeschwister = beitrag * 0.5m * Anzahl3Geschwisterkind;
            return Math.Round(normaleTeilnehmer + zweiGeschwister + dreiGeschwister, 2);
        }
    }


    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged()
    {
        TimeStampAenderung = DateTime.Now;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }
}