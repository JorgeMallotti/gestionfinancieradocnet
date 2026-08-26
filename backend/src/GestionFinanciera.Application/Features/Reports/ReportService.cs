using FluentValidation;

using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Companies.Interfaces;
using GestionFinanciera.Application.Features.Reports.DTOs;
using GestionFinanciera.Application.Features.Reports.Interfaces;
using GestionFinanciera.Application.Features.Transactions.DTOs;
using GestionFinanciera.Application.Features.Transactions.Interfaces;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Reports;

/// <summary>
/// Builds the report data (company name, transactions in range, totals) and
/// delegates generation/sending to the infrastructure ports. Contains no
/// QuestPDF/ClosedXML/MailKit code — that stays behind the ports.
/// </summary>
public sealed class ReportService(
    ITransactionRepository transactions,
    ICompanyRepository companies,
    IPdfService pdf,
    IExcelService excel,
    IEmailService email,
    IValidator<EmailReportDto> emailValidator) : IReportService
{
    public async Task<Result<byte[]>> GeneratePdfAsync(
        Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        if (IsInvalidRange(from, to))
            return Result<byte[]>.Failure(ErrorCode.Validation, "'From' cannot be after 'To'.");

        var data = await BuildReportDataAsync(companyId, from, to, ct);
        byte[] bytes = await pdf.GenerateAsync(data, ct);
        return Result<byte[]>.Success(bytes);
    }

    public async Task<Result<byte[]>> GenerateExcelAsync(
        Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        if (IsInvalidRange(from, to))
            return Result<byte[]>.Failure(ErrorCode.Validation, "'From' cannot be after 'To'.");

        var data = await BuildReportDataAsync(companyId, from, to, ct);
        byte[] bytes = await excel.GenerateAsync(data, ct);
        return Result<byte[]>.Success(bytes);
    }

    public async Task<Result> SendByEmailAsync(
        EmailReportDto dto, Guid companyId, CancellationToken ct)
    {
        var validation = await emailValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result.Failure(validation.Errors.First().ErrorMessage);

        var data = await BuildReportDataAsync(companyId, dto.From, dto.To, ct);
        byte[] pdfBytes = await pdf.GenerateAsync(data, ct);

        return await email.SendReportAsync(
            dto.Email, data.CompanyName, dto.From, dto.To, pdfBytes, ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private async Task<ReportDataDto> BuildReportDataAsync(
        Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        string companyName = await companies.GetNameAsync(companyId, ct) ?? "Company";
        var items = await transactions.GetInRangeAsync(companyId, from, to, ct);

        var dtos = items.Select(TransactionDto.FromEntity).ToList();

        return new ReportDataDto(
            companyName,
            dtos,
            dtos.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
            dtos.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
            from,
            to);
    }

    private static bool IsInvalidRange(DateTimeOffset? from, DateTimeOffset? to) =>
        from.HasValue && to.HasValue && from.Value > to.Value;
}
