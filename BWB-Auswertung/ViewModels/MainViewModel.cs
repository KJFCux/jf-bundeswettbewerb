using BWB_Auswertung.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using BWB_Auswertung.IO;
using System.Linq;
using System.Windows;
using System.IO;

public class MainViewModel : INotifyPropertyChanged
{
    private ObservableCollection<Gruppe> gruppen;
    private Settings einstellungen;
    private ObservableCollection<Art> artList;

    public ObservableCollection<Art> ArtList
    {
        get { return artList; }
        set
        {
            artList = value;
            OnPropertyChanged(nameof(ArtList));
        }
    }

    private double _scaleFactor = 1.0;
    public double ScaleFactor
    {
        get { return _scaleFactor; }
        set
        {
            if (_scaleFactor != value)
            {
                _scaleFactor = value;
                OnPropertyChanged(nameof(ScaleFactor));
            }
        }
    }

    private double _scaleFactorEvaluation = 0.6;
    public double ScaleFactorEvaluation
    {
        get { return _scaleFactorEvaluation; }
        set
        {
            if (_scaleFactorEvaluation != value)
            {
                _scaleFactorEvaluation = value;
                OnPropertyChanged(nameof(ScaleFactorEvaluation));
            }
        }
    }

    private double _scaleFactorSettings = 1.0;
    public double ScaleFactorSettings
    {
        get { return _scaleFactorSettings; }
        set
        {
            if (_scaleFactorSettings != value)
            {
                _scaleFactorSettings = value;
                OnPropertyChanged(nameof(ScaleFactorSettings));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<Gruppe> Gruppen
    {
        get
        {
            return gruppen;
        }
        set
        {
            gruppen = value;
            OnPropertyChanged(nameof(Gruppen));
            //Bei jeder größeren Veränderung von Daten, alles direkt in die XML-Dateien sichern
            //foreach (var gruppe in gruppen)
            //{    
            // TODO Erstmal deaktiviert, da sich die Frage stellt ob wirklich bei jedem Tastenanschlag gespeichert werden muss
            // Vielleicht reicht auch immer beim durchklicken der Listen
            // WriteFile.writeText(Path.Combine("TODOPFAD",$"{gruppe.Feuerwehr} - {gruppe.GruppenName}.xml"), SerializeXML<Gruppe>.Serialize(gruppe));
            //}
        }
    }

    public List<PersonTeilnehmendenliste> PersonenTeilnehmendenliste => alleTeilnehmenden();
    public List<PersonTeilnehmendenliste> PersonenMitGeburtstagBeimWettbewerb => personenMitGeburtstagBeimWettbewerb();

    public Settings Einstellungen
    {
        get
        {
            return einstellungen;
        }
        set
        {
            einstellungen = value;
            OnPropertyChanged(nameof(Einstellungen));
        }
    }

    public MainViewModel()
    {
        //Leere Collections erstellen
        Einstellungen = new Settings();
        Gruppen = new ObservableCollection<Gruppe>()
        {
            //Testdaten können hier eingefügt werden
        };
        ArtList = new ObservableCollection<Art>
        {
            Art.UNTERFLURHYDRANT,
            Art.OFFENESGEWAESSER,
            Art.KEINEVORGABEZEIT
        };
    }
    public void OverrideSettings(Settings einstellungenNew)
    {
        Einstellungen = einstellungenNew;
        OnPropertyChanged(nameof(Einstellungen));
    }

    //Erstellt eine Leere Gruppe
    public void AddEmptyGruppe(string feuerwehr, string gruppenName)
    {

        //10 Leere Personen erstellen damit diese bearbeitet werden können
        List<Person> persons = new List<Person>();
        for (int i = 0; i <= 9; i++)
        {
            persons.Add(new Person());
        }

        //Leere Gruppe erstellen
        Gruppe newGruppe = new Gruppe
        {
            Feuerwehr = feuerwehr,
            GruppenName = gruppenName,
            Persons = persons,
            TimeStampAnmeldung = DateTime.Now,
            TimeStampAenderung = DateTime.Now
        };

        Gruppen.Add(newGruppe);

    }
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        //Alles Gleichzeitig speichern, bei Veränderung erstmal wieder entfernt -> Testen
        /*Parallel.ForEach(Gruppen, new ParallelOptions { MaxDegreeOfParallelism = 10 }, async (Gruppe gruppe) =>
        {
            await writeText.WriteTextAsync($"{gruppe.Feuerwehr} - {gruppe.GruppenName}.xml", SerializeXML<Gruppe>.Serialize(gruppe));
        });*/
    }

    //Bei Veränderungen in den Gruppen die StartNr./Plätze zuweisen
    //und dann die Liste neu sortieren
    public void Sort(int indexSortBy)
    {
        if (Gruppen != null)
        {
            // Sortieren der Gruppen nach Startzeit und weisen Startnummern zu
            var sortedGruppen = Gruppen.OrderBy(gruppe => gruppe.StartzeitATeil).ThenBy(gruppe => gruppe.WettbewerbsbahnATeil).ThenBy(gruppe => gruppe.GruppenName).ToList();
            for (int i = 0; i < sortedGruppen.Count; i++)
            {
                sortedGruppen[i].StartNr = i + 1; // Startnummer zuweisen (beginnend bei 1)
            }

            // Sortieren der Gruppen nach Punkten und Plätze zuweisen
            sortedGruppen = Gruppen.OrderByDescending(gruppe => gruppe.GesamtPunkte)
                .ThenBy(gruppe => gruppe.FehlerATeil)//Kriterium 1=> "Anzahl der Fehlerpunkte gemäß Wertungsbögen"
                .ThenByDescending(gruppe => gruppe.PunkteATeil) //Kriterium 2 => "Besseres Endergebnis im A-Teil"
                .ThenByDescending(gruppe => gruppe.PunkteBTeil) //Kriterium 3 => "Besseres Endergebnis im B-Teil"
                .ThenBy(gruppe => gruppe.FehlerBTeil) //Kriterium 4 => Geringere Anzahl Minuspunkte im 400-m-Hindernislauf "Nur Summe der Fehlerpunkte gemäß Wertungsbögen"
                .ThenBy(gruppe => (gruppe.DurchschnittszeitKnotenATeil + gruppe.FehlerA + gruppe.FehlerW)) //Kriterium 5 => Besserer Zeittakt + Fehlerpunkte A und W.
                                                                                                           //Hinweis: BWB Ordnung sagt "Summe der Fehlerpunkte gemäß Wertungsbögen während des Zeittaktes für den Angriffstrupp und den Wassertrupp"
                                                                                                           //Leider ist dies Technisch nicht umsetzbar da die Beschränkung auf den Zeittakt bei den Fehlern nicht eingetragen wird, sondern nur die Gesamtfehler
                .ThenBy(gruppe => gruppe.Losentscheid) //Sortierung nach Losentscheid wenn eingegeben
                .ThenBy(gruppe => Guid.NewGuid()) //Final eine Random Sortierung
                .ToList();
            for (int i = 0; i < sortedGruppen.Count; i++)
            {
                sortedGruppen[i].Platz = i + 1; // Platz zuweisen (beginnend bei 1)
            }

            //Sofern etwas zum sortieren gewählt wurde, die Gruppen entsprechend sortieren
            switch (indexSortBy)
            {
                case 0:
                    sortedGruppen = Gruppen.OrderBy(gruppe => gruppe.GruppenName).ToList();
                    break;
                case 1:
                    sortedGruppen = Gruppen.OrderBy(gruppe => gruppe.StartzeitATeil).ThenBy(gruppe=>gruppe.WettbewerbsbahnATeil).ToList();
                    break;
                case 2:
                    sortedGruppen = Gruppen.OrderBy(gruppe => gruppe.StartzeitBTeil).ThenBy(gruppe => gruppe.WettbewerbsbahnBTeil).ToList();
                    break;
                case 3:
                    sortedGruppen = Gruppen.OrderByDescending(gruppe => gruppe.GesamtPunkte).ToList();
                    break;
                case 4:
                    sortedGruppen = Gruppen.OrderBy(gruppe => gruppe.GesamtAlter).ToList();
                    break;
                default:
                    break;
            }

            Gruppen = new ObservableCollection<Gruppe>(sortedGruppen);
            OnPropertyChanged(nameof(Gruppen));
        }

    }
    public List<PersonTeilnehmendenliste> alleTeilnehmenden()
    {
        List<PersonTeilnehmendenliste> alleTeilnehmenden = gruppen
            .SelectMany(gruppe => gruppe.Persons.Select(person => new PersonTeilnehmendenliste { Feuerwehr = gruppe.Feuerwehr, Gruppenname = gruppe.GruppenName, Person = person }))
            .ToList();

        return alleTeilnehmenden;
    }

    public List<PersonTeilnehmendenliste> personenMitGeburtstagBeimWettbewerb()
    {
        List<PersonTeilnehmendenliste> alleMitGeburtstagBeimWettbewerb = alleTeilnehmenden().Where(p =>
        p.Person.Geburtsdatum.Day == Einstellungen.Veranstaltungsdatum.Day &&
        p.Person.Geburtsdatum.Month == Einstellungen.Veranstaltungsdatum.Month
        ).ToList();
        return alleMitGeburtstagBeimWettbewerb;
    }


    public void RemoveSelectedGroup(Gruppe gruppe, bool loeschdialog = true)
    {
        if (loeschdialog)
        {
            MessageBoxResult result = MessageBox.Show($"Möchten Sie diese Gruppe wirklich löschen?\n{gruppe.GruppenName}", "Bestätigung", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                Gruppen.Remove(gruppe);
            }
        }
        else
        {
            Gruppen.Remove(gruppe);
        }

    }

    public void switchBoxTeilnehmer(int zielIndex, Gruppe gruppe)
    {
        Person ersatz = gruppe.Persons.Last();

        //Den zu ersetzenden vorher zwischenspeichern um ihn später hinten anzufügen
        Person zuErsetzen = new Person
        {
            Geburtsdatum = gruppe.Persons[zielIndex - 1].Geburtsdatum,
            Geschlecht = gruppe.Persons[zielIndex - 1].Geschlecht,
            Nachname = gruppe.Persons[zielIndex - 1].Nachname,
            Vorname = gruppe.Persons[zielIndex - 1].Vorname,

        };

        // An gewünschter Position den Ersatzmann/Frau eintragen
        gruppe.Persons[zielIndex - 1].Vorname = ersatz.Vorname;
        gruppe.Persons[zielIndex - 1].Nachname = ersatz.Nachname;
        gruppe.Persons[zielIndex - 1].Geschlecht = ersatz.Geschlecht;
        gruppe.Persons[zielIndex - 1].Geburtsdatum = ersatz.Geburtsdatum;

        // An letzter Position den ersetzten wieder eintragen
        gruppe.Persons[(gruppe.Persons.Count - 1)].Vorname = zuErsetzen.Vorname;
        gruppe.Persons[(gruppe.Persons.Count - 1)].Nachname = zuErsetzen.Nachname;
        gruppe.Persons[(gruppe.Persons.Count - 1)].Geschlecht = zuErsetzen.Geschlecht;
        gruppe.Persons[(gruppe.Persons.Count - 1)].Geburtsdatum = zuErsetzen.Geburtsdatum;

        OnPropertyChanged(nameof(Gruppen));
    }

    private ICommand removeGroupCommand;

    public ICommand RemoveGroupCommand
    {
        get
        {
            if (removeGroupCommand == null)
            {
                removeGroupCommand = new RelayCommand(param => this.RemoveSelectedGroup(param as Gruppe),
                                                        param => this.CanRemoveGroup(param as Gruppe));
            }
            return removeGroupCommand;
        }
    }

    private bool CanRemoveGroup(Gruppe gruppe)
    {
        // Überprüfen ob die Person entfernt werden kann, z.B. ob sie ausgewählt ist
        return gruppe != null;
    }

    internal void AddGroup(Gruppe gruppe)
    {
        // Falls Personen nicht gesetzt, werden leere Personen hinzugefügt bis 10 erreicht
        while (gruppe.Persons.Count < 10)
        {
            gruppe.Persons.Add(new Person());
        }
        gruppen.Add(gruppe);
    }



}