namespace CompileCare.ApiClient.Abstractions;

public interface ICorrelationIdProvider
{
    string GetCorrelationId();
}

public sealed class DefaultCorrelationIdProvider : ICorrelationIdProvider
{
    public string GetCorrelationId()
        => System.Diagnostics.Activity.Current?.TraceId.ToString()
           ?? Guid.NewGuid().ToString("N");
}