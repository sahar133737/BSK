using System;
using System.Windows.Forms;
using BGSK1.Services;
using BGSK1.UI;

namespace BGSK1
{
    public sealed class RequestCreateForm : Form
    {
        private readonly ComboBox _cmbEquipment;
        private readonly TextBox _txtProblem;
        private readonly ComboBox _cmbPriority;
        private readonly ComboBox _cmbAssigned;

        public RequestCreateForm()
        {
            ThemeHelper.ApplyForm(this, "Создание заявки");
            Width = 700;
            Height = 300;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            _cmbEquipment = new ComboBox { Left = 20, Top = 48, Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            _txtProblem = new TextBox { Left = 350, Top = 48, Width = 320 };
            _cmbPriority = new ComboBox { Left = 20, Top = 106, Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbPriority.Items.AddRange(new[] { "Низкий", "Средний", "Высокий" });
            _cmbPriority.SelectedIndex = 1;
            _cmbAssigned = new ComboBox { Left = 190, Top = 106, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };

            var btnCreate = new Button { Left = 20, Top = 190, Width = 320, Height = 34, Text = "Создать заявку" };
            var btnCancel = new Button { Left = 350, Top = 190, Width = 320, Height = 34, Text = "Отмена" };
            ThemeHelper.StyleButton(btnCreate, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnCancel, ThemeHelper.Secondary);
            btnCreate.Click += BtnCreate_Click;
            btnCancel.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                LabelAt("Техника", 20, 20, 120), LabelAt("Неисправность", 350, 20, 120),
                LabelAt("Приоритет", 20, 78, 120), LabelAt("Исполнитель", 190, 78, 120),
                _cmbEquipment, _txtProblem, _cmbPriority, _cmbAssigned, btnCreate, btnCancel
            });

            Load += RequestCreateForm_Load;
        }

        private void RequestCreateForm_Load(object sender, EventArgs e)
        {
            _cmbEquipment.DataSource = EquipmentService.GetEquipmentLookup();
            _cmbEquipment.DisplayMember = "DisplayName";
            _cmbEquipment.ValueMember = "Id";

            var assignees = UserService.GetActiveUsersLookup();
            _cmbAssigned.DataSource = assignees;
            _cmbAssigned.DisplayMember = "FullName";
            _cmbAssigned.ValueMember = "FullName";
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            if (_cmbEquipment.SelectedValue == null || string.IsNullOrWhiteSpace(_txtProblem.Text))
            {
                MessageBox.Show("Выберите технику и заполните неисправность.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RepairRequestService.CreateRequest(Convert.ToInt32(_cmbEquipment.SelectedValue), _txtProblem.Text.Trim(), _cmbPriority.Text, _cmbAssigned.Text);
            DialogResult = DialogResult.OK;
            Close();
        }

        private static Label LabelAt(string text, int left, int top, int width)
        {
            return ThemeHelper.FormFieldLabel(text, left, top, width);
        }
    }
}
