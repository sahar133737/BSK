using System;
using System.Windows.Forms;
using BGSK1.Services;
using BGSK1.UI;

namespace BGSK1
{
    public sealed class PartEditForm : Form
    {
        private readonly int _id;
        private readonly TextBox _txtName;
        private readonly TextBox _txtNumber;
        private readonly NumericUpDown _numQty;
        private readonly NumericUpDown _numMin;

        public PartEditForm(int id, string partName, string partNumber, int quantity, int minQuantity)
        {
            _id = id;
            ThemeHelper.ApplyForm(this, "Редактирование запчасти");
            Width = 560;
            Height = 270;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            _txtName = new TextBox { Left = 20, Top = 48, Width = 250, Text = partName ?? string.Empty };
            _txtNumber = new TextBox { Left = 280, Top = 48, Width = 250, Text = partNumber ?? string.Empty };
            _numQty = new NumericUpDown { Left = 20, Top = 106, Width = 120, Minimum = 0, Maximum = 100000, Value = quantity < 0 ? 0 : quantity };
            _numMin = new NumericUpDown { Left = 150, Top = 106, Width = 120, Minimum = 0, Maximum = 100000, Value = minQuantity < 0 ? 0 : minQuantity };

            var btnSave = new Button { Left = 20, Top = 170, Width = 250, Height = 34, Text = "Сохранить" };
            var btnCancel = new Button { Left = 280, Top = 170, Width = 250, Height = 34, Text = "Отмена" };
            ThemeHelper.StyleButton(btnSave, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnCancel, ThemeHelper.Secondary);
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                LabelAt("Наименование",20,20,120), LabelAt("Артикул",280,20,120), LabelAt("Остаток",20,78,80), LabelAt("Мин. остаток",150,78,100),
                _txtName, _txtNumber, _numQty, _numMin, btnSave, btnCancel
            });
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("Укажите наименование запчасти.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SparePartService.UpdatePart(_id, _txtName.Text.Trim(), _txtNumber.Text.Trim(), Convert.ToInt32(_numQty.Value), Convert.ToInt32(_numMin.Value), "шт");
            DialogResult = DialogResult.OK;
            Close();
        }

        private static Label LabelAt(string text, int left, int top, int width)
        {
            return ThemeHelper.FormFieldLabel(text, left, top, width);
        }
    }
}
