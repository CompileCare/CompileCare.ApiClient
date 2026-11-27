namespace HeapCare.ApiClient;

public sealed class HeapCareApiClientOptions
{
    public Uri? BaseAddress { get; set; }

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
