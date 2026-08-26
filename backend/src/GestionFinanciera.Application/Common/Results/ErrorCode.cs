namespace GestionFinanciera.Application.Common.Results;

/// <summary>
/// Typed error codes for expected failures. Each code maps to exactly one
/// HTTP status so the API layer can build a correct <c>ProblemDetails</c>
/// response without duplicating the mapping in every controller.
/// </summary>
public enum ErrorCode
{
    /// <summary>Input did not pass validation (business or FluentValidation) → HTTP 400.</summary>
    Validation,

    /// <summary>Resource does not exist or is not visible to the caller (hides existence) → HTTP 404.</summary>
    NotFound,

    /// <summary>State conflict, e.g. a duplicate name or a delete blocked by dependencies → HTTP 409.</summary>
    Conflict,

    /// <summary>Authenticated but not authorized for the operation → HTTP 403.</summary>
    Forbidden,

    /// <summary>Unexpected server-side failure (should not normally happen) → HTTP 500.</summary>
    InternalError,
}
