using System;
using System.Data;
using System.Windows.Forms;
using BGSK1.Services;
using BGSK1.UI;

namespace BGSK1
{
    public sealed class EquipmentCreateForm : Form
    {
        private readonly TextBox _txtInv;
        private readonly TextBox _txtName;
        private readonly ComboBox _cmbType;
        private readonly ComboBox _cmbLocation;
        private readonly ComboBox _cmbResponsible;

        public EquipmentCreateForm()
        {
            ThemeHelper.ApplyForm(this, "Создание техники");
            Width = 560;
            Height = 330;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            _txtInv = new TextBox { Left = 20, Top = 48, Width = 240 };
            _txtName = new TextBox { Left = 280, Top = 48, Width = 240 };
            _cmbType = new ComboBox { Left = 20, Top = 106, Width = 130, DropDownStyle = ComboBoxStyle.DropDown };
            var btnAddType = LookupUiHelper.CreateAddLookupButton(152, 106, "Добавить тип техники", (s, e) => AddLookup(_cmbType, LookupDictionaryService.EquipmentType, "Новый тип техники"));
            _cmbLocation = new ComboBox { Left = 184, Top = 106, Width = 130, DropDownStyle = ComboBoxStyle.DropDown };
            var btnAddLoc = LookupUiHelper.CreateAddLookupButton(316, 106, "Добавить кабинет / локацию", (s, e) => AddLookup(_cmbLocation, LookupDictionaryService.Location, "Новая локация (кабинет)"));
            _cmbResponsible = new ComboBox { Left = 348, Top = 106, Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };

            var btnCreate = new Button { Left = 20, Top = 218, Width = 240, Height = 34, Text = "Создать" };
            var btnCancel = new Button { Left = 280, Top = 218, Width = 240, Height = 34, Text = "Отмена" };
            ThemeHelper.StyleButton(btnCreate, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnCancel, ThemeHelper.Secondary);
            btnCreate.Click += BtnCreate_Click;
            btnCancel.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                LabelAt("Инв. номер", 20, 20, 120), LabelAt("Наименование", 280, 20, 120),
                LabelAt("Тип", 20, 78, 80), LabelAt("Локация", 184, 78, 90), LabelAt("Ответственный", 348, 78, 120),
                _txtInv, _txtName, _cmbType, btnAddType, _cmbLocation, btnAddLoc, _cmbResponsible, btnCreate, btnCancel
            });

            Load += (s, e) =>
            {
                FillCombo(_cmbType, EquipmentService.GetTypeLookup());
                FillCombo(_cmbLocation, EquipmentService.GetLocationLookup());
                FillUsersCombo(_cmbResponsible);
            };
        }

        private void AddLookup(ComboBox combo, string category, string title)
        {
            if (!LookupUiHelper.TryPromptAndAddValue(this, category, title, out var value))
            {
                return;
            }

            FillCombo(_cmbType, EquipmentService.GetTypeLookup());
            FillCombo(_cmbLocation, EquipmentService.GetLocationLookup());
            combo.Text = value;
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtInv.Text) || string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("Заполните инвентарный номер и наименование.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            EquipmentService.AddEquipment(_txtInv.Text.Trim(), _txtName.Text.Trim(), _cmbType.Text.Trim(), _cmbLocation.Text.Trim(), _cmbResponsible.Text.Trim());
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
