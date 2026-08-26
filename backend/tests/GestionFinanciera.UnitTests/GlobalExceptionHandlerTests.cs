using System.Text.Json;

using GestionFinanciera.Api.Middleware;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace GestionFinanciera.UnitTests;

/// <summary>
/// Verifies the global exception handler: unexpected errors become RFC 7807
/// ProblemDetails, generic details in production, stack traces dev-only, and
/// the response never leaks internal exception messages.
/// </summary>
public sealed class GlobalExceptionHandlerTests
{
    private static async Task<ProblemDetails> InvokeAsync(Exception exception, bool isDevelopment = false)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        if (isDevelopment)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IWebHostEnvironment>(new FakeWebHostEnvironment
            {
                EnvironmentName = Environments.Development,
            });
            httpContext.RequestServices = services.BuildServiceProvider();
        }

        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        bool handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);
        Assert.True(handled);

        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body);
        string json = await reader.ReadToEndAsync();

        return JsonSerializer.Deserialize<ProblemDetails>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        })!;
    }

    [Fact]
    public async Task UnknownException_Returns500_WithGenericDetail()
    {
        var problem = await InvokeAsync(new InvalidOperationException("secret internal message"));

        Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
        Assert.Equal("Internal Server Error", problem.Title);
        Assert.DoesNotContain("secret internal message", problem.Detail);
        Assert.False(problem.Extensions.ContainsKey("exception"));
    }

    [Fact]
    public async Task UnknownException_IncludesTraceId()
    {
        var problem = await InvokeAsync(new InvalidOperationException("boom"));

        Assert.True(problem.Extensions.ContainsKey("traceId"));
        Assert.False(string.IsNullOrWhiteSpace(problem.Extensions["traceId"]?.ToString()));
    }

    [Fact]
    public async Task BadHttpRequestException_Returns400_WithMessage()
    {
        var problem = await InvokeAsync(new BadHttpRequestException("Invalid JSON body"));

        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("Invalid JSON body", problem.Detail);
    }

    [Fact]
    public async Task Development_IncludesExceptionDetails()
    {
        var problem = await InvokeAsync(new InvalidOperationException("secret internal message"), isDevelopment: true);

        Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
        Assert.True(problem.Extensions.ContainsKey("exception"));
        Assert.Contains("secret internal message", problem.Extensions["exception"]!.ToString());
    }

    [Fact]
    public async Task OperationCanceled_IsHandled_WithoutWritingAResponse()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);

        bool handled = await handler.TryHandleAsync(
            httpContext, new OperationCanceledException(), CancellationToken.None);

        Assert.True(handled);
        // StatusCode stays at its default (200) — the important contract is that
        // the handler never writes a body for client cancellations.
        Assert.Equal(0, httpContext.Response.Body.Length);
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "GestionFinanciera.Tests";

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; } = string.Empty;

        public string EnvironmentName { get; set; } = Environments.Production;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = string.Empty;
    }
}
