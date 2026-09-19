using K26_DotNet.Models;
using K26_DotNet.Services;

namespace _26K1_DotNet
{
    public partial class FormStudentDetail : Form
    {
        private StudentService _studentService;
        private Student? _student;
        private bool _isNewStudent;
        private ErrorProvider _ep;

        public FormStudentDetail(StudentService studentService, Student? student)
        {
            InitializeComponent();
            _studentService = studentService;
            _student = student;
            _isNewStudent = student == null;
            _ep = new ErrorProvider(this) { BlinkStyle = ErrorBlinkStyle.NeverBlink };
            Shown += (s, e) => textBoxFullName.Focus();
        }

        private void FormStudentDetail_Load(object sender, EventArgs e)
        {
            Text = _isNewStudent ? "Thêm Sinh Viên Mới" : "Sửa Thông Tin Sinh Viên";
            labelHeaderTitle.Text = _isNewStudent ? "👤  THÊM SINH VIÊN MỚI" : "👤  CHỈNH SỬA SINH VIÊN";
            labelHeaderSub.Text = _isNewStudent ? "Nhập thông tin để thêm hồ sơ sinh viên mới vào hệ thống" : $"Chỉnh sửa thông tin cho sinh viên: {_student?.FullName}";

            if (_isNewStudent)
            {
                textBoxId.Text = (_studentService.GetMaxStudentId() + 1).ToString();
                textBoxId.ReadOnly = true;
                dateTimePickerDOB.Value = DateTime.Now.AddYears(-18);
            }
            else if (_student != null)
            {
                textBoxId.Text = _student.Id.ToString();
                textBoxId.ReadOnly = true;
                textBoxFullName.Text = _student.FullName;
                textBoxEmail.Text = _student.Email;
                textBoxPhoneNumber.Text = _student.PhoneNumber;
                dateTimePickerDOB.Value = _student.DateOfBirth;
                textBoxClassName.Text = _student.ClassName;
            }
        }

        private void ButtonSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                int id = int.Parse(textBoxId.Text);
                string fullName = textBoxFullName.Text.Trim();
                string email = textBoxEmail.Text.Trim();
                string phoneNumber = textBoxPhoneNumber.Text.Trim();
                DateTime dateOfBirth = dateTimePickerDOB.Value;
                string className = textBoxClassName.Text.Trim();

                if (_isNewStudent)
                {
                    var newStudent = new Student(id, fullName, email, phoneNumber, dateOfBirth, className);
                    _studentService.AddStudent(newStudent);
                    UiFeedback.ShowSuccess("Thêm sinh viên thành công!");
                }
                else if (_student != null)
                {
                    _student.FullName = fullName;
                    _student.Email = email;
                    _student.PhoneNumber = phoneNumber;
                    _student.DateOfBirth = dateOfBirth;
                    _student.ClassName = className;
                    _studentService.UpdateStudent(_student);
                    UiFeedback.ShowSuccess("Cập nhật sinh viên thành công!");
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                UiFeedback.ShowException(ex, "Lỗi lưu sinh viên");
            }
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private bool ValidateInput()
        {
            bool valid = true;
            valid &= UiFeedback.ValidateRequired(_ep, textBoxFullName, "họ tên");
            valid &= UiFeedback.ValidateEmail(_ep, textBoxEmail);
            valid &= UiFeedback.ValidateRequired(_ep, textBoxPhoneNumber, "điện thoại");
            valid &= UiFeedback.ValidateRequired(_ep, textBoxClassName, "lớp");
            if (!valid) UiFeedback.FocusFirstError(_ep, textBoxFullName, textBoxEmail, textBoxPhoneNumber, textBoxClassName);
            return valid;
        }
    }
}
