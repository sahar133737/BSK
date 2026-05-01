using System;
using System.Data;
using System.Windows.Forms;
using BGSK1.Services;
using BGSK1.UI;

namespace BGSK1
{
    public sealed class MaintenanceEditForm : Form
    {
        private readonly int _id;
        private readonly ComboBox _cmbEquipment;
        private readonly ComboBox _cmbType;
        private readonly NumericUpDown _numPeriod;
        private readonly DateTimePicker _dtNext;
        private readonly ComboBox _cmbResponsible;
        private readonly CheckBox _chkActive;

        public MaintenanceEditForm(int id, int equipmentId, string maintenanceType, int periodDays, DateTime nextDate, string responsible, bool isActive)
        {
            _id = id;
            ThemeHelper.ApplyForm(this, "Редактирование плана ТО");
            Width = 760;
            Height = 330;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            _cmbEquipment = new ComboBox { Left = 20, Top = 48, Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbType = new ComboBox { Left = 350, Top = 48, Width = 318, DropDownStyle = ComboBoxStyle.DropDown, Text = maintenanceType ?? string.Empty };
            var btnAddType = LookupUiHelper.CreateAddLookupButton(672, 48, "Добавить вид ТО", (s, e) => AddLookup(_cmbType, LookupDictionaryService.MaintenanceType, "Новый вид планового ТО"));
            _numPeriod = new NumericUpDown { Left = 20, Top = 106, Width = 140, Minimum = 1, Maximum = 365, Value = periodDays <= 0 ? 30 : periodDays };
            _dtNext = new DateTimePicker { Left = 170, Top = 106, Width = 170, Value = nextDate == DateTime.MinValue ? DateTime.Today : nextDate };
            _cmbResponsible = new ComboBox { Left = 350, Top = 106, Width = 252, DropDownStyle = ComboBoxStyle.DropDownList, Text = responsible ?? string.Empty };
            _chkActive = new CheckBox { Left = 642, Top = 109, Width = 90, Text = "Активен", Checked = isActive };

            var btnSave = new Button { Left = 20, Top = 220, Width = 350, Height = 34, Text = "Сохранить" };
            var btnCancel = new Button { Left = 380, Top = 220, Width = 350, Height = 34, Text = "Отмена" };
            ThemeHelper.StyleButton(btnSave, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnCancel, ThemeHelper.Secondary);
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                LabelAt("Техника",20,20,120), LabelAt("Вид ТО",350,20,120), LabelAt("Период (дн.)",20,78,100),
                LabelAt("Следующая дата",170,78,120), LabelAt("Ответственный",350,78,120),
                _cmbEquipment,_cmbType,btnAddType,_numPeriod,_dtNext,_cmbResponsible,_chkActive,btnSave,btnCancel
            });

            Load += (s, e) =>
            {
                _cmbEquipment.DataSource = EquipmentService.GetEquipmentLookup();
                _cmbEquipment.DisplayMember = "DisplayName";
                _cmbEquipment.ValueMember = "Id";
                _cmbEquipment.SelectedValue = equipmentId;
                FillCombo(_cmbType, MaintenanceService.GetMaintenanceTypeLookup());
                FillUsersCombo(_cmbResponsible);
                _cmbResponsible.Text = responsible ?? string.Empty;
            };
        }

        private void AddLookup(ComboBox combo, string category, string title)
        {
            if (!LookupUiHelper.TryPromptAndAddValue(this, category, title, out var value))
            {
                return;
            }

            FillCombo(_cmbType, MaintenanceService.GetMaintenanceTypeLookup());
            combo.Text = value;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_cmbEquipment.SelectedValue == null || string.IsNullOrWhiteSpace(_cmbType.Text))
            {
                MessageBox.Show("Выберите технику и вид ТО.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MaintenanceService.UpdatePlan(_id, Convert.ToInt32(_cmbEquipment.SelectedValue), _cmbType.Text.Trim(), Convert.ToInt32(_numPeriod.Value), _dtNext.Value.Date, _cmbResponsible.Text.Trim(), _chkActive.Checked);
            DialogResult = DialogResult.OK;
            Close();
        }

        private static void FillCombo(ComboBox combo, DataTable source)
        {
            var current = combo.Text;
            combo.Items.Clear();
            foreach (DataRow row in source.Rows)
            {
                combo.Items.Add(row["Value"].ToString());
            }
            combo.Text = current;
        }

        private static Label LabelAt(string text, int left, int top, int width)
        {
            return ThemeHelper.FormFieldLabel(text, left, top, width);
        }

        private static void FillUsersCombo(ComboBox combo)
        {
            var current = combo.Text;
            combo.Items.Clear();
            var source = UserService.GetActiveUsersLookup();
            foreach (DataRow row in source.Rows)
            {
                combo.Items.Add(row["FullName"].ToString());
            }
            combo.Text = current;
        }
    }
}
