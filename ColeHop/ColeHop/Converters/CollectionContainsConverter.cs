using System.Collections;
using System.Globalization;

namespace ColeHop.Converters
{
    public class CollectionContainsConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is IEnumerable collection && parameter != null)
            {
                foreach (var item in collection)
                {
                    if (item == parameter || (item != null && item.Equals(parameter)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
