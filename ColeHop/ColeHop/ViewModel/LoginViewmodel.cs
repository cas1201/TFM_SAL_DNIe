using ColeHop.Core.Services.Auth;
using ColeHop.Model.Identity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ColeHop.ViewModel;

public sealed partial class LoginViewmodel : BaseViewModel
{
    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    public LoginViewmodel(IAuthService auth) : base(auth) { }

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
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
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
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
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