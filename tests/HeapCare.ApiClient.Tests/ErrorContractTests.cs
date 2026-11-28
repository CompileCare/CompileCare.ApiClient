using System.Net;
using System.Text;
using HeapCare.ApiClient.Errors;
using Xunit;

namespace HeapCare.ApiClient.Tests;

public sealed class ErrorContractTests
{
    [Fact]
    public async Task EnsureSuccessOrThrowAsync_MapsProblemDetailsAndCorrelationId()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(
                """
                {
                  "type": "about:blank",
                  "title": "Conflict",
                  "code": "facility_conflict",
                  "status": 409,
                  "detail": "The resource conflicts with an existing facility.",
                  "instance": "/api/v1/hospitals",
                  "traceId": "trace-123"
                }
                """,
                Encoding.UTF8,
                "application/problem+json")
        };
        response.Headers.Add("X-Correlation-ID", "corr-123");

        var exception = await Assert.ThrowsAsync<HeapCareApiException>(
            () => response.EnsureSuccessOrThrowAsync());

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal("about:blank", exception.ProblemDetails.Type);
        Assert.Equal("facility_conflict", exception.ProblemDetails.Code);
        Assert.Equal("Conflict", exception.ProblemDetails.Title);
        Assert.Equal("The resource conflicts with an existing facility.", exception.ProblemDetails.Detail);
        Assert.Equal("trace-123", exception.ProblemDetails.TraceId);
        Assert.Equal("corr-123", exception.CorrelationId);
        Assert.DoesNotContain("The resource conflicts", exception.Message);
    }

    [Fact]
    public async Task EnsureSuccessOrThrowAsync_UsesStatusOnlyFallbackForNonProblemResponse()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("upstream failure", Encoding.UTF8, "text/plain")
        };

        var exception = await Assert.ThrowsAsync<HeapCareApiException>(
            () => response.EnsureSuccessOrThrowAsync());

        Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
        Assert.Equal((int)HttpStatusCode.BadGateway, exception.ProblemDetails.Status);
    }

    [Fact]
    public async Task EnsureSuccessOrThrowAsync_DoesNotThrowForSuccessfulResponse()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NoContent);
        await response.EnsureSuccessOrThrowAsync();
    }
}
