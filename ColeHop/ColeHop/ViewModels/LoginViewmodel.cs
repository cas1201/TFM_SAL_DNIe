using ColeHop.Services.Alert;
using ColeHop.Services.Auth;
using ColeHop.Models;
using ColeHop.Resources.Strings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ColeHop.ViewModels;

public sealed partial class LoginViewmodel : BaseViewModel
{
    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    public LoginViewmodel(IAuthService auth, IAlertService alertService) : base(auth, alertService) { }

    [RelayCommand]
    private async Task GoToTeacherAsync()
    {
        try
        {
            IsBusy = true;
            // Simulación de login como profesor
            await Auth.SimulateLoginAsync(UserRole.Teacher);
            // El evento AuthenticationStateChanged en AppShell se encargará de la navegación
        }
        catch (Exception ex)
        {
            await Alert.ShowAsync(AppResources.Error, ex.Message, AppResources.OK);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToTutorAsync()
    {
        try
        {
            IsBusy = true;
            // Simulación de login como tutor
            await Auth.SimulateLoginAsync(UserRole.Tutor);
            // El evento AuthenticationStateChanged en AppShell se encargará de la navegación
        }
        catch (Exception ex)
        {
            await Alert.ShowAsync(AppResources.Error, ex.Message, AppResources.OK);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToSignupAsync()
    {
        await Shell.Current.GoToAsync("signup");
    }
}