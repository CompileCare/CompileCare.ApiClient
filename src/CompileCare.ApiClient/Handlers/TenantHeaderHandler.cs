using CompileCare.ApiClient.Abstractions;

namespace CompileCare.ApiClient.Handlers;

public sealed class TenantHeaderHandler(ITenantContextProvider tenantContextProvider) : DelegatingHandler
{
    public const string HeaderName = "X-Tenant-ID";

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // The provider is authoritative: never forward caller-supplied or stale tenant context.
        request.Headers.Remove(HeaderName);

        var tenantId = tenantContextProvider.GetTenantId();

        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            request.Headers.TryAddWithoutValidation(HeaderName, tenantId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}