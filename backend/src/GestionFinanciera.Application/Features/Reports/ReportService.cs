using FluentValidation;

using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Accounts.Interfaces;
using GestionFinanciera.Application.Features.Companies.Interfaces;
using GestionFinanciera.Application.Features.Movements.DTOs;
using GestionFinanciera.Application.Features.Movements.Interfaces;
using GestionFinanciera.Application.Features.Reports.DTOs;
using GestionFinanciera.Application.Features.Reports.Interfaces;

namespace GestionFinanciera.Application.Features.Reports;

/// <summary>
/// Builds the report data (bank name, account display name, own movements in
/// range, incoming/outgoing totals) and delegates generation/sending to the
/// infrastructure ports. Contains no QuestPDF/ClosedXML/MailKit code — that
/// stays behind the ports.
/// </summary>
public sealed class ReportService(
    IAccountRepository accounts,
    IMovementRepository movements,
    ICompanyRepository companies,
    IPdfService pdf,
    IExcelService excel,
    IEmailService email,
    IValidator<EmailReportDto> emailValidator) : IReportService
{
    public async Task<Result<byte[]>> GeneratePdfAsync(
        Guid companyId, Guid userId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        var dataResult = await BuildReportDataAsync(companyId, userId, from, to, ct);
        if (dataResult.IsFailure)
            return Result<byte[]>.Failure(dataResult.Code, dataResult.Error!);

        byte[] bytes = await pdf.GenerateAsync(dataResult.Value!, ct);
        return Result<byte[]>.Success(bytes);
    }

    public async Task<Result<byte[]>> GenerateExcelAsync(
        Guid companyId, Guid userId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        var dataResult = await BuildReportDataAsync(companyId, userId, from, to, ct);
        if (dataResult.IsFailure)
            return Result<byte[]>.Failure(dataResult.Code, dataResult.Error!);

        byte[] bytes = await excel.GenerateAsync(dataResult.Value!, ct);
        return Result<byte[]>.Success(bytes);
    }

    public async Task<Result> SendByEmailAsync(
        EmailReportDto dto, Guid companyId, Guid userId, CancellationToken ct)
    {
        var validation = await emailValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result.Failure(validation.Errors.First().ErrorMessage);

        var dataResult = await BuildReportDataAsync(companyId, userId, dto.From, dto.To, ct);
        if (dataResult.IsFailure)
            return Result.Failure(dataResult.Code, dataResult.Error!);

        byte[] pdfBytes = await pdf.GenerateAsync(dataResult.Value!, ct);

        return await email.SendReportAsync(
            dto.Email, dataResult.Value!.CompanyName, dto.From, dto.To, pdfBytes, ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private async Task<Result<ReportDataDto>> BuildReportDataAsync(
        Guid companyId, Guid userId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
            return Result<ReportDataDto>.Failure(ErrorCode.Validation, "'From' cannot be after 'To'.");

        var account = await accounts.GetByOwnerUserIdAsync(companyId, userId, ct);
        if (account is null)
            return Result<ReportDataDto>.Failure(ErrorCode.NotFound, "No account is linked to this user.");

        string companyName = await companies.GetNameAsync(companyId, ct) ?? "Bank";
        var items = await movements.GetByAccountInRangeAsync(companyId, account.Id, from, to, ct);

        var dtos = items.Select(MovementDto.FromEntity).ToList();

        var data = new ReportDataDto(
            companyName,
            account.DisplayName,
            dtos,
            dtos.Where(m => m.ToAccountId == account.Id).Sum(m => m.Amount),
            dtos.Where(m => m.FromAccountId == account.Id).Sum(m => m.Amount),
            from,
            to);

        return Result<ReportDataDto>.Success(data);
    }
}
