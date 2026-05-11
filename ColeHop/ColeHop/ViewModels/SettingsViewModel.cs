using ColeHop.Services.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;

namespace ColeHop.ViewModels
{
    public sealed partial class SettingsViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _selectedLanguage = "es";

        [ObservableProperty]
        private bool _notificationsEnabled = true;

        [ObservableProperty]
        private string _appVersion = "1.0.0";

        public SettingsViewModel(IAuthService auth) : base(auth)
        {
            _selectedLanguage = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        }

        [RelayCommand]
        private async Task ChangeLanguageAsync(string languageCode)
        {
            SelectedLanguage = languageCode;
            var culture = new CultureInfo(languageCode);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            await Shell.Current.DisplayAlertAsync(
                Resources.Strings.AppResources.Success,
                "Language changed. Restart app to apply changes.",
                Resources.Strings.AppResources.OK);
        }

        [RelayCommand]
        private void ToggleNotifications()
        {
            NotificationsEnabled = !NotificationsEnabled;
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