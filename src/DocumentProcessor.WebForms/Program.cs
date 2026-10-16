using DocumentProcessor.WebForms.Components;
using DocumentProcessor.WebForms.Configuration;
using DocumentProcessor.WebForms.Data;
using DocumentProcessor.WebForms.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Strongly-typed options — bind once, inject everywhere via IOptions<T>.
// ---------------------------------------------------------------------------
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<BedrockOptions>(builder.Configuration.GetSection(BedrockOptions.SectionName));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));

// ---------------------------------------------------------------------------
// Database connection resolution (Secrets Manager fallback → Web.config).
//
// Global.asax called DatabaseConnectionResolver.Resolve() and stored the
// resulting connection string on a static property. On Core we resolve it
// once at startup and feed it into the DI registrations below.
// ---------------------------------------------------------------------------
var configuredConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection in configuration.");

var databaseOptions = builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
    ?? new DatabaseOptions();

string resolvedConnectionString;
DatabaseInfo? databaseInfo;

if (databaseOptions.UseSecretsManager)
{
    try
    {
        var connection = DatabaseConnectionResolver.Resolve(configuredConnectionString, databaseOptions);
        resolvedConnectionString = connection.ConnectionString;
        databaseInfo = connection.Info;

        if (connection.Warning != null)
        {
            // Mirror the legacy Trace.TraceWarning from Application_Start.
            Console.WriteLine("[Warning] " + connection.Warning);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("[Warning] Secrets Manager lookup failed (" + ex.Message + "); using the configured connection string.");
        resolvedConnectionString = configuredConnectionString;
        databaseInfo = new DatabaseInfo(
            DatabaseConnectionResolver.DetectProvider(configuredConnectionString),
            "appsettings.json",
            HostOfConnectionString(configuredConnectionString));
    }
}
else
{
    resolvedConnectionString = configuredConnectionString;
    databaseInfo = new DatabaseInfo(
        DatabaseConnectionResolver.DetectProvider(configuredConnectionString),
        "appsettings.json",
        HostOfConnectionString(configuredConnectionString));
}

// Register DatabaseInfo as a singleton so MainLayout can @inject it.
builder.Services.AddSingleton(databaseInfo);

// ---------------------------------------------------------------------------
// Entity Framework Core — replaces DocumentDbContext.ConnectionString static
// property + Database.SetInitializer<T>(null).
// ---------------------------------------------------------------------------
builder.Services.AddDbContextFactory<DocumentDbContext>(options =>
    options.UseSqlServer(resolvedConnectionString));

// ---------------------------------------------------------------------------
// Application services — replace the 'new' calls scattered across pages.
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IDocumentStorage, FileSystemDocumentStorage>();
builder.Services.AddScoped<IDocumentSummarizer, BedrockDocumentSummarizer>();
builder.Services.AddScoped<DocumentTextExtractor>();
builder.Services.AddScoped<DocumentPipeline>();

// ---------------------------------------------------------------------------
// Blazor Server
// ---------------------------------------------------------------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ---------------------------------------------------------------------------
// Request size limits — carry over maxRequestLength (512 000 KB) and
// maxAllowedContentLength (524 288 000 bytes) from Web.config.
// ---------------------------------------------------------------------------
var maxBodySize = builder.Configuration.GetValue<long?>("Kestrel:Limits:MaxRequestBodySize")
    ?? 524_288_000L;

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxBodySize;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxBodySize;
});

// ---------------------------------------------------------------------------
// Build
// ---------------------------------------------------------------------------
var app = builder.Build();

// ---------------------------------------------------------------------------
// Database schema initialization — replaces DatabaseInitializer.EnsureSchema
// called from Application_Start. Uses the app's ContentRootPath to locate
// App_Data/Schema.sql instead of HostingEnvironment.MapPath.
// ---------------------------------------------------------------------------
try
{
    DatabaseInitializer.EnsureSchema(resolvedConnectionString, app.Environment.ContentRootPath);
}
catch (Exception ex)
{
    // Do not take the whole application down (mirrors the legacy catch in Application_Start).
    app.Logger.LogError(ex, "Could not prepare the database.");
}

// ---------------------------------------------------------------------------
// Middleware pipeline — order is load-bearing; see ASP.NET Core middleware docs.
// ---------------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/ErrorPage");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ---------------------------------------------------------------------------
// Application_Error → centralised exception logging.
// ---------------------------------------------------------------------------
// The UseExceptionHandler above handles error responses; the logging equivalent
// of the legacy Application_Error is built into ASP.NET Core's request logging.
// For extra visibility a custom middleware could be added, but the built-in
// ILogger infrastructure covers the same ground.

app.Run();

// ---------------------------------------------------------------------------
// Local helper — extract the host portion from a SQL Server connection string
// for the DatabaseInfo display. Mirrors the legacy HostOf logic.
// ---------------------------------------------------------------------------
static string HostOfConnectionString(string connectionString)
{
    try
    {
        return new SqlConnectionStringBuilder(connectionString).DataSource;
    }
    catch
    {
        return "unknown";
    }
}
