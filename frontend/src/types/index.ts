export interface ApiResponse<T = any> {
  success: boolean;
  data?: T;
  error?: string;
}

export interface Business {
  name: string;
  gstin?: string;
  state: string;
  address: string;
}

export interface User {
  id: string;
  name: string;
  email: string;
  role: string;
  business: Business;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface Client {
  id: string;
  name: string;
  email: string;
  gstin?: string;
  state: string;
  billingAddress: string;
  phone?: string;
  createdAt: string;
  updatedAt: string;
}

export interface ClientRequest {
  name: string;
  email: string;
  gstin?: string;
  state: string;
  billingAddress?: string;
  phone?: string;
}

export interface LineItem {
  description: string;
  hsnCode?: string;
  quantity: number;
  rate: number;
  taxRate: number;
  amount: number;
}

export interface Payment {
  id: string;
  amount: number;
  date: string;
  method: string;
  notes?: string;
  createdAt: string;
}

export interface Invoice {
  id: string;
  clientId: string;
  clientName: string;
  clientState: string;
  clientGstin?: string;
  invoiceNumber: string;
  issueDate: string;
  dueDate: string;
  lineItems: LineItem[];
  subtotal: number;
  cgst: number;
  sgst: number;
  igst: number;
  totalTax: number;
  grandTotal: number;
  currency: string;
  notes?: string;
  status: 'draft' | 'sent' | 'partial' | 'paid' | 'overdue' | 'cancelled';
  totalPaid: number;
  balanceDue: number;
  payments: Payment[];
  createdAt: string;
  updatedAt: string;
}

export interface LineItemRequest {
  description: string;
  hsnCode?: string;
  quantity: number;
  rate: number;
  taxRate: number;
}

export interface InvoiceCreateRequest {
  clientId: string;
  issueDate: string;
  dueDate: string;
  lineItems: LineItemRequest[];
  notes?: string;
}

export interface LineItemTaxDetail {
  description: string;
  amount: number;
  taxRate: number;
  cgst: number;
  sgst: number;
  igst: number;
  totalTax: number;
  total: number;
}

export interface TaxBreakdown {
  subtotal: number;
  cgst: number;
  sgst: number;
  igst: number;
  totalTax: number;
  grandTotal: number;
  taxType: 'intra' | 'inter';
  lineItemDetails: LineItemTaxDetail[];
}

export interface DashboardSummary {
  totalRevenue: number;
  totalOutstanding: number;
  totalOverdue: number;
  invoiceCount: number;
  paidCount: number;
  overdueCount: number;
  clientCount: number;
  thisMonthRevenue: number;
  lastMonthRevenue: number;
  revenueGrowthPercent: number;
  revenueTrend: { month: string; revenue: number; taxCollected: number }[];
  recentInvoices: {
    id: string;
    invoiceNumber: string;
    clientName: string;
    grandTotal: number;
    status: string;
    dueDate: string;
  }[];
}

export interface TaxReportItem {
  invoiceNumber: string;
  clientName: string;
  issueDate: string;
  subtotal: number;
  cgst: number;
  sgst: number;
  igst: number;
  totalTax: number;
  grandTotal: number;
}

export interface TaxReport {
  quarter: string;
  totalCgst: number;
  totalSgst: number;
  totalIgst: number;
  totalTax: number;
  totalRevenue: number;
  invoiceCount: number;
  items: TaxReportItem[];
}

export interface RecurringTemplate {
  id: string;
  clientId: string;
  clientName: string;
  frequency: string;
  nextRunDate: string;
  lastRunDate?: string;
  lineItems: LineItem[];
  notes?: string;
  autoSendEmail: boolean;
  isActive: boolean;
  createdAt: string;
}

export interface RecurringTemplateRequest {
  clientId: string;
  frequency: string;
  startDate: string;
  lineItems: LineItemRequest[];
  notes?: string;
  autoSendEmail: boolean;
}
