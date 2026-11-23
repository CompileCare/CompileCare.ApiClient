using CompileCare.ApiClient.Abstractions;
using CompileCare.ApiClient.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CompileCare.ApiClient.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddCompileCareApiClient(
        this IServiceCollection services,
        string name,
        Action<CompileCareApiClientOptions> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<CompileCareApiClientOptions>(name)
            .Configure(configure)
            .Validate(options => options.BaseAddress is { IsAbsoluteUri: true },
                "CompileCare API client BaseAddress must be an absolute URI.")
            .Validate(options => options.Timeout > TimeSpan.Zero,
                "CompileCare API client Timeout must be greater than zero.")
            .ValidateOnStart();

        services.TryAddTransient<ITenantContextProvider, NullTenantContextProvider>();
        services.TryAddTransient<IAccessTokenProvider, NullAccessTokenProvider>();
        services.TryAddSingleton<ICorrelationIdProvider, DefaultCorrelationIdProvider>();
        services.AddTransient<TenantHeaderHandler>();
        services.AddTransient<CorrelationIdHandler>();
        services.AddTransient<BearerTokenHandler>();

        return services.AddHttpClient(name)
            .ConfigureHttpClient((serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<
                    Microsoft.Extensions.Options.IOptionsMonitor<CompileCareApiClientOptions>>()
                    .Get(name);

                httpClient.BaseAddress = options.BaseAddress;
                httpClient.Timeout = options.Timeout;
            })
            .AddHttpMessageHandler<TenantHeaderHandler>()
            .AddHttpMessageHandler<CorrelationIdHandler>()
            .AddHttpMessageHandler<BearerTokenHandler>();
    }
}