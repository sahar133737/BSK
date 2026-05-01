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
        private readonly ComboBox _cmbEquipment;
        private readonly ComboBox _cmbType;
        private readonly NumericUpDown _numPeriod;
        private readonly DateTimePicker _dtNext;
        private readonly ComboBox _cmbResponsible;
        private readonly CheckBox _chkActive;

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

            var card = new GroupBox { Dock = DockStyle.Top, Height = 128, Text = "  Карточка плана ТО  " };
            _cmbEquipment = new ComboBox { Left = 12, Top = 48, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbType = new ComboBox { Left = 278, Top = 48, Width = 120, DropDownStyle = ComboBoxStyle.DropDown };
            var btnAddMaintType = LookupUiHelper.CreateAddLookupButton(400, 48, "Добавить вид ТО", (s, e) => AddLookupAndRefresh(_cmbType, LookupDictionaryService.MaintenanceType, "Новый вид планового ТО"));
            _numPeriod = new NumericUpDown { Left = 434, Top = 48, Width = 90, Minimum = 1, Maximum = 365, Value = 30 };
            _dtNext = new DateTimePicker { Left = 530, Top = 48, Width = 150 };
            _cmbResponsible = new ComboBox { Left = 686, Top = 48, Width = 145, DropDownStyle = ComboBoxStyle.DropDown };
            var btnAddMaintResp = LookupUiHelper.CreateAddLookupButton(835, 48, "Добавить ответственного за ТО", (s, e) => AddLookupAndRefresh(_cmbResponsible, LookupDictionaryService.MaintenanceResponsible, "Новый ответственный за ТО (ФИО)"));
            _chkActive = new CheckBox { Left = 870, Top = 51, Width = 80, Text = "Активен", Checked = true };
            var btnAdd = new Button { Left = 12, Top = 82, Width = 120, Height = 28, Text = "Добавить" };
            var btnUpdate = new Button { Left = 138, Top = 82, Width = 120, Height = 28, Text = "Обновить" };
            var btnDelete = new Button { Left = 266, Top = 82, Width = 200, Height = 28, Text = "Удалить запись" };
            ThemeHelper.StyleButton(btnAdd, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnUpdate, ThemeHelper.Secondary);
            ThemeHelper.StyleButton(btnDelete, ThemeHelper.Danger);
            btnAdd.Click += BtnAdd_Click;
            btnUpdate.Click += BtnUpdate_Click;
            btnDelete.Click += BtnDelete_Click;
            card.Controls.AddRange(new Control[]
            {
                LabelAt("Техника",12,20,260), LabelAt("Вид ТО",278,20,120), LabelAt("Период (дн.)",434,20,90),
                LabelAt("Следующая дата",530,20,150), LabelAt("Ответственный",686,20,145),
                _cmbEquipment,_cmbType,btnAddMaintType,_numPeriod,_dtNext,_cmbResponsible,btnAddMaintResp,_chkActive,btnAdd,btnUpdate,btnDelete
            });

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

            _cmbEquipment.DataSource = EquipmentService.GetEquipmentLookup();
            _cmbEquipment.DisplayMember = "DisplayName";
            _cmbEquipment.ValueMember = "Id";
            BindLookups();

            GridHeaderMap.Apply(_grid, "maintenance", "Id", "IsActive");
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

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (_grid.CurrentRow == null)
            {
                return;
            }

            if (_grid.Columns.Contains("MaintenanceType")) _cmbType.Text = _grid.CurrentRow.Cells["MaintenanceType"]?.Value?.ToString() ?? string.Empty;
            if (_grid.Columns.Contains("PeriodDays")) _numPeriod.Value = Convert.ToDecimal(_grid.CurrentRow.Cells["PeriodDays"]?.Value ?? 30);
            if (_grid.Columns.Contains("NextDate"))
            {
                DateTime dt;
                if (DateTime.TryParse(_grid.CurrentRow.Cells["NextDate"]?.Value?.ToString(), out dt)) _dtNext.Value = dt;
            }
            if (_grid.Columns.Contains("ResponsiblePerson")) _cmbResponsible.Text = _grid.CurrentRow.Cells["ResponsiblePerson"]?.Value?.ToString() ?? string.Empty;
            if (_grid.Columns.Contains("IsActive")) _chkActive.Checked = Convert.ToBoolean(_grid.CurrentRow.Cells["IsActive"]?.Value ?? true);
        }

        private static Label LabelAt(string text, int left, int top, int width)
        {
            return ThemeHelper.FormFieldLabel(text, left, top, width);
        }

        private int ResolveEquipmentIdForCurrentRow()
        {
            var displayInv = _grid.CurrentRow.Cells["InventoryNumber"]?.Value?.ToString() ?? string.Empty;
            var lookup = EquipmentService.GetEquipmentLookup();
            foreach (DataRow row in lookup.Rows)
            {
                var display = row["DisplayName"]?.ToString() ?? string.Empty;
                if (display.StartsWith(displayInv + " - ", StringComparison.OrdinalIgnoreCase))
                {
                    return Convert.ToInt32(row["Id"]);
                }
            }
            return Convert.ToInt32(_cmbEquipment.SelectedValue);
        }

        private void BindLookups()
        {
            FillCombo(_cmbType, MaintenanceService.GetMaintenanceTypeLookup());
            FillCombo(_cmbResponsible, MaintenanceService.GetMaintenanceResponsibleLookup());
        }

        private void AddLookupAndRefresh(ComboBox combo, string category, string dialogTitle)
        {
            if (!LookupUiHelper.TryPromptAndAddValue(this, category, dialogTitle, out var value))
            {
                return;
            }

            BindLookups();
            combo.Text = value;
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
    }
}
