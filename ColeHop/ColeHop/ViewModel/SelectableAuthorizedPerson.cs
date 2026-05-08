using ColeHop.Model.Domain;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ColeHop.ViewModel
{
    public partial class SelectableAuthorizedPerson : ObservableObject
    {
        public AuthorizedPerson Person { get; }

        [ObservableProperty]
        private bool _isSelected;

        public SelectableAuthorizedPerson(AuthorizedPerson person)
        {
            Person = person;
        }

        public string Id => Person.Id;
        public string FullName => Person.FullName;
    }
}
