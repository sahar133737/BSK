using System;
using System.Data;
using System.Windows.Forms;
using BGSK1.Services;
using BGSK1.UI;

namespace BGSK1
{
    public sealed class EquipmentForm : Form
    {
        private readonly DataGridView _grid;
        private readonly TextBox _txtSearch;

        public EquipmentForm()
        {
            ThemeHelper.ApplyForm(this, "Техника");
            Width = 1200;
            Height = 700;
            MinimumSize = new System.Drawing.Size(980, 620);
            if (!RolePermissionService.HasPermission("module.equipment"))
            {
                Shown += (s, e) => { MessageBox.Show("Нет доступа к модулю.", "Доступ запрещен", MessageBoxButtons.OK, MessageBoxIcon.Warning); Close(); };
            }

            var card = new GroupBox { Dock = DockStyle.Top, Height = 100, Text = "  Техника  " };
            var lblHint = new Label
            {
                Left = 16,
                Top = 22,
                Width = 980,
                Height = 32,
                ForeColor = ThemeHelper.MutedText,
                Text = "Выберите строку в таблице. «Добавить» и «Обновить» открывают форму ввода; типы и локации задаются внутри этих форм."
            };
            var btnAdd = new Button { Left = 16, Top = 56, Width = 120, Height = 30, Text = "Добавить" };
            var btnUpdate = new Button { Left = 142, Top = 56, Width = 120, Height = 30, Text = "Обновить" };
            var btnDelete = new Button { Left = 268, Top = 56, Width = 200, Height = 30, Text = "Удалить запись" };
            var btnHelp = new Button { Left = 474, Top = 56, Width = 120, Height = 30, Text = "Справка" };
            ThemeHelper.StyleButton(btnAdd, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnUpdate, ThemeHelper.Secondary);
            ThemeHelper.StyleButton(btnDelete, ThemeHelper.Danger);
            ThemeHelper.StyleButton(btnHelp, ThemeHelper.Accent);
            btnAdd.Click += BtnAdd_Click;
            btnUpdate.Click += BtnUpdate_Click;
            btnDelete.Click += BtnDelete_Click;
            btnHelp.Click += (s, e) => ModuleHelpProvider.ShowHelp("equipment", this);
            card.Controls.AddRange(new Control[] { lblHint, btnAdd, btnUpdate, btnDelete, btnHelp });

            var top = new Panel { Dock = DockStyle.Top, Height = 48 };
            _txtSearch = new TextBox { Left = 12, Top = 12, Width = 320 };
            var btnSearch = new Button { Left = 338, Top = 10, Width = 100, Height = 28, Text = "Поиск" };
            var btnReset = new Button { Left = 442, Top = 10, Width = 100, Height = 28, Text = "Сброс" };
            btnSearch.Click += (s, e) => ApplySearch();
            btnReset.Click += (s, e) => { _txtSearch.Clear(); LoadData(); };
            top.Controls.AddRange(new Control[] { _txtSearch, btnSearch, btnReset });

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
            ModuleHelpProvider.BindF11(this, "equipment");
            Load += (s, e) => LoadData();
        }

        private void LoadData()
        {
            _grid.DataSource = EquipmentService.GetEquipment();
            ConfigureGrid();
        }

        private void ApplySearch()
        {
            var table = EquipmentService.GetEquipment();
            var view = table.DefaultView;
            var search = _txtSearch.Text.Trim().Replace("'", "''");
            if (!string.IsNullOrWhiteSpace(search))
            {
                view.RowFilter = $"InventoryNumber LIKE '%{search}%' OR Name LIKE '%{search}%' OR TypeName LIKE '%{search}%' OR LocationName LIKE '%{search}%'";
            }
            _grid.DataSource = view.ToTable();
            ConfigureGrid();
        }

        private void ConfigureGrid()
        {
            GridHeaderMap.Apply(_grid, "equipment", "Id");
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            BtnUpdate_Click(sender, e);
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var dialog = new EquipmentCreateForm())
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    LoadData();
                }
            }
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (_grid.CurrentRow == null)
            {
                MessageBox.Show("Выберите запись в таблице для обновления.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            var status = _grid.CurrentRow.Cells["StatusName"]?.Value?.ToString() ?? "В эксплуатации";
            using (var dialog = new EquipmentEditForm(
                id,
                _grid.CurrentRow.Cells["InventoryNumber"]?.Value?.ToString() ?? string.Empty,
                _grid.CurrentRow.Cells["Name"]?.Value?.ToString() ?? string.Empty,
                _grid.CurrentRow.Cells["TypeName"]?.Value?.ToString() ?? string.Empty,
                _grid.CurrentRow.Cells["LocationName"]?.Value?.ToString() ?? string.Empty,
                _grid.CurrentRow.Cells["ResponsiblePerson"]?.Value?.ToString() ?? string.Empty,
                status))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    LoadData();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_grid.CurrentRow == null)
            {
                MessageBox.Show("Выберите запись для удаления.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Удалить запись техники без возможности восстановления?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                var id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
                EquipmentService.DeleteEquipmentPermanently(id);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Удаление невозможно", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

    }
}
