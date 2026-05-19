using ColeHop.Services.Alert;
using ColeHop.Services.Auth;
using ColeHop.Services.TutorManagement;
using ColeHop.Models;
using ColeHop.Resources.Strings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ColeHop.ViewModels
{
    public sealed partial class AuthorizationViewModel : BaseViewModel
    {
        private readonly ITutorManagementService _tutorManagementService;

        [ObservableProperty]
        private int _currentStep = 1;

        [ObservableProperty]
        private int _totalSteps = 4;

        [ObservableProperty]
        private DateTime _fromDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _toDate = DateTime.Today.AddDays(7);

        [ObservableProperty]
        private DateTime _minimumDate = DateTime.Today;

        [ObservableProperty]
        private ObservableCollection<SelectableChild> _children = new();

        [ObservableProperty]
        private ObservableCollection<SelectableAuthorizedPerson> _authorizedPeople = new();

        [ObservableProperty]
        private string _selectedQuickDate = "";

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private bool _isStep1Visible = true;

        [ObservableProperty]
        private bool _isStep2Visible;

        [ObservableProperty]
        private bool _isStep3Visible;

        [ObservableProperty]
        private bool _isStep4Visible;

        [ObservableProperty]
        private bool _canGoBack;

        [ObservableProperty]
        private bool _canGoNext = true;

        [ObservableProperty]
        private string _nextButtonText = string.Empty;

        public double StepProgress => CurrentStep / (double)TotalSteps;

        public AuthorizationViewModel(IAuthService auth, IAlertService alertService, ITutorManagementService tutorManagementService)
            : base(auth, alertService)
        {
            _tutorManagementService = tutorManagementService;
            UpdateNavigationState();
        }

        public async Task InitializeAsync()
        {
            await LoadDataAsync();
        }

        private void UpdateNavigationState()
        {
            CanGoBack = CurrentStep > 1;
            CanGoNext = CurrentStep < TotalSteps;

            NextButtonText = CurrentStep == TotalSteps 
                ? AppResources.Finish 
                : AppResources.Next;

            OnPropertyChanged(nameof(StepProgress));
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;
                var tutorId = Auth.CurrentUserId;
                if (string.IsNullOrEmpty(tutorId))
                    return;

                var children = await _tutorManagementService.GetChildrenAsync(tutorId);
                var authorizedPeople = await _tutorManagementService.GetAuthorizedPersonsAsync(tutorId);

                Children.Clear();
                foreach (var child in children)
                {
                    Children.Add(new SelectableChild(child));
                }

                AuthorizedPeople.Clear();
                foreach (var person in authorizedPeople)
                {
                    AuthorizedPeople.Add(new SelectableAuthorizedPerson(person));
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"{AppResources.ErrorLoadingData}: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void ToggleChildSelection(SelectableChild child)
        {
            child.IsSelected = !child.IsSelected;
        }

        [RelayCommand]
        private void SelectPerson(SelectableAuthorizedPerson person)
        {
            // Deselect all others
            foreach (var p in AuthorizedPeople)
            {
                p.IsSelected = false;
            }
            // Select this one
            person.IsSelected = true;
        }

        [RelayCommand]
        private void SetQuickDate(string period)
        {
            SelectedQuickDate = period;
            FromDate = DateTime.Today;

            ToDate = period switch
            {
                "today" => DateTime.Today,
                "week" => DateTime.Today.AddDays(7),
                "month" => DateTime.Today.AddMonths(1),
                _ => ToDate
            };
        }

        [RelayCommand]
        private void NextStep()
        {
            HasError = false;
            ErrorMessage = string.Empty;

            // Validar paso actual antes de avanzar
            if (CurrentStep == 1 && !Children.Any(c => c.IsSelected))
            {
                HasError = true;
                ErrorMessage = AppResources.SelectAtLeastOneChildMessage;
                return;
            }

            if (CurrentStep == 2 && !AuthorizedPeople.Any(p => p.IsSelected))
            {
                HasError = true;
                ErrorMessage = AppResources.SelectAuthorizedPerson;
                return;
            }

            if (CurrentStep == 3)
            {
                // Validar fechas antes de pasar al resumen
                if (ToDate < FromDate)
                {
                    HasError = true;
                    ErrorMessage = AppResources.EndDateMustBeAfterStartDate;
                    return;
                }
            }

            if (CurrentStep < TotalSteps)
            {
                CurrentStep++;
                UpdateStepVisibility();
                UpdateNavigationState();
            }
            else
            {
                _ = CreateAuthorizationAsync();
            }
        }

        [RelayCommand]
        private void PreviousStep()
        {
            if (CurrentStep > 1)
            {
                CurrentStep--;
                UpdateStepVisibility();
                UpdateNavigationState();
                HasError = false;
                ErrorMessage = string.Empty;
            }
        }

        private void UpdateStepVisibility()
        {
            IsStep1Visible = CurrentStep == 1;
            IsStep2Visible = CurrentStep == 2;
            IsStep3Visible = CurrentStep == 3;
            IsStep4Visible = CurrentStep == 4;
        }

        private async Task CreateAuthorizationAsync()
        {
            if (!ValidateInput())
                return;

            try
            {
                IsBusy = true;
                HasError = false;
                ErrorMessage = string.Empty;

                var tutorId = Auth.CurrentUserId 
                    ?? throw new InvalidOperationException("Usuario no autenticado");

                var selectedChildren = Children.Where(c => c.IsSelected).ToList();
                var selectedPerson = AuthorizedPeople.FirstOrDefault(p => p.IsSelected);

                if (selectedPerson == null)
                {
                    HasError = true;
                    ErrorMessage = AppResources.SelectAuthorizedPerson;
                    return;
                }

                var childIds = selectedChildren.Select(c => c.Id).ToList();
                var authorizationData = new AuthorizationData(
                    childIds,
                    selectedPerson.Id,
                    DateOnly.FromDateTime(FromDate),
                    DateOnly.FromDateTime(ToDate)
                );

                await _tutorManagementService.CreateAuthorizationAsync(tutorId, authorizationData);
                await Alert.ShowAsync(AppResources.Authorization, AppResources.AuthorizationCreatedSuccessfully, AppResources.OK);
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"{AppResources.ErrorCreatingAuthorization}: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        private bool ValidateInput()
        {
            if (!Children.Any(c => c.IsSelected))
            {
                HasError = true;
                ErrorMessage = AppResources.SelectAtLeastOneChild;
                return false;
            }

            if (!AuthorizedPeople.Any(p => p.IsSelected))
            {
                HasError = true;
                ErrorMessage = AppResources.SelectAuthorizedPerson;
                return false;
            }

            if (ToDate < FromDate)
            {
                HasError = true;
                ErrorMessage = AppResources.EndDateMustBeAfterStartDate;
                return false;
            }

            return true;
        }
    }
}
