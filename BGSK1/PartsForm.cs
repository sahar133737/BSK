using System;
using System.Data;
using System.Windows.Forms;
using BGSK1.Services;
using BGSK1.UI;

namespace BGSK1
{
    public sealed class PartsForm : Form
    {
        private readonly DataGridView _grid;
        private readonly CheckBox _chkLowStock;
        private readonly CheckBox _chkSoonStock;
        private readonly TextBox _txtSearch;
        private readonly TextBox _txtName;
        private readonly TextBox _txtNumber;
        private readonly NumericUpDown _numQty;
        private readonly NumericUpDown _numMin;

        public PartsForm()
        {
            ThemeHelper.ApplyForm(this, "Склад запчастей");
            Width = 1000;
            Height = 620;
            MinimumSize = new System.Drawing.Size(920, 560);
            if (!RolePermissionService.HasPermission("module.parts"))
            {
                Shown += (s, e) => { MessageBox.Show("Нет доступа к модулю.", "Доступ запрещен", MessageBoxButtons.OK, MessageBoxIcon.Warning); Close(); };
            }

            var card = new GroupBox { Dock = DockStyle.Top, Height = 116, Text = "  Карточка запчасти  " };
            _txtName = new TextBox { Left = 12, Top = 48, Width = 250 };
            _txtNumber = new TextBox { Left = 268, Top = 48, Width = 160 };
            _numQty = new NumericUpDown { Left = 434, Top = 48, Width = 110, Minimum = 0, Maximum = 100000, Value = 1 };
            _numMin = new NumericUpDown { Left = 550, Top = 48, Width = 110, Minimum = 0, Maximum = 100000, Value = 1 };
            var btnAdd = new Button { Left = 666, Top = 46, Width = 100, Height = 30, Text = "Добавить" };
            var btnUpdate = new Button { Left = 770, Top = 46, Width = 100, Height = 30, Text = "Обновить" };
            var btnWriteOff = new Button { Left = 874, Top = 46, Width = 110, Height = 30, Text = "Списать 1" };
            var btnDelete = new Button { Left = 770, Top = 80, Width = 214, Height = 26, Text = "Удалить запись" };
            var btnHelp = new Button { Left = 666, Top = 80, Width = 100, Height = 26, Text = "Справка" };
            ThemeHelper.StyleButton(btnAdd, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnUpdate, ThemeHelper.Secondary);
            ThemeHelper.StyleButton(btnWriteOff, ThemeHelper.Danger);
            ThemeHelper.StyleButton(btnDelete, ThemeHelper.Danger);
            ThemeHelper.StyleButton(btnHelp, ThemeHelper.Accent);
            btnAdd.Click += BtnAdd_Click;
            btnUpdate.Click += BtnUpdate_Click;
            btnWriteOff.Click += BtnWriteOff_Click;
            btnDelete.Click += BtnDelete_Click;
            btnHelp.Click += (s, e) => ModuleHelpProvider.ShowHelp("parts", this);
            card.Controls.AddRange(new Control[]
            {
                LabelAt("Наименование",12,20,250), LabelAt("Артикул",268,20,160), LabelAt("Остаток",434,20,110), LabelAt("Мин. остаток",550,20,110),
                _txtName,_txtNumber,_numQty,_numMin,btnAdd,btnUpdate,btnWriteOff,btnDelete,btnHelp
            });

            var top = new Panel { Dock = DockStyle.Top, Height = 46 };
            _chkLowStock = new CheckBox { Left = 12, Top = 14, Width = 252, Text = "Ниже или на минимуме" };
            _chkSoonStock = new CheckBox { Left = 268, Top = 14, Width = 242, Text = "Только скоро минимум" };
            _txtSearch = new TextBox { Left = 514, Top = 10, Width = 200 };
            var btnSearch = new Button { Left = 718, Top = 9, Width = 95, Height = 28, Text = "Поиск" };
            var btnReset = new Button { Left = 817, Top = 9, Width = 95, Height = 28, Text = "Сброс" };
            _chkLowStock.CheckedChanged += ChkPartsFilter_Changed;
            _chkSoonStock.CheckedChanged += ChkPartsFilter_Changed;
            btnSearch.Click += (s, e) => LoadData();
            btnReset.Click += (s, e) =>
            {
                _txtSearch.Clear();
                _chkLowStock.Checked = false;
                _chkSoonStock.Checked = false;
                LoadData();
            };

            top.Controls.Add(_chkLowStock);
            top.Controls.Add(_chkSoonStock);
            top.Controls.Add(_txtSearch);
            top.Controls.Add(btnSearch);
            top.Controls.Add(btnReset);

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };
            ThemeHelper.StyleGrid(_grid);
            _grid.SelectionChanged += Grid_SelectionChanged;

            Controls.Add(_grid);
            Controls.Add(top);
            Controls.Add(card);
            ModuleHelpProvider.BindF11(this, "parts");
            Load += (s, e) => LoadData();
        }

        private void ChkPartsFilter_Changed(object sender, EventArgs e)
        {
            if (_chkLowStock.Checked && _chkSoonStock.Checked)
            {
                if (sender == _chkLowStock)
                    _chkSoonStock.Checked = false;
                else
                    _chkLowStock.Checked = false;
            }

            LoadData();
        }

        private void LoadData()
        {
            var table = _chkLowStock.Checked ? SparePartService.GetLowStock()
                : _chkSoonStock.Checked ? SparePartService.GetLowStockSoon()
                : SparePartService.GetParts();
            if (!string.IsNullOrWhiteSpace(_txtSearch.Text))
            {
                var view = table.DefaultView;
                var s = _txtSearch.Text.Trim().Replace("'", "''");
                view.RowFilter = $"PartName LIKE '%{s}%' OR PartNumber LIKE '%{s}%'";
                _grid.DataSource = view.ToTable();
            }
            else
            {
                _grid.DataSource = table;
            }
            GridHeaderMap.Apply(_grid, "parts", "Id");
        }

        private void BtnAdd_Click(object sender, System.EventArgs e)
        {
            using (var dialog = new PartCreateForm())
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    LoadData();
                }
            }
        }

        private void BtnUpdate_Click(object sender, System.EventArgs e)
        {
            if (_grid.CurrentRow == null || !_grid.Columns.Contains("Id"))
            {
                MessageBox.Show("Выберите запись для обновления.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = System.Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            using (var dialog = new PartEditForm(
                id,
                _grid.CurrentRow.Cells["PartName"]?.Value?.ToString() ?? string.Empty,
                _grid.CurrentRow.Cells["PartNumber"]?.Value?.ToString() ?? string.Empty,
                System.Convert.ToInt32(_grid.CurrentRow.Cells["QuantityInStock"]?.Value ?? 0),
                System.Convert.ToInt32(_grid.CurrentRow.Cells["MinQuantity"]?.Value ?? 0)))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    LoadData();
                }
            }
        }

        private void BtnWriteOff_Click(object sender, System.EventArgs e)
        {
            if (_grid.CurrentRow == null || !_grid.Columns.Contains("Id"))
            {
                MessageBox.Show("Выберите запчасть для списания.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SparePartService.WriteOffPart(System.Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value), 1);
            LoadData();
        }

        private void BtnDelete_Click(object sender, System.EventArgs e)
        {
            if (_grid.CurrentRow == null || !_grid.Columns.Contains("Id"))
            {
                MessageBox.Show("Выберите запчасть для удаления.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Удалить выбранную запчасть?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                SparePartService.DeletePart(System.Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value));
                LoadData();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Удаление невозможно", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Grid_SelectionChanged(object sender, System.EventArgs e)
        {
            if (_grid.CurrentRow == null)
            {
                return;
            }

            _txtName.Text = _grid.CurrentRow.Cells["PartName"]?.Value?.ToString() ?? string.Empty;
            _txtNumber.Text = _grid.CurrentRow.Cells["PartNumber"]?.Value?.ToString() ?? string.Empty;
            if (_grid.Columns.Contains("QuantityInStock")) _numQty.Value = System.Convert.ToDecimal(_grid.CurrentRow.Cells["QuantityInStock"]?.Value ?? 0);
            if (_grid.Columns.Contains("MinQuantity")) _numMin.Value = System.Convert.ToDecimal(_grid.CurrentRow.Cells["MinQuantity"]?.Value ?? 0);
        }

        private static Label LabelAt(string text, int left, int top, int width)
        {
            return ThemeHelper.FormFieldLabel(text, left, top, width);
        }
    }
}
