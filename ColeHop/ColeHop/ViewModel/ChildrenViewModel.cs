using ColeHop.Core.Services.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ColeHop.ViewModel
{
    public sealed partial class ChildrenViewModel : BaseViewModel
    {
        [ObservableProperty]
        private ObservableCollection<ChildItem> _children = new();

        public ChildrenViewModel(IAuthService auth) : base(auth) { }

        public void Initialize()
        {
            // Datos simulados para navegación básica
            Children.Clear();
            Children.Add(new ChildItem("Juan Pérez", "3º Primaria"));
            Children.Add(new ChildItem("Ana García", "1º Primaria"));
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    // Modelo simple para la lista
    public sealed record ChildItem(string FullName, string Grade);
}
