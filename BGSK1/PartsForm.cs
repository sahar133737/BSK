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
            if (!RolePermissionService.HasPermission("module.parts"))
            {
                Shown += (s, e) => { MessageBox.Show("Нет доступа к модулю.", "Доступ запрещен", MessageBoxButtons.OK, MessageBoxIcon.Warning); Close(); };
            }

            var card = new GroupBox { Dock = DockStyle.Top, Height = 110, Text = "  Карточка запчасти  " };
            _txtName = new TextBox { Left = 12, Top = 45, Width = 250 };
            _txtNumber = new TextBox { Left = 268, Top = 45, Width = 160 };
            _numQty = new NumericUpDown { Left = 434, Top = 45, Width = 110, Minimum = 0, Maximum = 100000, Value = 1 };
            _numMin = new NumericUpDown { Left = 550, Top = 45, Width = 110, Minimum = 0, Maximum = 100000, Value = 1 };
            var btnAdd = new Button { Left = 666, Top = 42, Width = 100, Height = 30, Text = "Добавить" };
            var btnUpdate = new Button { Left = 770, Top = 42, Width = 100, Height = 30, Text = "Обновить" };
            var btnWriteOff = new Button { Left = 874, Top = 42, Width = 110, Height = 30, Text = "Списать 1" };
            ThemeHelper.StyleButton(btnAdd, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnUpdate, ThemeHelper.Secondary);
            ThemeHelper.StyleButton(btnWriteOff, ThemeHelper.Danger);
            btnAdd.Click += BtnAdd_Click;
            btnUpdate.Click += BtnUpdate_Click;
            btnWriteOff.Click += BtnWriteOff_Click;
            card.Controls.AddRange(new Control[]
            {
                LabelAt("Наименование",12,27,250), LabelAt("Артикул",268,27,160), LabelAt("Остаток",434,27,110), LabelAt("Мин. остаток",550,27,110),
                _txtName,_txtNumber,_numQty,_numMin,btnAdd,btnUpdate,btnWriteOff
            });

            var top = new Panel { Dock = DockStyle.Top, Height = 46 };
            _chkLowStock = new CheckBox { Left = 12, Top = 14, Width = 300, Text = "Только позиции ниже минимума" };
            _txtSearch = new TextBox { Left = 318, Top = 10, Width = 220 };
            var btnSearch = new Button { Left = 542, Top = 9, Width = 95, Height = 28, Text = "Поиск" };
            var btnReset = new Button { Left = 641, Top = 9, Width = 95, Height = 28, Text = "Сброс" };
            _chkLowStock.CheckedChanged += (s, e) => LoadData();
            btnSearch.Click += (s, e) => LoadData();
            btnReset.Click += (s, e) => { _txtSearch.Clear(); _chkLowStock.Checked = false; LoadData(); };
            top.Controls.Add(_chkLowStock);
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
            Load += (s, e) => LoadData();
        }

        private void LoadData()
        {
            var table = _chkLowStock.Checked ? SparePartService.GetLowStock() : SparePartService.GetParts();
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
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("Укажите название запчасти.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SparePartService.AddPart(_txtName.Text.Trim(), _txtNumber.Text.Trim(), System.Convert.ToInt32(_numQty.Value), System.Convert.ToInt32(_numMin.Value), "шт");
            LoadData();
        }

        private void BtnUpdate_Click(object sender, System.EventArgs e)
        {
            if (_grid.CurrentRow == null || !_grid.Columns.Contains("Id"))
            {
                MessageBox.Show("Выберите запись для обновления.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = System.Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            SparePartService.UpdatePart(id, _txtName.Text.Trim(), _txtNumber.Text.Trim(), System.Convert.ToInt32(_numQty.Value), System.Convert.ToInt32(_numMin.Value), "шт");
            LoadData();
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
            return new Label { Text = text, Left = left, Top = top, Width = width };
        }
    }
}
