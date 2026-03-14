using System;
using System.Globalization;
using System.Windows.Data;
using System.Collections.Generic;

namespace CommonTerms.Helpers
{
    public class KeyValueToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is KeyValuePair<string, int> kv)
                return $"{kv.Key} = {kv.Value}";

            var type = value?.GetType();
            if (type?.IsGenericType == true && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                var key = type.GetProperty("Key")?.GetValue(value)?.ToString() ?? string.Empty;
                var val = type.GetProperty("Value")?.GetValue(value)?.ToString() ?? "0";
                return $"{key} = {val}";
            }

            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}