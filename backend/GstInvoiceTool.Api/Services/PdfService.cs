namespace GstInvoiceTool.Api.Services;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using GstInvoiceTool.Api.Models;

public class PdfService
{
    static PdfService()
    {
        // Set QuestPDF community license (free for non-commercial or open-source / small business under $1M)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInvoicePdf(Invoice invoice, User owner)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36); // 0.5 inch
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Header().Element(header => ComposeHeader(header, invoice, owner));
                page.Content().Element(content => ComposeContent(content, invoice, owner));
                page.Footer().Element(footer => ComposeFooter(footer, invoice));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, Invoice invoice, User owner)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(owner.Business.Name)
                    .FontSize(20).Bold().FontColor(Colors.Blue.Darken3);

                if (!string.IsNullOrEmpty(owner.Business.Gstin))
                {
                    col.Item().Text($"GSTIN: {owner.Business.Gstin}")
                        .FontSize(9).SemiBold();
                }

                col.Item().Text($"State: {owner.Business.State}").FontSize(9);
                if (!string.IsNullOrEmpty(owner.Business.Address))
                {
                    col.Item().Text(owner.Business.Address).FontSize(9);
                }
                col.Item().Text($"Email: {owner.Email}").FontSize(9);
            });

            row.ConstantItem(200).Column(col =>
            {
                col.Item().AlignRight().Text("TAX INVOICE")
                    .FontSize(18).Bold().FontColor(Colors.Grey.Darken4);

                col.Item().AlignRight().Text($"Invoice #: {invoice.InvoiceNumber}")
                    .FontSize(11).Bold();

                col.Item().AlignRight().Text($"Date: {invoice.IssueDate:dd MMM yyyy}")
                    .FontSize(9);

                col.Item().AlignRight().Text($"Due Date: {invoice.DueDate:dd MMM yyyy}")
                    .FontSize(9);

                col.Item().AlignRight().Text($"Status: {invoice.Status.ToUpper()}")
                    .FontSize(9).Bold().FontColor(GetStatusColor(invoice.Status));
            });
        });
    }

    private static void ComposeContent(IContainer container, Invoice invoice, User owner)
    {
        container.PaddingVertical(15).Column(col =>
        {
            // Billed To box
            col.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("BILLED TO:").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text(invoice.ClientName).FontSize(11).Bold();
                    if (!string.IsNullOrEmpty(invoice.ClientGstin))
                    {
                        c.Item().Text($"GSTIN: {invoice.ClientGstin}").FontSize(9);
                    }
                    c.Item().Text($"State: {invoice.ClientState}").FontSize(9);
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("TAX TYPE:").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                    var isIntraState = string.Equals(
                        owner.Business.State.Trim(),
                        invoice.ClientState.Trim(),
                        StringComparison.OrdinalIgnoreCase);

                    if (isIntraState)
                    {
                        c.Item().Text("Intra-State Transaction").FontSize(10).SemiBold();
                        c.Item().Text("(CGST + SGST applicable)").FontSize(9);
                    }
                    else
                    {
                        c.Item().Text("Inter-State Transaction").FontSize(10).SemiBold();
                        c.Item().Text("(IGST applicable)").FontSize(9);
                    }
                });
            });

            col.Item().Height(15);

            // Table of Line Items
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);  // #
                    columns.RelativeColumn(4);   // Description
                    columns.RelativeColumn(2);   // HSN/SAC
                    columns.ConstantColumn(50);  // Qty
                    columns.RelativeColumn(2);   // Rate
                    columns.ConstantColumn(45);  // GST %
                    columns.RelativeColumn(2);   // Amount
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("#").FontColor(Colors.White).Bold().FontSize(9);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Description").FontColor(Colors.White).Bold().FontSize(9);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("HSN/SAC").FontColor(Colors.White).Bold().FontSize(9);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignRight().Text("Qty").FontColor(Colors.White).Bold().FontSize(9);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignRight().Text("Rate (₹)").FontColor(Colors.White).Bold().FontSize(9);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignRight().Text("GST %").FontColor(Colors.White).Bold().FontSize(9);
                    header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignRight().Text("Amount (₹)").FontColor(Colors.White).Bold().FontSize(9);
                });

                int index = 1;
                foreach (var item in invoice.LineItems)
                {
                    var bg = index % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;

                    table.Cell().Background(bg).Padding(5).Text(index.ToString()).FontSize(9);
                    table.Cell().Background(bg).Padding(5).Text(item.Description).FontSize(9);
                    table.Cell().Background(bg).Padding(5).Text(item.HsnCode ?? "-").FontSize(9);
                    table.Cell().Background(bg).Padding(5).AlignRight().Text(item.Quantity.ToString("0.##")).FontSize(9);
                    table.Cell().Background(bg).Padding(5).AlignRight().Text(item.Rate.ToString("N2")).FontSize(9);
                    table.Cell().Background(bg).Padding(5).AlignRight().Text($"{item.TaxRate}%").FontSize(9);
                    table.Cell().Background(bg).Padding(5).AlignRight().Text(item.Amount.ToString("N2")).FontSize(9);

                    index++;
                }
            });

            col.Item().Height(15);

            // Totals Summary
            col.Item().Row(row =>
            {
                row.RelativeItem(2).Column(notesCol =>
                {
                    if (!string.IsNullOrEmpty(invoice.Notes))
                    {
                        notesCol.Item().Text("Notes & Terms:").Bold().FontSize(9);
                        notesCol.Item().Text(invoice.Notes).FontSize(8);
                    }
                });

                row.RelativeItem(3).Column(summaryCol =>
                {
                    summaryCol.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Subtotal:").AlignRight().FontSize(9);
                        r.ConstantItem(100).Text($"₹{invoice.Subtotal:N2}").AlignRight().FontSize(9);
                    });

                    if (invoice.Cgst > 0)
                    {
                        summaryCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text("CGST:").AlignRight().FontSize(9);
                            r.ConstantItem(100).Text($"₹{invoice.Cgst:N2}").AlignRight().FontSize(9);
                        });
                        summaryCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text("SGST:").AlignRight().FontSize(9);
                            r.ConstantItem(100).Text($"₹{invoice.Sgst:N2}").AlignRight().FontSize(9);
                        });
                    }

                    if (invoice.Igst > 0)
                    {
                        summaryCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text("IGST:").AlignRight().FontSize(9);
                            r.ConstantItem(100).Text($"₹{invoice.Igst:N2}").AlignRight().FontSize(9);
                        });
                    }

                    summaryCol.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Total Tax:").AlignRight().FontSize(9).Bold();
                        r.ConstantItem(100).Text($"₹{invoice.TotalTax:N2}").AlignRight().FontSize(9).Bold();
                    });

                    summaryCol.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    summaryCol.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Grand Total:").AlignRight().FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                        r.ConstantItem(100).Text($"₹{invoice.GrandTotal:N2}").AlignRight().FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                    });

                    if (invoice.Payments.Any())
                    {
                        summaryCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Total Paid:").AlignRight().FontSize(9);
                            r.ConstantItem(100).Text($"₹{invoice.TotalPaid:N2}").AlignRight().FontSize(9);
                        });
                        summaryCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Balance Due:").AlignRight().FontSize(10).Bold().FontColor(Colors.Red.Medium);
                            r.ConstantItem(100).Text($"₹{invoice.BalanceDue:N2}").AlignRight().FontSize(10).Bold().FontColor(Colors.Red.Medium);
                        });
                    }
                });
            });
        });
    }

    private static void ComposeFooter(IContainer container, Invoice invoice)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            col.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("Thank you for your business!").FontSize(8).Italic();
                row.RelativeItem().AlignRight().Text("Computer-generated tax invoice").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private static string GetStatusColor(string status) => status.ToLower() switch
    {
        "paid" => Colors.Green.Darken1,
        "overdue" => Colors.Red.Medium,
        "partial" => Colors.Orange.Medium,
        "sent" => Colors.Blue.Medium,
        _ => Colors.Grey.Darken1
    };
}
