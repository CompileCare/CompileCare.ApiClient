using CompileCare.ApiClient.Abstractions;

namespace CompileCare.ApiClient.Handlers;

public sealed class CorrelationIdHandler(ICorrelationIdProvider correlationIdProvider) : DelegatingHandler
{
    public const string HeaderName = "X-Correlation-ID";

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Caller-supplied values are never trusted. Always replace or generate a safe value.
        request.Headers.Remove(HeaderName);

        var correlationId = correlationIdProvider.GetCorrelationId();
        if (!CorrelationIdValidator.IsValid(correlationId))
            correlationId = Guid.NewGuid().ToString("N");

        request.Headers.TryAddWithoutValidation(HeaderName, correlationId);
        return base.SendAsync(request, cancellationToken);
    }
}