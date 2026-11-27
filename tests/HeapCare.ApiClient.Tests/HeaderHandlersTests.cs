using HeapCare.ApiClient.Abstractions;
using HeapCare.ApiClient.Handlers;
using Xunit;

namespace HeapCare.ApiClient.Tests;

public class HeaderHandlersTests
{
    [Fact]
    public async Task TenantHeaderHandler_AddsTenantHeader_WhenTenantIsAvailable()
    {
        using var client = CreateClient(new TenantHeaderHandler(new FixedTenantProvider(Guid.Parse("b313443b-8a35-42a7-a4c3-3d0fa213b5db"))));
        using var response = await client.GetAsync("https://example.test/");
        Assert.Equal("b313443b-8a35-42a7-a4c3-3d0fa213b5db", response.RequestMessage?.Headers.GetValues(TenantHeaderHandler.HeaderName).Single());
    }

    [Fact]
    public async Task TenantHeaderHandler_DoesNotAddHeader_WhenTenantIsMissing()
    {
        using var client = CreateClient(new TenantHeaderHandler(new FixedTenantProvider(null)));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");
        request.Headers.Add(TenantHeaderHandler.HeaderName, "caller-tenant");

        using var response = await client.SendAsync(request);

        Assert.False(response.RequestMessage?.Headers.Contains(TenantHeaderHandler.HeaderName));
    }

    [Fact]
    public async Task TenantHeaderHandler_OverwritesExistingHeader_WhenProviderReturnsTenant()
    {
        using var client = CreateClient(new TenantHeaderHandler(new FixedTenantProvider(Guid.Parse("a1a720d9-1682-42b4-a90c-7ce52fbe2df1"))));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");
        request.Headers.Add(TenantHeaderHandler.HeaderName, "c9e5861e-2658-4355-9f29-4895a96cc0c0");

        using var response = await client.SendAsync(request);

        Assert.Equal("a1a720d9-1682-42b4-a90c-7ce52fbe2df1", response.RequestMessage?.Headers.GetValues(TenantHeaderHandler.HeaderName).Single());
    }

    [Fact]
    public async Task TenantHeaderHandler_RejectsEmptyTenantIdentifier()
    {
        using var client = CreateClient(new TenantHeaderHandler(new FixedTenantProvider(Guid.Empty)));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetAsync("https://example.test/"));
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

    [Theory]
    [InlineData("")]
    [InlineData("invalid value")]
    [InlineData("bad\r\nheader")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task CorrelationIdHandler_ReplacesInvalidProviderValueAndCallerHeader(string invalidId)
    {
        using var client = CreateClient(new CorrelationIdHandler(new FixedCorrelationProvider(invalidId)));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");
        request.Headers.Add(CorrelationIdHandler.HeaderName, "caller-correlation");

        using var response = await client.SendAsync(request);

        var generated = response.RequestMessage?.Headers.GetValues(CorrelationIdHandler.HeaderName).Single();
        Assert.True(Guid.TryParseExact(generated, "N", out _));
        Assert.NotEqual("caller-correlation", generated);
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

    [Theory]
    [InlineData("unsafe value")]
    [InlineData("bad\r\nheader")]
    public void DefaultCorrelationIdProvider_RejectsUnsafeScope(string value)
        => Assert.Throws<ArgumentException>(() => new DefaultCorrelationIdProvider().BeginScope(value));

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

    private sealed class FixedTenantProvider(Guid? tenantId) : ITenantContextProvider
    {
        public Guid? GetTenantId() => tenantId;
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