namespace CompileCare.ApiClient.Abstractions;

public interface ITenantContextProvider
{
    string? GetTenantId();
}

public sealed class NullTenantContextProvider : ITenantContextProvider
{
    public string? GetTenantId() => null;
}