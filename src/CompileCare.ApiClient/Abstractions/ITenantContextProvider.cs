namespace CompileCare.ApiClient.Abstractions;

/// <summary>Provides the authenticated application's selected tenant identifier.</summary>
public interface ITenantContextProvider
{
    Guid? GetTenantId();
}

public sealed class NullTenantContextProvider : ITenantContextProvider
{
    public Guid? GetTenantId() => null;
}
