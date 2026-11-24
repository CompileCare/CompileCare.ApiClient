namespace CompileCare.ApiClient.Abstractions;

public interface ICorrelationIdProvider
{
    string GetCorrelationId();
}

public sealed class DefaultCorrelationIdProvider : ICorrelationIdProvider
{
    private readonly AsyncLocal<string?> _scopedCorrelationId = new();

    public string GetCorrelationId()
        => System.Diagnostics.Activity.Current?.TraceId.ToString()
           ?? _scopedCorrelationId.Value
           ?? Guid.NewGuid().ToString("N");

    /// <summary>
    /// Establishes a correlation ID for the current async execution flow.
    /// Prefer an active Activity; this scope is a fallback for callers without one.
    /// </summary>
    public IDisposable BeginScope(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        if (!CorrelationIdValidator.IsValid(correlationId))
            throw new ArgumentException("Correlation IDs must be 1-64 ASCII letters, digits, dots, underscores or hyphens.", nameof(correlationId));

        var previous = _scopedCorrelationId.Value;
        _scopedCorrelationId.Value = correlationId;
        return new CorrelationIdScope(() => _scopedCorrelationId.Value = previous);
    }

    private sealed class CorrelationIdScope(Action restore) : IDisposable
    {
        private Action? _restore = restore;

        public void Dispose() => Interlocked.Exchange(ref _restore, null)?.Invoke();
    }
}