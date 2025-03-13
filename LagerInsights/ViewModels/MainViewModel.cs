using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using LagerInsights.Models;

public class MainViewModel : INotifyPropertyChanged
{
    private double _scaleFactor = 1.0;

    private double _scaleFactorEvaluation = 0.6;

    private double _scaleFactorSettings = 1.0;
    private ObservableCollection<Art> artList;
    private Settings einstellungen;
    private ObservableCollection<Jugendfeuerwehr> gruppen;
    private ObservableCollection<string> zeltdoerfer;


    private ICommand removeGroupCommand;
    private ObservableCollection<Status> statusList;

    public MainViewModel()
    {
        //Leere Collections erstellen
        Einstellungen = new Settings();
        Gruppen = new ObservableCollection<Jugendfeuerwehr>
        {
            //Testdaten können hier eingefügt werden
        };
        ArtList = new ObservableCollection<Art>
        {
            Art.UNTERFLURHYDRANT,
            Art.OFFENESGEWAESSER,
            Art.KEINEVORGABEZEIT
        };
        Zeltdoerfer = new ObservableCollection<string>(Einstellungen.Zeltdoerfer);

    }

    public ObservableCollection<Art> ArtList
    {
        get => artList;
        set
        {
            artList = value;
            OnPropertyChanged(nameof(ArtList));
        }
    }

    public ObservableCollection<Status> StatusList
    {
        get => statusList;
        set
        {
            statusList = value;
            OnPropertyChanged(nameof(StatusList));
        }
    }

    public double ScaleFactor
    {
        get => _scaleFactor;
        set
        {
            if (_scaleFactor != value)
            {
                _scaleFactor = value;
                OnPropertyChanged(nameof(ScaleFactor));
            }
        }
    }

    public double ScaleFactorEvaluation
    {
        get => _scaleFactorEvaluation;
        set
        {
            if (_scaleFactorEvaluation != value)
            {
                _scaleFactorEvaluation = value;
                OnPropertyChanged(nameof(ScaleFactorEvaluation));
            }
        }
    }

    public double ScaleFactorSettings
    {
        get => _scaleFactorSettings;
        set
        {
            if (_scaleFactorSettings != value)
            {
                _scaleFactorSettings = value;
                OnPropertyChanged(nameof(ScaleFactorSettings));
            }
        }
    }

    public ObservableCollection<Jugendfeuerwehr> Gruppen
    {
        get => gruppen;
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
    public ObservableCollection<string> Zeltdoerfer
    {
        get => zeltdoerfer;
        set
        {
            zeltdoerfer = value;
            OnPropertyChanged(nameof(Zeltdoerfer));
        }
    }


    public List<PersonTeilnehmendenliste> PersonenTeilnehmendenliste => alleTeilnehmenden();
    public List<PersonTeilnehmendenliste> PersonenMitGeburtstagBeimZeltlager => personenMitGeburtstagBeimWettbewerb();

    public decimal BereitsInsgesamtBezahlt => Gruppen.Sum(gruppe => gruppe.GezahlterBeitrag ?? 0);
    public decimal ZuBezahlenderBetragGesamt => Gruppen.Sum(gruppe => gruppe.ZuBezahlenderBetrag);

    public Settings Einstellungen
    {
        get => einstellungen;
        set
        {
            einstellungen = value;
            OnPropertyChanged(nameof(Einstellungen));
        }
    }

    public ICommand RemoveGroupCommand
    {
        get
        {
            if (removeGroupCommand == null)
                removeGroupCommand = new RelayCommand(param => RemoveSelectedGroup(param as Jugendfeuerwehr),
                    param => CanRemoveGroup(param as Jugendfeuerwehr));
            return removeGroupCommand;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void OverrideSettings(Settings einstellungenNew)
    {
        Einstellungen = einstellungenNew;
        Zeltdoerfer.Clear();
        foreach (var zeltdorf in einstellungenNew.Zeltdoerfer)
        {
            Zeltdoerfer.Add(zeltdorf);
        }
        OnPropertyChanged(nameof(Einstellungen));
    }

    //Erstellt eine Leere Gruppe
    public void AddEmptyGruppe(string feuerwehr)
    {
        //10 Leere Personen erstellen damit diese bearbeitet werden können
        List<Person> persons = new();
        for (var i = 0; i <= 9; i++) persons.Add(new Person());

        var verantwortlicher = new Verantwortlicher();

        //Leere Gruppe erstellen
        var newJugendfeuerwehr = new Jugendfeuerwehr
        {
            Feuerwehr = feuerwehr,
            Persons = persons,
            Verantwortlicher = verantwortlicher,
            TimeStampAnmeldung = DateTime.Now,
            TimeStampAenderung = DateTime.Now
        };

        Gruppen.Add(newJugendfeuerwehr);
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
            // Sortieren der Gruppen nach Anmeldezeit und weise Lagernummern zu
            var sortedGruppen = Gruppen.OrderBy(gruppe => gruppe.TimeStampAnmeldung).ThenBy(gruppe => gruppe.Feuerwehr)
                .ToList();
            for (var i = 0; i < sortedGruppen.Count; i++)
                sortedGruppen[i].LagerNr = i + 1; // Lagernummer zuweisen (beginnend bei 1)


            //Sofern etwas zum sortieren gewählt wurde, die Gruppen entsprechend sortieren
            switch (indexSortBy)
            {
                case 0:
                    sortedGruppen = Gruppen.OrderBy(gruppe => gruppe.Feuerwehr).ToList();
                    break;
                case 1:
                    sortedGruppen = Gruppen.OrderByDescending(gruppe => gruppe.Feuerwehr).ToList();
                    break;
                case 2:
                    sortedGruppen = Gruppen.OrderBy(gruppe => gruppe.Organisationseinheit)
                        .ThenBy(gruppe => gruppe.Feuerwehr).ToList();
                    break;
                case 3:
                    sortedGruppen = Gruppen.OrderBy(gruppe => gruppe.TimeStampAnmeldung)
                        .ThenBy(gruppe => gruppe.Feuerwehr).ToList();
                    break;
                case 4:
                    sortedGruppen = Gruppen.OrderBy(gruppe => gruppe.LagerNr).ToList();
                    break;
            }

            Gruppen = new ObservableCollection<Jugendfeuerwehr>(sortedGruppen);
            OnPropertyChanged(nameof(Gruppen));
        }
    }

    public List<PersonTeilnehmendenliste> alleTeilnehmenden()
    {
        List<PersonTeilnehmendenliste> alleTeilnehmenden = gruppen
            .SelectMany(gruppe => gruppe.Persons.Select(person => new PersonTeilnehmendenliste
            { Feuerwehr = gruppe.Feuerwehr, Person = person }))
            .ToList();

        return alleTeilnehmenden;
    }

    public List<PersonTeilnehmendenliste> personenMitGeburtstagBeimWettbewerb()
    {
        List<PersonTeilnehmendenliste> alleMitGeburtstagBeimWettbewerb = alleTeilnehmenden().Where(p =>
            p.Person.Geburtsdatum.Day >= Einstellungen.Veranstaltungsdatum.Day &&
            p.Person.Geburtsdatum.Month >= Einstellungen.Veranstaltungsdatum.Month &&
            p.Person.Geburtsdatum.Day <= Einstellungen.VeranstaltungsdatumEnde.Day &&
            p.Person.Geburtsdatum.Month <= Einstellungen.VeranstaltungsdatumEnde.Month
        ).ToList();
        return alleMitGeburtstagBeimWettbewerb;
    }

    public List<PersonTeilnehmendenliste> PersonenMitEssgewohnheitenUndUnvertraeglichkeitenBeimZeltlager()
    {
        List<PersonTeilnehmendenliste> alleMitUnvertraeglichkeitenBeimZeltlager = alleTeilnehmenden().Where(p =>
            p.Person.Essgewohnheiten.ToLower() != "alles" ||
            p.Person.Unvertraeglichkeiten.ToLower() != "keine"
        ).ToList();
        return alleMitUnvertraeglichkeitenBeimZeltlager;
    }
    public List<PersonTeilnehmendenliste> PersonenMitEssgewohnheitenBeimZeltlager()
    {
        List<PersonTeilnehmendenliste> alleMitUnvertraeglichkeitenBeimZeltlager = alleTeilnehmenden().Where(p =>
            p.Person.Essgewohnheiten.ToLower() != "alles"
        ).ToList();
        return alleMitUnvertraeglichkeitenBeimZeltlager;
    }

    public List<PersonTeilnehmendenliste> PersonenMitUnvertraeglichkeitenBeimZeltlager()
    {
        List<PersonTeilnehmendenliste> alleMitUnvertraeglichkeitenBeimZeltlager = alleTeilnehmenden().Where(p =>
            p.Person.Unvertraeglichkeiten.ToLower() != "keine"
        ).ToList();
        return alleMitUnvertraeglichkeitenBeimZeltlager;
    }


    public int AnzahlVegetarisch()
    {
        int alleVegetarisch = alleTeilnehmenden().Where(p =>
            p.Person.Essgewohnheiten.ToLower().Contains("vegetarisch")).ToList().Count;
        return alleVegetarisch;
    }

    public int AnzahlVegan()
    {
        int alleVegan = alleTeilnehmenden().Where(p =>
            p.Person.Essgewohnheiten.ToLower().Contains("vegan")).ToList().Count;
        return alleVegan;
    }
    public int AnzahlSonstigeEssgewohnheiten()
    {
        int alleVegan = alleTeilnehmenden().Where(p =>
            !p.Person.Essgewohnheiten.ToLower().Contains("vegan") &&
            !p.Person.Essgewohnheiten.ToLower().Contains("vegetarisch") &&
            !p.Person.Essgewohnheiten.ToLower().Contains("alles")
            ).ToList().Count;
        return alleVegan;
    }

    public int AnzahlUnvertraeglichkeiten()
    {
        int alleUnvertraeglichkeiten = alleTeilnehmenden().Where(p =>
            p.Person.Unvertraeglichkeiten.ToLower() != "keine").ToList().Count;
        return alleUnvertraeglichkeiten;
    }


    public void RemoveSelectedGroup(Jugendfeuerwehr jugendfeuerwehr, bool loeschdialog = true)
    {
        if (loeschdialog)
        {
            var result = MessageBox.Show($"Möchten Sie diese Feuerwehr wirklich löschen?\n{jugendfeuerwehr.Feuerwehr}",
                "Bestätigung", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK) Gruppen.Remove(jugendfeuerwehr);
        }
        else
        {
            Gruppen.Remove(jugendfeuerwehr);
        }
    }

    private bool CanRemoveGroup(Jugendfeuerwehr jugendfeuerwehr)
    {
        // Überprüfen ob die Person entfernt werden kann, z.B. ob sie ausgewählt ist
        return jugendfeuerwehr != null;
    }

    internal void AddGroup(Jugendfeuerwehr jugendfeuerwehr)
    {
        gruppen.Add(jugendfeuerwehr);
    }
}