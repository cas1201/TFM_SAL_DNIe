using ColeHop.Services.Alert;
using ColeHop.Services.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ColeHop.ViewModels
{
    public sealed partial class SettingsViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _selectedLanguage = "es";

        [ObservableProperty]
        private string _selectedTheme = "System";

        [ObservableProperty]
        private bool _notificationsEnabled;

        [ObservableProperty]
        private string _appVersion = AppInfo.VersionString;

        public string VersionDisplay => $"{Resources.Strings.AppResources.VersionString}{AppVersion}";

        public bool IsSpanishSelected => SelectedLanguage == "es";
        public bool IsEnglishSelected => SelectedLanguage == "en";

        public bool IsLightSelected => SelectedTheme == "Light";
        public bool IsDarkSelected => SelectedTheme == "Dark";
        public bool IsSystemThemeSelected => SelectedTheme == "System";

        public SettingsViewModel(IAuthService auth, IAlertService alertService) : base(auth, alertService)
        {
            _selectedLanguage = Preferences.Get("app_language", "es");
            _selectedTheme = Preferences.Get("app_theme", "System");
            _notificationsEnabled = Preferences.Get("notifications_enabled", false);
        }

        partial void OnSelectedLanguageChanged(string value)
        {
            OnPropertyChanged(nameof(IsSpanishSelected));
            OnPropertyChanged(nameof(IsEnglishSelected));
        }

        partial void OnSelectedThemeChanged(string value)
        {
            OnPropertyChanged(nameof(IsLightSelected));
            OnPropertyChanged(nameof(IsDarkSelected));
            OnPropertyChanged(nameof(IsSystemThemeSelected));
        }

        [RelayCommand]
        private async Task ChangeLanguageAsync(string languageCode)
        {
            if (languageCode == SelectedLanguage)
                return;

            var title = Resources.Strings.AppResources.Success;
            var message = Resources.Strings.AppResources.LanguageChangedRestart;
            var accept = Resources.Strings.AppResources.OK;
            var cancel = Resources.Strings.AppResources.Cancel;

            var confirmed = await Alert.ShowConfirmAsync(title, message, accept, cancel);
            if (!confirmed)
                return;

            SelectedLanguage = languageCode;
            Preferences.Set("app_language", languageCode);

            // Aplicar cultura y recrear toda la UI
            App.ApplyLanguage(languageCode);
            App.RestartUI();
        }

        [RelayCommand]
        private void ChangeTheme(string theme)
        {
            SelectedTheme = theme;
            Preferences.Set("app_theme", theme);

            if (Application.Current is not null)
            {
                Application.Current.UserAppTheme = theme switch
                {
                    "Light" => AppTheme.Light,
                    "Dark" => AppTheme.Dark,
                    _ => AppTheme.Unspecified
                };
            }
        }

        async partial void OnNotificationsEnabledChanged(bool value)
        {
            if (value)
            {
                var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.PostNotifications>();
                    if (status != PermissionStatus.Granted)
                    {
                        _notificationsEnabled = false;
                        OnPropertyChanged(nameof(NotificationsEnabled));
                        Preferences.Set("notifications_enabled", false);
                        return;
                    }
                }
            }
            Preferences.Set("notifications_enabled", value);
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            await Auth.LogoutAsync();
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}