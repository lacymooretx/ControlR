using ControlR.Libraries.WebSocketRelay.Common.Extensions;
using ControlR.Web.Client.Components.Layout;
using ControlR.Web.Server.Components;
using ControlR.Web.Server.Components.Account;
using ControlR.Web.Server.Startup;
using ControlR.Web.ServiceDefaults;
using Microsoft.Extensions.FileProviders;
using Scalar.AspNetCore;
using System.Reflection;
using ControlR.Libraries.Shared.Constants;
using ControlR.Web.Server.Middleware;
using Microsoft.AspNetCore.StaticFiles;

var isOpenApiBuild = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSystemd();

await builder.AddControlrServer(isOpenApiBuild);

var appOptions = builder.Configuration
  .GetSection(AppOptions.SectionKey)
  .Get<AppOptions>() ?? new AppOptions();

var app = builder.Build();

app.UseForwardedHeaders();

if (appOptions.UseHttpLogging)
{
  app.UseWhen(
    ctx => !ctx.Request.Path.StartsWithSegments("/health"),
    appBuilder => appBuilder.UseHttpLogging());
}

if (!app.Environment.IsEnvironment("Testing"))
{
  app.MapDefaultEndpoints();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
  app.MapScalarApiReference();
  app.UseWebAssemblyDebugging();
  app.UseMigrationsEndPoint();
}
else
{
  app.UseHttpsRedirection();
  app.UseExceptionHandler("/Error", true);
  app.UseHsts();
}

// Serve agent download binaries from physical disk in an isolated branch.
// app.Map creates a separate pipeline that short-circuits completely,
// preventing MapStaticAssets from serving stale manifest entries.
var downloadsPath = Path.Combine(builder.Environment.WebRootPath, "downloads");
if (Directory.Exists(downloadsPath))
{
  app.Map("/downloads", downloadApp =>
  {
    downloadApp.UseStaticFiles(new StaticFileOptions
    {
      FileProvider = new PhysicalFileProvider(downloadsPath),
      ServeUnknownFileTypes = true,
      DefaultContentType = "application/octet-stream"
    });
  });
}

app.MapStaticAssets();

var novncContentTypeProvider = new FileExtensionContentTypeProvider();
novncContentTypeProvider.Mappings[".cur"] = "image/x-icon";
novncContentTypeProvider.Mappings[".wasm"] = "application/wasm";
novncContentTypeProvider.Mappings[".map"] = "application/json";
novncContentTypeProvider.Mappings[".webmanifest"] = "application/manifest+json";

app.UseStaticFiles(new StaticFileOptions
{
  FileProvider = new PhysicalFileProvider(
    Path.Combine(builder.Environment.ContentRootPath, "novnc")),
  RequestPath = "/novnc",
  ContentTypeProvider = novncContentTypeProvider
});

app.MapHub<AgentHub>(AppConstants.AgentHubPath);

app.UseRequestTimeouts();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TotpEnforcementMiddleware>();
app.UseAntiforgery();

app.MapWebSocketRelay();

// Configure output cache - must be before any middleware that generates response
app.UseOutputCache();

app.MapControllers();

app.UseWhen(
  ctx => !ctx.Request.Path.StartsWithSegments("/api"),
  _ =>
  {
    app.MapRazorComponents<App>()
      .AddInteractiveWebAssemblyRenderMode()
      .AddAdditionalAssemblies(typeof(MainLayout).Assembly);
  });

app.MapAdditionalIdentityEndpoints();
app.MapManagementEndpoints();

app.MapHub<ViewerHub>(AppConstants.ViewerHubPath);

if (appOptions.UseInMemoryDatabase)
{
  await app.AddBuiltInRoles();
}
else
{
  await app.ApplyMigrations();
  await app.SetAllDevicesOffline();
  await app.SetAllUsersOffline();
  await app.RemoveEmptyTenants();
}

await app.RunAsync();
