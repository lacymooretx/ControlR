using ControlR.Libraries.Shared.Constants;
using ControlR.Web.Client.Authz;
using ControlR.Web.Server.Authn;
using ControlR.Web.Server.Data;

namespace ControlR.Web.Server.Middleware;

public class TotpEnforcementMiddleware(
  RequestDelegate next,
  ILogger<TotpEnforcementMiddleware> logger)
{
  private static readonly HashSet<string> _exemptPaths =
  [
    "/Account/Manage/TwoFactorAuthentication",
    "/Account/Manage/EnableAuthenticator",
    "/Account/Manage/ResetAuthenticator",
    "/Account/Manage/GenerateRecoveryCodes",
    "/Account/Manage/ShowRecoveryCodes",
    "/Account/Logout",
    "/api/logout",
  ];

  public async Task InvokeAsync(HttpContext context)
  {
    if (!context.User.Identity?.IsAuthenticated == true)
    {
      await next(context);
      return;
    }

    // Skip enforcement for PAT and logon token auth
    var authMethod = context.User.FindFirst(UserClaimTypes.AuthenticationMethod)?.Value;
    if (authMethod == PersonalAccessTokenAuthenticationSchemeOptions.DefaultScheme ||
        authMethod == LogonTokenAuthenticationSchemeOptions.DefaultScheme)
    {
      await next(context);
      return;
    }

    // Skip for API requests, SignalR hubs, and static files
    var path = context.Request.Path.Value ?? "";
    if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/hubs/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/_framework/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/_content/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase))
    {
      await next(context);
      return;
    }

    // Skip exempt paths (2FA setup pages, logout)
    if (_exemptPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
    {
      await next(context);
      return;
    }

    if (!context.User.TryGetTenantId(out var tenantId))
    {
      await next(context);
      return;
    }

    // Check if tenant requires TOTP
    var dbFactory = context.RequestServices.GetRequiredService<IDbContextFactory<AppDb>>();
    await using var db = await dbFactory.CreateDbContextAsync();

    var requireTotpSetting = await db.TenantSettings
      .AsNoTracking()
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Name == TenantSettingsNames.RequireTotp);

    if (requireTotpSetting is null ||
        !bool.TryParse(requireTotpSetting.Value, out var requireTotp) ||
        !requireTotp)
    {
      await next(context);
      return;
    }

    // Check if user has 2FA enabled
    if (!context.User.TryGetUserId(out var userId))
    {
      await next(context);
      return;
    }

    var userManager = context.RequestServices.GetRequiredService<UserManager<AppUser>>();
    var user = await userManager.FindByIdAsync(userId.ToString());

    if (user is null)
    {
      await next(context);
      return;
    }

    var isTwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user);
    if (isTwoFactorEnabled)
    {
      await next(context);
      return;
    }

    // Redirect to 2FA setup
    logger.LogInformation(
      "User {UserName} redirected to 2FA setup. Tenant {TenantId} requires TOTP.",
      user.UserName,
      tenantId);

    context.Response.Redirect("/Account/Manage/TwoFactorAuthentication");
  }
}
