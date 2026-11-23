using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CompileCare.ApiClient.Errors;

/// <summary>Safe RFC 9457 Problem Details fields returned by CompileCare APIs.</summary>
public sealed class CompileCareApiProblemDetails
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("status")]
    public int? Status { get; init; }

    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    [JsonPropertyName("instance")]
    public string? Instance { get; init; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    [JsonPropertyName("errors")]
    public Dictionary<string, string[]>? Errors { get; init; }
}

/// <summary>
/// Represents a non-successful CompileCare API response. The response body is not
/// included in the exception message to avoid accidental disclosure of sensitive data.
/// </summary>
public sealed class CompileCareApiException : HttpRequestException
{
    public CompileCareApiException(
        HttpStatusCode statusCode,
        CompileCareApiProblemDetails problemDetails,
        string? correlationId)
        : base(
            problemDetails.Detail ?? problemDetails.Title ?? $"CompileCare API request failed with status {(int)statusCode}.",
            inner: null,
            statusCode)
    {
        ProblemDetails = problemDetails;
        CorrelationId = correlationId;
    }

    public CompileCareApiProblemDetails ProblemDetails { get; }

    public string? CorrelationId { get; }
}

public static class HttpResponseMessageExtensions
{
    public static async Task EnsureSuccessOrThrowAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.IsSuccessStatusCode)
            return;

        CompileCareApiProblemDetails? problem = null;
        if (response.Content is not null &&
            (response.Content.Headers.ContentType?.MediaType is "application/problem+json" or "application/json"))
        {
            try
            {
                problem = await response.Content.ReadFromJsonAsync<CompileCareApiProblemDetails>(
                    cancellationToken: cancellationToken);
            }
            catch (System.Text.Json.JsonException)
            {
                // Fall back to a status-only exception for malformed/nonconforming error bodies.
            }
        }

        var statusCode = response.StatusCode;
        problem ??= new CompileCareApiProblemDetails
        {
            Title = response.ReasonPhrase ?? "The API request failed.",
            Status = (int)statusCode
        };

        var correlationId = response.Headers.TryGetValues("X-Correlation-ID", out var values)
            ? values.FirstOrDefault()
            : null;

        throw new CompileCareApiException(statusCode, problem, correlationId);
    }
}
