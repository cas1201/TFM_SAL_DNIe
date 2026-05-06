using System.Globalization;

namespace ColeHop.Converters
{
    public sealed class PickupStatusColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool alreadyPickedUp)
            {
                return alreadyPickedUp ? Colors.LightGreen : Color.FromArgb("#2196F3");
            }

            return Colors.Gray;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
