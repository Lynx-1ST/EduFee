using K26_DotNet.Models;
using K26_DotNet.Services;

namespace _26K1_DotNet
{
    public class FormTuitionDetail : Form
    {
        private readonly StudentService _svSvc;
        private readonly SemesterService _semSvc;
        private readonly TuitionService _tuiSvc;
        private readonly TuitionFee? _fee;
        private readonly bool _isNew;

        private ComboBox cmbStudent = null!, cmbSemester = null!, cmbDiscount = null!;
        private NumericUpDown numCredits = null!, numDiscount = null!;
        private DateTimePicker dtpDueDate = null!;
        private Label lblStudentInfo = null!, lblTotal = null!, lblFormula = null!, lblPrice = null!;
        private TextBox txtNote = null!;
        private ErrorProvider _errorProvider = null!;

        public FormTuitionDetail(StudentService sv, SemesterService sem, TuitionService tui, TuitionFee? fee)
        {
            _svSvc = sv; _semSvc = sem; _tuiSvc = tui;
            _fee = fee; _isNew = fee == null;
            BuildUI();
        }

        private void BuildUI()
        {
            Text = _isNew ? "Thêm Phiếu Học Phí" : "Sửa Phiếu Học Phí";
            ClientSize = new Size(560, 560);
            MinimumSize = new Size(560, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = UITheme.Surface;
            Font = UITheme.FontBody;
            AutoScaleMode = AutoScaleMode.Font;

            // ── Color header ──────────────────────────────────────────────
            var header = new Panel
            {
                Dock = DockStyle.Top, Height = 56,
                BackColor = _isNew ? UITheme.Success : UITheme.Primary
            };
            header.Controls.Add(new Label
            {
                Text = _isNew ? "Thêm phiếu học phí" : "Sửa phiếu học phí",
                Font = UITheme.FontH1, ForeColor = Color.White,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            });

            // ── Fields ────────────────────────────────────────────────────
            var body = new Panel
            {
                Dock = DockStyle.Fill, BackColor = UITheme.Surface,
                Padding = new Padding(28, 16, 28, 16)
            };

            int y = 14;

            // Sinh viên
            AddLabel(body, "Sinh viên *", y);
            cmbStudent = AddCombo(body, y, 180, 310);
            cmbStudent.SelectedIndexChanged += (s, e) =>
            {
                if (cmbStudent.SelectedItem is SvItem si)
                    lblStudentInfo.Text = $"Lớp: {si.Class}";
            };
            y += 32;

            lblStudentInfo = new Label
            {
                Location = new Point(180, y), AutoSize = true,
                Font = UITheme.FontSmall, ForeColor = UITheme.TextMuted
            };
            body.Controls.Add(lblStudentInfo);
            y += 22;

            // Học kỳ
            AddLabel(body, "Học kỳ *", y);
            cmbSemester = AddCombo(body, y, 180, 310);
            cmbSemester.SelectedIndexChanged += (s, e) =>
            {
                if (_isNew && cmbSemester.SelectedItem is SemItem sm)
                {
                    var sem = _semSvc.GetById(sm.Id);
                    if (sem != null) dtpDueDate.Value = sem.DueDate;
                }
                if (cmbDiscount.SelectedIndex > 0 && !cmbDiscount.SelectedItem!.ToString()!.Contains("Tùy chỉnh"))
                    OnDiscountPolicyChanged();
                else
                    UpdateTotal();
            };
            y += 36;

            // Hạn nộp
            AddLabel(body, "Hạn nộp *", y);
            dtpDueDate = new DateTimePicker
            {
                Location = new Point(180, y), Size = new Size(180, 30),
                Font = UITheme.FontBody, Format = DateTimePickerFormat.Short
            };
            body.Controls.Add(dtpDueDate);
            y += 36;

            // Số tín chỉ + đơn giá
            AddLabel(body, "Số tín chỉ *", y);
            numCredits = new NumericUpDown
            {
                Location = new Point(180, y), Size = new Size(90, 30),
                Font = UITheme.FontBodyLarge,
                Minimum = 1, Maximum = 200, Value = 15,
                BackColor = UITheme.SurfaceAlt
            };
            numCredits.ValueChanged += (s, e) =>
            {
                if (cmbDiscount.SelectedIndex > 0 && !cmbDiscount.SelectedItem!.ToString()!.Contains("Tùy chỉnh"))
                    OnDiscountPolicyChanged();
                else
                    UpdateTotal();
            };
            body.Controls.Add(numCredits);

            lblPrice = new Label
            {
                Text = string.Empty,
                Location = new Point(278, y + 5), AutoSize = true,
                Font = UITheme.FontSmall, ForeColor = UITheme.TextSecondary
            };
            body.Controls.Add(lblPrice);
            y += 36;

            // Miễn giảm / Học bổng
            AddLabel(body, "Miễn giảm / HB", y);
            cmbDiscount = AddCombo(body, y, 180, 310);
            cmbDiscount.Items.AddRange(new object[]
            {
                "Không miễn giảm (0%)",
                "Học bổng Xuất sắc (100%)",
                "Học bổng Giỏi (50%)",
                "Học bổng Khá (30%)",
                "Diện chính sách / Hộ nghèo (50%)",
                "Con thương binh - liệt sĩ (100%)",
                "Tùy chỉnh số tiền giảm"
            });
            cmbDiscount.SelectedIndex = 0;
            cmbDiscount.SelectedIndexChanged += (s, e) => OnDiscountPolicyChanged();
            body.Controls.Add(cmbDiscount);
            y += 36;

            // Tiền miễn giảm
            AddLabel(body, "Tiền giảm", y);
            numDiscount = new NumericUpDown
            {
                Location = new Point(180, y), Size = new Size(160, 30),
                Font = UITheme.FontBody,
                Minimum = 0, Maximum = 100_000_000, Increment = 100_000,
                ThousandsSeparator = true,
                BackColor = UITheme.SurfaceAlt,
                Enabled = false
            };
            numDiscount.ValueChanged += (s, e) => UpdateTotal();
            body.Controls.Add(numDiscount);

            var lblDiscountUnit = new Label
            {
                Text = "VNĐ", Location = new Point(348, y + 5), AutoSize = true,
                Font = UITheme.FontSmall, ForeColor = UITheme.TextSecondary
            };
            body.Controls.Add(lblDiscountUnit);
            y += 38;

            // Tổng học phí (auto-calculated, readonly display)
            AddLabel(body, "Phải nộp thực tế", y);
            var feeBox = new Panel
            {
                Location = new Point(180, y - 2), Size = new Size(310, 34),
                BackColor = UITheme.PrimaryLight
            };
            feeBox.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Primary, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, feeBox.Width - 1, feeBox.Height - 1);
            };
            lblTotal = new Label
            {
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight,
                Font = UITheme.FontH2Regular,
                ForeColor = UITheme.Primary,
                Padding = new Padding(0, 0, 10, 0)
            };
            feeBox.Controls.Add(lblTotal);
            body.Controls.Add(feeBox);
            y += 38;

            lblFormula = new Label
            {
                Location = new Point(180, y), Size = new Size(310, 18),
                Font = UITheme.FontSmall, ForeColor = UITheme.TextMuted
            };
            body.Controls.Add(lblFormula);
            y += 24;

            // Ghi chú
            AddLabel(body, "Ghi chú", y);
            txtNote = new TextBox
            {
                Location = new Point(180, y), Size = new Size(310, 48),
                Font = UITheme.FontBody, Multiline = true,
                BackColor = UITheme.SurfaceAlt, BorderStyle = BorderStyle.FixedSingle
            };
            body.Controls.Add(txtNote);
            y += 62;

            // Footer with action buttons
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = UITheme.Surface };
            footer.Controls.Add(UITheme.HSep(DockStyle.Top));

            var btnSave = UITheme.PrimaryBtn(_isNew ? "Tạo phiếu" : "Lưu thay đổi", 140, 38);
            btnSave.Click += BtnSave_Click;
            btnSave.AccessibleName = _isNew ? "Thêm mới phiếu học phí" : "Lưu thay đổi phiếu học phí";

            var btnCancel = UITheme.GhostBtn("Hủy", 80, 38);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            btnCancel.AccessibleName = "Hủy bỏ";

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            var flowBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0, 13, 24, 0),
                BackColor = Color.Transparent
            };
            flowBtns.Controls.Add(btnSave);
            flowBtns.Controls.Add(btnCancel);
            footer.Controls.Add(flowBtns);

            Controls.Add(body);
            Controls.Add(footer);
            Controls.Add(header);

            _errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

            LoadData();
            UpdateTotal();
        }

        private static void AddLabel(Panel p, string text, int y)
        {
            p.Controls.Add(new Label
            {
                Text = text + ":", Location = new Point(16, y + 5), AutoSize = true,
                Font = UITheme.FontSmall, ForeColor = UITheme.TextSecondary
            });
        }

        private static ComboBox AddCombo(Panel p, int y, int x, int w)
        {
            var c = new ComboBox
            {
                Location = new Point(x, y), Size = new Size(w, 30),
                Font = UITheme.FontBody, DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = UITheme.SurfaceAlt
            };
            p.Controls.Add(c);
            return c;
        }

        private void OnDiscountPolicyChanged()
        {
            int credits = (int)numCredits.Value;
            decimal original = credits * GetTuitionPerCredit();
            string sel = cmbDiscount.SelectedItem?.ToString() ?? "";

            if (sel.Contains("100%"))
            {
                numDiscount.Value = original;
                numDiscount.Enabled = false;
            }
            else if (sel.Contains("50%"))
            {
                numDiscount.Value = Math.Round(original * 0.5m);
                numDiscount.Enabled = false;
            }
            else if (sel.Contains("30%"))
            {
                numDiscount.Value = Math.Round(original * 0.3m);
                numDiscount.Enabled = false;
            }
            else if (sel.StartsWith("Không"))
            {
                numDiscount.Value = 0;
                numDiscount.Enabled = false;
            }
            else // Tùy chỉnh
            {
                numDiscount.Enabled = true;
            }
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            int credits = (int)numCredits.Value;
            decimal pricePerCredit = GetTuitionPerCredit();
            decimal original = credits * pricePerCredit;
            decimal discount = numDiscount != null ? numDiscount.Value : 0;
            if (discount > original)
            {
                discount = original;
                if (numDiscount != null) numDiscount.Value = discount;
            }
            decimal net = original - discount;
            lblTotal.Text = $"{net:N0} VNĐ";
            if (discount > 0)
                lblFormula.Text = $"Gốc: {original:N0}đ  -  Giảm: {discount:N0}đ  =  {net:N0} VNĐ";
            else
                lblFormula.Text = $"{credits} tín chỉ  ×  {pricePerCredit:N0}  =  {net:N0} VNĐ";
            lblPrice.Text = $"×  {pricePerCredit:N0} VNĐ/tín";
        }

        private decimal GetTuitionPerCredit()
        {
            if (!_isNew && _fee != null && _fee.PricePerCreditSnapshot > 0)
                return _fee.PricePerCreditSnapshot;
            if (cmbSemester?.SelectedItem is SemItem semesterItem)
                return _semSvc.GetById(semesterItem.Id)?.TuitionPerCredit ?? Semester.DefaultTuitionPerCredit;
            return Semester.DefaultTuitionPerCredit;
        }

        private void LoadData()
        {
            foreach (var sv in _svSvc.GetAllStudents())
                cmbStudent.Items.Add(new SvItem(sv.Id, sv.StudentCode, sv.FullName, sv.ClassName));

            foreach (var sem in _semSvc.GetAll())
                cmbSemester.Items.Add(new SemItem(sem.Id, sem.Name));

            if (!_isNew && _fee != null)
            {
                for (int i = 0; i < cmbStudent.Items.Count; i++)
                    if (cmbStudent.Items[i] is SvItem si && si.Id == _fee.StudentId) { cmbStudent.SelectedIndex = i; break; }
                for (int i = 0; i < cmbSemester.Items.Count; i++)
                    if (cmbSemester.Items[i] is SemItem sm && sm.Id == _fee.SemesterId) { cmbSemester.SelectedIndex = i; break; }

                numCredits.Value = _fee.Credits > 0 ? _fee.Credits : 15;
                dtpDueDate.Value = _fee.DueDate ?? _semSvc.GetById(_fee.SemesterId)?.DueDate ?? DateTime.Now.AddMonths(1);
                txtNote.Text = _fee.Note;
                cmbStudent.Enabled = cmbSemester.Enabled = false;

                // Load discount
                if (!string.IsNullOrEmpty(_fee.DiscountReason))
                {
                    int dIdx = cmbDiscount.Items.IndexOf(_fee.DiscountReason);
                    if (dIdx >= 0) cmbDiscount.SelectedIndex = dIdx;
                    else cmbDiscount.SelectedIndex = cmbDiscount.Items.Count - 1;
                }
                else if (_fee.DiscountAmount > 0)
                {
                    cmbDiscount.SelectedIndex = cmbDiscount.Items.Count - 1;
                }
                else
                {
                    cmbDiscount.SelectedIndex = 0;
                }
                numDiscount.Value = Math.Min(numDiscount.Maximum, _fee.DiscountAmount);
            }
            else
            {
                if (cmbStudent.Items.Count > 0) cmbStudent.SelectedIndex = 0;
                var active = _semSvc.GetActive();
                if (active != null)
                {
                    for (int i = 0; i < cmbSemester.Items.Count; i++)
                        if (cmbSemester.Items[i] is SemItem sm && sm.Id == active.Id) { cmbSemester.SelectedIndex = i; break; }
                    dtpDueDate.Value = active.DueDate;
                }
                else
                {
                    dtpDueDate.Value = DateTime.Now.AddMonths(1);
                }
                if (cmbSemester.SelectedIndex < 0 && cmbSemester.Items.Count > 0) cmbSemester.SelectedIndex = 0;
                cmbDiscount.SelectedIndex = 0;
            }
        }

        private void BtnSave_Click(object? s, EventArgs e)
        {
            _errorProvider.Clear();
            bool isValid = true;
            if (cmbStudent.SelectedItem == null)
            { _errorProvider.SetError(cmbStudent, "Vui lòng chọn sinh viên!"); isValid = false; }
            if (cmbSemester.SelectedItem == null)
            { _errorProvider.SetError(cmbSemester, "Vui lòng chọn học kỳ!"); isValid = false; }
            if (numCredits.Value < 1)
            { _errorProvider.SetError(numCredits, "Số tín chỉ phải ít nhất là 1!"); isValid = false; }

            if (!isValid)
            {
                UiFeedback.ShowWarning("Vui lòng kiểm tra lại thông tin nhập!");
                return;
            }

            try
            {
                int studentId  = ((SvItem)cmbStudent.SelectedItem!).Id;
                int semesterId = ((SemItem)cmbSemester.SelectedItem!).Id;
                int credits    = (int)numCredits.Value;
                decimal pricePerCredit = GetTuitionPerCredit();
                DateTime dueDate = dtpDueDate.Value;
                decimal discount = numDiscount.Value;
                string discountReason = cmbDiscount.SelectedIndex > 0 ? cmbDiscount.SelectedItem!.ToString()! : "";
                decimal netAmount = Math.Max(0, (credits * pricePerCredit) - discount);

                if (_isNew)
                {
                    var fee = new TuitionFee(0, studentId, semesterId, credits, pricePerCredit, txtNote.Text.Trim(), dueDate, discount, discountReason);
                    _tuiSvc.Add(fee);
                    UiFeedback.ShowSuccess($"Đã tạo phiếu học phí:\n{credits} tín chỉ × {pricePerCredit:N0} = {credits * pricePerCredit:N0} VNĐ\nMiễn giảm: {discount:N0} VNĐ\nThực nộp: {netAmount:N0} VNĐ\nHạn nộp: {dueDate:dd/MM/yyyy}");
                }
                else
                {
                    if (netAmount < _fee!.PaidAmount)
                    {
                        UiFeedback.ShowWarning($"Không thể giảm học phí thực nộp xuống {netAmount:N0} VNĐ vì sinh viên đã đóng {_fee.PaidAmount:N0} VNĐ!\nVui lòng chọn số tín chỉ hoặc mức miễn giảm phù hợp.");
                        return;
                    }

                    var updatedFee = new TuitionFee
                    {
                        Id = _fee.Id,
                        StudentId = _fee.StudentId,
                        SemesterId = _fee.SemesterId,
                        PaidAmount = _fee.PaidAmount,
                        PaidDate = _fee.PaidDate,
                        Credits = credits,
                        DiscountAmount = discount,
                        DiscountReason = discountReason,
                        TotalAmount = netAmount,
                        DueDate = dueDate,
                        Note = txtNote.Text.Trim()
                    };
                    updatedFee.UpdateStatus(_semSvc.GetById(_fee.SemesterId)?.DueDate);
                    _tuiSvc.Update(updatedFee);

                    // Sync to caller's object reference only after durable update succeeds
                    _fee.Credits = updatedFee.Credits;
                    _fee.DiscountAmount = updatedFee.DiscountAmount;
                    _fee.DiscountReason = updatedFee.DiscountReason;
                    _fee.TotalAmount = updatedFee.TotalAmount;
                    _fee.DueDate = updatedFee.DueDate;
                    _fee.Note = updatedFee.Note;
                    _fee.Status = updatedFee.Status;
                    UiFeedback.ShowSuccess("Cập nhật thành công!");
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                UiFeedback.ShowException(ex, "Lỗi lưu học phí");
            }
        }

        private class SvItem
        {
            public int Id;
            public string Code, Name, Class;
            public SvItem(int id, string code, string name, string className) { Id = id; Code = code; Name = name; Class = className; }
            public override string ToString() => $"{Code} — {Name}  (Lớp {Class})";
        }
    }
}
