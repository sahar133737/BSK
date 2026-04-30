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
        private readonly TextBox _txtType;
        private readonly NumericUpDown _numPeriod;
        private readonly DateTimePicker _dtNext;
        private readonly TextBox _txtResponsible;
        private readonly CheckBox _chkActive;

        public MaintenanceForm()
        {
            ThemeHelper.ApplyForm(this, "Плановое ТО");
            Width = 1100;
            Height = 650;
            if (!RolePermissionService.HasPermission("module.maintenance"))
            {
                Shown += (s, e) => { MessageBox.Show("Нет доступа к модулю.", "Доступ запрещен", MessageBoxButtons.OK, MessageBoxIcon.Warning); Close(); };
            }

            var card = new GroupBox { Dock = DockStyle.Top, Height = 118, Text = "  Карточка плана ТО  " };
            _cmbEquipment = new ComboBox { Left = 12, Top = 45, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            _txtType = new TextBox { Left = 278, Top = 45, Width = 180 };
            _numPeriod = new NumericUpDown { Left = 464, Top = 45, Width = 90, Minimum = 1, Maximum = 365, Value = 30 };
            _dtNext = new DateTimePicker { Left = 560, Top = 45, Width = 150 };
            _txtResponsible = new TextBox { Left = 716, Top = 45, Width = 170 };
            _chkActive = new CheckBox { Left = 892, Top = 49, Width = 80, Text = "Активен", Checked = true };
            var btnAdd = new Button { Left = 974, Top = 42, Width = 80, Height = 30, Text = "Добавить" };
            var btnUpdate = new Button { Left = 1058, Top = 42, Width = 80, Height = 30, Text = "Обновить" };
            ThemeHelper.StyleButton(btnAdd, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnUpdate, ThemeHelper.Secondary);
            btnAdd.Click += BtnAdd_Click;
            btnUpdate.Click += BtnUpdate_Click;
            card.Controls.AddRange(new Control[]
            {
                LabelAt("Техника",12,27,260), LabelAt("Вид ТО",278,27,180), LabelAt("Период (дн.)",464,27,90),
                LabelAt("Следующая дата",560,27,150), LabelAt("Ответственный",716,27,170),
                _cmbEquipment,_txtType,_numPeriod,_dtNext,_txtResponsible,_chkActive,btnAdd,btnUpdate
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

            GridHeaderMap.Apply(_grid, "maintenance", "Id");
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (_cmbEquipment.SelectedValue == null || string.IsNullOrWhiteSpace(_txtType.Text))
            {
                MessageBox.Show("Заполните технику и вид ТО.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MaintenanceService.AddPlan(Convert.ToInt32(_cmbEquipment.SelectedValue), _txtType.Text.Trim(), Convert.ToInt32(_numPeriod.Value), _dtNext.Value.Date, _txtResponsible.Text.Trim());
            LoadData();
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (_grid.CurrentRow == null || !_grid.Columns.Contains("Id"))
            {
                MessageBox.Show("Выберите план ТО для обновления.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            MaintenanceService.UpdatePlan(id, Convert.ToInt32(_cmbEquipment.SelectedValue), _txtType.Text.Trim(), Convert.ToInt32(_numPeriod.Value), _dtNext.Value.Date, _txtResponsible.Text.Trim(), _chkActive.Checked);
            LoadData();
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (_grid.CurrentRow == null)
            {
                return;
            }

            if (_grid.Columns.Contains("MaintenanceType")) _txtType.Text = _grid.CurrentRow.Cells["MaintenanceType"]?.Value?.ToString() ?? string.Empty;
            if (_grid.Columns.Contains("PeriodDays")) _numPeriod.Value = Convert.ToDecimal(_grid.CurrentRow.Cells["PeriodDays"]?.Value ?? 30);
            if (_grid.Columns.Contains("NextDate"))
            {
                DateTime dt;
                if (DateTime.TryParse(_grid.CurrentRow.Cells["NextDate"]?.Value?.ToString(), out dt)) _dtNext.Value = dt;
            }
            if (_grid.Columns.Contains("ResponsiblePerson")) _txtResponsible.Text = _grid.CurrentRow.Cells["ResponsiblePerson"]?.Value?.ToString() ?? string.Empty;
            if (_grid.Columns.Contains("IsActive")) _chkActive.Checked = Convert.ToBoolean(_grid.CurrentRow.Cells["IsActive"]?.Value ?? true);
        }

        private static Label LabelAt(string text, int left, int top, int width)
        {
            return new Label { Text = text, Left = left, Top = top, Width = width };
        }
    }
}
