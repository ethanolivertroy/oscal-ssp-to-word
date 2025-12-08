using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OSCALSSPMapper.Web.Services;

namespace OSCALSSPMapper.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IOscalConversionService _conversionService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<IndexModel> _logger;

    [BindProperty]
    public IFormFile? UploadedFile { get; set; }

    public string? StatusMessage { get; set; }
    public string? DownloadUrl { get; set; }
    public bool Success { get; set; }
    public string? Baseline { get; set; }

    public IndexModel(
        IOscalConversionService conversionService,
        IWebHostEnvironment environment,
        ILogger<IndexModel> logger)
    {
        _conversionService = conversionService;
        _environment = environment;
        _logger = logger;
    }

    public void OnGet()
    {
        // Initial page load
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (UploadedFile == null || UploadedFile.Length == 0)
        {
            StatusMessage = "Please select a file to upload.";
            Success = false;
            return Page();
        }

        // Validate file extension
        var extension = Path.GetExtension(UploadedFile.FileName).ToLower();
        if (extension != ".xml")
        {
            StatusMessage = "Invalid file type. Please upload an XML file.";
            Success = false;
            return Page();
        }

        try
        {
            // Save uploaded file
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsPath);

            var fileName = $"{Guid.NewGuid()}-{UploadedFile.FileName}";
            var filePath = Path.Combine(uploadsPath, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await UploadedFile.CopyToAsync(stream);
            }

            _logger.LogInformation("Uploaded file saved to: {FilePath}", filePath);

            // Perform conversion
            var result = await _conversionService.ConvertAsync(filePath);

            if (result.Success)
            {
                Success = true;
                StatusMessage = "Conversion completed successfully!";
                DownloadUrl = $"/downloads/{result.OutputFileName}";
                Baseline = result.Baseline.ToString();

                _logger.LogInformation("Conversion successful: {OutputFile}", result.OutputFileName);
            }
            else
            {
                Success = false;
                StatusMessage = $"Conversion failed: {result.ErrorMessage}";
                _logger.LogWarning("Conversion failed: {Error}", result.ErrorMessage);
            }

            // Clean up uploaded file
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during file upload and conversion");
            StatusMessage = $"An error occurred: {ex.Message}";
            Success = false;
        }

        return Page();
    }
}
