using ColeHop.Resources.Strings;
using System.Globalization;

namespace ColeHop.Converters
{
    public sealed class PickupStatusTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool alreadyPickedUp)
            {
                return alreadyPickedUp ? AppResources.AlreadyPickedUp : AppResources.PendingPickup;
            }

            return AppResources.UnknownStatus;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
