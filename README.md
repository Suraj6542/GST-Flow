# GST/Invoice Compliance Tool 🚀

A full-stack invoicing & GST compliance tool designed for solo freelancers, independent consultants, and small businesses in India.

---

## 🌟 Key Features

1. **⭐ Live GST Tax Calculation Engine ("The Aha Moment")**
   - Instant calculation of **CGST & SGST** (Intra-State supply) vs **IGST** (Inter-State supply) based on business state vs client state.
   - Live recalculation as line items, quantities, rates, and GST tax slabs (0%, 5%, 12%, 18%, 28%) change.

2. **Client Directory & State Scoping**
   - Manage client profiles with full address, GSTIN numbers, and state classification.

3. **QuestPDF Invoice Export**
   - Download professional, computer-generated Tax Invoice PDFs with business header, line items, and tax breakdown.

4. **Payment Tracking & Status Transitions**
   - Track invoice statuses (`draft` → `sent` → `partial` → `paid` / `overdue`).
   - Record partial and full payments with method breakdown (Bank Transfer, UPI, Cash, Cheque).

5. **⭐ Automated Background Jobs & Hangfire Engine (Your USP)**
   - Daily background worker powered by **Hangfire & Memory/MongoDB storage**.
   - **Recurring Invoice Automation:** Automatically generates tax invoices on weekly, monthly, or quarterly schedules.
   - **Automated Email Dispatching:** Auto-generates QuestPDF invoices and emails them directly to clients upon schedule execution.
   - **Automated Overdue Reminders:** Automatically checks for overdue invoices and dispatches payment reminder emails.
   - **Hangfire Live Dashboard:** Integrated live dashboard at `/hangfire` for monitoring background job execution and queues.

6. **⭐ Per-User SMTP Settings UI**
   - Each logged-in owner can configure their own SMTP credentials (Gmail App Password, Outlook, Yahoo, or Custom SMTP server) via a dedicated **Email Settings** page.
   - Emails and PDFs are dispatched directly from the user's email address rather than a single hardcoded sender.
   - Integrated test email utility to verify SMTP setup instantly.

7. **Custom Toast & Modal Notification System**
   - Replaced native browser popups with sleek, dark-themed slide-in toast notifications and custom confirmation modals.

8. **Financial Dashboard & Recharts Analytics**
   - Total revenue, outstanding balances, overdue alerts, and 12-month revenue trend visualization using Recharts.

9. **Quarterly GST Tax Return Compliance Reports**
   - Aggregated CGST, SGST, and IGST totals grouped by Indian Fiscal Quarters (Q1: Apr-Jun, Q2: Jul-Sep, Q3: Oct-Dec, Q4: Jan-Mar).
   - One-click **CSV export for accountants**.

---

## 🛠️ Technology Stack

| Layer | Tech | Description |
|---|---|---|
| **Backend API** | ASP.NET Core 9 Web API | C#, Controller-based API structure |
| **Database** | MongoDB Atlas / Local MongoDB | MongoDB.Driver with BSON document mappings |
| **Background Jobs**| Hangfire | Background job server for recurring billing & automated payment reminders |
| **Frontend** | React 18 + TypeScript + Vite | Built with React Router v6 & Tailwind CSS v4 |
| **Styling** | Tailwind CSS v4 | Modern CSS-first utility classes with dark theme |
| **Charts** | Recharts | Interactive area charts for monthly revenue trends |
| **PDF Generation**| QuestPDF | Server-side tax invoice PDF generation |
| **Auth** | JWT (Access + Refresh Tokens) | JWT authentication with refresh token rotation & BCrypt hashing |
| **Unit Testing** | xUnit | Unit test suite for GST tax calculation rules |

---

## 🔒 Security & Compliance Checklist

- **Owner-Scoped Data Isolation (IDOR Protection):** Every query and mutation at the repository layer strictly filters by `ownerId` from authenticated JWT claims.
- **Password Security:** Passwords hashed using **BCrypt.Net-Next** before storage.
- **JWT Refresh Token Rotation:** 15-minute access tokens with 7-day refresh tokens that rotate upon use.
- **Input Validation:** Data Transfer Objects (DTOs) enforced with Regex for GSTIN format validation (`^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$`).
- **Rate Limiting:** Built-in rate limiting on authentication routes (10 requests/min) to prevent brute-force attacks.

---

## ⚠️ Known Limitations & Future Directions

- **GSTIN Validation:** Standard format validation (Regex pattern), not live API lookup with the GST portal.
- **Payment Processing:** Payments are manually recorded rather than processed via razorpay/Stripe gateways.
- **Future Directions:** Multi-client accountant/CAs portal view, multi-currency invoice support, and automated WhatsApp/email invoice reminders.

---

## ⚙️ Running Locally

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js v20+](https://nodejs.org/)
- MongoDB Atlas cluster (configured in `appsettings.json`).

### 1. Backend Setup
```bash
cd backend/GstInvoiceTool.Api
dotnet restore
dotnet run
```
*The API will start at `http://localhost:5000` (or `https://localhost:5001`).*

### 2. Run Unit Tests
```bash
cd backend/GstInvoiceTool.Tests
dotnet test
```

### 3. Frontend Setup
```bash
cd frontend
npm install
npm run dev
```
*Open `http://localhost:5173` in your browser.*

---

## 👤 Demo Login Credentials

If seeded automatically on startup:
- **Email:** `demo@gstflow.com`
- **Password:** `Demo1234!`
