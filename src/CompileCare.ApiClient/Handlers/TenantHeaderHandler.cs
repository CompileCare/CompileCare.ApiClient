using CompileCare.ApiClient.Abstractions;

namespace CompileCare.ApiClient.Handlers;

public sealed class TenantHeaderHandler(ITenantContextProvider tenantContextProvider) : DelegatingHandler
{
    public const string HeaderName = "X-Tenant-ID";

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var tenantId = tenantContextProvider.GetTenantId();

        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            request.Headers.Remove(HeaderName);
            request.Headers.TryAddWithoutValidation(HeaderName, tenantId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}