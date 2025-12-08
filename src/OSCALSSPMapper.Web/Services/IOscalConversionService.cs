namespace OSCALSSPMapper.Web.Services;

/// <summary>
/// Service for converting OSCAL SSP XML files to Word documents.
/// </summary>
public interface IOscalConversionService
{
    /// <summary>
    /// Converts an OSCAL SSP XML file to a Word document.
    /// </summary>
    /// <param name="xmlFilePath">Path to the uploaded OSCAL SSP XML file.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <returns>Result containing the path to the generated Word document.</returns>
    Task<ConversionResult> ConvertAsync(string xmlFilePath, IProgress<ConversionProgress>? progress = null);

    /// <summary>
    /// Validates an OSCAL SSP XML file against the schema.
    /// </summary>
    /// <param name="xmlFilePath">Path to the XML file to validate.</param>
    /// <returns>Validation result.</returns>
    ValidationResult Validate(string xmlFilePath);

    /// <summary>
    /// Detects the baseline level (LOW, MODERATE, HIGH) from the XML file.
    /// </summary>
    /// <param name="xmlFilePath">Path to the XML file.</param>
    /// <returns>The detected baseline level.</returns>
    BaselineLevel DetectBaseline(string xmlFilePath);
}

/// <summary>
/// Result of a conversion operation.
/// </summary>
public class ConversionResult
{
    public bool Success { get; set; }
    public string? OutputFilePath { get; set; }
    public string? OutputFileName { get; set; }
    public string? ErrorMessage { get; set; }
    public BaselineLevel Baseline { get; set; }
}

/// <summary>
/// Result of a validation operation.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Progress information for conversion operations.
/// </summary>
public class ConversionProgress
{
    public int PercentComplete { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// FedRAMP baseline levels.
/// </summary>
public enum BaselineLevel
{
    Unknown,
    Low,
    Moderate,
    High
}
