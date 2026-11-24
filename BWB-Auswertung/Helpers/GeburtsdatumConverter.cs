using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace BWB_Auswertung
{
    /// <summary>
    /// Wandelt ein Geburtsdatum in Text (dd.MM.yyyy) und zurück.
    /// Beim Zurückwandeln werden auch Kurzformen akzeptiert, damit die Eingabe schneller geht:
    /// 03.05.2014, 3.5.2014, 3.5.14, 03052014, 030514, 2014-05-03.
    /// Zweistellige Jahre werden so ergänzt, dass das Datum nicht in der Zukunft liegt.
    /// </summary>
    public class GeburtsdatumConverter : IValueConverter
    {
        private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

        private static readonly string[] FormateVierstelligesJahr =
        {
            "d.M.yyyy", "dd.MM.yyyy", "ddMMyyyy", "yyyy-MM-dd", "d-M-yyyy", "d/M/yyyy"
        };

        private static readonly string[] FormateZweistelligesJahr =
        {
            "d.M.yy", "dd.MM.yy", "ddMMyy", "d-M-yy", "d/M/yy"
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is DateTime datum ? datum.ToString("dd.MM.yyyy", De) : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (TryParse(value as string, DateTime.Today, out DateTime datum))
            {
                return datum;
            }
            //Ungültige Eingabe: Wert nicht übernehmen, TextBox zeigt den Validierungsfehler (roter Rahmen)
            return DependencyProperty.UnsetValue;
        }

        public static bool TryParse(string? text, DateTime heute, out DateTime datum)
        {
            datum = default;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string eingabe = new string(text.Trim().Where(c => !char.IsWhiteSpace(c)).ToArray());

            if (DateTime.TryParseExact(eingabe, FormateVierstelligesJahr, De, DateTimeStyles.None, out datum))
            {
                return true;
            }

            if (DateTime.TryParseExact(eingabe, FormateZweistelligesJahr, De, DateTimeStyles.None, out DateTime kurz))
            {
                //Jahrhundert selbst bestimmen statt der Windows-Einstellung (Standard: bis 2049 -> 20xx)
                int jahr = 2000 + (kurz.Year % 100);
                if (jahr > heute.Year)
                {
                    jahr -= 100;
                }
                if (DateTime.DaysInMonth(jahr, kurz.Month) < kurz.Day)
                {
                    return false; //z.B. 29.02. in einem Nicht-Schaltjahr
                }
                datum = new DateTime(jahr, kurz.Month, kurz.Day);
                return true;
            }

            return false;
        }
    }
}
