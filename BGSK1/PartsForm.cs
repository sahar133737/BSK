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

            var card = new GroupBox { Dock = DockStyle.Top, Height = 108, Text = "  Склад запчастей  " };
            var lblHint = new Label
            {
                Left = 12,
                Top = 22,
                Width = 920,
                Height = 28,
                ForeColor = ThemeHelper.MutedText,
                Text = "Выберите позицию в таблице. Номенклатура и остатки редактируются в формах «Добавить» / «Обновить»; «Списать 1» уменьшает остаток выбранной строки на единицу."
            };
            var btnAdd = new Button { Left = 12, Top = 52, Width = 100, Height = 30, Text = "Добавить" };
            var btnUpdate = new Button { Left = 116, Top = 52, Width = 100, Height = 30, Text = "Обновить" };
            var btnWriteOff = new Button { Left = 220, Top = 52, Width = 110, Height = 30, Text = "Списать 1" };
            var btnDelete = new Button { Left = 334, Top = 52, Width = 200, Height = 30, Text = "Удалить запись" };
            var btnHelp = new Button { Left = 540, Top = 52, Width = 100, Height = 30, Text = "Справка" };
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
            card.Controls.AddRange(new Control[] { lblHint, btnAdd, btnUpdate, btnWriteOff, btnDelete, btnHelp });

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
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            ThemeHelper.StyleGrid(_grid);
            _grid.CellDoubleClick += Grid_CellDoubleClick;

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

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            BtnUpdate_Click(sender, e);
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

    }
}
