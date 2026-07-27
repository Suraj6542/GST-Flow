import React, { useState, useEffect } from 'react';
import api from '../api/axiosInstance';
import type { RecurringTemplate, Client, ApiResponse, LineItemRequest } from '../types';
import { EmptyState } from '../components/common/EmptyState';
import { useToast } from '../components/common/ToastProvider';
import {
  Clock,
  Plus,
  Trash2,
  Calendar,
  Zap,
  Mail,
  X,
} from 'lucide-react';

export const RecurringTemplates: React.FC = () => {
  const { showToast, confirm } = useToast();
  const [templates, setTemplates] = useState<RecurringTemplate[]>([]);
  const [clients, setClients] = useState<Client[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);

  const [selectedClientId, setSelectedClientId] = useState('');
  const [frequency, setFrequency] = useState('monthly');
  const [startDate, setStartDate] = useState(() => new Date().toISOString().split('T')[0]);
  const [autoSendEmail, setAutoSendEmail] = useState(true);
  const [notes] = useState('Monthly retainer invoice automatically generated.');
  const [lineItems, setLineItems] = useState<LineItemRequest[]>([
    { description: 'Monthly Retainer Services', hsnCode: '998314', quantity: 1, rate: 30000, taxRate: 18 },
  ]);

  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');

  const fetchData = async () => {
    try {
      const [tRes, cRes] = await Promise.all([
        api.get<ApiResponse<RecurringTemplate[]>>('/recurring-templates'),
        api.get<ApiResponse<Client[]>>('/clients'),
      ]);
      if (tRes.data.success && tRes.data.data) setTemplates(tRes.data.data);
      if (cRes.data.success && cRes.data.data) {
        setClients(cRes.data.data);
        if (cRes.data.data.length > 0) setSelectedClientId(cRes.data.data[0].id);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

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
      await api.post('/recurring-templates', {
        clientId: selectedClientId,
        frequency,
        startDate,
        lineItems,
        notes,
        autoSendEmail,
      });
      setModalOpen(false);
      fetchData();
    } catch (err: any) {
      setError(err.response?.data?.error || 'Failed to create recurring schedule');
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (id: string) => {
    const isConfirmed = await confirm({
      title: 'Delete Recurring Schedule',
      message: 'Are you sure you want to delete this schedule? Invoices will no longer be generated automatically.',
      confirmLabel: 'Delete Schedule',
      variant: 'danger',
    });
    if (!isConfirmed) return;
    try {
      await api.delete(`/recurring-templates/${id}`);
      showToast('success', 'Schedule Deleted', 'The recurring billing schedule was deleted.');
      fetchData();
    } catch (err: any) {
      showToast('error', 'Delete Failed', err.response?.data?.error || 'Failed to delete template');
    }
  };
  const [triggeringWorker, setTriggeringWorker] = useState(false);
  const [triggerModalOpen, setTriggerModalOpen] = useState(false);
  const [triggerStatus, setTriggerStatus] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const handleOpenTriggerModal = () => {
    setTriggerStatus(null);
    setTriggerModalOpen(true);
  };

  const handleConfirmTriggerWorker = async () => {
    setTriggeringWorker(true);
    setTriggerStatus(null);
    try {
      await api.post('/recurring-templates/trigger');
      setTriggerStatus({
        type: 'success',
        message: 'Hangfire worker executed successfully! Due recurring invoices were generated and emailed.',
      });
      fetchData();
    } catch (err: any) {
      setTriggerStatus({
        type: 'error',
        message: err.response?.data?.error || 'Failed to trigger Hangfire worker. Make sure backend is running.',
      });
    } finally {
      setTriggeringWorker(false);
    }
  };

  const hangfireUrl = import.meta.env.VITE_API_URL
    ? `${import.meta.env.VITE_API_URL.replace('/api', '')}/hangfire`
    : 'http://localhost:5050/hangfire';

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-100 tracking-tight flex items-center gap-2">
            <Clock className="w-6 h-6 text-indigo-400" />
            Automated Recurring Billing (USP)
          </h1>
          <p className="text-xs text-slate-400 mt-1">
            Hangfire background worker automatically creates GST compliant invoices and emails clients on schedule
          </p>
        </div>
        <button
          onClick={() => setModalOpen(true)}
          className="flex items-center justify-center gap-2 px-4 py-2.5 bg-indigo-600 hover:bg-indigo-500 text-white font-medium text-sm rounded-xl shadow-lg shadow-indigo-500/20 transition-all"
        >
          <Plus className="w-4 h-4" />
          New Recurring Schedule
        </button>
      </div>

      {/* Hangfire Monitor Badge */}
      <div className="bg-slate-900 border border-slate-800/80 rounded-2xl p-4 flex items-center justify-between flex-wrap gap-4">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center text-emerald-400">
            <Zap className="w-5 h-5 animate-pulse" />
          </div>
          <div>
            <p className="text-sm font-bold text-slate-200">Hangfire Engine Active</p>
            <p className="text-xs text-slate-400">Daily background cron worker automatically checks for due invoices.</p>
          </div>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={handleOpenTriggerModal}
            disabled={triggeringWorker}
            className="px-3.5 py-2 bg-indigo-600/20 hover:bg-indigo-600/30 text-xs font-bold text-indigo-300 rounded-xl border border-indigo-500/30 transition-all disabled:opacity-50 cursor-pointer flex items-center gap-1.5 shadow-sm"
          >
            <Zap className="w-3.5 h-3.5 text-amber-400" />
            <span>Run Worker Now</span>
          </button>
          <a
            href={hangfireUrl}
            target="_blank"
            rel="noreferrer"
            className="text-xs font-semibold text-indigo-400 hover:text-indigo-300 hover:underline flex items-center gap-1"
          >
            Open Hangfire Dashboard →
          </a>
        </div>
      </div>

      {/* Content */}
      {loading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {[1, 2].map((n) => (
            <div key={n} className="h-44 bg-slate-900/60 border border-slate-800 rounded-2xl animate-pulse" />
          ))}
        </div>
      ) : templates.length === 0 ? (
        <EmptyState
          icon={Clock}
          title="No recurring schedules"
          description="Create automated recurring invoices so you never forget to bill retainer clients monthly!"
          actionLabel="Schedule First Recurring Invoice"
          onAction={() => setModalOpen(true)}
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {templates.map((tpl) => (
            <div
              key={tpl.id}
              className="bg-slate-900 border border-slate-800/80 rounded-2xl p-5 shadow-lg flex flex-col justify-between"
            >
              <div>
                <div className="flex items-start justify-between gap-2 mb-3">
                  <div>
                    <span className="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider bg-indigo-500/20 text-indigo-300 border border-indigo-500/30">
                      {tpl.frequency}
                    </span>
                    <h3 className="font-bold text-slate-100 text-base mt-2">{tpl.clientName}</h3>
                  </div>

                  <button
                    onClick={() => handleDelete(tpl.id)}
                    className="p-1.5 text-slate-400 hover:text-red-400 hover:bg-slate-800 rounded-lg transition-colors"
                    title="Delete Recurring Schedule"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>

                <div className="space-y-1.5 text-xs text-slate-400 mt-3">
                  <div className="flex items-center gap-2">
                    <Calendar className="w-3.5 h-3.5 text-slate-500 shrink-0" />
                    <span>Next Run Date: <strong className="text-emerald-400">{new Date(tpl.nextRunDate).toLocaleDateString('en-IN')}</strong></span>
                  </div>

                  <div className="flex items-center gap-2">
                    <Mail className="w-3.5 h-3.5 text-slate-500 shrink-0" />
                    <span>Auto-Email PDF: {tpl.autoSendEmail ? <strong className="text-emerald-400">Yes</strong> : 'No'}</span>
                  </div>
                </div>

                <div className="mt-4 pt-3 border-t border-slate-800/80 text-xs">
                  <span className="font-semibold text-slate-300">Template Items:</span>
                  <ul className="mt-1 space-y-1 text-slate-400 text-[11px]">
                    {tpl.lineItems.map((li, idx) => (
                      <li key={idx} className="flex justify-between">
                        <span>• {li.description}</span>
                        <span className="font-mono text-slate-200">₹{li.amount.toFixed(2)}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Modal */}
      {modalOpen && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-xl w-full p-6 shadow-2xl relative max-h-[90vh] overflow-y-auto">
            <button
              onClick={() => setModalOpen(false)}
              className="absolute top-5 right-5 text-slate-400 hover:text-white"
            >
              <X className="w-5 h-5" />
            </button>

            <h3 className="text-xl font-bold text-slate-100 mb-1">Create Recurring Schedule</h3>
            <p className="text-xs text-slate-400 mb-5">
              Automate invoice creation and PDF emailing via Hangfire worker.
            </p>

            {error && (
              <div className="mb-4 p-3 bg-red-500/10 border border-red-500/30 rounded-xl text-red-400 text-xs font-medium">
                {error}
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-medium text-slate-300 mb-1">Select Client</label>
                  <select
                    value={selectedClientId}
                    onChange={(e) => setSelectedClientId(e.target.value)}
                    className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-xl text-slate-100 text-xs focus:outline-none focus:border-indigo-500"
                  >
                    {clients.map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.name}
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-medium text-slate-300 mb-1">Billing Frequency</label>
                  <select
                    value={frequency}
                    onChange={(e) => setFrequency(e.target.value)}
                    className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-xl text-slate-100 text-xs focus:outline-none focus:border-indigo-500"
                  >
                    <option value="weekly">Weekly</option>
                    <option value="monthly">Monthly</option>
                    <option value="quarterly">Quarterly</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-xs font-medium text-slate-300 mb-1">First Run Date</label>
                <input
                  type="date"
                  required
                  value={startDate}
                  onChange={(e) => setStartDate(e.target.value)}
                  className="w-full px-3 py-2 bg-slate-950 border border-slate-800 rounded-xl text-slate-100 text-xs focus:outline-none focus:border-indigo-500"
                />
              </div>

              <div className="flex items-center gap-2 py-1">
                <input
                  type="checkbox"
                  id="autoSendEmail"
                  checked={autoSendEmail}
                  onChange={(e) => setAutoSendEmail(e.target.checked)}
                  className="rounded bg-slate-950 border-slate-800 text-indigo-600 focus:ring-0"
                />
                <label htmlFor="autoSendEmail" className="text-xs text-slate-300 cursor-pointer">
                  Auto-email PDF invoice to client when job triggers
                </label>
              </div>

              {/* Line Items */}
              <div className="space-y-2 pt-2 border-t border-slate-800">
                <div className="flex items-center justify-between">
                  <label className="text-xs font-bold uppercase tracking-wider text-slate-300">
                    Template Line Items
                  </label>
                  <button
                    type="button"
                    onClick={addLineItem}
                    className="text-xs text-indigo-400 font-semibold hover:underline"
                  >
                    + Add Item
                  </button>
                </div>

                {lineItems.map((item, index) => (
                  <div key={index} className="grid grid-cols-12 gap-2 items-center bg-slate-950 p-2 rounded-xl border border-slate-800">
                    <input
                      type="text"
                      required
                      placeholder="Description"
                      value={item.description}
                      onChange={(e) => handleLineItemChange(index, 'description', e.target.value)}
                      className="col-span-5 px-2 py-1.5 bg-slate-900 border border-slate-800 rounded text-xs text-slate-100"
                    />
                    <input
                      type="number"
                      min="0.01"
                      required
                      placeholder="Qty"
                      value={item.quantity}
                      onChange={(e) => handleLineItemChange(index, 'quantity', parseFloat(e.target.value) || 0)}
                      className="col-span-2 px-2 py-1.5 bg-slate-900 border border-slate-800 rounded text-xs text-slate-100"
                    />
                    <input
                      type="number"
                      min="0.01"
                      required
                      placeholder="Rate"
                      value={item.rate}
                      onChange={(e) => handleLineItemChange(index, 'rate', parseFloat(e.target.value) || 0)}
                      className="col-span-3 px-2 py-1.5 bg-slate-900 border border-slate-800 rounded text-xs text-slate-100"
                    />
                    <button
                      type="button"
                      onClick={() => removeLineItem(index)}
                      className="col-span-2 text-slate-500 hover:text-red-400 text-xs text-right p-1"
                    >
                      Remove
                    </button>
                  </div>
                ))}
              </div>

              <div className="flex items-center justify-end gap-3 pt-4">
                <button
                  type="button"
                  onClick={() => setModalOpen(false)}
                  className="px-4 py-2 text-xs text-slate-400 hover:text-white"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="px-5 py-2.5 bg-indigo-600 hover:bg-indigo-500 text-white font-medium text-xs rounded-xl shadow-lg shadow-indigo-500/20 transition-all disabled:opacity-50"
                >
                  {submitting ? 'Creating...' : 'Save Recurring Schedule'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
      {/* Custom Run Worker Confirmation Modal */}
      {triggerModalOpen && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-md w-full p-6 shadow-2xl relative">
            <button
              onClick={() => setTriggerModalOpen(false)}
              className="absolute top-4 right-4 text-slate-400 hover:text-white"
            >
              <X className="w-5 h-5" />
            </button>

            <div className="flex items-center gap-3 mb-3">
              <div className="w-10 h-10 rounded-xl bg-amber-500/10 border border-amber-500/20 flex items-center justify-center text-amber-400">
                <Zap className="w-5 h-5" />
              </div>
              <h3 className="text-lg font-bold text-slate-100">Trigger Hangfire Worker</h3>
            </div>

            <p className="text-xs text-slate-300 leading-relaxed mb-4">
              Are you sure you want to run the Hangfire recurring invoice background worker now?
              This will evaluate all due recurring schedules, auto-generate tax invoices, and dispatch PDF email attachments to clients.
            </p>

            {triggerStatus && (
              <div
                className={`mb-4 p-3 rounded-xl text-xs font-medium border ${
                  triggerStatus.type === 'success'
                    ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-400'
                    : 'bg-red-500/10 border-red-500/30 text-red-400'
                }`}
              >
                {triggerStatus.message}
              </div>
            )}

            <div className="flex items-center justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setTriggerModalOpen(false)}
                className="px-4 py-2 text-xs font-medium text-slate-400 hover:text-white transition-colors cursor-pointer"
              >
                {triggerStatus?.type === 'success' ? 'Close' : 'Cancel'}
              </button>

              {triggerStatus?.type !== 'success' && (
                <button
                  type="button"
                  onClick={handleConfirmTriggerWorker}
                  disabled={triggeringWorker}
                  className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 text-white font-medium text-xs rounded-xl shadow-lg shadow-indigo-500/20 transition-all disabled:opacity-50 flex items-center gap-2 cursor-pointer"
                >
                  {triggeringWorker ? (
                    <>
                      <Zap className="w-3.5 h-3.5 animate-spin" />
                      <span>Processing Jobs...</span>
                    </>
                  ) : (
                    <span>Yes, Run Worker Now</span>
                  )}
                </button>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
