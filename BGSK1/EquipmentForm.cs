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
        private readonly TextBox _txtType;
        private readonly TextBox _txtLocation;
        private readonly TextBox _txtResponsible;

        public EquipmentForm()
        {
            ThemeHelper.ApplyForm(this, "Техника");
            Width = 1200;
            Height = 700;
            if (!RolePermissionService.HasPermission("module.equipment"))
            {
                Shown += (s, e) => { MessageBox.Show("Нет доступа к модулю.", "Доступ запрещен", MessageBoxButtons.OK, MessageBoxIcon.Warning); Close(); };
            }

            var card = new GroupBox { Dock = DockStyle.Top, Height = 108, Text = "  Карточка оборудования  " };
            _txtInv = new TextBox { Left = 16, Top = 46, Width = 140 };
            _txtName = new TextBox { Left = 164, Top = 46, Width = 240 };
            _txtType = new TextBox { Left = 412, Top = 46, Width = 150 };
            _txtLocation = new TextBox { Left = 570, Top = 46, Width = 160 };
            _txtResponsible = new TextBox { Left = 738, Top = 46, Width = 180 };
            var btnAdd = new Button { Left = 926, Top = 43, Width = 110, Height = 30, Text = "Добавить" };
            var btnUpdate = new Button { Left = 1042, Top = 43, Width = 110, Height = 30, Text = "Обновить" };
            ThemeHelper.StyleButton(btnAdd, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnUpdate, ThemeHelper.Secondary);
            btnAdd.Click += BtnAdd_Click;
            btnUpdate.Click += BtnUpdate_Click;
            card.Controls.AddRange(new Control[]
            {
                LabelAt("Инв. номер", 16, 27, 140), LabelAt("Наименование", 164, 27, 240), LabelAt("Тип", 412, 27, 150),
                LabelAt("Локация", 570, 27, 160), LabelAt("Ответственный", 738, 27, 180),
                _txtInv, _txtName, _txtType, _txtLocation, _txtResponsible, btnAdd, btnUpdate
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
            _txtType.Text = _grid.CurrentRow.Cells["TypeName"]?.Value?.ToString() ?? string.Empty;
            _txtLocation.Text = _grid.CurrentRow.Cells["LocationName"]?.Value?.ToString() ?? string.Empty;
            _txtResponsible.Text = _grid.CurrentRow.Cells["ResponsiblePerson"]?.Value?.ToString() ?? string.Empty;
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtInv.Text) || string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("Заполните обязательные поля: инв. номер и наименование.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            EquipmentService.AddEquipment(_txtInv.Text.Trim(), _txtName.Text.Trim(), _txtType.Text.Trim(), _txtLocation.Text.Trim(), _txtResponsible.Text.Trim());
            LoadData();
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
            EquipmentService.UpdateEquipment(id, _txtInv.Text.Trim(), _txtName.Text.Trim(), _txtType.Text.Trim(), _txtLocation.Text.Trim(), _txtResponsible.Text.Trim(), status);
            LoadData();
        }

        private static Label LabelAt(string text, int left, int top, int width)
        {
            return new Label { Text = text, Left = left, Top = top, Width = width };
        }
    }
}
