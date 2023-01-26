namespace Cogs.Wpf.Validation;

/// <summary>
/// Provides a way to create a rule in order to check that user input does not contain any invalid file name characters
/// </summary>
public sealed class ValidFileNameValidationRule :
    InvalidCharactersValidationRule
{
    /// <summary>
    /// Creates a new instance of the <see cref="ValidFileNameValidationRule"/> class
    /// </summary>
    public ValidFileNameValidationRule() :
        base() =>
        InvalidCharacters = Path.GetInvalidFileNameChars();

    /// <summary>
    /// Gets/sets information about the invalidity
    /// </summary>
    public override object ErrorContent { get; set; } = "Cannot contain invalid file name characters";
}
