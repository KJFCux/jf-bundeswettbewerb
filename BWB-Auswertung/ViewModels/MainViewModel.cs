using LagerInsights.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using LagerInsights.IO;
using System.Linq;
using System.Windows;
using System.IO;

public class MainViewModel : INotifyPropertyChanged
{
    private ObservableCollection<Gruppe> gruppen;
    private Settings einstellungen;
    private ObservableCollection<Art> artList;
    private ObservableCollection<Status> statusList;

    public ObservableCollection<Art> ArtList
    {
        get { return artList; }
        set
        {
            artList = value;
            OnPropertyChanged(nameof(ArtList));
        }
    }

    public ObservableCollection<Status> StatusList
    {
        get { return statusList; }
        set
        {
            statusList = value;
            OnPropertyChanged(nameof(StatusList));
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
    public List<PersonTeilnehmendenliste> PersonenMitGeburtstagBeimZeltlager => personenMitGeburtstagBeimWettbewerb();

    public decimal BereitsInsgesamtBezahlt => Gruppen.Sum(gruppe => gruppe.GezahlterBeitrag ?? 0);
    public decimal ZuBezahlenderBetragGesamt => Gruppen.Sum(gruppe => gruppe.ZuBezahlenderBetrag);

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
    public void AddEmptyGruppe(string feuerwehr)
    {

        //10 Leere Personen erstellen damit diese bearbeitet werden können
        List<Person> persons = new List<Person>();
        for (int i = 0; i <= 9; i++)
        {
            persons.Add(new Person());
        }

        Verantwortlicher verantwortlicher = new Verantwortlicher();

        //Leere Gruppe erstellen
        Gruppe newGruppe = new Gruppe
        {
            Feuerwehr = feuerwehr,
            Persons = persons,
            Verantwortlicher = verantwortlicher,
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
            // Sortieren der Gruppen nach Anmeldezeit und weise Lagernummern zu
            var sortedGruppen = Gruppen.OrderBy(gruppe => gruppe.TimeStampAnmeldung).ThenBy(gruppe => gruppe.Feuerwehr).ToList();
            for (int i = 0; i < sortedGruppen.Count; i++)
            {
                sortedGruppen[i].LagerNr = i + 1; // Lagernummer zuweisen (beginnend bei 1)
            }


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
                    sortedGruppen = Gruppen.OrderBy(gruppe => gruppe.Organisationseinheit).ThenBy(gruppe => gruppe.Feuerwehr).ToList();
                    break;
                case 3:
                    sortedGruppen = Gruppen.OrderBy(gruppe => gruppe.TimeStampAnmeldung).ThenBy(gruppe => gruppe.Feuerwehr).ToList();
                    break;
                case 4:
                    sortedGruppen = Gruppen.OrderBy(gruppe => gruppe.LagerNr).ToList();
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
            .SelectMany(gruppe => gruppe.Persons.Select(person => new PersonTeilnehmendenliste { Feuerwehr = gruppe.Feuerwehr, Person = person }))
            .ToList();

        return alleTeilnehmenden;
    }

    public List<PersonTeilnehmendenliste> personenMitGeburtstagBeimWettbewerb()
    {
        List<PersonTeilnehmendenliste> alleMitGeburtstagBeimWettbewerb = alleTeilnehmenden().Where(p =>
        (p.Person.Geburtsdatum.Day >= Einstellungen.Veranstaltungsdatum.Day &&
        p.Person.Geburtsdatum.Month >= Einstellungen.Veranstaltungsdatum.Month) &&
        (p.Person.Geburtsdatum.Day <= Einstellungen.VeranstaltungsdatumEnde.Day &&
         p.Person.Geburtsdatum.Month <= Einstellungen.VeranstaltungsdatumEnde.Month)
        ).ToList();
        return alleMitGeburtstagBeimWettbewerb;
    }


    public void RemoveSelectedGroup(Gruppe gruppe, bool loeschdialog = true)
    {
        if (loeschdialog)
        {
            MessageBoxResult result = MessageBox.Show($"Möchten Sie diese Feuerwehr wirklich löschen?\n{gruppe.Feuerwehr}", "Bestätigung", MessageBoxButton.OKCancel);
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
        gruppen.Add(gruppe);
    }



}