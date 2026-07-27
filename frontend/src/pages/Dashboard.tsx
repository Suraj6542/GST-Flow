import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import api from '../api/axiosInstance';
import type { DashboardSummary, ApiResponse } from '../types';
import {
  DollarSign,
  Clock,
  AlertTriangle,
  Users,
  TrendingUp,
  Plus,
  ArrowUpRight,
  Building,
} from 'lucide-react';
import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';

export const Dashboard: React.FC = () => {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchSummary = async () => {
      try {
        const res = await api.get<ApiResponse<DashboardSummary>>('/reports/summary');
        if (res.data.success && res.data.data) {
          setSummary(res.data.data);
        }
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    fetchSummary();
  }, []);

  if (loading) {
    return (
      <div className="space-y-6 animate-pulse">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {[1, 2, 3, 4].map((n) => (
            <div key={n} className="h-28 bg-slate-900/60 border border-slate-800 rounded-2xl" />
          ))}
        </div>
        <div className="h-80 bg-slate-900/60 border border-slate-800 rounded-2xl" />
      </div>
    );
  }

  return (
    <div className="space-y-8">
      {/* Top Banner / Actions */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-100 tracking-tight">Dashboard Overview</h1>
          <p className="text-xs text-slate-400 mt-1">
            Real-time compliance, revenue metrics & outstanding payments
          </p>
        </div>
        <div className="flex items-center gap-3">
          <Link
            to="/clients"
            className="px-4 py-2.5 bg-slate-900 hover:bg-slate-800 border border-slate-800 text-slate-200 font-medium text-xs rounded-xl transition-all flex items-center gap-2"
          >
            <Users className="w-4 h-4 text-indigo-400" /> Add Client
          </Link>
          <Link
            to="/invoices/new"
            className="px-4 py-2.5 bg-indigo-600 hover:bg-indigo-500 text-white font-medium text-xs rounded-xl shadow-lg shadow-indigo-500/20 transition-all flex items-center gap-2"
          >
            <Plus className="w-4 h-4" /> Create Invoice
          </Link>
        </div>
      </div>

      {/* Metric Cards Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {/* Total Revenue Card */}
        <div className="bg-slate-900 border border-slate-800/80 rounded-2xl p-5 shadow-lg relative overflow-hidden">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Total Revenue
            </span>
            <div className="w-9 h-9 rounded-xl bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center text-emerald-400">
              <DollarSign className="w-5 h-5" />
            </div>
          </div>
          <p className="text-2xl font-black text-slate-100 mt-3">
            ₹{summary?.totalRevenue.toLocaleString('en-IN', { minimumFractionDigits: 2 }) || '0.00'}
          </p>
          <div className="flex items-center gap-1.5 text-xs text-emerald-400 font-semibold mt-2">
            <TrendingUp className="w-3.5 h-3.5" />
            <span>+{summary?.revenueGrowthPercent || 0}% this month</span>
          </div>
        </div>

        {/* Outstanding Card */}
        <div className="bg-slate-900 border border-slate-800/80 rounded-2xl p-5 shadow-lg relative overflow-hidden">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Total Outstanding
            </span>
            <div className="w-9 h-9 rounded-xl bg-indigo-500/10 border border-indigo-500/20 flex items-center justify-center text-indigo-400">
              <Clock className="w-5 h-5" />
            </div>
          </div>
          <p className="text-2xl font-black text-indigo-300 mt-3">
            ₹{summary?.totalOutstanding.toLocaleString('en-IN', { minimumFractionDigits: 2 }) || '0.00'}
          </p>
          <p className="text-xs text-slate-400 mt-2">
            {summary?.invoiceCount || 0} total invoices issued
          </p>
        </div>

        {/* Overdue Card */}
        <div className="bg-slate-900 border border-slate-800/80 rounded-2xl p-5 shadow-lg relative overflow-hidden">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Overdue Amount
            </span>
            <div className="w-9 h-9 rounded-xl bg-red-500/10 border border-red-500/20 flex items-center justify-center text-red-400">
              <AlertTriangle className="w-5 h-5" />
            </div>
          </div>
          <p className="text-2xl font-black text-red-400 mt-3">
            ₹{summary?.totalOverdue.toLocaleString('en-IN', { minimumFractionDigits: 2 }) || '0.00'}
          </p>
          <p className="text-xs text-red-400 font-medium mt-2">
            {summary?.overdueCount || 0} overdue invoice(s) needing attention
          </p>
        </div>

        {/* Active Clients Card */}
        <div className="bg-slate-900 border border-slate-800/80 rounded-2xl p-5 shadow-lg relative overflow-hidden">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Active Clients
            </span>
            <div className="w-9 h-9 rounded-xl bg-amber-500/10 border border-amber-500/20 flex items-center justify-center text-amber-400">
              <Building className="w-5 h-5" />
            </div>
          </div>
          <p className="text-2xl font-black text-slate-100 mt-3">{summary?.clientCount || 0}</p>
          <p className="text-xs text-slate-400 mt-2">Clients in directory</p>
        </div>
      </div>

      {/* Revenue Trend Chart (Recharts) */}
      <div className="bg-slate-900 border border-slate-800/80 rounded-2xl p-6 shadow-xl">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h3 className="text-lg font-bold text-slate-100">Revenue & GST Collected Trend</h3>
            <p className="text-xs text-slate-400">Monthly revenue for the last 12 months</p>
          </div>
          <Link
            to="/reports"
            className="text-xs font-semibold text-indigo-400 hover:underline flex items-center gap-1"
          >
            View Tax Report <ArrowUpRight className="w-3.5 h-3.5" />
          </Link>
        </div>

        <div className="h-72 w-full">
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={summary?.revenueTrend || []}>
              <defs>
                <linearGradient id="revenueGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#6366f1" stopOpacity={0.4} />
                  <stop offset="95%" stopColor="#6366f1" stopOpacity={0} />
                </linearGradient>
                <linearGradient id="taxGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#10b981" stopOpacity={0.4} />
                  <stop offset="95%" stopColor="#10b981" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" />
              <XAxis dataKey="month" stroke="#64748b" fontSize={11} />
              <YAxis stroke="#64748b" fontSize={11} />
              <Tooltip
                contentStyle={{
                  backgroundColor: '#0f172a',
                  borderColor: '#334155',
                  borderRadius: '12px',
                  color: '#f8fafc',
                  fontSize: '12px',
                }}
              />
              <Area
                type="monotone"
                dataKey="revenue"
                name="Revenue (₹)"
                stroke="#6366f1"
                strokeWidth={2}
                fillOpacity={1}
                fill="url(#revenueGrad)"
              />
              <Area
                type="monotone"
                dataKey="taxCollected"
                name="Tax Collected (₹)"
                stroke="#10b981"
                strokeWidth={2}
                fillOpacity={1}
                fill="url(#taxGrad)"
              />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Recent Invoices Table */}
      <div className="bg-slate-900 border border-slate-800/80 rounded-2xl p-6 shadow-xl space-y-4">
        <div className="flex items-center justify-between">
          <h3 className="text-base font-bold text-slate-100">Recent Invoices</h3>
          <Link to="/invoices" className="text-xs text-indigo-400 hover:underline font-medium">
            View All
          </Link>
        </div>

        {summary?.recentInvoices.length === 0 ? (
          <p className="text-xs text-slate-500 py-4 text-center">No recent invoices created.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="border-b border-slate-800 text-slate-400 uppercase text-[10px]">
                <tr>
                  <th className="py-2.5">Invoice</th>
                  <th className="py-2.5">Client</th>
                  <th className="py-2.5 text-right">Grand Total</th>
                  <th className="py-2.5 text-center">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/60">
                {summary?.recentInvoices.map((inv) => (
                  <tr key={inv.id} className="hover:bg-slate-800/40">
                    <td className="py-3 font-mono font-bold text-indigo-300">{inv.invoiceNumber}</td>
                    <td className="py-3 font-medium text-slate-200">{inv.clientName}</td>
                    <td className="py-3 text-right font-bold text-slate-100">
                      ₹{inv.grandTotal.toFixed(2)}
                    </td>
                    <td className="py-3 text-center">
                      <span className="capitalize px-2 py-0.5 rounded text-[10px] font-semibold bg-slate-800 text-slate-300">
                        {inv.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};
