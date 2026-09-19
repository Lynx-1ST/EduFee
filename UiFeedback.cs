using System;
using System.Drawing;
using System.Windows.Forms;

namespace _26K1_DotNet
{
    /// <summary>
    /// Shared UI feedback helper — standardizes Vietnamese dialog titles, icons,
    /// confirmation buttons, busy states, and exception display.
    /// Pure UI helper: no service or persistence dependencies.
    /// </summary>
    public static class UiFeedback
    {
        // ── Standard Dialog Titles ────────────────────────────────────────────
        private const string TitleInfo    = "Thông báo";
        private const string TitleSuccess = "Thành công";
        private const string TitleWarning = "Cảnh báo";
        private const string TitleError   = "Lỗi";
        private const string TitleConfirm = "Xác nhận";

        // ── Success / Info ────────────────────────────────────────────────────

        /// <summary>
        /// Show a success notification with optional next-step hint.
        /// </summary>
        public static void ShowSuccess(string message, string? nextStep = null)
        {
            var text = nextStep != null ? $"{message}\n\n💡 {nextStep}" : message;
            MessageBox.Show(text, TitleSuccess, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Show an informational notification.
        /// </summary>
        public static void ShowInfo(string message)
        {
            MessageBox.Show(message, TitleInfo, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ── Warnings ──────────────────────────────────────────────────────────

        /// <summary>
        /// Show a warning notification (e.g., missing selection, empty field).
        /// </summary>
        public static void ShowWarning(string message)
        {
            MessageBox.Show(message, TitleWarning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // ── Errors ────────────────────────────────────────────────────────────

        /// <summary>
        /// Show an error notification.
        /// </summary>
        public static void ShowError(string message)
        {
            MessageBox.Show(message, TitleError, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Show a contextualized exception notification.
        /// </summary>
        public static void ShowException(Exception ex, string context = "Lỗi hệ thống")
        {
            MessageBox.Show($"{context}: {ex.Message}", TitleError, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void ShowException(string context, Exception ex)
        {
            ShowException(ex, context);
        }

        // ── Confirmations ─────────────────────────────────────────────────────

        /// <summary>
        /// Confirm a destructive action (delete, overwrite).
        /// Returns true if user chose Yes. Defaults to Button2 (No) to prevent accidental execution.
        /// </summary>
        public static bool ConfirmDelete(string itemName)
        {
            return MessageBox.Show(
                $"Xác nhận xóa «{itemName}»?\nHành động này không thể hoàn tác.",
                TitleConfirm,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }

        /// <summary>
        /// Displays a lightweight non-blocking tooltip notification that auto-dismisses.
        /// </summary>
        public static void ShowToast(Control control, string message, int durationMs = 1500)
        {
            if (control == null || control.IsDisposed) return;
            var tip = new ToolTip
            {
                IsBalloon = false,
                ShowAlways = true,
                BackColor = UITheme.Surface,
                ForeColor = UITheme.TextPrimary
            };
            tip.Show(message, control, 0, -28, durationMs);
        }

        /// <summary>
        /// Confirm a general action. Returns true if user chose Yes.
        /// </summary>
        public static bool ConfirmAction(string message, string title = TitleConfirm)
        {
            return MessageBox.Show(message, title, MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes;
        }

        public static bool Confirm(string message, string title = TitleConfirm)
        {
            return ConfirmAction(message, title);
        }

        // ── Busy State Scope ──────────────────────────────────────────────────

        /// <summary>
        /// Creates a disposable busy scope that disables the trigger control or form,
        /// shows a wait cursor, and optionally updates a status label.
        /// Usage: using (UiFeedback.BusyScope(btnSave, lblStatus, "Đang lưu...")) { await work; }
        /// </summary>
        public static IDisposable BusyScope(Control? target = null, Label? statusLabel = null, string? busyText = null)
        {
            return new BusyStateScope(target, statusLabel, busyText);
        }

        public static IDisposable BusyScope(Control target, string? busyText)
        {
            return new BusyStateScope(target, null, busyText);
        }

        public static IDisposable BusyScope()
        {
            return new BusyStateScope(null, null, null);
        }

        private sealed class BusyStateScope : IDisposable
        {
            private readonly Control? _target;
            private readonly Label? _statusLabel;
            private readonly string? _originalStatus;
            private readonly Color _originalStatusColor;
            private readonly Cursor _originalCursor;
            private bool _disposed;

            public BusyStateScope(Control? target, Label? statusLabel, string? busyText)
            {
                _target = target;
                _statusLabel = statusLabel;

                Form? form = target as Form ?? target?.FindForm() ?? Form.ActiveForm;
                _originalCursor = form?.Cursor ?? Cursors.Default;

                if (form != null)
                {
                    form.UseWaitCursor = true;
                    form.Cursor = Cursors.WaitCursor;
                }

                if (target != null && target is not Form)
                {
                    target.Enabled = false;
                }

                if (_statusLabel != null)
                {
                    _originalStatus = _statusLabel.Text;
                    _originalStatusColor = _statusLabel.ForeColor;
                    _statusLabel.Text = busyText ?? "Đang xử lý...";
                    _statusLabel.ForeColor = UITheme.Primary;
                    _statusLabel.Visible = true;
                }
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                Form? form = _target as Form ?? _target?.FindForm() ?? Form.ActiveForm;

                if (form != null)
                {
                    form.UseWaitCursor = false;
                    form.Cursor = _originalCursor;
                }

                if (_target != null && _target is not Form)
                {
                    _target.Enabled = true;
                }

                if (_statusLabel != null)
                {
                    _statusLabel.Text = _originalStatus ?? "";
                    _statusLabel.ForeColor = _originalStatusColor;
                }
            }
        }

        // ── Inline Validation Helpers ─────────────────────────────────────────

        /// <summary>
        /// Validate that a TextBox is not empty. Sets ErrorProvider if invalid.
        /// Returns true if valid.
        /// </summary>
        public static bool ValidateRequired(ErrorProvider ep, TextBox txt, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(txt.Text))
            {
                ep.SetError(txt, $"Vui lòng nhập {fieldName}!");
                return false;
            }
            ep.SetError(txt, "");
            return true;
        }

        /// <summary>
        /// Validate email format. Sets ErrorProvider if invalid.
        /// Returns true if valid.
        /// </summary>
        public static bool ValidateEmail(ErrorProvider ep, TextBox txt)
        {
            if (string.IsNullOrWhiteSpace(txt.Text))
            {
                ep.SetError(txt, "Vui lòng nhập email!");
                return false;
            }
            // Basic check: must contain @ and a dot after @
            var atIdx = txt.Text.IndexOf('@');
            if (atIdx < 1 || txt.Text.LastIndexOf('.') <= atIdx)
            {
                ep.SetError(txt, "Email không hợp lệ!");
                return false;
            }
            ep.SetError(txt, "");
            return true;
        }

        /// <summary>
        /// Validate that a ComboBox has a selection. Sets ErrorProvider if invalid.
        /// Returns true if valid.
        /// </summary>
        public static bool ValidateComboSelection(ErrorProvider ep, ComboBox cmb, string fieldName)
        {
            if (cmb.SelectedIndex < 0 || cmb.SelectedItem == null)
            {
                ep.SetError(cmb, $"Vui lòng chọn {fieldName}!");
                return false;
            }
            ep.SetError(cmb, "");
            return true;
        }

        /// <summary>
        /// Focus the first control that has an ErrorProvider error set.
        /// </summary>
        public static void FocusFirstError(ErrorProvider ep, params Control[] controls)
        {
            foreach (var c in controls)
            {
                if (!string.IsNullOrEmpty(ep.GetError(c)))
                {
                    c.Focus();
                    return;
                }
            }
        }
    }
}
