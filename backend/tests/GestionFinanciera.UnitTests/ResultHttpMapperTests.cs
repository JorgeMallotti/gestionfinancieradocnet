using GestionFinanciera.Api.Extensions;
using GestionFinanciera.Application.Common.Results;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GestionFinanciera.UnitTests;

/// <summary>
/// Verifies the central Result → HTTP mapping: every ErrorCode must produce the
/// correct ProblemDetails status, and success must produce 200/204.
/// </summary>
public sealed class ResultHttpMapperTests
{
    private static ControllerBase CreateController()
    {
        var services = new ServiceCollection();
        services.AddOptions<ApiBehaviorOptions>();
        services.AddSingleton<ProblemDetailsFactory, DefaultProblemDetailsFactory>();
        var provider = services.BuildServiceProvider();

        return new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = provider },
            },
        };
    }

    [Fact]
    public void Success_ResultOfT_ReturnsOkObjectResult()
    {
        var controller = CreateController();

        var action = controller.ToActionResult(Result<string>.Success("ok"));

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Equal("ok", ok.Value);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    }

    [Fact]
    public void Success_Result_ReturnsNoContentResult()
    {
        var controller = CreateController();

        var action = controller.ToActionResult(Result.Success());

        Assert.IsType<NoContentResult>(action);
    }

    [Theory]
    [InlineData(ErrorCode.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorCode.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorCode.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorCode.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorCode.InternalError, StatusCodes.Status500InternalServerError)]
    public void Failure_ResultOfT_MapsCodeToStatus(ErrorCode code, int expectedStatus)
    {
        var controller = CreateController();

        var action = controller.ToActionResult(Result<string>.Failure(code, "something failed"));

        var objectResult = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(expectedStatus, problem.Status);
        Assert.Equal("something failed", problem.Detail);
    }

    [Theory]
    [InlineData(ErrorCode.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorCode.Forbidden, StatusCodes.Status403Forbidden)]
    public void Failure_Result_MapsCodeToStatus(ErrorCode code, int expectedStatus)
    {
        var controller = CreateController();

        var action = controller.ToActionResult(Result.Failure(code, "nope"));

        var objectResult = Assert.IsType<ObjectResult>(action);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
    }

    private sealed class TestController : ControllerBase;
}
