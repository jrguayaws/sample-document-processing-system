using Amazon;
using Amazon.BedrockRuntime;
using Amazon.SecretsManager;
using DocumentProcessor.WebForms.Components;
using DocumentProcessor.WebForms.Configuration;
using DocumentProcessor.WebForms.Data;
using DocumentProcessor.WebForms.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Logging (replaces the Trace.Listeners TextWriterTraceListener on App_Data/trace.log).
// System.Diagnostics.Trace calls are now ILogger<T> calls throughout the services.
// ---------------------------------------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.AddDebug();
// PORT-TODO
// {"was": "System.Diagnostics.TextWriterTraceListener", "note": "The legacy app wrote its trace to App_Data/trace.log. ASP.NET Core has no built-in file logger; logs now go to the console and debug output. If a log file is still required, the owner must choose a logging provider (e.g. Serilog file sink) and add its package."}

// [PORT-NOTE] dropped ServicePointManager.SecurityProtocol |= Tls12: .NET negotiates TLS 1.2+ from the OS by default and the AWS SDK requires it.

// ---------------------------------------------------------------------------
// Configuration wrapper (legacy static AppSettings over Web.config, same key names).
// ---------------------------------------------------------------------------
var settings = new AppSettings(builder.Configuration);
builder.Services.AddSingleton(settings);

// ---------------------------------------------------------------------------
// Request body limits (replaces httpRuntime maxRequestLength and
// requestLimits maxAllowedContentLength): one upload of Storage.MaxFilesPerUpload
// files at Storage.MaxFileSizeMegabytes each.
// ---------------------------------------------------------------------------
var maxRequestBodyBytes = settings.StorageMaxRequestBodyBytes;
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = maxRequestBodyBytes);
builder.Services.Configure<IISServerOptions>(options => options.MaxRequestBodySize = maxRequestBodyBytes);
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = maxRequestBodyBytes);
// [PORT-NOTE] dropped httpRuntime executionTimeout="600": summarizing no longer runs inside the request;
// uploads are queued to DocumentProcessingService, so no long request timeout is needed.

// ---------------------------------------------------------------------------
// AWS clients (singletons; the static Bedrock client in BedrockDocumentSummarizer is gone).
// ---------------------------------------------------------------------------
// Built lazily: only when Database.UseSecretsManager is on (or something injects it). A client
// that cannot be built (no region / credentials) is caught by the resolver, which then falls
// back to ConnectionStrings:DefaultConnection just like a failed lookup in the legacy app.
var secretsManagerClient = new Lazy<IAmazonSecretsManager>(() => new AmazonSecretsManagerClient());
builder.Services.AddSingleton<IAmazonSecretsManager>(_ => secretsManagerClient.Value);

builder.Services.AddSingleton<IAmazonBedrockRuntime>(_ => new AmazonBedrockRuntimeClient(
    new AmazonBedrockRuntimeConfig
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(settings.BedrockRegion)
    }));

// ---------------------------------------------------------------------------
// Database connection: AWS Secrets Manager (Database.UseSecretsManager) with fallback
// to ConnectionStrings:DefaultConnection. Replaces DocumentDbContext.ConnectionString
// and Application["DatabaseInfo"].
// ---------------------------------------------------------------------------
var connectionResolver = new DatabaseConnectionResolver(settings, () => secretsManagerClient.Value);
var connection = await connectionResolver.ResolveAsync();

builder.Services.AddSingleton(connectionResolver);
builder.Services.AddSingleton(connection);
builder.Services.AddSingleton(connection.Info);

// A DbContext instance is not thread safe, so it is not shared as a singleton. The singleton
// factory (configured with the resolved connection string) lets the singleton pipeline and the
// components create a short-lived context per operation; DocumentDbContext is also resolvable
// directly (scoped, i.e. one per Blazor circuit).
builder.Services.AddDbContextFactory<DocumentDbContext>(
    options => options.UseSqlServer(connection.ConnectionString),
    ServiceLifetime.Singleton);

// ---------------------------------------------------------------------------
// Application services (singletons; the parameterless DocumentPipeline constructor is gone).
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IDocumentStorage, FileSystemDocumentStorage>();
builder.Services.AddSingleton<IDocumentSummarizer, BedrockDocumentSummarizer>();
builder.Services.AddSingleton<DocumentTextExtractor>();
builder.Services.AddSingleton<DocumentPipeline>();

// Uploads are queued and processed in the background so the circuit is not blocked
// while Bedrock answers; the list shows Pending/Processing/Processed/Failed.
builder.Services.AddSingleton<DocumentProcessingQueue>();
builder.Services.AddHostedService<DocumentProcessingService>();

// ---------------------------------------------------------------------------
// Blazor Server host.
// ---------------------------------------------------------------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (connection.Warning != null)
{
    app.Logger.LogWarning("{Warning}", connection.Warning);
}

// Startup schema initialization from App_Data/Schema.sql (no EnsureCreated / Migrate).
try
{
    DatabaseInitializer.EnsureSchema(connection.ConnectionString, app.Environment.ContentRootPath);
}
catch (Exception ex)
{
    // Do not take the whole application down: the pages already report a
    // failure to read documents, and this way the error is visible in one place.
    app.Logger.LogError(ex, "Could not prepare the database.");
}

// customErrors mode="RemoteOnly" defaultRedirect="~/ErrorPage.aspx" + Application_Error:
// outside Development unhandled exceptions are logged by the exception-handler middleware
// and the error page is re-executed.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// <error statusCode="404" redirect="~/ErrorPage.aspx?code=404" />: status codes without a body
// (404 included) re-execute the error page with ?code=<status>.
app.UseStatusCodePagesWithReExecute("/Error", "?code={0}");

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
