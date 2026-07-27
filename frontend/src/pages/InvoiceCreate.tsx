import React, { useState, useEffect, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../api/axiosInstance';
import { useAuth } from '../context/AuthContext';
import type { Client, ApiResponse, LineItemRequest, TaxBreakdown } from '../types';
import { GST_RATES } from '../constants/indianStates';
import {
  FileText,
  Plus,
  Trash2,
  Calendar,
  Zap,
  ArrowLeft,
  Sparkles,
  Building,
  MapPin,
  CheckCircle2,
  AlertCircle,
} from 'lucide-react';

export const InvoiceCreate: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();

  const [clients, setClients] = useState<Client[]>([]);
  const [selectedClientId, setSelectedClientId] = useState('');
  const [issueDate, setIssueDate] = useState(() => new Date().toISOString().split('T')[0]);
  const [dueDate, setDueDate] = useState(() => {
    const d = new Date();
    d.setDate(d.getDate() + 15);
    return d.toISOString().split('T')[0];
  });
  const [notes, setNotes] = useState('Payment due within 15 days. Bank details provided upon request.');

  const [lineItems, setLineItems] = useState<LineItemRequest[]>([
    { description: 'Web Development Services', hsnCode: '998314', quantity: 1, rate: 25000, taxRate: 18 },
  ]);

  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchClients = async () => {
      try {
        const res = await api.get<ApiResponse<Client[]>>('/clients');
        if (res.data.success && res.data.data) {
          setClients(res.data.data);
          if (res.data.data.length > 0) {
            setSelectedClientId(res.data.data[0].id);
          }
        }
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    fetchClients();
  }, []);

  const selectedClient = useMemo(
    () => clients.find((c) => c.id === selectedClientId),
    [clients, selectedClientId]
  );

  // ⭐ LIVE TAX CALCULATION ENGINE (Client-side mirror of TaxCalculationService) ⭐
  const liveTaxBreakdown: TaxBreakdown = useMemo(() => {
    const ownerState = user?.business?.state || '';
    const clientState = selectedClient?.state || ownerState;

    const isIntraState = ownerState.trim().toLowerCase() === clientState.trim().toLowerCase();

    let subtotal = 0;
    let cgst = 0;
    let sgst = 0;
    let igst = 0;

    const details = lineItems.map((item) => {
      const amount = Math.round((item.quantity || 0) * (item.rate || 0) * 100) / 100;
      subtotal += amount;

      let itemCgst = 0;
      let itemSgst = 0;
      let itemIgst = 0;

      if (isIntraState) {
        itemCgst = Math.round(amount * (item.taxRate / 2) / 100 * 100) / 100;
        itemSgst = Math.round(amount * (item.taxRate / 2) / 100 * 100) / 100;
        cgst += itemCgst;
        sgst += itemSgst;
      } else {
        itemIgst = Math.round(amount * item.taxRate / 100 * 100) / 100;
        igst += itemIgst;
      }

      const totalTax = itemCgst + itemSgst + itemIgst;
      return {
        description: item.description,
        amount,
        taxRate: item.taxRate,
        cgst: itemCgst,
        sgst: itemSgst,
        igst: itemIgst,
        totalTax,
        total: amount + totalTax,
      };
    });

    const totalTax = cgst + sgst + igst;
    return {
      subtotal: Math.round(subtotal * 100) / 100,
      cgst: Math.round(cgst * 100) / 100,
      sgst: Math.round(sgst * 100) / 100,
      igst: Math.round(igst * 100) / 100,
      totalTax: Math.round(totalTax * 100) / 100,
      grandTotal: Math.round((subtotal + totalTax) * 100) / 100,
      taxType: isIntraState ? 'intra' : 'inter',
      lineItemDetails: details,
    };
  }, [lineItems, selectedClient, user]);

  const handleLineItemChange = (index: number, field: keyof LineItemRequest, value: any) => {
    const updated = [...lineItems];
    updated[index] = { ...updated[index], [field]: value };
    setLineItems(updated);
  };

  const addLineItem = () => {
    setLineItems([
      ...lineItems,
      { description: '', hsnCode: '9983', quantity: 1, rate: 0, taxRate: 18 },
    ]);
  };

  const removeLineItem = (index: number) => {
    if (lineItems.length === 1) return;
    setLineItems(lineItems.filter((_, i) => i !== index));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedClientId) {
      setError('Please select a client');
      return;
    }
    setError('');
    setSubmitting(true);

    try {
      const payload = {
        clientId: selectedClientId,
        issueDate,
        dueDate,
        lineItems,
        notes,
      };
      const res = await api.post<ApiResponse<any>>('/invoices', payload);
      if (res.data.success) {
        navigate('/invoices');
      }
    } catch (err: any) {
      setError(err.response?.data?.error || 'Failed to create invoice');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64 text-slate-400">
        Loading invoice builder...
      </div>
    );
  }

  if (clients.length === 0) {
    return (
      <div className="bg-slate-900 border border-slate-800 rounded-2xl p-8 text-center max-w-lg mx-auto mt-12">
        <AlertCircle className="w-12 h-12 text-indigo-400 mx-auto mb-3" />
        <h3 className="text-xl font-bold text-slate-100 mb-2">No Clients Registered</h3>
        <p className="text-sm text-slate-400 mb-6">
          You need at least one client before creating a tax invoice. Add your first client now!
        </p>
        <button
          onClick={() => navigate('/clients')}
          className="px-5 py-2.5 bg-indigo-600 hover:bg-indigo-500 text-white font-medium rounded-xl shadow-lg shadow-indigo-500/20"
        >
          Add Client First
        </button>
      </div>
    );
  }

  return (
    <div className="max-w-5xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <button
          onClick={() => navigate('/invoices')}
          className="flex items-center gap-2 text-xs font-semibold text-slate-400 hover:text-white transition-colors"
        >
          <ArrowLeft className="w-4 h-4" />
          Back to Invoices
        </button>
        <div className="flex items-center gap-2">
          <Sparkles className="w-4 h-4 text-emerald-400" />
          <span className="text-xs text-emerald-400 font-semibold uppercase tracking-wider">
            Live GST Engine Active
          </span>
        </div>
      </div>

      <div className="bg-slate-900 border border-slate-800/80 rounded-2xl p-6 lg:p-8 shadow-2xl space-y-8">
        <div>
          <h1 className="text-2xl font-extrabold text-slate-100 tracking-tight flex items-center gap-3">
            <FileText className="w-7 h-7 text-indigo-400" />
            Create Tax Invoice
          </h1>
          <p className="text-xs text-slate-400 mt-1">
            Fill in line items — CGST, SGST, and IGST are calculated live based on state matching.
          </p>
        </div>

        {error && (
          <div className="p-3 bg-red-500/10 border border-red-500/30 rounded-xl text-red-400 text-xs font-medium">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-8">
          {/* Section 1: Client & Dates */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 p-5 bg-slate-950/60 rounded-2xl border border-slate-800/80">
            {/* Client Picker */}
            <div className="md:col-span-1">
              <label className="block text-xs font-semibold text-slate-300 mb-2 flex items-center gap-1.5">
                <Building className="w-3.5 h-3.5 text-indigo-400" />
                Billed To (Client)
              </label>
              <select
                value={selectedClientId}
                onChange={(e) => setSelectedClientId(e.target.value)}
                className="w-full px-3 py-2.5 bg-slate-900 border border-slate-800 rounded-xl text-slate-100 text-sm font-medium focus:outline-none focus:border-indigo-500"
              >
                {clients.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name} ({c.state})
                  </option>
                ))}
              </select>

              {selectedClient && (
                <div className="mt-3 p-3 bg-slate-900/80 rounded-xl border border-slate-800 text-xs space-y-1">
                  <div className="flex items-center justify-between text-slate-300">
                    <span className="text-slate-500">Client State:</span>
                    <span className="font-semibold text-indigo-300">{selectedClient.state}</span>
                  </div>
                  <div className="flex items-center justify-between text-slate-300">
                    <span className="text-slate-500">GSTIN:</span>
                    <span className="font-mono text-emerald-400">{selectedClient.gstin || 'Unregistered'}</span>
                  </div>
                </div>
              )}
            </div>

            {/* Issue Date */}
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-2 flex items-center gap-1.5">
                <Calendar className="w-3.5 h-3.5 text-indigo-400" />
                Issue Date
              </label>
              <input
                type="date"
                required
                value={issueDate}
                onChange={(e) => setIssueDate(e.target.value)}
                className="w-full px-3 py-2.5 bg-slate-900 border border-slate-800 rounded-xl text-slate-100 text-sm focus:outline-none focus:border-indigo-500"
              />
            </div>

            {/* Due Date */}
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-2 flex items-center gap-1.5">
                <Calendar className="w-3.5 h-3.5 text-indigo-400" />
                Payment Due Date
              </label>
              <input
                type="date"
                required
                value={dueDate}
                onChange={(e) => setDueDate(e.target.value)}
                className="w-full px-3 py-2.5 bg-slate-900 border border-slate-800 rounded-xl text-slate-100 text-sm focus:outline-none focus:border-indigo-500"
              />
            </div>
          </div>

          {/* ⭐ STATE MATCHING BANNER (Visual indicator of Tax Type) ⭐ */}
          <div
            className={`p-4 rounded-2xl border flex items-center justify-between transition-all duration-300 ${
              liveTaxBreakdown.taxType === 'intra'
                ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-300'
                : 'bg-indigo-500/10 border-indigo-500/30 text-indigo-300'
            }`}
          >
            <div className="flex items-center gap-3">
              <div
                className={`w-9 h-9 rounded-xl flex items-center justify-center font-bold text-sm ${
                  liveTaxBreakdown.taxType === 'intra'
                    ? 'bg-emerald-500/20 text-emerald-400'
                    : 'bg-indigo-500/20 text-indigo-400'
                }`}
              >
                <MapPin className="w-5 h-5" />
              </div>
              <div>
                <p className="font-bold text-sm tracking-tight">
                  {liveTaxBreakdown.taxType === 'intra'
                    ? 'Intra-State Supply (Same State)'
                    : 'Inter-State Supply (Different State)'}
                </p>
                <p className="text-xs opacity-80 mt-0.5">
                  Your state ({user?.business?.state}) vs Client state ({selectedClient?.state}). Tax split:{' '}
                  {liveTaxBreakdown.taxType === 'intra' ? 'CGST (half) + SGST (half)' : 'IGST (full tax)'}.
                </p>
              </div>
            </div>
            <span className="hidden sm:inline-block px-3 py-1 text-xs font-extrabold uppercase tracking-wider rounded-lg bg-slate-950/60 border border-current">
              {liveTaxBreakdown.taxType === 'intra' ? 'CGST + SGST' : 'IGST ONLY'}
            </span>
          </div>

          {/* Section 2: Line Items Table */}
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <h3 className="text-sm font-bold text-slate-200 uppercase tracking-wider">Line Items</h3>
              <button
                type="button"
                onClick={addLineItem}
                className="flex items-center gap-1.5 text-xs font-semibold text-indigo-400 hover:text-indigo-300"
              >
                <Plus className="w-4 h-4" /> Add Item
              </button>
            </div>

            <div className="space-y-3">
              {lineItems.map((item, index) => (
                <div
                  key={index}
                  className="grid grid-cols-12 gap-3 p-3 bg-slate-950/40 border border-slate-800 rounded-xl items-center"
                >
                  <div className="col-span-12 lg:col-span-4">
                    <input
                      type="text"
                      required
                      placeholder="Item / Service description"
                      value={item.description}
                      onChange={(e) => handleLineItemChange(index, 'description', e.target.value)}
                      className="w-full px-3 py-2 bg-slate-900 border border-slate-800 rounded-lg text-slate-100 text-xs focus:outline-none focus:border-indigo-500"
                    />
                  </div>

                  <div className="col-span-4 lg:col-span-2">
                    <input
                      type="text"
                      placeholder="HSN/SAC"
                      value={item.hsnCode || ''}
                      onChange={(e) => handleLineItemChange(index, 'hsnCode', e.target.value)}
                      className="w-full px-3 py-2 bg-slate-900 border border-slate-800 rounded-lg text-slate-100 text-xs font-mono focus:outline-none focus:border-indigo-500"
                    />
                  </div>

                  <div className="col-span-4 lg:col-span-2">
                    <input
                      type="number"
                      min="0.01"
                      step="any"
                      required
                      placeholder="Qty"
                      value={item.quantity}
                      onChange={(e) => handleLineItemChange(index, 'quantity', parseFloat(e.target.value) || 0)}
                      className="w-full px-3 py-2 bg-slate-900 border border-slate-800 rounded-lg text-slate-100 text-xs focus:outline-none focus:border-indigo-500"
                    />
                  </div>

                  <div className="col-span-4 lg:col-span-2">
                    <input
                      type="number"
                      min="0.01"
                      step="any"
                      required
                      placeholder="Rate (₹)"
                      value={item.rate}
                      onChange={(e) => handleLineItemChange(index, 'rate', parseFloat(e.target.value) || 0)}
                      className="w-full px-3 py-2 bg-slate-900 border border-slate-800 rounded-lg text-slate-100 text-xs focus:outline-none focus:border-indigo-500"
                    />
                  </div>

                  <div className="col-span-10 lg:col-span-1">
                    <select
                      value={item.taxRate}
                      onChange={(e) => handleLineItemChange(index, 'taxRate', parseFloat(e.target.value))}
                      className="w-full px-2 py-2 bg-slate-900 border border-slate-800 rounded-lg text-slate-100 text-xs font-semibold focus:outline-none focus:border-indigo-500"
                    >
                      {GST_RATES.map((rate) => (
                        <option key={rate} value={rate}>
                          {rate}%
                        </option>
                      ))}
                    </select>
                  </div>

                  <div className="col-span-2 lg:col-span-1 text-right">
                    <button
                      type="button"
                      onClick={() => removeLineItem(index)}
                      disabled={lineItems.length === 1}
                      className="p-1.5 text-slate-500 hover:text-red-400 disabled:opacity-30"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* ⭐ LIVE TAX BREAKDOWN SUMMARY CARD (The "Aha" moment visual display) ⭐ */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 pt-4 border-t border-slate-800">
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-2">Notes & Terms</label>
              <textarea
                rows={4}
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-xl text-slate-100 text-xs focus:outline-none focus:border-indigo-500"
              />
            </div>

            {/* Tax Breakdown Card */}
            <div className="bg-slate-950 border border-slate-800 rounded-2xl p-5 space-y-3 relative overflow-hidden">
              <div className="flex items-center justify-between border-b border-slate-800 pb-3">
                <div className="flex items-center gap-2">
                  <Zap className="w-4 h-4 text-emerald-400 animate-pulse" />
                  <span className="font-bold text-xs uppercase tracking-wider text-slate-200">
                    Live Tax Breakdown Summary
                  </span>
                </div>
                <span className="text-[10px] font-bold px-2 py-0.5 rounded bg-emerald-500/20 text-emerald-300">
                  {liveTaxBreakdown.taxType === 'intra' ? 'CGST + SGST' : 'IGST'}
                </span>
              </div>

              <div className="space-y-2 text-xs">
                <div className="flex justify-between text-slate-400">
                  <span>Subtotal (Pre-tax):</span>
                  <span className="font-medium text-slate-200">₹{liveTaxBreakdown.subtotal.toLocaleString('en-IN', { minimumFractionDigits: 2 })}</span>
                </div>

                {liveTaxBreakdown.cgst > 0 && (
                  <div className="flex justify-between text-slate-400">
                    <span>CGST (Central Tax):</span>
                    <span className="font-medium text-emerald-400">₹{liveTaxBreakdown.cgst.toLocaleString('en-IN', { minimumFractionDigits: 2 })}</span>
                  </div>
                )}

                {liveTaxBreakdown.sgst > 0 && (
                  <div className="flex justify-between text-slate-400">
                    <span>SGST (State Tax):</span>
                    <span className="font-medium text-emerald-400">₹{liveTaxBreakdown.sgst.toLocaleString('en-IN', { minimumFractionDigits: 2 })}</span>
                  </div>
                )}

                {liveTaxBreakdown.igst > 0 && (
                  <div className="flex justify-between text-slate-400">
                    <span>IGST (Integrated Tax):</span>
                    <span className="font-medium text-indigo-400">₹{liveTaxBreakdown.igst.toLocaleString('en-IN', { minimumFractionDigits: 2 })}</span>
                  </div>
                )}

                <div className="flex justify-between text-slate-400 pt-2 border-t border-slate-800/80">
                  <span className="font-semibold text-slate-300">Total GST Calculated:</span>
                  <span className="font-bold text-slate-100">₹{liveTaxBreakdown.totalTax.toLocaleString('en-IN', { minimumFractionDigits: 2 })}</span>
                </div>

                <div className="flex justify-between items-center text-sm pt-2 border-t border-slate-800">
                  <span className="font-extrabold text-slate-100">Grand Total:</span>
                  <span className="text-lg font-black text-indigo-400">
                    ₹{liveTaxBreakdown.grandTotal.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
                  </span>
                </div>
              </div>
            </div>
          </div>

          {/* Submit Action */}
          <div className="flex justify-end gap-4 pt-4 border-t border-slate-800">
            <button
              type="button"
              onClick={() => navigate('/invoices')}
              className="px-5 py-2.5 text-xs font-semibold text-slate-400 hover:text-white"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="flex items-center gap-2 px-6 py-3 bg-gradient-to-r from-indigo-600 to-indigo-500 hover:from-indigo-500 hover:to-indigo-400 text-white font-bold text-sm rounded-xl shadow-lg shadow-indigo-500/25 transition-all disabled:opacity-50"
            >
              <CheckCircle2 className="w-4 h-4" />
              {submitting ? 'Generating Invoice...' : 'Save & Issue Tax Invoice'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
