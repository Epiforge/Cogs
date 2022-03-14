namespace Cogs.Wpf.Validation;

/// <summary>
/// Provides a way to create a rule in order to check that user input does not contain any invalid file system path characters
/// </summary>
public class ValidPathValidationRule :
    InvalidCharactersValidationRule
{
    /// <summary>
    /// Creates a new instance of the <see cref="ValidPathValidationRule"/> class
    /// </summary>
    public ValidPathValidationRule() :
        base() =>
        InvalidCharacters = Path.GetInvalidPathChars();

    /// <summary>
    /// Gets/sets information about the invalidity
    /// </summary>
    public override object ErrorContent { get; set; } = "Cannot contain invalid file system path characters";
}
