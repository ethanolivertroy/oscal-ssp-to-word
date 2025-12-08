using System.Xml;
using System.Xml.Schema;
using OSCALHelperClasses;

namespace OSCALSSPMapper.Web.Services;

/// <summary>
/// Implementation of OSCAL SSP to Word conversion service.
/// </summary>
public class OscalConversionService : IOscalConversionService
{
    private readonly IWordDocumentService _wordService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<OscalConversionService> _logger;

    private const string XMLNamespace = "http://csrc.nist.gov/ns/oscal/1.0";
    private const string SSPSchema = "oscal_ssp_schema.xsd";

    private static readonly Dictionary<string, int> ImplementationStatusDict = new()
    {
        { "implemented", 0 },
        { "partially-implemented", 1 },
        { "planned", 2 },
        { "alternative-implementation", 3 },
        { "not-applicable", 4 }
    };

    private static readonly Dictionary<string, int> OriginationStatusDict = new()
    {
        { "service-provider-corporate", 5 },
        { "service-provider-system-specific", 6 },
        { "service-provider-hybrid", 7 },
        { "configured-by-customer", 8 },
        { "provided-by-customer", 9 },
        { "shared", 10 },
        { "inherited", 11 }
    };

    public OscalConversionService(
        IWordDocumentService wordService,
        IWebHostEnvironment environment,
        ILogger<OscalConversionService> logger)
    {
        _wordService = wordService;
        _environment = environment;
        _logger = logger;
    }

    public async Task<ConversionResult> ConvertAsync(string xmlFilePath, IProgress<ConversionProgress>? progress = null)
    {
        try
        {
            progress?.Report(new ConversionProgress { PercentComplete = 5, Message = "Starting conversion..." });

            // Validate the XML
            var validation = Validate(xmlFilePath);
            if (!validation.IsValid)
            {
                return new ConversionResult
                {
                    Success = false,
                    ErrorMessage = string.Join("; ", validation.Errors)
                };
            }

            progress?.Report(new ConversionProgress { PercentComplete = 10, Message = "XML validation passed" });

            // Detect baseline
            var baseline = DetectBaseline(xmlFilePath);
            var templateFile = GetTemplateFile(baseline);
            var templatePath = Path.Combine(_environment.WebRootPath, "Templates", templateFile);

            if (!File.Exists(templatePath))
            {
                return new ConversionResult
                {
                    Success = false,
                    ErrorMessage = $"Template file not found: {templateFile}"
                };
            }

            progress?.Report(new ConversionProgress { PercentComplete = 15, Message = $"Using {baseline} baseline template" });

            // Parse OSCAL data
            var schemaPath = Path.Combine(_environment.WebRootPath, "Templates", SSPSchema);
            var securityControls = new List<SecurityControl>();
            Metadata? metadata = null;
            SystemCharacteristics? systemCharacteristics = null;

            ParseOscalFile(xmlFilePath, schemaPath, ref metadata, ref systemCharacteristics, securityControls);

            progress?.Report(new ConversionProgress { PercentComplete = 30, Message = $"Parsed {securityControls.Count} security controls" });

            // Generate output file name
            var outputFileName = $"SSP-{baseline}-{DateTime.Now:yyyyMMdd-HHmmss}.docx";
            var outputPath = Path.Combine(_environment.WebRootPath, "downloads", outputFileName);

            // Ensure downloads directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            // Process the Word document
            await _wordService.ProcessDocumentAsync(
                templatePath,
                outputPath,
                metadata!,
                systemCharacteristics!,
                securityControls,
                progress);

            progress?.Report(new ConversionProgress { PercentComplete = 100, Message = "Conversion complete!" });

            return new ConversionResult
            {
                Success = true,
                OutputFilePath = outputPath,
                OutputFileName = outputFileName,
                Baseline = baseline
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OSCAL conversion");
            return new ConversionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public ValidationResult Validate(string xmlFilePath)
    {
        var result = new ValidationResult { IsValid = true };

        try
        {
            var doc = new XmlDocument();
            doc.Load(xmlFilePath);

            // Check for OSCAL namespace
            var root = doc.DocumentElement;
            if (root?.NamespaceURI != XMLNamespace)
            {
                result.IsValid = false;
                result.Errors.Add($"Invalid namespace. Expected {XMLNamespace}");
            }

            // Check for required elements
            var metadataNode = root?.SelectSingleNode("//*[local-name()='metadata']");
            if (metadataNode == null)
            {
                result.IsValid = false;
                result.Errors.Add("Missing required 'metadata' element");
            }

            var sysCharNode = root?.SelectSingleNode("//*[local-name()='system-characteristics']");
            if (sysCharNode == null)
            {
                result.IsValid = false;
                result.Errors.Add("Missing required 'system-characteristics' element");
            }

            var controlImplNode = root?.SelectSingleNode("//*[local-name()='control-implementation']");
            if (controlImplNode == null)
            {
                result.Warnings.Add("No 'control-implementation' element found");
            }
        }
        catch (XmlException ex)
        {
            result.IsValid = false;
            result.Errors.Add($"XML parsing error: {ex.Message}");
        }

        return result;
    }

    public BaselineLevel DetectBaseline(string xmlFilePath)
    {
        try
        {
            var doc = new XmlDocument();
            doc.Load(xmlFilePath);

            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("oscal", XMLNamespace);

            var sensitivityNode = doc.SelectSingleNode("//oscal:security-sensitivity-level", nsmgr);
            var level = sensitivityNode?.InnerText?.ToLower() ?? "";

            return level switch
            {
                "low" => BaselineLevel.Low,
                "moderate" => BaselineLevel.Moderate,
                "high" => BaselineLevel.High,
                _ => BaselineLevel.Moderate // Default to moderate
            };
        }
        catch
        {
            return BaselineLevel.Moderate;
        }
    }

    private static string GetTemplateFile(BaselineLevel baseline)
    {
        return baseline switch
        {
            BaselineLevel.Low => "FedRAMP-SSP-Low-Baseline-Template.docx",
            BaselineLevel.Moderate => "FedRAMP-SSP-Moderate-Baseline-Template.docx",
            BaselineLevel.High => "FedRAMP-SSP-High-Baseline-Template.docx",
            _ => "FedRAMP-SSP-Moderate-Baseline-Template.docx"
        };
    }

    private void ParseOscalFile(
        string xmlFilePath,
        string schemaPath,
        ref Metadata? metadata,
        ref SystemCharacteristics? systemCharacteristics,
        List<SecurityControl> securityControls)
    {
        var doc = new XmlDocument();
        doc.Load(xmlFilePath);

        foreach (XmlNode node in doc.DocumentElement!)
        {
            if (node.NodeType == XmlNodeType.Text || node.NodeType == XmlNodeType.Comment)
                continue;

            if (node.Name == "metadata")
            {
                metadata = new Metadata(node, true);
            }

            if (node.Name == "system-characteristics")
            {
                systemCharacteristics = new SystemCharacteristics(node, true);
            }

            // Parse security controls from control-implementation
            if (node.Name == "control-implementation")
            {
                ParseControlImplementation(node, securityControls);
            }
        }
    }

    private void ParseControlImplementation(XmlNode controlImplNode, List<SecurityControl> securityControls)
    {
        foreach (XmlNode child in controlImplNode.ChildNodes)
        {
            if (child.Name == "implemented-requirement" && child.Attributes != null)
            {
                var controlIdAttr = child.Attributes["control-id"];
                if (controlIdAttr != null)
                {
                    var control = new SecurityControl(child, 0, XMLNamespace);
                    if (control.ControlId != null)
                    {
                        securityControls.Add(control);
                    }
                }
            }
        }
    }
}
