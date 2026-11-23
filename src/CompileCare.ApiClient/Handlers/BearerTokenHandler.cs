using CompileCare.ApiClient.Abstractions;

namespace CompileCare.ApiClient.Handlers;

public sealed class BearerTokenHandler(IAccessTokenProvider accessTokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // The provider is authoritative: never forward caller-supplied or stale credentials.
        request.Headers.Authorization = null;

        var accessToken = await accessTokenProvider.GetAccessTokenAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}