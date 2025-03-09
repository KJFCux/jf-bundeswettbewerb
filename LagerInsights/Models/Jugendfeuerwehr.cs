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
    public decimal? GezahlterBeitrag { get; set; }

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