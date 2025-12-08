using OSCALSSPMapper.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();

// Register custom services
builder.Services.AddScoped<IOscalConversionService, OscalConversionService>();
builder.Services.AddScoped<IWordDocumentService, WordDocumentService>();

// Add memory cache for storing conversion results
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
