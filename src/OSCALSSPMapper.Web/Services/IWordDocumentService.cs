using OSCALHelperClasses;

namespace OSCALSSPMapper.Web.Services;

/// <summary>
/// Service for manipulating Word documents using Office Interop.
/// </summary>
public interface IWordDocumentService
{
    /// <summary>
    /// Processes a Word template with OSCAL data.
    /// </summary>
    /// <param name="templatePath">Path to the FedRAMP Word template.</param>
    /// <param name="outputPath">Path for the output Word document.</param>
    /// <param name="metadata">OSCAL metadata.</param>
    /// <param name="systemCharacteristics">OSCAL system characteristics.</param>
    /// <param name="securityControls">List of security controls.</param>
    /// <param name="progress">Optional progress reporter.</param>
    System.Threading.Tasks.Task ProcessDocumentAsync(
        string templatePath,
        string outputPath,
        Metadata metadata,
        SystemCharacteristics systemCharacteristics,
        List<SecurityControl> securityControls,
        IProgress<ConversionProgress>? progress = null);
}
