using ColeHop.Services.Alert;
using ColeHop.Services.Auth;
using ColeHop.Models;
using ColeHop.Resources.Strings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ColeHop.ViewModels;

public sealed partial class LoginViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    public LoginViewModel(IAuthService auth, IAlertService alertService) : base(auth, alertService) { }

    [RelayCommand]
    private async Task GoToTeacherAsync()
    {
        try
        {
            IsBusy = true;
            // Login como profesor
            await Auth.SimulateLoginAsync(UserRole.Teacher);
        }
        catch (Exception ex)
        {
            await Alert.ShowAsync(AppResources.Error, ex.Message, AppResources.OK, AlertIcon.Error);
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
            // Login como tutor
            await Auth.SimulateLoginAsync(UserRole.Tutor);
        }
        catch (Exception ex)
        {
            await Alert.ShowAsync(AppResources.Error, ex.Message, AppResources.OK, AlertIcon.Error);
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