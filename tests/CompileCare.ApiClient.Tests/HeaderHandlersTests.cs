using CompileCare.ApiClient.Abstractions;
using CompileCare.ApiClient.Handlers;
using Xunit;

namespace CompileCare.ApiClient.Tests;

public class HeaderHandlersTests
{
    [Fact]
    public async Task TenantHeaderHandler_AddsTenantHeader_WhenTenantIsAvailable()
    {
        using var client = CreateClient(new TenantHeaderHandler(new FixedTenantProvider("tenant-123")));
        using var response = await client.GetAsync("https://example.test/");
        Assert.Equal("tenant-123", response.RequestMessage?.Headers.GetValues(TenantHeaderHandler.HeaderName).Single());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TenantHeaderHandler_DoesNotAddHeader_WhenTenantIsMissingOrBlank(string? tenantId)
    {
        using var client = CreateClient(new TenantHeaderHandler(new FixedTenantProvider(tenantId)));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");
        request.Headers.Add(TenantHeaderHandler.HeaderName, "caller-tenant");

        using var response = await client.SendAsync(request);

        Assert.False(response.RequestMessage?.Headers.Contains(TenantHeaderHandler.HeaderName));
    }

    [Fact]
    public async Task TenantHeaderHandler_OverwritesExistingHeader_WhenProviderReturnsTenant()
    {
        using var client = CreateClient(new TenantHeaderHandler(new FixedTenantProvider("tenant-new")));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");
        request.Headers.Add(TenantHeaderHandler.HeaderName, "tenant-old");

        using var response = await client.SendAsync(request);

        Assert.Equal("tenant-new", response.RequestMessage?.Headers.GetValues(TenantHeaderHandler.HeaderName).Single());
    }

    [Fact]
    public async Task CorrelationIdHandler_AddsCorrelationHeader()
    {
        using var client = CreateClient(new CorrelationIdHandler(new FixedCorrelationProvider("corr-abc")));
        using var response = await client.GetAsync("https://example.test/");
        Assert.Equal("corr-abc", response.RequestMessage?.Headers.GetValues(CorrelationIdHandler.HeaderName).Single());
    }

    [Fact]
    public async Task CorrelationIdHandler_OverwritesExistingHeader()
    {
        using var client = CreateClient(new CorrelationIdHandler(new FixedCorrelationProvider("corr-new")));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");
        request.Headers.Add(CorrelationIdHandler.HeaderName, "corr-old");

        using var response = await client.SendAsync(request);

        Assert.Equal("corr-new", response.RequestMessage?.Headers.GetValues(CorrelationIdHandler.HeaderName).Single());
    }

    [Fact]
    public void DefaultCorrelationIdProvider_UsesScopedId_WhenNoActivityExists()
    {
        var provider = new DefaultCorrelationIdProvider();
        using (provider.BeginScope("scope-123"))
        {
            Assert.Equal("scope-123", provider.GetCorrelationId());
            Assert.Equal("scope-123", provider.GetCorrelationId());
        }
        Assert.NotEqual("scope-123", provider.GetCorrelationId());
    }

    [Fact]
    public void DefaultCorrelationIdProvider_RejectsBlankScope()
        => Assert.Throws<ArgumentException>(() => new DefaultCorrelationIdProvider().BeginScope(" "));

    [Fact]
    public async Task BearerTokenHandler_AddsAuthorization_WhenTokenExists()
    {
        using var client = CreateClient(new BearerTokenHandler(new FixedTokenProvider("token-value")));
        using var response = await client.GetAsync("https://example.test/");
        Assert.Equal("Bearer", response.RequestMessage?.Headers.Authorization?.Scheme);
        Assert.Equal("token-value", response.RequestMessage?.Headers.Authorization?.Parameter);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BearerTokenHandler_ClearsAuthorization_WhenTokenIsMissingOrBlank(string? token)
    {
        using var client = CreateClient(new BearerTokenHandler(new FixedTokenProvider(token)));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "caller-token");

        using var response = await client.SendAsync(request);

        Assert.Null(response.RequestMessage?.Headers.Authorization);
    }

    [Fact]
    public async Task BearerTokenHandler_OverwritesExistingAuthorization_WhenTokenExists()
    {
        using var client = CreateClient(new BearerTokenHandler(new FixedTokenProvider("provider-token")));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "caller-token");

        using var response = await client.SendAsync(request);

        Assert.Equal("provider-token", response.RequestMessage?.Headers.Authorization?.Parameter);
    }

    private static HttpClient CreateClient(DelegatingHandler handler)
    {
        handler.InnerHandler = new CaptureHandler();
        return new HttpClient(handler);
    }

    private sealed class FixedTenantProvider(string? tenantId) : ITenantContextProvider
    {
        public string? GetTenantId() => tenantId;
    }

    private sealed class FixedCorrelationProvider(string correlationId) : ICorrelationIdProvider
    {
        public string GetCorrelationId() => correlationId;
    }

    private sealed class FixedTokenProvider(string? token) : IAccessTokenProvider
    {
        public ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(token);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { RequestMessage = request });
    }
}