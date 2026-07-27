import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import api from '../api/axiosInstance';
import type { Invoice, ApiResponse } from '../types';
import { EmptyState } from '../components/common/EmptyState';
import { useToast } from '../components/common/ToastProvider';
import {
  FileText,
  Plus,
  Download,
  CreditCard,
  CheckCircle2,
  Clock,
  AlertTriangle,
  Send,
  Eye,
  X,
  Calendar,
  Filter,
  Mail,
} from 'lucide-react';

export const InvoiceList: React.FC = () => {
  const navigate = useNavigate();
  const { showToast } = useToast();
  const [sendingEmailId, setSendingEmailId] = useState<string | null>(null);
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedStatus, setSelectedStatus] = useState<string>('all');

  // Payment Modal State
  const [paymentModalInvoice, setPaymentModalInvoice] = useState<Invoice | null>(null);
  const [paymentAmount, setPaymentAmount] = useState<number>(0);
  const [paymentMethod, setPaymentMethod] = useState<string>('bank_transfer');
  const [paymentNotes, setPaymentNotes] = useState<string>('');
  const [paymentSubmitting, setPaymentSubmitting] = useState<boolean>(false);
  const [paymentError, setPaymentError] = useState<string>('');

  // Invoice Detail Modal State
  const [detailModalInvoice, setDetailModalInvoice] = useState<Invoice | null>(null);

  const fetchInvoices = async () => {
    try {
      const url = selectedStatus === 'all' ? '/invoices' : `/invoices?status=${selectedStatus}`;
      const res = await api.get<ApiResponse<Invoice[]>>(url);
      if (res.data.success && res.data.data) {
        setInvoices(res.data.data);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchInvoices();
  }, [selectedStatus]);

  const handleDownloadPdf = async (invoiceId: string, invoiceNumber: string) => {
    try {
      const response = await api.get(`/invoices/${invoiceId}/pdf`, {
        responseType: 'blob',
      });
      const url = window.URL.createObjectURL(new Blob([response.data], { type: 'application/pdf' }));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `${invoiceNumber}.pdf`);
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (err) {
      showToast('error', 'Download Failed', 'Failed to download invoice PDF');
    }
  };

  const handleStatusChange = async (invoiceId: string, newStatus: string) => {
    try {
      await api.patch(`/invoices/${invoiceId}/status`, { status: newStatus });
      fetchInvoices();
      if (detailModalInvoice?.id === invoiceId) {
        const updated = await api.get<ApiResponse<Invoice>>(`/invoices/${invoiceId}`);
        if (updated.data.data) setDetailModalInvoice(updated.data.data);
      }
      showToast('success', 'Status Updated', `Invoice status changed to ${newStatus}`);
    } catch (err: any) {
      showToast('error', 'Update Failed', err.response?.data?.error || 'Failed to update status');
    }
  };

  const openPaymentModal = (invoice: Invoice) => {
    setPaymentModalInvoice(invoice);
    setPaymentAmount(invoice.balanceDue);
    setPaymentMethod('bank_transfer');
    setPaymentNotes('');
    setPaymentError('');
  };

  const handleRecordPayment = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!paymentModalInvoice) return;
    setPaymentError('');
    setPaymentSubmitting(true);

    try {
      await api.post(`/invoices/${paymentModalInvoice.id}/payments`, {
        amount: paymentAmount,
        date: new Date().toISOString(),
        method: paymentMethod,
        notes: paymentNotes,
      });
      setPaymentModalInvoice(null);
      fetchInvoices();
    } catch (err: any) {
      setPaymentError(err.response?.data?.error || 'Failed to record payment');
    } finally {
      setPaymentSubmitting(false);
    }
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'paid':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
            <CheckCircle2 className="w-3 h-3" /> Paid
          </span>
        );
      case 'overdue':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-red-500/10 text-red-400 border border-red-500/20">
            <AlertTriangle className="w-3 h-3" /> Overdue
          </span>
        );
      case 'partial':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-amber-500/10 text-amber-400 border border-amber-500/20">
            <Clock className="w-3 h-3" /> Partial
          </span>
        );
      case 'sent':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-indigo-500/10 text-indigo-400 border border-indigo-500/20">
            <Send className="w-3 h-3" /> Sent
          </span>
        );
      default:
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-slate-800 text-slate-400 border border-slate-700">
            Draft
          </span>
        );
    }
  };

  const statuses = ['all', 'draft', 'sent', 'partial', 'paid', 'overdue'];

  return (
    <div className="space-y-6">
      {/* Top Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-100 tracking-tight">Invoices</h1>
          <p className="text-xs text-slate-400 mt-1">
            Track status, record payments, and download QuestPDF compliance tax invoices
          </p>
        </div>
        <Link
          to="/invoices/new"
          className="flex items-center justify-center gap-2 px-4 py-2.5 bg-indigo-600 hover:bg-indigo-500 text-white font-medium text-sm rounded-xl shadow-lg shadow-indigo-500/20 transition-all"
        >
          <Plus className="w-4 h-4" />
          Create Invoice
        </Link>
      </div>

      {/* Filter Tabs */}
      <div className="flex items-center gap-2 overflow-x-auto pb-1 border-b border-slate-800">
        <Filter className="w-4 h-4 text-slate-500 shrink-0 mr-1" />
        {statuses.map((st) => (
          <button
            key={st}
            onClick={() => setSelectedStatus(st)}
            className={`px-3 py-1.5 rounded-xl text-xs font-semibold capitalize whitespace-nowrap transition-all ${
              selectedStatus === st
                ? 'bg-indigo-600 text-white shadow-md shadow-indigo-500/20'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900'
            }`}
          >
            {st}
          </button>
        ))}
      </div>

      {/* Table / Cards */}
      {loading ? (
        <div className="space-y-3">
          {[1, 2, 3].map((n) => (
            <div key={n} className="h-16 bg-slate-900/60 border border-slate-800 rounded-xl animate-pulse" />
          ))}
        </div>
      ) : invoices.length === 0 ? (
        <EmptyState
          icon={FileText}
          title="No invoices found"
          description={
            selectedStatus !== 'all'
              ? `No invoices matching status '${selectedStatus}'.`
              : "Create your first invoice to see GST calculation in action!"
          }
          actionLabel="Create Invoice"
          onAction={() => navigate('/invoices/new')}
        />
      ) : (
        <div className="bg-slate-900 border border-slate-800/80 rounded-2xl overflow-hidden shadow-xl">
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-slate-800 bg-slate-950/60 text-[11px] font-bold text-slate-400 uppercase tracking-wider">
                  <th className="py-3.5 px-4">Invoice #</th>
                  <th className="py-3.5 px-4">Client</th>
                  <th className="py-3.5 px-4">Issue / Due Date</th>
                  <th className="py-3.5 px-4 text-right">Tax Split</th>
                  <th className="py-3.5 px-4 text-right">Grand Total</th>
                  <th className="py-3.5 px-4 text-center">Status</th>
                  <th className="py-3.5 px-4 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/60 text-xs">
                {invoices.map((inv) => (
                  <tr key={inv.id} className="hover:bg-slate-800/40 transition-colors">
                    <td className="py-3.5 px-4 font-mono font-bold text-indigo-300">
                      {inv.invoiceNumber}
                    </td>

                    <td className="py-3.5 px-4">
                      <p className="font-semibold text-slate-200">{inv.clientName}</p>
                      <p className="text-[10px] text-slate-400">{inv.clientState}</p>
                    </td>

                    <td className="py-3.5 px-4 text-slate-400">
                      <div className="flex items-center gap-1 text-[11px]">
                        <Calendar className="w-3 h-3 text-slate-500" />
                        {new Date(inv.issueDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short' })}
                      </div>
                      <p className="text-[10px] text-slate-500 mt-0.5">
                        Due: {new Date(inv.dueDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short' })}
                      </p>
                    </td>

                    <td className="py-3.5 px-4 text-right">
                      {inv.cgst > 0 ? (
                        <div className="text-[11px]">
                          <span className="text-emerald-400">CGST+SGST: ₹{(inv.cgst + inv.sgst).toFixed(2)}</span>
                        </div>
                      ) : (
                        <div className="text-[11px]">
                          <span className="text-indigo-400">IGST: ₹{inv.igst.toFixed(2)}</span>
                        </div>
                      )}
                    </td>

                    <td className="py-3.5 px-4 text-right font-bold text-slate-100 text-sm">
                      ₹{inv.grandTotal.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
                    </td>

                    <td className="py-3.5 px-4 text-center">{getStatusBadge(inv.status)}</td>

                    <td className="py-3.5 px-4 text-right space-x-1">
                      <button
                        onClick={() => setDetailModalInvoice(inv)}
                        className="p-1.5 text-slate-400 hover:text-indigo-400 hover:bg-slate-800 rounded-lg transition-colors"
                        title="View Details"
                      >
                        <Eye className="w-4 h-4" />
                      </button>

                      <button
                        onClick={() => handleDownloadPdf(inv.id, inv.invoiceNumber)}
                        className="p-1.5 text-slate-400 hover:text-emerald-400 hover:bg-slate-800 rounded-lg transition-colors"
                        title="Download QuestPDF"
                      >
                        <Download className="w-4 h-4" />
                      </button>

                      <button
                        disabled={sendingEmailId === inv.id}
                        onClick={async () => {
                          setSendingEmailId(inv.id);
                          try {
                            await api.post(`/invoices/${inv.id}/send-email`);
                            showToast('success', 'Email Sent!', `Invoice #${inv.invoiceNumber} with PDF attachment sent to ${inv.clientName}.`);
                            fetchInvoices();
                          } catch (err: any) {
                            showToast('error', 'Email Failed', err.response?.data?.error || 'Failed to send email. Check your SMTP settings.');
                          } finally {
                            setSendingEmailId(null);
                          }
                        }}
                        className="p-1.5 text-slate-400 hover:text-indigo-400 hover:bg-slate-800 rounded-lg transition-colors disabled:opacity-50"
                        title="Email PDF to Client"
                      >
                        <Mail className={`w-4 h-4 ${sendingEmailId === inv.id ? 'animate-pulse text-indigo-400' : ''}`} />
                      </button>

                      {inv.status === 'draft' && (
                        <button
                          onClick={() => handleStatusChange(inv.id, 'sent')}
                          className="p-1.5 text-slate-400 hover:text-indigo-400 hover:bg-slate-800 rounded-lg transition-colors"
                          title="Mark Sent"
                        >
                          <Send className="w-4 h-4" />
                        </button>
                      )}

                      {(inv.status === 'sent' || inv.status === 'partial' || inv.status === 'overdue') && (
                        <button
                          onClick={() => openPaymentModal(inv)}
                          className="p-1.5 text-slate-400 hover:text-emerald-400 hover:bg-slate-800 rounded-lg transition-colors"
                          title="Record Payment"
                        >
                          <CreditCard className="w-4 h-4" />
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Record Payment Modal */}
      {paymentModalInvoice && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-md w-full p-6 shadow-2xl relative">
            <button
              onClick={() => setPaymentModalInvoice(null)}
              className="absolute top-5 right-5 text-slate-400 hover:text-white"
            >
              <X className="w-5 h-5" />
            </button>

            <h3 className="text-lg font-bold text-slate-100 mb-1">Record Payment</h3>
            <p className="text-xs text-slate-400 mb-4">
              Invoice #{paymentModalInvoice.invoiceNumber} — Balance Due: ₹
              {paymentModalInvoice.balanceDue.toFixed(2)}
            </p>

            {paymentError && (
              <div className="mb-4 p-3 bg-red-500/10 border border-red-500/30 rounded-xl text-red-400 text-xs font-medium">
                {paymentError}
              </div>
            )}

            <form onSubmit={handleRecordPayment} className="space-y-4">
              <div>
                <label className="block text-xs font-medium text-slate-300 mb-1">Payment Amount (₹)</label>
                <input
                  type="number"
                  step="any"
                  max={paymentModalInvoice.balanceDue}
                  required
                  value={paymentAmount}
                  onChange={(e) => setPaymentAmount(parseFloat(e.target.value) || 0)}
                  className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-xl text-slate-100 text-sm font-semibold focus:outline-none focus:border-indigo-500"
                />
              </div>

              <div>
                <label className="block text-xs font-medium text-slate-300 mb-1">Payment Method</label>
                <select
                  value={paymentMethod}
                  onChange={(e) => setPaymentMethod(e.target.value)}
                  className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-xl text-slate-100 text-sm focus:outline-none focus:border-indigo-500"
                >
                  <option value="bank_transfer">Bank Transfer (NEFT/RTGS/IMPS)</option>
                  <option value="upi">UPI / GPay / PhonePe</option>
                  <option value="cash">Cash</option>
                  <option value="cheque">Cheque</option>
                </select>
              </div>

              <div>
                <label className="block text-xs font-medium text-slate-300 mb-1">Notes / Transaction Reference</label>
                <input
                  type="text"
                  placeholder="e.g. UPI Ref #987654"
                  value={paymentNotes}
                  onChange={(e) => setPaymentNotes(e.target.value)}
                  className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-xl text-slate-100 text-xs focus:outline-none focus:border-indigo-500"
                />
              </div>

              <div className="flex items-center justify-end gap-3 pt-3">
                <button
                  type="button"
                  onClick={() => setPaymentModalInvoice(null)}
                  className="px-4 py-2 text-xs text-slate-400 hover:text-white"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={paymentSubmitting}
                  className="px-5 py-2.5 bg-emerald-600 hover:bg-emerald-500 text-white font-medium text-xs rounded-xl shadow-lg shadow-emerald-500/20 transition-all disabled:opacity-50"
                >
                  {paymentSubmitting ? 'Recording...' : 'Record Payment'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Invoice Detail Modal */}
      {detailModalInvoice && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-2xl w-full p-6 shadow-2xl relative max-h-[90vh] overflow-y-auto">
            <button
              onClick={() => setDetailModalInvoice(null)}
              className="absolute top-5 right-5 text-slate-400 hover:text-white"
            >
              <X className="w-5 h-5" />
            </button>

            <div className="flex items-center justify-between mb-4">
              <div>
                <span className="text-xs font-bold text-indigo-400 uppercase tracking-wider">
                  Tax Invoice Details
                </span>
                <h2 className="text-2xl font-extrabold text-slate-100">{detailModalInvoice.invoiceNumber}</h2>
              </div>
              <div>{getStatusBadge(detailModalInvoice.status)}</div>
            </div>

            <div className="grid grid-cols-2 gap-4 p-4 bg-slate-950 rounded-xl border border-slate-800 text-xs mb-6">
              <div>
                <p className="text-slate-500">Billed To:</p>
                <p className="font-bold text-slate-200 text-sm mt-0.5">{detailModalInvoice.clientName}</p>
                <p className="text-slate-400">State: {detailModalInvoice.clientState}</p>
              </div>
              <div className="text-right">
                <p className="text-slate-500">Dates:</p>
                <p className="text-slate-300">Issued: {new Date(detailModalInvoice.issueDate).toLocaleDateString()}</p>
                <p className="text-slate-300">Due: {new Date(detailModalInvoice.dueDate).toLocaleDateString()}</p>
              </div>
            </div>

            {/* Line Items */}
            <div className="space-y-2 mb-6">
              <h4 className="text-xs font-bold uppercase tracking-wider text-slate-400">Line Items</h4>
              <div className="bg-slate-950 rounded-xl border border-slate-800 overflow-hidden">
                <table className="w-full text-left text-xs">
                  <thead className="border-b border-slate-800 bg-slate-900/50 text-[10px] text-slate-400">
                    <tr>
                      <th className="p-2.5">Description</th>
                      <th className="p-2.5 text-right">Qty</th>
                      <th className="p-2.5 text-right">Rate</th>
                      <th className="p-2.5 text-right">GST %</th>
                      <th className="p-2.5 text-right">Amount</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-800/50">
                    {detailModalInvoice.lineItems.map((li, idx) => (
                      <tr key={idx}>
                        <td className="p-2.5 text-slate-200">{li.description}</td>
                        <td className="p-2.5 text-right text-slate-400">{li.quantity}</td>
                        <td className="p-2.5 text-right text-slate-400">₹{li.rate.toFixed(2)}</td>
                        <td className="p-2.5 text-right text-indigo-400">{li.taxRate}%</td>
                        <td className="p-2.5 text-right font-medium text-slate-100">₹{li.amount.toFixed(2)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            {/* Totals */}
            <div className="bg-slate-950 p-4 rounded-xl border border-slate-800 space-y-2 text-xs mb-6">
              <div className="flex justify-between text-slate-400">
                <span>Subtotal:</span>
                <span>₹{detailModalInvoice.subtotal.toFixed(2)}</span>
              </div>

              {detailModalInvoice.cgst > 0 && (
                <>
                  <div className="flex justify-between text-emerald-400">
                    <span>CGST:</span>
                    <span>₹{detailModalInvoice.cgst.toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between text-emerald-400">
                    <span>SGST:</span>
                    <span>₹{detailModalInvoice.sgst.toFixed(2)}</span>
                  </div>
                </>
              )}

              {detailModalInvoice.igst > 0 && (
                <div className="flex justify-between text-indigo-400">
                  <span>IGST:</span>
                  <span>₹{detailModalInvoice.igst.toFixed(2)}</span>
                </div>
              )}

              <div className="flex justify-between font-bold text-slate-100 text-sm pt-2 border-t border-slate-800">
                <span>Grand Total:</span>
                <span className="text-indigo-400">₹{detailModalInvoice.grandTotal.toFixed(2)}</span>
              </div>
            </div>

            <div className="flex justify-end gap-3">
              <button
                onClick={() => handleDownloadPdf(detailModalInvoice.id, detailModalInvoice.invoiceNumber)}
                className="flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white font-medium text-xs rounded-xl shadow-lg shadow-emerald-500/20"
              >
                <Download className="w-4 h-4" /> Download PDF
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
