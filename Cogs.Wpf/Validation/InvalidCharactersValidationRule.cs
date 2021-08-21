namespace Cogs.Wpf.Validation;

/// <summary>
/// Provides a way to create a rule in order to check that user input does not contain any invalid characters
/// </summary>
public class InvalidCharactersValidationRule : ValidationRule
{
    /// <summary>
    /// The array of characters that cannot appear in the value
    /// </summary>
    protected char[] InvalidCharacters = new char[0];

    /// <summary>
    /// Gets/sets information about the invalidity
    /// </summary>
    public virtual object ErrorContent { get; set; } = "Cannot contain invalid characters";

    /// <summary>
    /// Gets/sets the array of characters that cannot appear in the value
    /// </summary>
    public ImmutableArray<char> InvalidCharactersArray
    {
        get => InvalidCharacters.ToImmutableArray();
        set => InvalidCharacters = value.ToArray();
    }

    /// <summary>
    /// Gets/sets a string comprised of the array of characters that cannot appear in the value
    /// </summary>
    public string InvalidCharactersString
    {
        get => new string(InvalidCharacters);
        set
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            InvalidCharacters = value.ToCharArray();
        }
    }

    /// <summary>
    /// Performs validation checks on a value
    /// </summary>
    /// <param name="value">The value from the binding target to check</param>
    /// <param name="cultureInfo">The culture to use in this rule</param>
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        var str = Convert.ToString(value);
        if ((str?.IndexOfAny(InvalidCharacters) ?? -1) >= 0)
            return new ValidationResult(false, ErrorContent);
        return ValidationResult.ValidResult;
    }
}
