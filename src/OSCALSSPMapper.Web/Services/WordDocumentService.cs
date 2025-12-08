using System.Runtime.InteropServices;
using OSCALHelperClasses;

namespace OSCALSSPMapper.Web.Services;

/// <summary>
/// Service for manipulating Word documents using Office Interop.
/// Note: This service requires Windows with Microsoft Office installed.
/// </summary>
public class WordDocumentService : IWordDocumentService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<WordDocumentService> _logger;

    public WordDocumentService(
        IWebHostEnvironment environment,
        ILogger<WordDocumentService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task ProcessDocumentAsync(
        string templatePath,
        string outputPath,
        Metadata metadata,
        SystemCharacteristics systemCharacteristics,
        List<SecurityControl> securityControls,
        IProgress<ConversionProgress>? progress = null)
    {
        // Check if running on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.LogWarning("Word document processing requires Windows with Office installed. Creating placeholder output.");
            await CreatePlaceholderDocumentAsync(templatePath, outputPath, metadata, securityControls, progress);
            return;
        }

        // Run Office Interop operations on a dedicated thread
        await System.Threading.Tasks.Task.Run(() =>
        {
            ProcessDocumentWithInterop(templatePath, outputPath, metadata, systemCharacteristics, securityControls, progress);
        });
    }

    private void ProcessDocumentWithInterop(
        string templatePath,
        string outputPath,
        Metadata metadata,
        SystemCharacteristics systemCharacteristics,
        List<SecurityControl> securityControls,
        IProgress<ConversionProgress>? progress)
    {
#if WINDOWS
        Microsoft.Office.Interop.Word.Application? wordApp = null;
        Microsoft.Office.Interop.Word.Document? document = null;

        try
        {
            progress?.Report(new ConversionProgress { PercentComplete = 35, Message = "Opening Word application..." });

            wordApp = new Microsoft.Office.Interop.Word.Application();
            wordApp.Visible = false;

            progress?.Report(new ConversionProgress { PercentComplete = 40, Message = "Loading template..." });

            // Copy template to output path
            File.Copy(templatePath, outputPath, true);

            document = wordApp.Documents.Open(outputPath);

            progress?.Report(new ConversionProgress { PercentComplete = 50, Message = "Processing metadata..." });

            // Process content controls and checkboxes
            ProcessContentControls(document, metadata, systemCharacteristics);

            progress?.Report(new ConversionProgress { PercentComplete = 70, Message = $"Processing {securityControls.Count} security controls..." });

            ProcessSecurityControls(document, securityControls, progress);

            progress?.Report(new ConversionProgress { PercentComplete = 95, Message = "Saving document..." });

            document.Save();
            document.Close();
            document = null;

            wordApp.Quit();
            wordApp = null;

            _logger.LogInformation("Successfully generated Word document: {OutputPath}", outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Word document");
            throw;
        }
        finally
        {
            if (document != null)
            {
                try { document.Close(false); } catch { }
                Marshal.ReleaseComObject(document);
            }
            if (wordApp != null)
            {
                try { wordApp.Quit(); } catch { }
                Marshal.ReleaseComObject(wordApp);
            }
        }
#else
        throw new PlatformNotSupportedException("Word document processing requires Windows.");
#endif
    }

#if WINDOWS
    private void ProcessContentControls(
        Microsoft.Office.Interop.Word.Document document,
        Metadata metadata,
        SystemCharacteristics systemCharacteristics)
    {
        // Map metadata fields to content controls
        foreach (Microsoft.Office.Interop.Word.ContentControl control in document.ContentControls)
        {
            var tag = control.Tag?.ToLower() ?? "";

            // Map common fields
            if (tag.Contains("title") && !string.IsNullOrEmpty(metadata.Title.Value))
            {
                control.Range.Text = metadata.Title.Value;
            }
            else if (tag.Contains("system-name") && !string.IsNullOrEmpty(systemCharacteristics.SystemName.Value))
            {
                control.Range.Text = systemCharacteristics.SystemName.Value;
            }
            else if (tag.Contains("system-id") && !string.IsNullOrEmpty(systemCharacteristics.SystemID.Value))
            {
                control.Range.Text = systemCharacteristics.SystemID.Value;
            }
        }
    }

    private void ProcessSecurityControls(
        Microsoft.Office.Interop.Word.Document document,
        List<SecurityControl> securityControls,
        IProgress<ConversionProgress>? progress)
    {
        var total = securityControls.Count;
        var processed = 0;

        foreach (var control in securityControls)
        {
            processed++;
            var percentComplete = 70 + (int)((processed / (double)total) * 25);
            progress?.Report(new ConversionProgress
            {
                PercentComplete = percentComplete,
                Message = $"Processing control {control.ControlId} ({processed}/{total})"
            });

            // Process checkboxes for implementation status
            ProcessImplementationStatus(document, control);
        }
    }

    private void ProcessImplementationStatus(
        Microsoft.Office.Interop.Word.Document document,
        SecurityControl control)
    {
        // Find and check the appropriate checkboxes based on control properties
        foreach (var prop in control.Properties)
        {
            if (prop.Name == "implementation-status" || prop.Name == "control-origination")
            {
                // Logic to find and check checkboxes would go here
                // This is specific to the FedRAMP template structure
            }
        }
    }
#endif

    /// <summary>
    /// Creates a placeholder document when Office Interop is not available.
    /// This allows development and testing on non-Windows systems.
    /// </summary>
    private async System.Threading.Tasks.Task CreatePlaceholderDocumentAsync(
        string templatePath,
        string outputPath,
        Metadata metadata,
        List<SecurityControl> securityControls,
        IProgress<ConversionProgress>? progress)
    {
        progress?.Report(new ConversionProgress { PercentComplete = 40, Message = "Creating placeholder document..." });

        // Copy the template as-is (without modifications)
        if (File.Exists(templatePath))
        {
            File.Copy(templatePath, outputPath, true);
            _logger.LogInformation("Copied template to output (no Office Interop available)");
        }
        else
        {
            // Create a simple text file as placeholder
            var content = $"""
                OSCAL SSP Conversion Placeholder
                ================================

                This is a placeholder document created because Microsoft Office is not available.
                To generate actual Word documents, run this application on Windows with Office installed.

                Metadata:
                - Title: {metadata.Title.Value}
                - Version: {metadata.Version.Value}
                - OSCAL Version: {metadata.OSCALVersion.Value}
                - Last Modified: {metadata.LastModified.Date}

                Security Controls Processed: {securityControls.Count}

                Controls:
                {string.Join("\n", securityControls.Select(c => $"- {c.ControlId}"))}
                """;

            // Change extension to .txt since we can't create real docx
            var txtPath = Path.ChangeExtension(outputPath, ".txt");
            await File.WriteAllTextAsync(txtPath, content);

            // Also copy the template if available
            if (File.Exists(templatePath))
            {
                File.Copy(templatePath, outputPath, true);
            }
        }

        progress?.Report(new ConversionProgress { PercentComplete = 95, Message = "Placeholder created" });
    }
}
