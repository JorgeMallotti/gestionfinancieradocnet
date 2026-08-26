using GestionFinanciera.Application.Common.Results;

using Microsoft.AspNetCore.Mvc;

namespace GestionFinanciera.Api.Extensions;

/// <summary>
/// Central mapping from the Result pattern (Application layer) to HTTP responses
/// (ProblemDetails, RFC 7807). Every controller uses this instead of hand-rolling
/// status codes, so error semantics stay consistent across the API.
/// </summary>
public static class ResultHttpMapper
{
    /// <summary>Maps a <see cref="Result{T}"/> to 200 OK or the right ProblemDetails error.</summary>
    public static ActionResult<T> ToActionResult<T>(this ControllerBase controller, Result<T> result)
    {
        if (result.IsSuccess)
            return controller.Ok(result.Value);

        return controller.ProblemFromError(result.Code, result.Error);
    }

    /// <summary>Maps a <see cref="Result"/> to 204 No Content or the right ProblemDetails error.</summary>
    public static IActionResult ToActionResult(this ControllerBase controller, Result result)
    {
        if (result.IsSuccess)
            return controller.NoContent();

        return controller.ProblemFromError(result.Code, result.Error);
    }

    private static ObjectResult ProblemFromError(this ControllerBase controller, ErrorCode code, string? detail)
    {
        int status = code switch
        {
            ErrorCode.NotFound => StatusCodes.Status404NotFound,
            ErrorCode.Conflict => StatusCodes.Status409Conflict,
            ErrorCode.Forbidden => StatusCodes.Status403Forbidden,
            ErrorCode.InternalError => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest, // Validation
        };

        return controller.Problem(
            statusCode: status,
            title: ProblemTitleFor(status),
            detail: detail);
    }

    private static string ProblemTitleFor(int status) => status switch
    {
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status500InternalServerError => "Internal Server Error",
        _ => "Bad Request",
    };
}
