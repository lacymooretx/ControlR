namespace ControlR.Web.Client.Services;

public class TenantOverrideHandler(ITenantSwitcherService tenantSwitcher) : DelegatingHandler
{
  protected override Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken)
  {
    if (tenantSwitcher.SelectedTenantId is { } tenantId)
    {
      request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId.ToString());
    }

    return base.SendAsync(request, cancellationToken);
  }
}
