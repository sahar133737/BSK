using System;
using System.Windows.Forms;
using BGSK1.Services;
using BGSK1.UI;

namespace BGSK1
{
    public sealed class RequestEditForm : Form
    {
        private readonly int _id;
        private readonly ComboBox _cmbEquipment;
        private readonly TextBox _txtProblem;
        private readonly ComboBox _cmbPriority;
        private readonly ComboBox _cmbStatus;
        private readonly ComboBox _cmbAssigned;

        public RequestEditForm(int id, int equipmentId, string problem, string priority, string status, string assignedTo)
        {
            _id = id;
            ThemeHelper.ApplyForm(this, "Редактирование заявки");
            Width = 760;
            Height = 320;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            _cmbEquipment = new ComboBox { Left = 20, Top = 48, Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            _txtProblem = new TextBox { Left = 350, Top = 48, Width = 380, Text = problem };
            _cmbPriority = new ComboBox { Left = 20, Top = 106, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbPriority.Items.AddRange(new[] { "Низкий", "Средний", "Высокий" });
            _cmbPriority.Text = string.IsNullOrWhiteSpace(priority) ? "Средний" : priority;
            _cmbStatus = new ComboBox { Left = 180, Top = 106, Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbStatus.Items.AddRange(new[] { "Новая", "В работе", "Ожидание", "Завершена" });
            _cmbStatus.Text = string.IsNullOrWhiteSpace(status) ? "Новая" : status;
            _cmbAssigned = new ComboBox { Left = 350, Top = 106, Width = 260, DropDownStyle = ComboBoxStyle.DropDown };
            _cmbAssigned.Text = assignedTo ?? string.Empty;

            var btnSave = new Button { Left = 20, Top = 210, Width = 350, Height = 34, Text = "Сохранить" };
            var btnCancel = new Button { Left = 380, Top = 210, Width = 350, Height = 34, Text = "Отмена" };
            ThemeHelper.StyleButton(btnSave, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnCancel, ThemeHelper.Secondary);
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                LabelAt("Техника",20,20,120), LabelAt("Неисправность",350,20,140), LabelAt("Приоритет",20,78,100),
                LabelAt("Статус",180,78,80), LabelAt("Исполнитель",350,78,120),
                _cmbEquipment,_txtProblem,_cmbPriority,_cmbStatus,_cmbAssigned,btnSave,btnCancel
            });

            Load += (s, e) =>
            {
                _cmbEquipment.DataSource = EquipmentService.GetEquipmentLookup();
                _cmbEquipment.DisplayMember = "DisplayName";
                _cmbEquipment.ValueMember = "Id";
                _cmbEquipment.SelectedValue = equipmentId;

                var users = UserService.GetActiveUsersLookup();
                foreach (System.Data.DataRow row in users.Rows)
                {
                    _cmbAssigned.Items.Add(row["FullName"].ToString());
                }
            };
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_cmbEquipment.SelectedValue == null || string.IsNullOrWhiteSpace(_txtProblem.Text))
            {
                MessageBox.Show("Выберите технику и укажите неисправность.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RepairRequestService.UpdateRequest(_id, Convert.ToInt32(_cmbEquipment.SelectedValue), _txtProblem.Text.Trim(), _cmbPriority.Text, _cmbStatus.Text, _cmbAssigned.Text.Trim());
            DialogResult = DialogResult.OK;
            Close();
        }

        private static Label LabelAt(string text, int left, int top, int width)
        {
            return ThemeHelper.FormFieldLabel(text, left, top, width);
        }
    }
}
