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
        private readonly TextBox _txtInv;
        private readonly TextBox _txtName;
        private readonly ComboBox _cmbType;
        private readonly ComboBox _cmbLocation;
        private readonly ComboBox _cmbResponsible;

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

            var card = new GroupBox { Dock = DockStyle.Top, Height = 114, Text = "  Карточка оборудования  " };
            _txtInv = new TextBox { Left = 16, Top = 48, Width = 140 };
            _txtName = new TextBox { Left = 164, Top = 48, Width = 240 };
            _cmbType = new ComboBox { Left = 412, Top = 48, Width = 124, DropDownStyle = ComboBoxStyle.DropDown };
            var btnAddType = LookupUiHelper.CreateAddLookupButton(538, 48, "Добавить тип техники в справочник", (s, e) => AddLookupAndRefresh(_cmbType, LookupDictionaryService.EquipmentType, "Новый тип техники"));
            _cmbLocation = new ComboBox { Left = 570, Top = 48, Width = 136, DropDownStyle = ComboBoxStyle.DropDown };
            var btnAddLocation = LookupUiHelper.CreateAddLookupButton(708, 48, "Добавить кабинет / локацию", (s, e) => AddLookupAndRefresh(_cmbLocation, LookupDictionaryService.Location, "Новая локация (кабинет)"));
            _cmbResponsible = new ComboBox { Left = 740, Top = 48, Width = 152, DropDownStyle = ComboBoxStyle.DropDown };
            var btnAddResp = LookupUiHelper.CreateAddLookupButton(894, 48, "Добавить ответственного за технику", (s, e) => AddLookupAndRefresh(_cmbResponsible, LookupDictionaryService.EquipmentResponsible, "Новый ответственный (ФИО)"));
            var btnAdd = new Button { Left = 928, Top = 46, Width = 110, Height = 30, Text = "Добавить" };
            var btnUpdate = new Button { Left = 1044, Top = 46, Width = 110, Height = 30, Text = "Обновить" };
            var btnDelete = new Button { Left = 928, Top = 80, Width = 226, Height = 24, Text = "Удалить запись" };
            ThemeHelper.StyleButton(btnAdd, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnUpdate, ThemeHelper.Secondary);
            ThemeHelper.StyleButton(btnDelete, ThemeHelper.Danger);
            btnAdd.Click += BtnAdd_Click;
            btnUpdate.Click += BtnUpdate_Click;
            btnDelete.Click += BtnDelete_Click;
            card.Controls.AddRange(new Control[]
            {
                LabelAt("Инв. номер", 16, 20, 140), LabelAt("Наименование", 164, 20, 240), LabelAt("Тип", 412, 20, 124),
                LabelAt("Локация", 568, 20, 136), LabelAt("Ответственный", 740, 20, 152),
                _txtInv, _txtName, _cmbType, btnAddType, _cmbLocation, btnAddLocation, _cmbResponsible, btnAddResp, btnAdd, btnUpdate, btnDelete
            });

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
            _grid.SelectionChanged += Grid_SelectionChanged;

            Controls.Add(_grid);
            Controls.Add(top);
            Controls.Add(card);
            Load += (s, e) => LoadData();
        }

        private void LoadData()
        {
            _grid.DataSource = EquipmentService.GetEquipment();
            ConfigureGrid();
            BindLookups();
        }

        private void BindLookups()
        {
            FillCombo(_cmbType, EquipmentService.GetTypeLookup());
            FillCombo(_cmbLocation, EquipmentService.GetLocationLookup());
            FillCombo(_cmbResponsible, EquipmentService.GetResponsibleLookup());
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

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (_grid.CurrentRow == null)
            {
                return;
            }

            _txtInv.Text = _grid.CurrentRow.Cells["InventoryNumber"]?.Value?.ToString() ?? string.Empty;
            _txtName.Text = _grid.CurrentRow.Cells["Name"]?.Value?.ToString() ?? string.Empty;
            _cmbType.Text = _grid.CurrentRow.Cells["TypeName"]?.Value?.ToString() ?? string.Empty;
            _cmbLocation.Text = _grid.CurrentRow.Cells["LocationName"]?.Value?.ToString() ?? string.Empty;
            _cmbResponsible.Text = _grid.CurrentRow.Cells["ResponsiblePerson"]?.Value?.ToString() ?? string.Empty;
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

        private static Label LabelAt(string text, int left, int top, int width)
        {
            return ThemeHelper.FormFieldLabel(text, left, top, width);
        }
    }
}
