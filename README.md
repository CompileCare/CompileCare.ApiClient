# CompileCare.ApiClient

Reusable HTTP client infrastructure for CompileCare services and API consumers.

## Features

- Named HttpClient registration with validated base address and timeout.
- Automatic `X-Tenant-ID` propagation through an injectable tenant context provider.
- Automatic `X-Correlation-ID` propagation through an injectable correlation provider.
- Optional Bearer token injection through an injectable access-token provider.
- Compatible with NSwag-generated clients and ordinary HttpClient consumers.
- No retry policy is enabled by default. Consumers should explicitly add resilience policies based on operation safety.

> **Security:** The tenant header is context only. It is not authentication or authorization. The API must validate the authenticated user's tenant membership and enforce tenant scope server-side. Never log tokens, credentials, or PHI.

## Registration

```csharp
using CompileCare.ApiClient.DependencyInjection;

builder.Services.AddCompileCareApiClient(
    "CompileCare",
    options =>
    {
        options.BaseAddress = new Uri(builder.Configuration["CompileCare:BaseUrl"]!);
        options.Timeout = TimeSpan.FromSeconds(30);
    });
```

Use the named client:

```csharp
var client = httpClientFactory.CreateClient("CompileCare");
```

For a generated NSwag client, register it with the named client or configure its constructor to use the configured HttpClient. This package does not generate clients during build; generation remains a separate concern.

## Providers

Override the default providers in your application DI container:

- `ITenantContextProvider`: returns the tenant selected for the current request/session.
- `ICorrelationIdProvider`: returns the correlation ID for outbound calls.
- `IAccessTokenProvider`: asynchronously returns the current access token, or null for unauthenticated calls.

The defaults do not supply tenant or access token values. Applications should register their own implementations.

## Testing

```bash
dotnet test
```

## Current scope

This package provides shared request configuration and propagation. Authentication/authorization, tenant validation, retry policies, generated API contracts, and service-specific endpoints belong to their respective application layers.
