using System.Net;
using System.Text;
using CompileCare.ApiClient.Errors;
using Xunit;

namespace CompileCare.ApiClient.Tests;

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
                  "type": "https://compilecare.in/problems/facility-conflict",
                  "title": "Facility conflict",
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

        var exception = await Assert.ThrowsAsync<CompileCareApiException>(
            () => response.EnsureSuccessOrThrowAsync());

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal("Facility conflict", exception.ProblemDetails.Title);
        Assert.Equal("trace-123", exception.ProblemDetails.TraceId);
        Assert.Equal("corr-123", exception.CorrelationId);
    }

    [Fact]
    public async Task EnsureSuccessOrThrowAsync_UsesStatusOnlyFallbackForNonProblemResponse()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("upstream failure", Encoding.UTF8, "text/plain")
        };

        var exception = await Assert.ThrowsAsync<CompileCareApiException>(
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
