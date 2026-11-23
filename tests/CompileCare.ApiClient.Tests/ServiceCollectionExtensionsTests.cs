using CompileCare.ApiClient;
using CompileCare.ApiClient.Abstractions;
using CompileCare.ApiClient.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CompileCare.ApiClient.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCompileCareApiClient_RegistersClientAndDefaultProviders()
    {
        var services = new ServiceCollection();
        services.AddCompileCareApiClient("CompileCare", options =>
        {
            options.BaseAddress = new Uri("https://api.compilecare.test/");
            options.Timeout = TimeSpan.FromSeconds(15);
        });

        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptionsMonitor<CompileCareApiClientOptions>>().Get("CompileCare");
        Assert.Equal(new Uri("https://api.compilecare.test/"), options.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(15), options.Timeout);
        Assert.IsType<NullTenantContextProvider>(provider.GetRequiredService<ITenantContextProvider>());
        Assert.IsType<NullAccessTokenProvider>(provider.GetRequiredService<IAccessTokenProvider>());
        Assert.IsType<DefaultCorrelationIdProvider>(provider.GetRequiredService<ICorrelationIdProvider>());
        Assert.NotNull(provider.GetRequiredService<IHttpClientFactory>().CreateClient("CompileCare"));
    }

    [Fact]
    public void AddCompileCareApiClient_UsesCustomProvidersRegisteredBeforeExtension()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITenantContextProvider, CustomTenantProvider>();
        services.AddSingleton<IAccessTokenProvider, CustomTokenProvider>();
        services.AddCompileCareApiClient("CompileCare", options =>
            options.BaseAddress = new Uri("https://api.compilecare.test/"));

        using var provider = services.BuildServiceProvider();

        Assert.IsType<CustomTenantProvider>(provider.GetRequiredService<ITenantContextProvider>());
        Assert.IsType<CustomTokenProvider>(provider.GetRequiredService<IAccessTokenProvider>());
    }

    [Fact]
    public void AddCompileCareApiClient_RejectsRelativeBaseAddress()
    {
        var services = new ServiceCollection();
        services.AddCompileCareApiClient("CompileCare", options => options.BaseAddress = new Uri("/relative", UriKind.Relative));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<CompileCareApiClientOptions>>();

        Assert.Throws<OptionsValidationException>(() => options.Get("CompileCare"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddCompileCareApiClient_RejectsNonPositiveTimeout(int seconds)
    {
        var services = new ServiceCollection();
        services.AddCompileCareApiClient("CompileCare", options =>
        {
            options.BaseAddress = new Uri("https://api.compilecare.test/");
            options.Timeout = TimeSpan.FromSeconds(seconds);
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<CompileCareApiClientOptions>>();

        Assert.Throws<OptionsValidationException>(() => options.Get("CompileCare"));
    }

    [Fact]
    public void AddCompileCareTypedClient_ResolvesTypedClientWithConfiguredBaseAddress()
    {
        var services = new ServiceCollection();
        services.AddCompileCareTypedClient<TestTypedClient>(
            "TypedCompileCare",
            options => options.BaseAddress = new Uri("https://api.compilecare.test/"));

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<TestTypedClient>();

        Assert.Equal(new Uri("https://api.compilecare.test/"), client.BaseAddress);
    }

    private sealed class TestTypedClient(HttpClient httpClient)
    {
        public Uri? BaseAddress => httpClient.BaseAddress;
    }

    private sealed class CustomTenantProvider : ITenantContextProvider
    {
        public Guid? GetTenantId() => Guid.Parse("c0aa79b4-6d94-41c2-87e5-04c5d2a3b8c9");
    }

    private sealed class CustomTokenProvider : IAccessTokenProvider
    {
        public ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult<string?>("custom-token");
    }
}