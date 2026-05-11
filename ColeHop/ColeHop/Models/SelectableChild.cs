using CommunityToolkit.Mvvm.ComponentModel;

namespace ColeHop.Models
{
    public partial class SelectableChild : ObservableObject
    {
        public Child Child { get; }

        [ObservableProperty]
        private bool _isSelected;

        public SelectableChild(Child child)
        {
            Child = child;
        }

        public string Id => Child.Id;
        public string FullName => Child.FullName;
        public string Grade => Child.Grade;
    }
}
