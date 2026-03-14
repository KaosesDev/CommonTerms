using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Appearance;

namespace CommonTerms.Helpers
{
    /// <summary>
    /// Converts an <see cref="ApplicationTheme"/> enum value to a boolean for use in WPF bindings.
    /// Returns true if the enum value matches the parameter; otherwise, false.
    /// Also supports converting back from boolean to enum value.
    /// </summary>
    public class EnumToBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Converts an <see cref="ApplicationTheme"/> enum value to a boolean.
        /// Returns true if the value matches the enum name provided in <paramref name="parameter"/>.
        /// </summary>
        /// <param name="value">The enum value to compare.</param>
        /// <param name="targetType">The target binding type (ignored).</param>
        /// <param name="parameter">The enum name as a string.</param>
        /// <param name="culture">The culture info (ignored).</param>
        /// <returns>True if the value matches the parameter; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown if parameter is not a string or value is not a valid enum.</exception>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not String enumString)
            {
                throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName");
            }

            if (!Enum.IsDefined(typeof(ApplicationTheme), value))
            {
                throw new ArgumentException("ExceptionEnumToBooleanConverterValueMustBeAnEnum");
            }

            var enumValue = Enum.Parse(typeof(ApplicationTheme), enumString);

            return enumValue.Equals(value);
        }

        /// <summary>
        /// Converts a boolean value back to an <see cref="ApplicationTheme"/> enum value.
        /// Returns the enum value corresponding to the parameter.
        /// </summary>
        /// <param name="value">The boolean value (ignored).</param>
        /// <param name="targetType">The target binding type (ignored).</param>
        /// <param name="parameter">The enum name as a string.</param>
        /// <param name="culture">The culture info (ignored).</param>
        /// <returns>The enum value parsed from the parameter.</returns>
        /// <exception cref="ArgumentException">Thrown if parameter is not a string.</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not String enumString)
            {
                throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName");
            }

            return Enum.Parse(typeof(ApplicationTheme), enumString);
        }
    }
}