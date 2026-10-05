using DocumentProcessor.Web.Components;
using DocumentProcessor.Web.Configuration;
using DocumentProcessor.Web.Data;
using DocumentProcessor.Web.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddApplicationOptions(builder.Configuration);

var connection = await DatabaseConnectionResolver.ResolveAsync(
    builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured."),
    builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions());

builder.Services.AddDocumentProcessing(connection);

var app = builder.Build();

if (connection.Warning is not null)
{
    app.Logger.LogWarning("{Warning}", connection.Warning);
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await dbContextFactory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

// MapStaticAssets resolves framework assets such as _framework/blazor.web.js in every
// environment; UseStaticFiles only finds them when running as Development.
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
