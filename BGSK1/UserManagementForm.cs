using System;
using System.Data;
using System.Windows.Forms;
using BGSK1.Services;
using BGSK1.UI;

namespace BGSK1
{
    public sealed class UserManagementForm : Form
    {
        private readonly DataGridView _grid;
        private readonly TextBox _txtEmail;
        private readonly TextBox _txtFullName;
        private readonly ComboBox _cmbRole;
        private readonly CheckBox _chkActive;
        private readonly TextBox _txtPassword;
        private readonly TextBox _txtSearch;

        public UserManagementForm()
        {
            ThemeHelper.ApplyForm(this, "Пользователи и роли");
            Width = 1180;
            Height = 700;
            if (!RolePermissionService.HasPermission("module.users"))
            {
                Shown += (s, e) => { MessageBox.Show("Нет доступа к модулю пользователей.", "Доступ запрещен", MessageBoxButtons.OK, MessageBoxIcon.Warning); Close(); };
            }

            var card = new GroupBox { Dock = DockStyle.Top, Height = 120, Text = "  Карточка пользователя  " };
            _txtEmail = new TextBox { Left = 12, Top = 45, Width = 210 };
            _txtFullName = new TextBox { Left = 226, Top = 45, Width = 250 };
            _cmbRole = new ComboBox { Left = 480, Top = 45, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _chkActive = new CheckBox { Left = 634, Top = 49, Width = 90, Text = "Активен", Checked = true };
            _txtPassword = new TextBox { Left = 728, Top = 45, Width = 150, PasswordChar = '*' };
            var btnCreate = new Button { Left = 884, Top = 42, Width = 80, Height = 30, Text = "Создать" };
            var btnUpdate = new Button { Left = 968, Top = 42, Width = 85, Height = 30, Text = "Обновить" };
            var btnDelete = new Button { Left = 1057, Top = 42, Width = 80, Height = 30, Text = "Удалить" };
            var btnResetPass = new Button { Left = 884, Top = 76, Width = 253, Height = 28, Text = "Сброс пароля" };
            ThemeHelper.StyleButton(btnCreate, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnUpdate, ThemeHelper.Secondary);
            ThemeHelper.StyleButton(btnDelete, ThemeHelper.Danger);
            ThemeHelper.StyleButton(btnResetPass, ThemeHelper.Accent);
            btnCreate.Click += BtnCreate_Click;
            btnUpdate.Click += BtnUpdate_Click;
            btnDelete.Click += BtnDelete_Click;
            btnResetPass.Click += BtnResetPass_Click;
            card.Controls.AddRange(new Control[]
            {
                LabelAt("Email",12,27,210), LabelAt("ФИО",226,27,250), LabelAt("Роль",480,27,150), LabelAt("Новый пароль",728,27,150),
                _txtEmail, _txtFullName, _cmbRole, _chkActive, _txtPassword, btnCreate, btnUpdate, btnDelete, btnResetPass
            });

            var filter = new Panel { Dock = DockStyle.Top, Height = 44 };
            _txtSearch = new TextBox { Left = 12, Top = 10, Width = 280 };
            var btnSearch = new Button { Left = 298, Top = 8, Width = 100, Height = 28, Text = "Поиск" };
            var btnReset = new Button { Left = 402, Top = 8, Width = 100, Height = 28, Text = "Сброс" };
            btnSearch.Click += (s, e) => ApplySearch();
            btnReset.Click += (s, e) => { _txtSearch.Clear(); LoadData(); };
            filter.Controls.AddRange(new Control[] { _txtSearch, btnSearch, btnReset });

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
            Controls.Add(filter);
            Controls.Add(card);
            Load += UserManagementForm_Load;
        }

        private void UserManagementForm_Load(object sender, EventArgs e)
        {
            _cmbRole.DataSource = UserService.GetRoles();
            _cmbRole.DisplayMember = "Name";
            _cmbRole.ValueMember = "Id";
            LoadData();
        }

        private void LoadData()
        {
            _grid.DataSource = UserService.GetUsers();
            GridHeaderMap.Apply(_grid, "users", "Id", "IsDeleted");
        }

        private void ApplySearch()
        {
            var table = UserService.GetUsers();
            var view = table.DefaultView;
            var s = _txtSearch.Text.Trim().Replace("'", "''");
            if (!string.IsNullOrWhiteSpace(s))
            {
                view.RowFilter = $"Email LIKE '%{s}%' OR FullName LIKE '%{s}%' OR RoleName LIKE '%{s}%'";
            }
            _grid.DataSource = view.ToTable();
            GridHeaderMap.Apply(_grid, "users", "Id", "IsDeleted");
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (_grid.CurrentRow == null)
            {
                return;
            }

            _txtEmail.Text = _grid.CurrentRow.Cells["Email"]?.Value?.ToString() ?? string.Empty;
            _txtFullName.Text = _grid.CurrentRow.Cells["FullName"]?.Value?.ToString() ?? string.Empty;
            _chkActive.Checked = Convert.ToBoolean(_grid.CurrentRow.Cells["IsActive"]?.Value ?? true);
            _cmbRole.Text = _grid.CurrentRow.Cells["RoleName"]?.Value?.ToString() ?? string.Empty;
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtEmail.Text) || string.IsNullOrWhiteSpace(_txtPassword.Text))
            {
                MessageBox.Show("Заполните email и пароль.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            UserService.CreateUser(_txtEmail.Text.Trim(), _txtFullName.Text.Trim(), Convert.ToInt32(_cmbRole.SelectedValue), _txtPassword.Text);
            LoadData();
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (_grid.CurrentRow == null)
            {
                return;
            }
            var id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            UserService.UpdateUser(id, _txtEmail.Text.Trim(), _txtFullName.Text.Trim(), Convert.ToInt32(_cmbRole.SelectedValue), _chkActive.Checked);
            LoadData();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_grid.CurrentRow == null)
            {
                return;
            }
            var id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            UserService.SoftDeleteUser(id);
            LoadData();
        }

        private void BtnResetPass_Click(object sender, EventArgs e)
        {
            if (_grid.CurrentRow == null || string.IsNullOrWhiteSpace(_txtPassword.Text))
            {
                MessageBox.Show("Выберите пользователя и укажите новый пароль.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            UserService.ForceResetPassword(id, _txtPassword.Text);
            MessageBox.Show("Пароль сброшен.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static Label LabelAt(string text, int left, int top, int width)
        {
            return new Label { Text = text, Left = left, Top = top, Width = width };
        }
    }
}
