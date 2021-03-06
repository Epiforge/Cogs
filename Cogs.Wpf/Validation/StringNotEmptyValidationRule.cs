using System;
using System.Globalization;
using System.Windows.Controls;

namespace Cogs.Wpf.Validation
{
    /// <summary>
    /// Provides a way to create a rule in order to check that user input is not an empty string
    /// </summary>
    public class StringNotEmptyValidationRule : ValidationRule
    {
        /// <summary>
        /// Gets/sets information about the invalidity
        /// </summary>
        public object ErrorContent { get; set; } = "Cannot be empty";

        /// <summary>
        /// Performs validation checks on a value
        /// </summary>
        /// <param name="value">The value from the binding target to check</param>
        /// <param name="cultureInfo">The culture to use in this rule</param>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var str = Convert.ToString(value);
            if ((str?.Length ?? 0) == 0)
                return new ValidationResult(false, ErrorContent);
            return ValidationResult.ValidResult;
        }
    }
}
