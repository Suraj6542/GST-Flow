namespace GstInvoiceTool.Api.Services;

using GstInvoiceTool.Api.DTOs;

/// <summary>
/// Core GST tax calculation logic.
/// Determines CGST/SGST vs IGST split based on business vs client state.
/// 
/// Rule:
///   Same state  → CGST = taxRate/2, SGST = taxRate/2, IGST = 0
///   Diff state  → CGST = 0, SGST = 0, IGST = taxRate
/// </summary>
public class TaxCalculationService
{
    /// <summary>
    /// Calculate tax breakdown for a set of line items.
    /// </summary>
    /// <param name="businessState">The business owner's registered state</param>
    /// <param name="clientState">The client's state</param>
    /// <param name="lineItems">Line items with quantity, rate, and tax rate</param>
    /// <returns>Complete tax breakdown with per-item details</returns>
    public TaxBreakdown Calculate(string businessState, string clientState, List<LineItemRequest> lineItems)
    {
        var isIntraState = string.Equals(
            businessState?.Trim(), clientState?.Trim(),
            StringComparison.OrdinalIgnoreCase);

        var breakdown = new TaxBreakdown
        {
            TaxType = isIntraState ? "intra" : "inter",
            LineItemDetails = new List<LineItemTaxDetail>()
        };

        foreach (var item in lineItems)
        {
            var amount = Math.Round(item.Quantity * item.Rate, 2);
            var detail = CalculateLineItemTax(amount, item.TaxRate, isIntraState);
            detail.Description = item.Description;
            detail.TaxRate = item.TaxRate;
            detail.Amount = amount;

            breakdown.LineItemDetails.Add(detail);

            breakdown.Subtotal += amount;
            breakdown.Cgst += detail.Cgst;
            breakdown.Sgst += detail.Sgst;
            breakdown.Igst += detail.Igst;
            breakdown.TotalTax += detail.TotalTax;
        }

        breakdown.GrandTotal = breakdown.Subtotal + breakdown.TotalTax;

        // Final rounding
        breakdown.Subtotal = Math.Round(breakdown.Subtotal, 2);
        breakdown.Cgst = Math.Round(breakdown.Cgst, 2);
        breakdown.Sgst = Math.Round(breakdown.Sgst, 2);
        breakdown.Igst = Math.Round(breakdown.Igst, 2);
        breakdown.TotalTax = Math.Round(breakdown.TotalTax, 2);
        breakdown.GrandTotal = Math.Round(breakdown.GrandTotal, 2);

        return breakdown;
    }

    private static LineItemTaxDetail CalculateLineItemTax(
        decimal amount, decimal taxRate, bool isIntraState)
    {
        var detail = new LineItemTaxDetail();

        if (isIntraState)
        {
            // Intra-state: split tax equally between CGST and SGST
            detail.Cgst = Math.Round(amount * (taxRate / 2) / 100, 2);
            detail.Sgst = Math.Round(amount * (taxRate / 2) / 100, 2);
            detail.Igst = 0;
        }
        else
        {
            // Inter-state: full tax as IGST
            detail.Cgst = 0;
            detail.Sgst = 0;
            detail.Igst = Math.Round(amount * taxRate / 100, 2);
        }

        detail.TotalTax = detail.Cgst + detail.Sgst + detail.Igst;
        detail.Total = amount + detail.TotalTax;

        return detail;
    }
}
