using CompileCare.ApiClient.Abstractions;
using CompileCare.ApiClient.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;

namespace CompileCare.ApiClient.DependencyInjection;

public static class ServiceCollectionExtensions
{
    private const int DefaultMaxRetryAttempts = 2;

    /// <summary>
    /// Registers a named CompileCare HttpClient with authoritative tenant, correlation,
    /// and optional bearer-token propagation plus bounded standard HTTP resilience.
    /// Only safe HTTP methods are retried automatically.
    /// </summary>
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
            .AddHttpMessageHandler<BearerTokenHandler>()
            .AddStandardResilienceHandler(options =>
            {
                // GET/HEAD/OPTIONS/TRACE may be retried; unsafe methods are never
                // transparently replayed because their operations may not be idempotent.
                options.Retry.MaxRetryAttempts = DefaultMaxRetryAttempts;
                options.Retry.DisableForUnsafeHttpMethods();
            });
    }

    /// <summary>
    /// Registers a strongly typed client backed by the same named CompileCare HTTP pipeline.
    /// The client type must have a constructor accepting HttpClient.
    /// </summary>
    public static IHttpClientBuilder AddCompileCareTypedClient<TClient>(
        this IServiceCollection services,
        string name,
        Action<CompileCareApiClientOptions> configure)
        where TClient : class
    {
        return services
            .AddCompileCareApiClient(name, configure)
            .AddTypedClient<TClient>();
    }
}
