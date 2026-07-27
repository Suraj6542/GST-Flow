namespace GstInvoiceTool.Api.Data;

using MongoDB.Driver;
using GstInvoiceTool.Api.Models;
using GstInvoiceTool.Api.Services;

public static class SeedData
{
    public static async Task SeedAsync(IMongoDatabase db)
    {
        var users = db.GetCollection<User>("users");
        var clients = db.GetCollection<Client>("clients");
        var invoices = db.GetCollection<Invoice>("invoices");

        if (await users.CountDocumentsAsync(FilterDefinition<User>.Empty) > 0)
        {
            return; // Data already present
        }

        // 1. Create Demo User
        var user = new User
        {
            Name = "Rahul Sharma",
            Email = "demo@gstflow.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo1234!"),
            Role = "owner",
            Business = new BusinessInfo
            {
                Name = "Apex Tech Innovations",
                Gstin = "27AAACA1234A1Z5",
                State = "Maharashtra",
                Address = "Suite 402, Bandra-Kurla Complex, Mumbai, Maharashtra 400051"
            }
        };

        await users.InsertOneAsync(user);

        // 2. Create Realistic Clients (Intra and Inter-state)
        var clientList = new List<Client>
        {
            new()
            {
                OwnerId = user.Id,
                Name = "Acme Retail Pvt Ltd",
                Email = "billing@acmeretail.in",
                Gstin = "27AAACB5678B1Z2",
                State = "Maharashtra", // Intra-State
                BillingAddress = "Plot 12, MIDC Industrial Area, Pune, MH 411018",
                Phone = "+91 98200 12345"
            },
            new()
            {
                OwnerId = user.Id,
                Name = "Bangalore Cloud Labs",
                Email = "accounts@cloudlabs.io",
                Gstin = "29AAACC9012C1Z8",
                State = "Karnataka", // Inter-State
                BillingAddress = "Indiranagar 100ft Road, Bengaluru, KA 560038",
                Phone = "+91 98450 67890"
            },
            new()
            {
                OwnerId = user.Id,
                Name = "Delhi Media Agency",
                Email = "finance@delhimedia.com",
                Gstin = "07AAACD3456D1Z1",
                State = "Delhi", // Inter-State
                BillingAddress = "Connaught Place, New Delhi, DL 110001",
                Phone = "+91 98110 54321"
            },
            new()
            {
                OwnerId = user.Id,
                Name = "Solo Design Studio",
                Email = "hello@solodesign.in",
                Gstin = null, // Unregistered
                State = "Maharashtra", // Intra-State
                BillingAddress = "Viman Nagar, Pune, MH 411014",
                Phone = "+91 99220 11223"
            }
        };

        await clients.InsertManyAsync(clientList);

        // 3. Create Sample Invoices with Tax Breakdown
        var taxService = new TaxCalculationService();

        var inv1Items = new List<DTOs.LineItemRequest>
        {
            new() { Description = "Custom Web Application Development", HsnCode = "998314", Quantity = 1, Rate = 75000, TaxRate = 18 },
            new() { Description = "Cloud Deployment & Setup", HsnCode = "998315", Quantity = 1, Rate = 15000, TaxRate = 18 }
        };
        var tax1 = taxService.Calculate(user.Business.State, clientList[0].State, inv1Items);

        var inv1 = new Invoice
        {
            OwnerId = user.Id,
            ClientId = clientList[0].Id,
            ClientName = clientList[0].Name,
            ClientState = clientList[0].State,
            ClientGstin = clientList[0].Gstin,
            InvoiceNumber = "INV-2026-0001",
            IssueDate = DateTime.UtcNow.AddDays(-20),
            DueDate = DateTime.UtcNow.AddDays(-5),
            LineItems = inv1Items.Select(li => new LineItem
            {
                Description = li.Description,
                HsnCode = li.HsnCode,
                Quantity = li.Quantity,
                Rate = li.Rate,
                TaxRate = li.TaxRate,
                Amount = li.Quantity * li.Rate
            }).ToList(),
            Subtotal = tax1.Subtotal,
            Cgst = tax1.Cgst,
            Sgst = tax1.Sgst,
            Igst = tax1.Igst,
            TotalTax = tax1.TotalTax,
            GrandTotal = tax1.GrandTotal,
            Status = InvoiceStatus.Paid,
            Payments = new List<Payment>
            {
                new() { Amount = tax1.GrandTotal, Date = DateTime.UtcNow.AddDays(-10), Method = "bank_transfer", Notes = "NEFT Ref #98765432" }
            }
        };

        var inv2Items = new List<DTOs.LineItemRequest>
        {
            new() { Description = "Quarterly Software Retainer", HsnCode = "998313", Quantity = 3, Rate = 30000, TaxRate = 18 }
        };
        var tax2 = taxService.Calculate(user.Business.State, clientList[1].State, inv2Items);

        var inv2 = new Invoice
        {
            OwnerId = user.Id,
            ClientId = clientList[1].Id,
            ClientName = clientList[1].Name,
            ClientState = clientList[1].State,
            ClientGstin = clientList[1].Gstin,
            InvoiceNumber = "INV-2026-0002",
            IssueDate = DateTime.UtcNow.AddDays(-5),
            DueDate = DateTime.UtcNow.AddDays(10),
            LineItems = inv2Items.Select(li => new LineItem
            {
                Description = li.Description,
                HsnCode = li.HsnCode,
                Quantity = li.Quantity,
                Rate = li.Rate,
                TaxRate = li.TaxRate,
                Amount = li.Quantity * li.Rate
            }).ToList(),
            Subtotal = tax2.Subtotal,
            Cgst = tax2.Cgst,
            Sgst = tax2.Sgst,
            Igst = tax2.Igst,
            TotalTax = tax2.TotalTax,
            GrandTotal = tax2.GrandTotal,
            Status = InvoiceStatus.Sent
        };

        await invoices.InsertManyAsync(new[] { inv1, inv2 });
    }
}
