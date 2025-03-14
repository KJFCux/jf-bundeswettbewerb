using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Markup;

namespace LagerInsights.Models;

public enum Art
{
    [Description("Offenes Gewässer")] OFFENESGEWAESSER,
    [Description("Unterflurhydrant")] UNTERFLURHYDRANT,
    [Description("Keine Vorgabezeit")] KEINEVORGABEZEIT
}

[Serializable]
public class Settings
{
    public Settings()
    {
        Hostname = string.Empty;
        Pfad = string.Empty;
        Username = string.Empty;
        Password = string.Empty;
        Veranstaltungsleitung = string.Empty;
        Veranstaltungsort = string.Empty;
        Veranstaltungstitel = string.Empty;
        Teilnehmendenbeitrag = 0m;
        Art = Art.OFFENESGEWAESSER;
        Veranstaltungsdatum = DateTime.Today;
        VeranstaltungsdatumEnde = DateTime.Today.AddDays(7);
        Zeltdoerfer = new List<string>();
    }

    public string Veranstaltungsort { get; set; }

    public DateTime Veranstaltungsdatum { get; set; }
    public DateTime VeranstaltungsdatumEnde { get; set; }

    public Art Art { get; set; }

    public string Veranstaltungstitel { get; set; }
    public string Veranstaltungsleitung { get; set; }
    public string Logopfad { get; set; }

    //Segment für die Urkunden
    public string Namelinks { get; set; }
    public string Funktionlinks { get; set; }
    public string Unterschriftlinks { get; set; }


    public string Namerechts { get; set; }
    public string Funktionrechts { get; set; }
    public string Unterschriftrechts { get; set; }

    public decimal Teilnehmendenbeitrag { get; set; }

    public List<string> Zeltdoerfer { get; set; }

    //FTP Daten
    public string Hostname { get; set; }

    public string Pfad { get; set; }

    public string Username { get; set; }

    public string Password { get; set; }


    public int Vorgabezeit
    {
        get
        {
            switch (Art)
            {
                case Art.OFFENESGEWAESSER: return 420;
                case Art.UNTERFLURHYDRANT: return 360;
                case Art.KEINEVORGABEZEIT: return 0;
                default: return 0;
            }
        }
    }
}

public class EnumerationExtension : MarkupExtension
{
    private Type _enumType;


    public EnumerationExtension(Type enumType)
    {
        if (enumType == null)
            throw new ArgumentNullException("enumType");

        EnumType = enumType;
    }

    public Type EnumType
    {
        get => _enumType;
        private set
        {
            if (_enumType == value)
                return;

            var enumType = Nullable.GetUnderlyingType(value) ?? value;

            if (enumType.IsEnum == false)
                throw new ArgumentException("Type must be an Enum.");

            _enumType = value;
        }
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var enumValues = Enum.GetValues(EnumType);

        return (
            from object enumValue in enumValues
            select new EnumerationMember
            {
                Value = enumValue,
                Description = GetDescription(enumValue)
            }).ToArray();
    }

    private string GetDescription(object enumValue)
    {
        var descriptionAttribute = EnumType
            .GetField(enumValue.ToString())
            .GetCustomAttributes(typeof(DescriptionAttribute), false)
            .FirstOrDefault() as DescriptionAttribute;


        return descriptionAttribute != null
            ? descriptionAttribute.Description
            : enumValue.ToString();
    }

    public class EnumerationMember
    {
        public string Description { get; set; }
        public object Value { get; set; }
    }
}