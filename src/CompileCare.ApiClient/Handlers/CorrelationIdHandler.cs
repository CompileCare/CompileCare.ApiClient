using CompileCare.ApiClient.Abstractions;

namespace CompileCare.ApiClient.Handlers;

public sealed class CorrelationIdHandler(ICorrelationIdProvider correlationIdProvider) : DelegatingHandler
{
    public const string HeaderName = "X-Correlation-ID";

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            request.Headers.Remove(HeaderName);
            request.Headers.TryAddWithoutValidation(HeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}