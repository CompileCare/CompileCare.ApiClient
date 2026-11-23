namespace CompileCare.ApiClient;

public sealed class CompileCareApiClientOptions
{
    public Uri? BaseAddress { get; set; }

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}