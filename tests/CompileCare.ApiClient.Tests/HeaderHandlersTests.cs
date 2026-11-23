using CompileCare.ApiClient.Abstractions;
using CompileCare.ApiClient.Handlers;
using Xunit;

namespace CompileCare.ApiClient.Tests;

public class HeaderHandlersTests
{
    [Fact]
    public async Task TenantHeaderHandler_AddsTenantHeader_WhenTenantIsAvailable()
    {
        var handler = new TenantHeaderHandler(new FixedTenantProvider("tenant-123"))
        {
            InnerHandler = new CaptureHandler()
        };
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("https://example.test/");

        Assert.Equal("tenant-123", response.RequestMessage?.Headers.GetValues(
            TenantHeaderHandler.HeaderName).Single());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TenantHeaderHandler_DoesNotAddHeader_WhenTenantIsMissingOrBlank(string? tenantId)
    {
        var handler = new TenantHeaderHandler(new FixedTenantProvider(tenantId))
        {
            InnerHandler = new CaptureHandler()
        };
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("https://example.test/");

        Assert.False(response.RequestMessage?.Headers.Contains(TenantHeaderHandler.HeaderName));
    }

    [Fact]
    public async Task TenantHeaderHandler_OverwritesExistingHeader_WhenProviderReturnsTenant()
    {
        var handler = new TenantHeaderHandler(new FixedTenantProvider("tenant-new"))
        {
            InnerHandler = new CaptureHandler()
        };
        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");
        request.Headers.Add(TenantHeaderHandler.HeaderName, "tenant-old");

        using var response = await client.SendAsync(request);

        Assert.Equal("tenant-new", response.RequestMessage?.Headers.GetValues(
            TenantHeaderHandler.HeaderName).Single());
    }

    [Fact]
    public async Task CorrelationIdHandler_AddsCorrelationHeader()
    {
        var handler = new CorrelationIdHandler(new FixedCorrelationProvider("corr-abc"))
        {
            InnerHandler = new CaptureHandler()
        };
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("https://example.test/");

        Assert.Equal("corr-abc", response.RequestMessage?.Headers.GetValues(
            CorrelationIdHandler.HeaderName).Single());
    }

    [Fact]
    public async Task CorrelationIdHandler_OverwritesExistingHeader()
    {
        var handler = new CorrelationIdHandler(new FixedCorrelationProvider("corr-new"))
        {
            InnerHandler = new CaptureHandler()
        };
        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");
        request.Headers.Add(CorrelationIdHandler.HeaderName, "corr-old");

        using var response = await client.SendAsync(request);

        Assert.Equal("corr-new", response.RequestMessage?.Headers.GetValues(
            CorrelationIdHandler.HeaderName).Single());
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
    {
        var provider = new DefaultCorrelationIdProvider();

        Assert.Throws<ArgumentException>(() => provider.BeginScope(" "));
    }

    [Fact]
    public async Task BearerTokenHandler_AddsAuthorization_WhenTokenExists()
    {
        var handler = new BearerTokenHandler(new FixedTokenProvider("token-value"))
        {
            InnerHandler = new CaptureHandler()
        };
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("https://example.test/");

        Assert.Equal("Bearer", response.RequestMessage?.Headers.Authorization?.Scheme);
        Assert.Equal("token-value", response.RequestMessage?.Headers.Authorization?.Parameter);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BearerTokenHandler_DoesNotAddAuthorization_WhenTokenIsMissingOrBlank(string? token)
    {
        var handler = new BearerTokenHandler(new FixedTokenProvider(token))
        {
            InnerHandler = new CaptureHandler()
        };
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("https://example.test/");

        Assert.Null(response.RequestMessage?.Headers.Authorization);
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
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                RequestMessage = request
            });
    }
}
