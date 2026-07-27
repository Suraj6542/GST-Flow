namespace GstInvoiceTool.Api.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using GstInvoiceTool.Api.DTOs;
using GstInvoiceTool.Api.Models;
using GstInvoiceTool.Api.Repositories;
using GstInvoiceTool.Api.Services;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly InvoiceRepository _invoiceRepo;
    private readonly ClientRepository _clientRepo;

    public ReportsController(InvoiceRepository invoiceRepo, ClientRepository clientRepo)
    {
        _invoiceRepo = invoiceRepo;
        _clientRepo = clientRepo;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new AppException("User not authenticated.", 401);

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummary>>> GetSummary()
    {
        var ownerId = GetUserId();
        var collection = _invoiceRepo.GetCollection();

        // Get all non-cancelled invoices for this owner
        var filter = Builders<Invoice>.Filter.And(
            Builders<Invoice>.Filter.Eq(i => i.OwnerId, ownerId),
            Builders<Invoice>.Filter.Ne(i => i.Status, InvoiceStatus.Cancelled));

        var invoices = await collection.Find(filter).ToListAsync();

        var now = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        var paidInvoices = invoices.Where(i => i.Status == InvoiceStatus.Paid).ToList();
        var overdueInvoices = invoices.Where(i => i.Status == InvoiceStatus.Overdue).ToList();
        var outstandingInvoices = invoices.Where(i =>
            i.Status is InvoiceStatus.Sent or InvoiceStatus.Partial or InvoiceStatus.Overdue).ToList();

        var thisMonthRevenue = paidInvoices
            .Where(i => i.UpdatedAt >= thisMonthStart)
            .Sum(i => i.GrandTotal);

        var lastMonthRevenue = paidInvoices
            .Where(i => i.UpdatedAt >= lastMonthStart && i.UpdatedAt < thisMonthStart)
            .Sum(i => i.GrandTotal);

        var growthPercent = lastMonthRevenue > 0
            ? Math.Round((thisMonthRevenue - lastMonthRevenue) / lastMonthRevenue * 100, 1)
            : (thisMonthRevenue > 0 ? 100m : 0m);

        // Revenue trend — last 12 months
        var revenueTrend = new List<MonthlyRevenue>();
        for (int i = 11; i >= 0; i--)
        {
            var monthStart = thisMonthStart.AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);
            var monthLabel = monthStart.ToString("MMM yyyy");

            var monthPaid = paidInvoices
                .Where(inv => inv.UpdatedAt >= monthStart && inv.UpdatedAt < monthEnd)
                .ToList();

            revenueTrend.Add(new MonthlyRevenue
            {
                Month = monthLabel,
                Revenue = monthPaid.Sum(inv => inv.GrandTotal),
                TaxCollected = monthPaid.Sum(inv => inv.TotalTax)
            });
        }

        // Recent invoices (last 5)
        var recentInvoices = invoices
            .OrderByDescending(i => i.CreatedAt)
            .Take(5)
            .Select(i => new RecentInvoiceDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                ClientName = i.ClientName,
                GrandTotal = i.GrandTotal,
                Status = i.Status,
                DueDate = i.DueDate
            })
            .ToList();

        var clientCount = await _clientRepo.CountByOwnerAsync(ownerId);

        var summary = new DashboardSummary
        {
            TotalRevenue = paidInvoices.Sum(i => i.GrandTotal),
            TotalOutstanding = outstandingInvoices.Sum(i => i.BalanceDue),
            TotalOverdue = overdueInvoices.Sum(i => i.BalanceDue),
            InvoiceCount = invoices.Count,
            PaidCount = paidInvoices.Count,
            OverdueCount = overdueInvoices.Count,
            ClientCount = (int)clientCount,
            ThisMonthRevenue = thisMonthRevenue,
            LastMonthRevenue = lastMonthRevenue,
            RevenueGrowthPercent = growthPercent,
            RevenueTrend = revenueTrend,
            RecentInvoices = recentInvoices
        };

        return Ok(ApiResponse<DashboardSummary>.Ok(summary));
    }

    [HttpGet("tax")]
    public async Task<ActionResult<ApiResponse<TaxReport>>> GetTaxReport(
        [FromQuery] string? quarter, [FromQuery] int? year)
    {
        var ownerId = GetUserId();
        var now = DateTime.UtcNow;

        // Parse quarter (Q1=Apr-Jun, Q2=Jul-Sep, Q3=Oct-Dec, Q4=Jan-Mar — Indian fiscal year)
        var reportYear = year ?? now.Year;
        var (startDate, endDate, quarterLabel) = ParseIndianFiscalQuarter(
            quarter ?? GetCurrentIndianQuarter(now), reportYear);

        var filter = Builders<Invoice>.Filter.And(
            Builders<Invoice>.Filter.Eq(i => i.OwnerId, ownerId),
            Builders<Invoice>.Filter.Ne(i => i.Status, InvoiceStatus.Draft),
            Builders<Invoice>.Filter.Ne(i => i.Status, InvoiceStatus.Cancelled),
            Builders<Invoice>.Filter.Gte(i => i.IssueDate, startDate),
            Builders<Invoice>.Filter.Lt(i => i.IssueDate, endDate));

        var invoices = await _invoiceRepo.GetCollection().Find(filter)
            .SortBy(i => i.IssueDate)
            .ToListAsync();

        var report = new TaxReport
        {
            Quarter = quarterLabel,
            TotalCgst = invoices.Sum(i => i.Cgst),
            TotalSgst = invoices.Sum(i => i.Sgst),
            TotalIgst = invoices.Sum(i => i.Igst),
            TotalTax = invoices.Sum(i => i.TotalTax),
            TotalRevenue = invoices.Sum(i => i.Subtotal),
            InvoiceCount = invoices.Count,
            Items = invoices.Select(i => new TaxReportItem
            {
                InvoiceNumber = i.InvoiceNumber,
                ClientName = i.ClientName,
                IssueDate = i.IssueDate,
                Subtotal = i.Subtotal,
                Cgst = i.Cgst,
                Sgst = i.Sgst,
                Igst = i.Igst,
                TotalTax = i.TotalTax,
                GrandTotal = i.GrandTotal
            }).ToList()
        };

        return Ok(ApiResponse<TaxReport>.Ok(report));
    }

    /// <summary>
    /// Indian fiscal year quarters:
    /// Q1: April–June, Q2: July–September, Q3: October–December, Q4: January–March
    /// </summary>
    private static (DateTime start, DateTime end, string label) ParseIndianFiscalQuarter(
        string quarter, int year)
    {
        return quarter.ToUpper() switch
        {
            "Q1" => (new DateTime(year, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                     new DateTime(year, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                     $"Q1 (Apr-Jun {year})"),
            "Q2" => (new DateTime(year, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                     new DateTime(year, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                     $"Q2 (Jul-Sep {year})"),
            "Q3" => (new DateTime(year, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                     new DateTime(year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                     $"Q3 (Oct-Dec {year})"),
            "Q4" => (new DateTime(year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                     new DateTime(year + 1, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                     $"Q4 (Jan-Mar {year + 1})"),
            _ => throw new AppException($"Invalid quarter: {quarter}. Use Q1, Q2, Q3, or Q4.", 400)
        };
    }

    private static string GetCurrentIndianQuarter(DateTime date)
    {
        return date.Month switch
        {
            >= 4 and <= 6 => "Q1",
            >= 7 and <= 9 => "Q2",
            >= 10 and <= 12 => "Q3",
            _ => "Q4"
        };
    }
}
