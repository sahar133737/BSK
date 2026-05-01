using System;
using System.Data;
using System.Windows.Forms;
using BGSK1.Services;
using BGSK1.UI;

namespace BGSK1
{
    public sealed class EquipmentEditForm : Form
    {
        private readonly int _id;
        private readonly TextBox _txtInv;
        private readonly TextBox _txtName;
        private readonly ComboBox _cmbType;
        private readonly ComboBox _cmbLocation;
        private readonly ComboBox _cmbResponsible;
        private readonly ComboBox _cmbStatus;

        public EquipmentEditForm(int id, string inventoryNumber, string name, string typeName, string locationName, string responsiblePerson, string statusName)
        {
            _id = id;
            ThemeHelper.ApplyForm(this, "Редактирование техники");
            Width = 640;
            Height = 340;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            _txtInv = new TextBox { Left = 20, Top = 48, Width = 280, Text = inventoryNumber };
            _txtName = new TextBox { Left = 320, Top = 48, Width = 280, Text = name };
            _cmbType = new ComboBox { Left = 20, Top = 106, Width = 140, DropDownStyle = ComboBoxStyle.DropDown, Text = typeName };
            var btnAddType = LookupUiHelper.CreateAddLookupButton(162, 106, "Добавить тип техники", (s, e) => AddLookup(_cmbType, LookupDictionaryService.EquipmentType, "Новый тип техники"));
            _cmbLocation = new ComboBox { Left = 194, Top = 106, Width = 140, DropDownStyle = ComboBoxStyle.DropDown, Text = locationName };
            var btnAddLoc = LookupUiHelper.CreateAddLookupButton(336, 106, "Добавить кабинет / локацию", (s, e) => AddLookup(_cmbLocation, LookupDictionaryService.Location, "Новая локация (кабинет)"));
            _cmbResponsible = new ComboBox { Left = 368, Top = 106, Width = 150, DropDownStyle = ComboBoxStyle.DropDown, Text = responsiblePerson };
            var btnAddResp = LookupUiHelper.CreateAddLookupButton(522, 106, "Добавить ответственного", (s, e) => AddLookup(_cmbResponsible, LookupDictionaryService.EquipmentResponsible, "Новый ответственный (ФИО)"));
            _cmbStatus = new ComboBox { Left = 20, Top = 160, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbStatus.Items.AddRange(new[] { "В эксплуатации", "На диагностике", "Требует ремонта", "Списано" });
            _cmbStatus.Text = string.IsNullOrWhiteSpace(statusName) ? "В эксплуатации" : statusName;

            var btnSave = new Button { Left = 20, Top = 240, Width = 280, Height = 34, Text = "Сохранить" };
            var btnCancel = new Button { Left = 320, Top = 240, Width = 280, Height = 34, Text = "Отмена" };
            ThemeHelper.StyleButton(btnSave, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnCancel, ThemeHelper.Secondary);
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                LabelAt("Инв. номер",20,20,120), LabelAt("Наименование",320,20,120),
                LabelAt("Тип",20,78,80), LabelAt("Локация",194,78,90), LabelAt("Ответственный",368,78,120), LabelAt("Статус",20,132,90),
                _txtInv,_txtName,_cmbType,btnAddType,_cmbLocation,btnAddLoc,_cmbResponsible,btnAddResp,_cmbStatus,btnSave,btnCancel
            });

            Load += (s, e) =>
            {
                FillCombo(_cmbType, EquipmentService.GetTypeLookup());
                FillCombo(_cmbLocation, EquipmentService.GetLocationLookup());
                FillCombo(_cmbResponsible, EquipmentService.GetResponsibleLookup());
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
            FillCombo(_cmbResponsible, EquipmentService.GetResponsibleLookup());
            combo.Text = value;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtInv.Text) || string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("Заполните инвентарный номер и наименование.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            EquipmentService.UpdateEquipment(_id, _txtInv.Text.Trim(), _txtName.Text.Trim(), _cmbType.Text.Trim(), _cmbLocation.Text.Trim(), _cmbResponsible.Text.Trim(), _cmbStatus.Text);
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
    }
}
