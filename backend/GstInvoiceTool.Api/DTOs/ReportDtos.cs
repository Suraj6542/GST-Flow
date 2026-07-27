namespace GstInvoiceTool.Api.DTOs;

public class DashboardSummary
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal TotalOverdue { get; set; }
    public int InvoiceCount { get; set; }
    public int PaidCount { get; set; }
    public int OverdueCount { get; set; }
    public int ClientCount { get; set; }
    public decimal ThisMonthRevenue { get; set; }
    public decimal LastMonthRevenue { get; set; }
    public decimal RevenueGrowthPercent { get; set; }
    public List<MonthlyRevenue> RevenueTrend { get; set; } = new();
    public List<RecentInvoiceDto> RecentInvoices { get; set; } = new();
}

public class MonthlyRevenue
{
    public string Month { get; set; } = null!;
    public decimal Revenue { get; set; }
    public decimal TaxCollected { get; set; }
}

public class RecentInvoiceDto
{
    public string Id { get; set; } = null!;
    public string InvoiceNumber { get; set; } = null!;
    public string ClientName { get; set; } = null!;
    public decimal GrandTotal { get; set; }
    public string Status { get; set; } = null!;
    public DateTime DueDate { get; set; }
}

public class TaxReport
{
    public string Quarter { get; set; } = null!;
    public decimal TotalCgst { get; set; }
    public decimal TotalSgst { get; set; }
    public decimal TotalIgst { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalRevenue { get; set; }
    public int InvoiceCount { get; set; }
    public List<TaxReportItem> Items { get; set; } = new();
}

public class TaxReportItem
{
    public string InvoiceNumber { get; set; } = null!;
    public string ClientName { get; set; } = null!;
    public DateTime IssueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Cgst { get; set; }
    public decimal Sgst { get; set; }
    public decimal Igst { get; set; }
    public decimal TotalTax { get; set; }
    public decimal GrandTotal { get; set; }
}
