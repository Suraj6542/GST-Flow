import React, { useState, useEffect } from 'react';
import api from '../api/axiosInstance';
import type { ApiResponse } from '../types';
import {
  Settings as SettingsIcon,
  Mail,
  Server,
  Lock,
  Hash,
  Send,
  Save,
  Trash2,
  CheckCircle2,
  AlertTriangle,
  Loader2,
  Info,
  Eye,
  EyeOff,
  Shield,
  Zap,
  ExternalLink,
} from 'lucide-react';

interface SmtpConfigResponse {
  smtpHost: string | null;
  smtpPort: number;
  smtpUser: string | null;
  fromEmail: string | null;
  isConfigured: boolean;
}

const SMTP_PRESETS = [
  { label: 'Gmail', host: 'smtp.gmail.com', port: 587 },
  { label: 'Outlook / Hotmail', host: 'smtp.office365.com', port: 587 },
  { label: 'Yahoo Mail', host: 'smtp.mail.yahoo.com', port: 587 },
  { label: 'Custom', host: '', port: 587 },
];

export const Settings: React.FC = () => {
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [testing, setTesting] = useState(false);
  const [removing, setRemoving] = useState(false);
  const [showPassword, setShowPassword] = useState(false);

  const [isConfigured, setIsConfigured] = useState(false);
  const [successMsg, setSuccessMsg] = useState('');
  const [errorMsg, setErrorMsg] = useState('');

  const [formData, setFormData] = useState({
    smtpHost: '',
    smtpPort: 587,
    smtpUser: '',
    smtpPass: '',
    fromEmail: '',
  });

  const [testEmail, setTestEmail] = useState('');
  const [testSuccess, setTestSuccess] = useState('');
  const [testError, setTestError] = useState('');

  // Confirmation modal for remove
  const [showRemoveModal, setShowRemoveModal] = useState(false);

  const clearMessages = () => {
    setSuccessMsg('');
    setErrorMsg('');
    setTestSuccess('');
    setTestError('');
  };

  const fetchConfig = async () => {
    try {
      const res = await api.get<ApiResponse<SmtpConfigResponse>>('/settings/smtp');
      if (res.data.success && res.data.data) {
        const cfg = res.data.data;
        setIsConfigured(cfg.isConfigured);
        setFormData({
          smtpHost: cfg.smtpHost || '',
          smtpPort: cfg.smtpPort || 587,
          smtpUser: cfg.smtpUser || '',
          smtpPass: '', // never returned from API
          fromEmail: cfg.fromEmail || '',
        });
      }
    } catch {
      // Silently handle — new user with no config
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchConfig();
  }, []);

  const handlePresetSelect = (preset: typeof SMTP_PRESETS[number]) => {
    setFormData((prev) => ({
      ...prev,
      smtpHost: preset.host,
      smtpPort: preset.port,
    }));
    clearMessages();
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    clearMessages();
    setSaving(true);

    try {
      const payload = {
        smtpHost: formData.smtpHost.trim(),
        smtpPort: formData.smtpPort,
        smtpUser: formData.smtpUser.trim(),
        smtpPass: formData.smtpPass,
        fromEmail: formData.fromEmail.trim() || null,
      };
      const res = await api.put<ApiResponse<SmtpConfigResponse>>('/settings/smtp', payload);
      if (res.data.success) {
        setIsConfigured(true);
        setSuccessMsg('SMTP settings saved successfully!');
      }
    } catch (err: any) {
      setErrorMsg(err.response?.data?.error || 'Failed to save SMTP settings');
    } finally {
      setSaving(false);
    }
  };

  const handleTest = async () => {
    if (!testEmail.trim()) {
      setTestError('Please enter a recipient email address');
      return;
    }
    setTestSuccess('');
    setTestError('');
    setTesting(true);

    try {
      const res = await api.post<ApiResponse>('/settings/smtp/test', {
        toEmail: testEmail.trim(),
      });
      if (res.data.success) {
        setTestSuccess(`Test email sent to ${testEmail}! Check inbox (and spam folder).`);
      }
    } catch (err: any) {
      setTestError(err.response?.data?.error || 'Failed to send test email');
    } finally {
      setTesting(false);
    }
  };

  const handleRemove = async () => {
    setShowRemoveModal(false);
    clearMessages();
    setRemoving(true);

    try {
      await api.delete('/settings/smtp');
      setIsConfigured(false);
      setFormData({ smtpHost: '', smtpPort: 587, smtpUser: '', smtpPass: '', fromEmail: '' });
      setSuccessMsg('SMTP configuration removed. Email sending will use the system default (if configured).');
    } catch (err: any) {
      setErrorMsg(err.response?.data?.error || 'Failed to remove SMTP settings');
    } finally {
      setRemoving(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="w-6 h-6 text-indigo-400 animate-spin" />
      </div>
    );
  }

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      {/* Page Header */}
      <div>
        <h1 className="text-2xl font-bold text-slate-100 tracking-tight flex items-center gap-2">
          <SettingsIcon className="w-6 h-6 text-indigo-400" />
          Email Settings
        </h1>
        <p className="text-xs text-slate-400 mt-1">
          Configure your personal SMTP credentials so invoices and reminders are sent from your own email address.
        </p>
      </div>

      {/* Status Banner */}
      <div
        className={`flex items-center gap-3 px-4 py-3 rounded-xl border text-sm font-medium ${
          isConfigured
            ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-400'
            : 'bg-amber-500/10 border-amber-500/30 text-amber-400'
        }`}
      >
        {isConfigured ? (
          <>
            <CheckCircle2 className="w-4 h-4 shrink-0" />
            <span>
              SMTP is configured. Invoices will be sent from <strong>{formData.smtpUser || formData.fromEmail}</strong>.
            </span>
          </>
        ) : (
          <>
            <AlertTriangle className="w-4 h-4 shrink-0" />
            <span>
              No SMTP configured yet. Emails will only be logged to the server console.
            </span>
          </>
        )}
      </div>

      {/* Provider Presets */}
      <div className="bg-slate-900 border border-slate-800/80 rounded-2xl p-5">
        <h2 className="text-sm font-semibold text-slate-200 mb-3 flex items-center gap-2">
          <Zap className="w-4 h-4 text-indigo-400" />
          Quick Setup — Choose Your Provider
        </h2>
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
          {SMTP_PRESETS.map((preset) => (
            <button
              key={preset.label}
              type="button"
              onClick={() => handlePresetSelect(preset)}
              className={`px-3 py-2.5 text-xs font-medium rounded-xl border transition-all ${
                formData.smtpHost === preset.host && preset.host
                  ? 'bg-indigo-600/20 border-indigo-500/40 text-indigo-300'
                  : 'bg-slate-950 border-slate-800 text-slate-400 hover:border-slate-700 hover:text-slate-200'
              }`}
            >
              {preset.label}
            </button>
          ))}
        </div>

        {formData.smtpHost === 'smtp.gmail.com' && (
          <div className="mt-3 flex items-start gap-2 text-xs text-slate-400 bg-slate-950/60 px-3 py-2.5 rounded-xl border border-slate-800/60">
            <Info className="w-4 h-4 text-indigo-400 shrink-0 mt-0.5" />
            <span>
              Gmail requires an <strong className="text-indigo-300">App Password</strong>, not your regular password.
              Go to{' '}
              <a
                href="https://myaccount.google.com/apppasswords"
                target="_blank"
                rel="noreferrer"
                className="text-indigo-400 hover:text-indigo-300 underline underline-offset-2 inline-flex items-center gap-0.5"
              >
                Google App Passwords
                <ExternalLink className="w-3 h-3" />
              </a>{' '}
              (requires 2-Step Verification enabled).
            </span>
          </div>
        )}

        {formData.smtpHost === 'smtp.office365.com' && (
          <div className="mt-3 flex items-start gap-2 text-xs text-slate-400 bg-slate-950/60 px-3 py-2.5 rounded-xl border border-slate-800/60">
            <Info className="w-4 h-4 text-indigo-400 shrink-0 mt-0.5" />
            <span>
              Outlook / Microsoft 365 uses your regular email password. If you have MFA enabled, you may need an App Password from your Microsoft account security settings.
            </span>
          </div>
        )}
      </div>

      {/* SMTP Configuration Form */}
      <form onSubmit={handleSave} className="bg-slate-900 border border-slate-800/80 rounded-2xl p-5 space-y-5">
        <h2 className="text-sm font-semibold text-slate-200 flex items-center gap-2">
          <Shield className="w-4 h-4 text-indigo-400" />
          SMTP Configuration
        </h2>

        {/* Success/Error */}
        {successMsg && (
          <div className="flex items-center gap-2 px-3 py-2.5 bg-emerald-500/10 border border-emerald-500/30 rounded-xl text-emerald-400 text-xs font-medium">
            <CheckCircle2 className="w-4 h-4 shrink-0" />
            {successMsg}
          </div>
        )}
        {errorMsg && (
          <div className="flex items-center gap-2 px-3 py-2.5 bg-red-500/10 border border-red-500/30 rounded-xl text-red-400 text-xs font-medium">
            <AlertTriangle className="w-4 h-4 shrink-0" />
            {errorMsg}
          </div>
        )}

        {/* Host & Port */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div className="sm:col-span-2">
            <label className="block text-xs font-medium text-slate-300 mb-1">SMTP Host</label>
            <div className="relative">
              <Server className="w-4 h-4 text-slate-500 absolute left-3 top-3" />
              <input
                type="text"
                required
                value={formData.smtpHost}
                onChange={(e) => { clearMessages(); setFormData({ ...formData, smtpHost: e.target.value }); }}
                placeholder="smtp.gmail.com"
                className="w-full pl-10 pr-3 py-2.5 bg-slate-950 border border-slate-800 rounded-xl text-slate-100 text-sm focus:outline-none focus:border-indigo-500 transition-colors"
              />
            </div>
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-300 mb-1">Port</label>
            <div className="relative">
              <Hash className="w-4 h-4 text-slate-500 absolute left-3 top-3" />
              <input
                type="number"
                required
                min={1}
                max={65535}
                value={formData.smtpPort}
                onChange={(e) => { clearMessages(); setFormData({ ...formData, smtpPort: parseInt(e.target.value) || 587 }); }}
                className="w-full pl-10 pr-3 py-2.5 bg-slate-950 border border-slate-800 rounded-xl text-slate-100 text-sm focus:outline-none focus:border-indigo-500 transition-colors"
              />
            </div>
          </div>
        </div>

        {/* Username & Password */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-medium text-slate-300 mb-1">SMTP Username (Email)</label>
            <div className="relative">
              <Mail className="w-4 h-4 text-slate-500 absolute left-3 top-3" />
              <input
                type="email"
                required
                value={formData.smtpUser}
                onChange={(e) => { clearMessages(); setFormData({ ...formData, smtpUser: e.target.value }); }}
                placeholder="you@gmail.com"
                className="w-full pl-10 pr-3 py-2.5 bg-slate-950 border border-slate-800 rounded-xl text-slate-100 text-sm focus:outline-none focus:border-indigo-500 transition-colors"
              />
            </div>
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-300 mb-1">
              {formData.smtpHost === 'smtp.gmail.com' ? 'App Password' : 'Password'}
            </label>
            <div className="relative">
              <Lock className="w-4 h-4 text-slate-500 absolute left-3 top-3" />
              <input
                type={showPassword ? 'text' : 'password'}
                required={!isConfigured}
                value={formData.smtpPass}
                onChange={(e) => { clearMessages(); setFormData({ ...formData, smtpPass: e.target.value }); }}
                placeholder={isConfigured ? '••••••••••••••••' : 'Enter password / app password'}
                className="w-full pl-10 pr-10 py-2.5 bg-slate-950 border border-slate-800 rounded-xl text-slate-100 text-sm focus:outline-none focus:border-indigo-500 transition-colors"
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-3 top-2.5 text-slate-500 hover:text-slate-300 transition-colors"
              >
                {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
              </button>
            </div>
            {isConfigured && (
              <p className="text-[10px] text-slate-500 mt-1 italic">
                Leave blank to keep existing password unchanged.
              </p>
            )}
          </div>
        </div>

        {/* From Email (optional) */}
        <div>
          <label className="block text-xs font-medium text-slate-300 mb-1">
            From Email <span className="text-slate-500 font-normal">(optional — defaults to SMTP Username)</span>
          </label>
          <div className="relative">
            <Mail className="w-4 h-4 text-slate-500 absolute left-3 top-3" />
            <input
              type="email"
              value={formData.fromEmail}
              onChange={(e) => { clearMessages(); setFormData({ ...formData, fromEmail: e.target.value }); }}
              placeholder={formData.smtpUser || 'Same as SMTP Username'}
              className="w-full pl-10 pr-3 py-2.5 bg-slate-950 border border-slate-800 rounded-xl text-slate-100 text-sm focus:outline-none focus:border-indigo-500 transition-colors"
            />
          </div>
        </div>

        {/* Action Buttons */}
        <div className="flex items-center justify-between pt-2 flex-wrap gap-3">
          <div>
            {isConfigured && (
              <button
                type="button"
                onClick={() => setShowRemoveModal(true)}
                disabled={removing}
                className="flex items-center gap-1.5 px-3 py-2 text-xs font-medium text-red-400 hover:text-red-300 hover:bg-red-500/10 border border-red-500/20 hover:border-red-500/30 rounded-xl transition-all disabled:opacity-50"
              >
                <Trash2 className="w-3.5 h-3.5" />
                Remove Config
              </button>
            )}
          </div>
          <button
            type="submit"
            disabled={saving}
            className="flex items-center gap-2 px-5 py-2.5 bg-indigo-600 hover:bg-indigo-500 text-white font-medium text-sm rounded-xl shadow-lg shadow-indigo-500/20 transition-all disabled:opacity-50"
          >
            {saving ? (
              <Loader2 className="w-4 h-4 animate-spin" />
            ) : (
              <Save className="w-4 h-4" />
            )}
            {saving ? 'Saving...' : 'Save Settings'}
          </button>
        </div>
      </form>

      {/* Send Test Email — only visible when configured */}
      {isConfigured && (
        <div className="bg-slate-900 border border-slate-800/80 rounded-2xl p-5 space-y-4">
          <h2 className="text-sm font-semibold text-slate-200 flex items-center gap-2">
            <Send className="w-4 h-4 text-indigo-400" />
            Send Test Email
          </h2>
          <p className="text-xs text-slate-400">
            Verify your SMTP settings by sending a quick test email. The email will be sent from your configured address.
          </p>

          {testSuccess && (
            <div className="flex items-center gap-2 px-3 py-2.5 bg-emerald-500/10 border border-emerald-500/30 rounded-xl text-emerald-400 text-xs font-medium">
              <CheckCircle2 className="w-4 h-4 shrink-0" />
              {testSuccess}
            </div>
          )}
          {testError && (
            <div className="flex items-start gap-2 px-3 py-2.5 bg-red-500/10 border border-red-500/30 rounded-xl text-red-400 text-xs font-medium">
              <AlertTriangle className="w-4 h-4 shrink-0 mt-0.5" />
              <span>{testError}</span>
            </div>
          )}

          <div className="flex items-end gap-3">
            <div className="flex-1">
              <label className="block text-xs font-medium text-slate-300 mb-1">Send To</label>
              <div className="relative">
                <Mail className="w-4 h-4 text-slate-500 absolute left-3 top-3" />
                <input
                  type="email"
                  value={testEmail}
                  onChange={(e) => { setTestSuccess(''); setTestError(''); setTestEmail(e.target.value); }}
                  placeholder="recipient@example.com"
                  className="w-full pl-10 pr-3 py-2.5 bg-slate-950 border border-slate-800 rounded-xl text-slate-100 text-sm focus:outline-none focus:border-indigo-500 transition-colors"
                />
              </div>
            </div>
            <button
              type="button"
              onClick={handleTest}
              disabled={testing}
              className="flex items-center gap-2 px-4 py-2.5 bg-emerald-600 hover:bg-emerald-500 text-white font-medium text-sm rounded-xl shadow-lg shadow-emerald-500/20 transition-all disabled:opacity-50 whitespace-nowrap"
            >
              {testing ? (
                <Loader2 className="w-4 h-4 animate-spin" />
              ) : (
                <Send className="w-4 h-4" />
              )}
              {testing ? 'Sending...' : 'Send Test'}
            </button>
          </div>
        </div>
      )}

      {/* How It Works */}
      <div className="bg-slate-900/50 border border-slate-800/60 rounded-2xl p-5">
        <h2 className="text-sm font-semibold text-slate-200 mb-3 flex items-center gap-2">
          <Info className="w-4 h-4 text-indigo-400" />
          How Email Sending Works
        </h2>
        <div className="space-y-3 text-xs text-slate-400">
          <div className="flex items-start gap-2">
            <div className="w-5 h-5 rounded-full bg-indigo-500/15 border border-indigo-500/30 flex items-center justify-center text-indigo-400 text-[10px] font-bold shrink-0">1</div>
            <span>When you send an invoice email, the system uses <strong className="text-slate-300">your SMTP credentials</strong> to deliver the email directly from your address.</span>
          </div>
          <div className="flex items-start gap-2">
            <div className="w-5 h-5 rounded-full bg-indigo-500/15 border border-indigo-500/30 flex items-center justify-center text-indigo-400 text-[10px] font-bold shrink-0">2</div>
            <span>Your clients will see the email as coming from <strong className="text-slate-300">{formData.smtpUser || 'your email'}</strong>, not a generic system address.</span>
          </div>
          <div className="flex items-start gap-2">
            <div className="w-5 h-5 rounded-full bg-indigo-500/15 border border-indigo-500/30 flex items-center justify-center text-indigo-400 text-[10px] font-bold shrink-0">3</div>
            <span>Recurring invoice schedules and payment reminders also use your SMTP settings automatically.</span>
          </div>
          <div className="flex items-start gap-2">
            <div className="w-5 h-5 rounded-full bg-indigo-500/15 border border-indigo-500/30 flex items-center justify-center text-indigo-400 text-[10px] font-bold shrink-0">4</div>
            <span>Credentials are stored securely and <strong className="text-slate-300">never exposed</strong> in API responses.</span>
          </div>
        </div>
      </div>

      {/* Remove Confirmation Modal */}
      {showRemoveModal && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-sm w-full p-6 shadow-2xl">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-10 h-10 rounded-xl bg-red-500/10 border border-red-500/20 flex items-center justify-center">
                <Trash2 className="w-5 h-5 text-red-400" />
              </div>
              <div>
                <h3 className="text-base font-bold text-slate-100">Remove SMTP Config?</h3>
                <p className="text-xs text-slate-400">This action can't be undone.</p>
              </div>
            </div>
            <p className="text-sm text-slate-300 mb-5">
              Removing your SMTP configuration will stop all email sending from your account. You can always reconfigure it later.
            </p>
            <div className="flex items-center justify-end gap-3">
              <button
                onClick={() => setShowRemoveModal(false)}
                className="px-4 py-2 text-sm text-slate-400 hover:text-white transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={handleRemove}
                className="px-4 py-2.5 bg-red-600 hover:bg-red-500 text-white font-medium text-sm rounded-xl shadow-lg shadow-red-500/20 transition-all"
              >
                Yes, Remove
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
