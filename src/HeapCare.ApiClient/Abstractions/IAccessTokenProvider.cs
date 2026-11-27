namespace HeapCare.ApiClient.Abstractions;

public interface IAccessTokenProvider
{
    ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}

public sealed class NullAccessTokenProvider : IAccessTokenProvider
{
    public ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult<string?>(null);
}