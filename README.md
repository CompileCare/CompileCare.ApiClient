# HeapCare API Client

> **Migration status:** HeapCare namespaces, project paths and NuGet package ID are migrated and the package is published to GitHub Packages. The GitHub repository slug and package owner namespace remain `CompileCare` until the administrative rename is completed. No domain, DNS, email or deployment origin is assumed.

Reusable HTTP client infrastructure for HeapCare services and API consumers.

## Features

- Targets .NET 10.
- Named and strongly typed HttpClient registration with validated base address and timeout.
- Automatic `X-Tenant-ID` propagation through an injectable tenant context provider.
- Automatic `X-Correlation-ID` propagation through an injectable correlation provider; values are validated to the API-safe 1–64 character ASCII format, and invalid or caller-supplied values are replaced.
- Optional Bearer token injection through an injectable access-token provider.
- Compatible with NSwag-generated clients and ordinary HttpClient consumers.
- Standard bounded resilience is enabled: timeout, concurrency limiting, circuit breaker, and up to two retries for safe HTTP methods only. Unsafe methods are excluded from automatic retries.

> **Security:** The tenant header is context only. It is not authentication or authorization. The API must validate the authenticated user's tenant membership and enforce tenant scope server-side. Never log tokens, credentials, or PHI.

## Registration

```csharp
using HeapCare.ApiClient.DependencyInjection;

builder.Services.AddHeapCareApiClient(
    "HeapCare",
    options =>
    {
        options.BaseAddress = new Uri(builder.Configuration["HeapCare:BaseUrl"]!);
        options.Timeout = TimeSpan.FromSeconds(30);
    });
```

Use the named client:

```csharp
var client = httpClientFactory.CreateClient("HeapCare");
```

For a generated NSwag client or hand-written client with an HttpClient constructor, use the typed registration:

```csharp
builder.Services.AddHeapCareTypedClient<GeneratedHeapCareClient>(
    "HeapCare",
    options => options.BaseAddress = new Uri(builder.Configuration["HeapCare:BaseUrl"]!));
```

The same tenant, correlation, token, and resilience handlers are used by named and typed registrations. This package does not generate clients during build; generation remains a separate concern.

## Error handling

Use the response extension when a caller expects a successful response. It throws HeapCareApiException on non-success status codes and captures safe RFC 9457 Problem Details fields and the X-Correlation-ID response header when available.

```csharp
using HeapCare.ApiClient.Errors;

using var response = await client.GetAsync("api/v1/hospitals", cancellationToken);
await response.EnsureSuccessOrThrowAsync(cancellationToken);
```

The extension does not log response bodies, tokens, or patient data. Callers that intentionally handle statuses such as 404 or 409 can inspect the response directly instead of invoking the extension.

## Providers

Override the default providers in your application DI container:

- `ITenantContextProvider`: returns the tenant selected for the current request/session.
- `ICorrelationIdProvider`: returns the correlation ID for outbound calls.
- `IAccessTokenProvider`: asynchronously returns the current access token, or null for unauthenticated calls.

The defaults do not supply tenant or access token values.

The default correlation provider uses the active `Activity` trace ID when available. For execution flows without an Activity, establish a correlation scope to keep the same ID across multiple outbound calls:

```csharp
var correlationProvider = serviceProvider.GetRequiredService<ICorrelationIdProvider>()
    as DefaultCorrelationIdProvider;

using (correlationProvider?.BeginScope("request-correlation-id"))
{
    // Outbound calls in this async flow share the correlation ID.
}
```

## Testing

```bash
dotnet test
```

## Resilience and replay safety

The standard resilience pipeline bounds request duration and concurrency and uses a circuit breaker. Only safe HTTP methods are automatically retried. POST, PUT, PATCH, DELETE, and other unsafe methods are not replayed automatically; use an explicit idempotency contract before adding retries for a command. The HttpClient timeout remains configurable per named client.

## Current scope

This package provides reusable outbound HTTP infrastructure. It does not authenticate end users, authorize tenant membership, validate tenant access, generate API contracts, or define service-specific endpoints. Those responsibilities remain in the API/application layers.
