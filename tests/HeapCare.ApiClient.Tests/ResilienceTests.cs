using System.Net;
using HeapCare.ApiClient.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HeapCare.ApiClient.Tests;

public sealed class ResilienceTests
{
    [Fact]
    public async Task StandardResilience_RetriesTransientGetResponses()
    {
        var handler = new SequenceHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
        var services = new ServiceCollection();
        services.AddHeapCareApiClient("Resilient", options =>
            options.BaseAddress = new Uri("https://api.heapcare.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("Resilient");
        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task StandardResilience_DoesNotRetryUnsafePostRequests()
    {
        var handler = new SequenceHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
        var services = new ServiceCollection();
        services.AddHeapCareApiClient("Resilient", options =>
            options.BaseAddress = new Uri("https://api.heapcare.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("Resilient");
        using var content = new StringContent("{}");
        using var response = await client.PostAsync("/commands", content);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, handler.RequestCount);
    }

    private sealed class SequenceHandler(params HttpStatusCode[] responses) : HttpMessageHandler
    {
        private int _requestCount;

        public int RequestCount => Volatile.Read(ref _requestCount);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var index = Interlocked.Increment(ref _requestCount) - 1;
            var statusCode = responses[Math.Min(index, responses.Length - 1)];
            return Task.FromResult(new HttpResponseMessage(statusCode) { RequestMessage = request });
        }
    }
}
