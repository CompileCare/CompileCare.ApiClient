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
        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("The tenant context provider returned an empty tenant identifier.");
        }

        if (tenantId.HasValue)
        {
            request.Headers.TryAddWithoutValidation(HeaderName, tenantId.Value.ToString("D"));
        }

        return base.SendAsync(request, cancellationToken);
    }
}
