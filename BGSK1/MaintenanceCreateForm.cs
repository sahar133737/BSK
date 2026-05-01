using System;
using System.Data;
using System.Windows.Forms;
using BGSK1.Services;
using BGSK1.UI;

namespace BGSK1
{
    public sealed class MaintenanceCreateForm : Form
    {
        private readonly ComboBox _cmbEquipment;
        private readonly ComboBox _cmbType;
        private readonly NumericUpDown _numPeriod;
        private readonly DateTimePicker _dtNext;
        private readonly ComboBox _cmbResponsible;

        public MaintenanceCreateForm()
        {
            ThemeHelper.ApplyForm(this, "Создание плана ТО");
            Width = 720;
            Height = 300;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            _cmbEquipment = new ComboBox { Left = 20, Top = 48, Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbType = new ComboBox { Left = 350, Top = 48, Width = 268, DropDownStyle = ComboBoxStyle.DropDown };
            var btnAddType = LookupUiHelper.CreateAddLookupButton(622, 48, "Добавить вид ТО", (s, e) => AddLookup(_cmbType, LookupDictionaryService.MaintenanceType, "Новый вид планового ТО"));
            _numPeriod = new NumericUpDown { Left = 20, Top = 106, Width = 140, Minimum = 1, Maximum = 365, Value = 30 };
            _dtNext = new DateTimePicker { Left = 170, Top = 106, Width = 160 };
            _cmbResponsible = new ComboBox { Left = 340, Top = 106, Width = 268, DropDownStyle = ComboBoxStyle.DropDownList };

            var btnCreate = new Button { Left = 20, Top = 190, Width = 320, Height = 34, Text = "Создать план ТО" };
            var btnCancel = new Button { Left = 350, Top = 190, Width = 330, Height = 34, Text = "Отмена" };
            ThemeHelper.StyleButton(btnCreate, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnCancel, ThemeHelper.Secondary);
            btnCreate.Click += BtnCreate_Click;
            btnCancel.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                LabelAt("Техника",20,20,120), LabelAt("Вид ТО",350,20,120), LabelAt("Период (дн.)",20,78,120),
                LabelAt("Дата следующего ТО",170,78,150), LabelAt("Ответственный",340,78,120),
                _cmbEquipment, _cmbType, btnAddType, _numPeriod, _dtNext, _cmbResponsible, btnCreate, btnCancel
            });

            Load += MaintenanceCreateForm_Load;
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

        private void MaintenanceCreateForm_Load(object sender, EventArgs e)
        {
            _cmbEquipment.DataSource = EquipmentService.GetEquipmentLookup();
            _cmbEquipment.DisplayMember = "DisplayName";
            _cmbEquipment.ValueMember = "Id";
            FillCombo(_cmbType, MaintenanceService.GetMaintenanceTypeLookup());
            FillUsersCombo(_cmbResponsible);
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            if (_cmbEquipment.SelectedValue == null || string.IsNullOrWhiteSpace(_cmbType.Text))
            {
                MessageBox.Show("Выберите технику и вид ТО.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MaintenanceService.AddPlan(Convert.ToInt32(_cmbEquipment.SelectedValue), _cmbType.Text.Trim(), Convert.ToInt32(_numPeriod.Value), _dtNext.Value.Date, _cmbResponsible.Text.Trim());
            DialogResult = DialogResult.OK;
            Close();
        }

        private static void FillCombo(ComboBox combo, DataTable source)
        {
            combo.Items.Clear();
            foreach (DataRow row in source.Rows)
            {
                combo.Items.Add(row["Value"].ToString());
            }
        }

        private static Label LabelAt(string text, int left, int top, int width)
        {
            return ThemeHelper.FormFieldLabel(text, left, top, width);
        }

        private static void FillUsersCombo(ComboBox combo)
        {
            combo.Items.Clear();
            var source = UserService.GetActiveUsersLookup();
            foreach (DataRow row in source.Rows)
            {
                combo.Items.Add(row["FullName"].ToString());
            }
        }
    }
}
