using System;
using System.Data;
using System.Windows.Forms;
using BGSK1.Services;
using BGSK1.UI;

namespace BGSK1
{
    public sealed class MaintenanceForm : Form
    {
        private readonly DataGridView _grid;
        private readonly CheckBox _chkOverdue;
        private readonly TextBox _txtSearch;

        public MaintenanceForm()
        {
            ThemeHelper.ApplyForm(this, "Плановое ТО");
            Width = 1100;
            Height = 650;
            MinimumSize = new System.Drawing.Size(980, 560);
            AutoScroll = true;
            if (!RolePermissionService.HasPermission("module.maintenance"))
            {
                Shown += (s, e) => { MessageBox.Show("Нет доступа к модулю.", "Доступ запрещен", MessageBoxButtons.OK, MessageBoxIcon.Warning); Close(); };
            }

            var card = new GroupBox { Dock = DockStyle.Top, Height = 100, Text = "  Плановое ТО  " };
            var lblHint = new Label
            {
                Left = 12,
                Top = 22,
                Width = 900,
                Height = 32,
                ForeColor = ThemeHelper.MutedText,
                Text = "Выберите строку в таблице. Данные вносятся в отдельных окнах «Добавить» / «Обновить»."
            };
            var btnAdd = new Button { Left = 12, Top = 56, Width = 120, Height = 30, Text = "Добавить" };
            var btnUpdate = new Button { Left = 138, Top = 56, Width = 120, Height = 30, Text = "Обновить" };
            var btnDelete = new Button { Left = 266, Top = 56, Width = 200, Height = 30, Text = "Удалить запись" };
            var btnHelp = new Button { Left = 472, Top = 56, Width = 120, Height = 30, Text = "Справка" };
            ThemeHelper.StyleButton(btnAdd, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnUpdate, ThemeHelper.Secondary);
            ThemeHelper.StyleButton(btnDelete, ThemeHelper.Danger);
            ThemeHelper.StyleButton(btnHelp, ThemeHelper.Accent);
            btnAdd.Click += BtnAdd_Click;
            btnUpdate.Click += BtnUpdate_Click;
            btnDelete.Click += BtnDelete_Click;
            btnHelp.Click += (s, e) => ModuleHelpProvider.ShowHelp("maintenance", this);
            card.Controls.AddRange(new Control[] { lblHint, btnAdd, btnUpdate, btnDelete, btnHelp });

            var top = new Panel { Dock = DockStyle.Top, Height = 46 };
            _chkOverdue = new CheckBox { Left = 12, Top = 14, Width = 280, Text = "Только просроченные планы ТО" };
            _txtSearch = new TextBox { Left = 298, Top = 11, Width = 230 };
            var btnSearch = new Button { Left = 534, Top = 9, Width = 100, Height = 28, Text = "Поиск" };
            var btnReset = new Button { Left = 638, Top = 9, Width = 100, Height = 28, Text = "Сброс" };
            _chkOverdue.CheckedChanged += (s, e) => LoadData();
            btnSearch.Click += (s, e) => LoadData();
            btnReset.Click += (s, e) => { _txtSearch.Clear(); _chkOverdue.Checked = false; LoadData(); };
            top.Controls.Add(_chkOverdue);
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
            ModuleHelpProvider.BindF11(this, "maintenance");
            Load += (s, e) => LoadData();
        }

        private void LoadData()
        {
            var table = _chkOverdue.Checked ? DomainReportService.GetOverdueMaintenance() : MaintenanceService.GetPlans();
            if (!_chkOverdue.Checked && !string.IsNullOrWhiteSpace(_txtSearch.Text))
            {
                var view = table.DefaultView;
                var s = _txtSearch.Text.Trim().Replace("'", "''");
                view.RowFilter = $"EquipmentName LIKE '%{s}%' OR InventoryNumber LIKE '%{s}%' OR MaintenanceType LIKE '%{s}%' OR ResponsiblePerson LIKE '%{s}%'";
                _grid.DataSource = view.ToTable();
            }
            else
            {
                _grid.DataSource = table;
            }

            GridHeaderMap.Apply(_grid, "maintenance", "Id", "IsActive", "EquipmentId");
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
            using (var dialog = new MaintenanceCreateForm())
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    LoadData();
                }
            }
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (_grid.CurrentRow == null || !_grid.Columns.Contains("Id"))
            {
                MessageBox.Show("Выберите план ТО для обновления.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            var equipmentId = ResolveEquipmentIdForCurrentRow();
            if (equipmentId <= 0)
            {
                MessageBox.Show("Не удалось определить технику для плана ТО.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DateTime nextDate;
            DateTime.TryParse(_grid.CurrentRow.Cells["NextDate"]?.Value?.ToString(), out nextDate);
            using (var dialog = new MaintenanceEditForm(
                id,
                equipmentId,
                _grid.CurrentRow.Cells["MaintenanceType"]?.Value?.ToString() ?? string.Empty,
                Convert.ToInt32(_grid.CurrentRow.Cells["PeriodDays"]?.Value ?? 30),
                nextDate == DateTime.MinValue ? DateTime.Today : nextDate,
                _grid.CurrentRow.Cells["ResponsiblePerson"]?.Value?.ToString() ?? string.Empty,
                Convert.ToBoolean(_grid.CurrentRow.Cells["IsActive"]?.Value ?? true)))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    LoadData();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_grid.CurrentRow == null || !_grid.Columns.Contains("Id"))
            {
                MessageBox.Show("Выберите план ТО для удаления.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Удалить выбранный план ТО и связанную историю?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            MaintenanceService.DeletePlan(Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value));
            LoadData();
        }

        private int ResolveEquipmentIdForCurrentRow()
        {
            if (_grid.Columns.Contains("EquipmentId") && _grid.CurrentRow != null)
            {
                var v = _grid.CurrentRow.Cells["EquipmentId"].Value;
                if (v != null && v != DBNull.Value)
                {
                    return Convert.ToInt32(v);
                }
            }

            var displayInv = _grid.CurrentRow?.Cells["InventoryNumber"]?.Value?.ToString() ?? string.Empty;
            var lookup = EquipmentService.GetEquipmentLookup();
            foreach (DataRow row in lookup.Rows)
            {
                var display = row["DisplayName"]?.ToString() ?? string.Empty;
                if (display.StartsWith(displayInv + " - ", StringComparison.OrdinalIgnoreCase))
                {
                    return Convert.ToInt32(row["Id"]);
                }
            }

            return 0;
        }
    }
}
