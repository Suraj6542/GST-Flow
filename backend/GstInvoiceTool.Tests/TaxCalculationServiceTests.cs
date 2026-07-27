namespace GstInvoiceTool.Tests;

using GstInvoiceTool.Api.DTOs;
using GstInvoiceTool.Api.Services;
using Xunit;

public class TaxCalculationServiceTests
{
    private readonly TaxCalculationService _service;

    public TaxCalculationServiceTests()
    {
        _service = new TaxCalculationService();
    }

    [Fact]
    public void IntraState_SplitsTaxEquallyBetweenCGSTAndSGST()
    {
        // Arrange: Business in Maharashtra, Client in Maharashtra
        var businessState = "Maharashtra";
        var clientState = "Maharashtra";
        var items = new List<LineItemRequest>
        {
            new() { Description = "Web Development Services", Quantity = 1, Rate = 10000, TaxRate = 18 }
        };

        // Act
        var result = _service.Calculate(businessState, clientState, items);

        // Assert
        Assert.Equal("intra", result.TaxType);
        Assert.Equal(10000m, result.Subtotal);
        Assert.Equal(900m, result.Cgst); // 9% of 10,000
        Assert.Equal(900m, result.Sgst); // 9% of 10,000
        Assert.Equal(0m, result.Igst);
        Assert.Equal(1800m, result.TotalTax);
        Assert.Equal(11800m, result.GrandTotal);
    }

    [Fact]
    public void InterState_CalculatesFullTaxAsIGST()
    {
        // Arrange: Business in Maharashtra, Client in Karnataka
        var businessState = "Maharashtra";
        var clientState = "Karnataka";
        var items = new List<LineItemRequest>
        {
            new() { Description = "Consulting", Quantity = 2, Rate = 5000, TaxRate = 18 }
        };

        // Act
        var result = _service.Calculate(businessState, clientState, items);

        // Assert
        Assert.Equal("inter", result.TaxType);
        Assert.Equal(10000m, result.Subtotal);
        Assert.Equal(0m, result.Cgst);
        Assert.Equal(0m, result.Sgst);
        Assert.Equal(1800m, result.Igst); // 18% of 10,000
        Assert.Equal(1800m, result.TotalTax);
        Assert.Equal(11800m, result.GrandTotal);
    }

    [Fact]
    public void CaseInsensitiveAndTrimmedStateComparison()
    {
        // Arrange: case difference and trailing spaces
        var businessState = " delhi ";
        var clientState = "Delhi";
        var items = new List<LineItemRequest>
        {
            new() { Description = "Design", Quantity = 1, Rate = 1000, TaxRate = 12 }
        };

        // Act
        var result = _service.Calculate(businessState, clientState, items);

        // Assert
        Assert.Equal("intra", result.TaxType);
        Assert.Equal(60m, result.Cgst);
        Assert.Equal(60m, result.Sgst);
        Assert.Equal(0m, result.Igst);
    }

    [Fact]
    public void ZeroTaxRate_ResultsInZeroTax()
    {
        var items = new List<LineItemRequest>
        {
            new() { Description = "Exempt Goods", Quantity = 5, Rate = 200, TaxRate = 0 }
        };

        var result = _service.Calculate("Gujarat", "Gujarat", items);

        Assert.Equal(1000m, result.Subtotal);
        Assert.Equal(0m, result.TotalTax);
        Assert.Equal(1000m, result.GrandTotal);
    }

    [Fact]
    public void MultipleLineItems_AggregatesCorrectly()
    {
        var items = new List<LineItemRequest>
        {
            new() { Description = "Item 1 (18%)", Quantity = 1, Rate = 1000, TaxRate = 18 },
            new() { Description = "Item 2 (5%)", Quantity = 2, Rate = 500, TaxRate = 5 }
        };

        var result = _service.Calculate("Karnataka", "Karnataka", items);

        // Item 1: 1000 subtotal, 90 CGST, 90 SGST
        // Item 2: 1000 subtotal, 25 CGST, 25 SGST
        Assert.Equal(2000m, result.Subtotal);
        Assert.Equal(115m, result.Cgst);
        Assert.Equal(115m, result.Sgst);
        Assert.Equal(230m, result.TotalTax);
        Assert.Equal(2230m, result.GrandTotal);
    }

    [Fact]
    public void RoundingToTwoDecimalPlaces()
    {
        // 333.33 * 0.18 = 59.9994 -> CGST/SGST = 29.9997 -> rounded to 30.00 each
        var items = new List<LineItemRequest>
        {
            new() { Description = "Fractional Item", Quantity = 1, Rate = 333.33m, TaxRate = 18 }
        };

        var result = _service.Calculate("Tamil Nadu", "Tamil Nadu", items);

        Assert.Equal(333.33m, result.Subtotal);
        Assert.Equal(30.00m, result.Cgst);
        Assert.Equal(30.00m, result.Sgst);
    }
}
