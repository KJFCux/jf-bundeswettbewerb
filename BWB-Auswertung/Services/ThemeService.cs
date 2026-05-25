using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using BWB_Auswertung.Properties;
using ControlzEx.Theming;
using Microsoft.Win32;

namespace BWB_Auswertung.Services
{
    public enum ThemePreference
    {
        [System.ComponentModel.Description("Systemstandard")]
        System,
        [System.ComponentModel.Description("Hell")]
        Light,
        [System.ComponentModel.Description("Dunkel")]
        Dark
    }

    public sealed class ThemeService : INotifyPropertyChanged
    {
        public static readonly Color DjfBlue = (Color)ColorConverter.ConvertFromString("#00469D");

        private static ThemeService _instance;
        public static ThemeService Current => _instance ??= new ThemeService();

        private ThemePreference _preference;
        private bool _systemEventsHooked;
        private Theme _lightTheme;
        private Theme _darkTheme;

        public ThemePreference Preference
        {
            get => _preference;
            set
            {
                if (_preference == value) return;
                _preference = value;
                OnPropertyChanged();
                Apply(value);
            }
        }

        public void Initialize()
        {
            RegisterAccentThemes();

            var stored = Settings.Default.ThemePreference;
            if (!Enum.TryParse(stored, out ThemePreference pref))
                pref = ThemePreference.System;

            _preference = pref;
            Apply(pref);
            OnPropertyChanged(nameof(Preference));
        }

        private void RegisterAccentThemes()
        {
            _lightTheme = RuntimeThemeGenerator.Current.GenerateRuntimeTheme("Light", DjfBlue);
            _darkTheme = RuntimeThemeGenerator.Current.GenerateRuntimeTheme("Dark", DjfBlue);

            if (_lightTheme != null) ThemeManager.Current.AddTheme(_lightTheme);
            if (_darkTheme != null) ThemeManager.Current.AddTheme(_darkTheme);
        }

        public void Apply(ThemePreference pref)
        {
            if (pref == ThemePreference.System) HookSystemEvents();
            else UnhookSystemEvents();

            var theme = ResolveTheme(pref);
            if (theme == null) return;

            var app = System.Windows.Application.Current;
            if (app == null) return;

            ThemeManager.Current.ChangeTheme(app, theme);
        }

        private Theme ResolveTheme(ThemePreference pref)
        {
            switch (pref)
            {
                case ThemePreference.Light: return _lightTheme;
                case ThemePreference.Dark: return _darkTheme;
                default: return IsSystemDark() ? _darkTheme : _lightTheme;
            }
        }

        private static bool IsSystemDark()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                if (value is int i) return i == 0;
            }
            catch
            {
                // fall through to default light
            }
            return false;
        }

        private void HookSystemEvents()
        {
            if (_systemEventsHooked) return;
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            _systemEventsHooked = true;
        }

        private void UnhookSystemEvents()
        {
            if (!_systemEventsHooked) return;
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            _systemEventsHooked = false;
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.General) return;
            if (_preference != ThemePreference.System) return;

            var app = System.Windows.Application.Current;
            app?.Dispatcher.Invoke(() => Apply(ThemePreference.System));
        }

        public void Persist()
        {
            Settings.Default.ThemePreference = _preference.ToString();
            Settings.Default.Save();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
