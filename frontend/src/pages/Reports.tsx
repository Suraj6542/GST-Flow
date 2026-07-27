import React, { useState, useEffect } from 'react';
import api from '../api/axiosInstance';
import type { TaxReport, ApiResponse } from '../types';
import {
  Calendar,
  FileSpreadsheet,
  Info,
} from 'lucide-react';

export const Reports: React.FC = () => {
  const [quarter, setQuarter] = useState<string>('Q1');
  const [year, setYear] = useState<number>(new Date().getFullYear());
  const [report, setReport] = useState<TaxReport | null>(null);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    const fetchTaxReport = async () => {
      setLoading(true);
      try {
        const res = await api.get<ApiResponse<TaxReport>>(`/reports/tax?quarter=${quarter}&year=${year}`);
        if (res.data.success && res.data.data) {
          setReport(res.data.data);
        }
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    fetchTaxReport();
  }, [quarter, year]);

  const exportCsv = () => {
    if (!report || report.items.length === 0) return;

    const headers = ['Invoice #', 'Client Name', 'Date', 'Subtotal (Rs)', 'CGST (Rs)', 'SGST (Rs)', 'IGST (Rs)', 'Total Tax (Rs)', 'Grand Total (Rs)'];
    const rows = report.items.map((i) => [
      i.invoiceNumber,
      `"${i.clientName}"`,
      new Date(i.issueDate).toLocaleDateString(),
      i.subtotal.toFixed(2),
      i.cgst.toFixed(2),
      i.sgst.toFixed(2),
      i.igst.toFixed(2),
      i.totalTax.toFixed(2),
      i.grandTotal.toFixed(2),
    ]);

    const csvContent = 'data:text/csv;charset=utf-8,' + [headers.join(','), ...rows.map((e) => e.join(','))].join('\n');
    const encodedUri = encodeURI(csvContent);
    const link = document.createElement('a');
    link.setAttribute('href', encodedUri);
    link.setAttribute('download', `GST_Tax_Report_${quarter}_${year}.csv`);
    document.body.appendChild(link);
    link.click();
    link.remove();
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-100 tracking-tight">GST Compliance Tax Report</h1>
          <p className="text-xs text-slate-400 mt-1">
            Aggregated CGST, SGST, and IGST totals for filing GSTR-1 and GSTR-3B tax returns
          </p>
        </div>

        <button
          onClick={exportCsv}
          disabled={!report || report.items.length === 0}
          className="flex items-center justify-center gap-2 px-4 py-2.5 bg-emerald-600 hover:bg-emerald-500 disabled:opacity-40 text-white font-medium text-xs rounded-xl shadow-lg shadow-emerald-500/20 transition-all"
        >
          <FileSpreadsheet className="w-4 h-4" /> Export CSV for Accountant
        </button>
      </div>

      {/* Quarter & Year Selector */}
      <div className="bg-slate-900 border border-slate-800 rounded-2xl p-4 flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <Calendar className="w-5 h-5 text-indigo-400" />
          <span className="text-xs font-semibold text-slate-300">Indian Fiscal Quarter:</span>

          <div className="flex items-center gap-1 bg-slate-950 p-1 rounded-xl border border-slate-800">
            {['Q1', 'Q2', 'Q3', 'Q4'].map((q) => (
              <button
                key={q}
                onClick={() => setQuarter(q)}
                className={`px-3 py-1 text-xs font-bold rounded-lg transition-all ${
                  quarter === q
                    ? 'bg-indigo-600 text-white shadow-md'
                    : 'text-slate-400 hover:text-slate-200'
                }`}
              >
                {q}
              </button>
            ))}
          </div>
        </div>

        <div className="flex items-center gap-2">
          <span className="text-xs text-slate-400">Fiscal Year:</span>
          <select
            value={year}
            onChange={(e) => setYear(parseInt(e.target.value))}
            className="px-3 py-1.5 bg-slate-950 border border-slate-800 rounded-xl text-slate-100 text-xs font-semibold focus:outline-none focus:border-indigo-500"
          >
            <option value={2025}>2025</option>
            <option value={2026}>2026</option>
            <option value={2027}>2027</option>
          </select>
        </div>
      </div>

      {/* Report Info Banner */}
      <div className="bg-indigo-500/10 border border-indigo-500/20 p-4 rounded-2xl flex items-center gap-3 text-indigo-300 text-xs">
        <Info className="w-5 h-5 shrink-0 text-indigo-400" />
        <span>
          Showing tax data for <strong>{report?.quarter || quarter}</strong>. Indian fiscal quarters run: Q1 (Apr-Jun), Q2 (Jul-Sep), Q3 (Oct-Dec), Q4 (Jan-Mar).
        </span>
      </div>

      {/* Summary Metrics */}
      {loading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {[1, 2, 3, 4].map((n) => (
            <div key={n} className="h-28 bg-slate-900/60 border border-slate-800 rounded-2xl animate-pulse" />
          ))}
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <div className="bg-slate-900 border border-slate-800/80 rounded-2xl p-5 shadow-lg">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">Total CGST (Central)</span>
            <p className="text-2xl font-extrabold text-emerald-400 mt-2">
              ₹{report?.totalCgst.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
            </p>
            <p className="text-[10px] text-slate-500 mt-1">Intra-State Central Tax</p>
          </div>

          <div className="bg-slate-900 border border-slate-800/80 rounded-2xl p-5 shadow-lg">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">Total SGST (State)</span>
            <p className="text-2xl font-extrabold text-emerald-400 mt-2">
              ₹{report?.totalSgst.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
            </p>
            <p className="text-[10px] text-slate-500 mt-1">Intra-State State Tax</p>
          </div>

          <div className="bg-slate-900 border border-slate-800/80 rounded-2xl p-5 shadow-lg">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">Total IGST (Integrated)</span>
            <p className="text-2xl font-extrabold text-indigo-400 mt-2">
              ₹{report?.totalIgst.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
            </p>
            <p className="text-[10px] text-slate-500 mt-1">Inter-State Integrated Tax</p>
          </div>

          <div className="bg-slate-900 border border-slate-800/80 rounded-2xl p-5 shadow-lg bg-gradient-to-br from-slate-900 to-indigo-950/40">
            <span className="text-xs font-semibold text-indigo-300 uppercase tracking-wider">Total Tax Liability</span>
            <p className="text-2xl font-black text-slate-100 mt-2">
              ₹{report?.totalTax.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
            </p>
            <p className="text-[10px] text-indigo-400 mt-1 font-semibold">
              {report?.invoiceCount || 0} Invoice(s) Included
            </p>
          </div>
        </div>
      )}

      {/* Itemized Table */}
      <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6 shadow-xl space-y-4">
        <h3 className="text-base font-bold text-slate-100">Itemized GST Return Line Items</h3>

        {report?.items.length === 0 ? (
          <p className="text-xs text-slate-500 py-6 text-center">
            No invoices issued in {quarter} {year}.
          </p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs border-collapse">
              <thead>
                <tr className="border-b border-slate-800 bg-slate-950 text-[10px] font-bold text-slate-400 uppercase">
                  <th className="p-3">Invoice</th>
                  <th className="p-3">Client</th>
                  <th className="p-3">Date</th>
                  <th className="p-3 text-right">Subtotal</th>
                  <th className="p-3 text-right">CGST</th>
                  <th className="p-3 text-right">SGST</th>
                  <th className="p-3 text-right">IGST</th>
                  <th className="p-3 text-right">Total Tax</th>
                  <th className="p-3 text-right">Grand Total</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/60">
                {report?.items.map((item, idx) => (
                  <tr key={idx} className="hover:bg-slate-800/40">
                    <td className="p-3 font-mono font-bold text-indigo-300">{item.invoiceNumber}</td>
                    <td className="p-3 font-medium text-slate-200">{item.clientName}</td>
                    <td className="p-3 text-slate-400">{new Date(item.issueDate).toLocaleDateString('en-IN')}</td>
                    <td className="p-3 text-right text-slate-300">₹{item.subtotal.toFixed(2)}</td>
                    <td className="p-3 text-right text-emerald-400">₹{item.cgst.toFixed(2)}</td>
                    <td className="p-3 text-right text-emerald-400">₹{item.sgst.toFixed(2)}</td>
                    <td className="p-3 text-right text-indigo-400">₹{item.igst.toFixed(2)}</td>
                    <td className="p-3 text-right font-bold text-slate-100">₹{item.totalTax.toFixed(2)}</td>
                    <td className="p-3 text-right font-black text-indigo-300">₹{item.grandTotal.toFixed(2)}</td>
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
