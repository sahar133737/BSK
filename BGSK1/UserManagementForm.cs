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

            var card = new GroupBox { Dock = DockStyle.Top, Height = 124, Text = "  Карточка пользователя  " };
            _txtEmail = new TextBox { Left = 12, Top = 48, Width = 210 };
            _txtFullName = new TextBox { Left = 226, Top = 48, Width = 250 };
            _cmbRole = new ComboBox { Left = 480, Top = 48, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            _txtPassword = new TextBox { Left = 638, Top = 48, Width = 150, PasswordChar = '*' };
            var btnCreate = new Button { Left = 794, Top = 46, Width = 80, Height = 30, Text = "Создать" };
            var btnUpdate = new Button { Left = 878, Top = 46, Width = 85, Height = 30, Text = "Обновить" };
            var btnDelete = new Button { Left = 967, Top = 46, Width = 80, Height = 30, Text = "Удалить" };
            var btnResetPass = new Button { Left = 794, Top = 80, Width = 253, Height = 28, Text = "Сброс пароля" };
            var btnHelp = new Button { Left = 1050, Top = 80, Width = 80, Height = 28, Text = "Справка" };
            ThemeHelper.StyleButton(btnCreate, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnUpdate, ThemeHelper.Secondary);
            ThemeHelper.StyleButton(btnDelete, ThemeHelper.Danger);
            ThemeHelper.StyleButton(btnResetPass, ThemeHelper.Accent);
            ThemeHelper.StyleButton(btnHelp, ThemeHelper.Accent);
            btnCreate.Click += BtnCreate_Click;
            btnUpdate.Click += BtnUpdate_Click;
            btnDelete.Click += BtnDelete_Click;
            btnResetPass.Click += BtnResetPass_Click;
            btnHelp.Click += (s, e) => ModuleHelpProvider.ShowHelp("users", this);
            card.Controls.AddRange(new Control[]
            {
                LabelAt("Логин",12,20,210), LabelAt("ФИО",226,20,250), LabelAt("Роль",480,20,150), LabelAt("Новый пароль",638,20,150),
                _txtEmail, _txtFullName, _cmbRole, _txtPassword, btnCreate, btnUpdate, btnDelete, btnResetPass, btnHelp
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
            ModuleHelpProvider.BindF11(this, "users");
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
            GridHeaderMap.Apply(_grid, "users", "Id", "IsDeleted", "IsActive");
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
            GridHeaderMap.Apply(_grid, "users", "Id", "IsDeleted", "IsActive");
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (_grid.CurrentRow == null)
            {
                return;
            }

            _txtEmail.Text = _grid.CurrentRow.Cells["Email"]?.Value?.ToString() ?? string.Empty;
            _txtFullName.Text = _grid.CurrentRow.Cells["FullName"]?.Value?.ToString() ?? string.Empty;
            _cmbRole.Text = _grid.CurrentRow.Cells["RoleName"]?.Value?.ToString() ?? string.Empty;
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtEmail.Text) || string.IsNullOrWhiteSpace(_txtPassword.Text))
            {
                MessageBox.Show("Заполните логин и пароль.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!UserService.IsValidLogin(_txtEmail.Text))
            {
                MessageBox.Show("Логин не может быть пустым.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!UserService.IsStrongPassword(_txtPassword.Text))
            {
                MessageBox.Show("Пароль: минимум 8 символов, цифра, строчная и заглавная буква.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                UserService.CreateUser(_txtEmail.Text.Trim(), _txtFullName.Text.Trim(), Convert.ToInt32(_cmbRole.SelectedValue), _txtPassword.Text);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (_grid.CurrentRow == null)
            {
                return;
            }
            if (!UserService.IsValidLogin(_txtEmail.Text))
            {
                MessageBox.Show("Логин не может быть пустым.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                var id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
                var isActive = Convert.ToBoolean(_grid.CurrentRow.Cells["IsActive"]?.Value ?? true);
                UserService.UpdateUser(id, _txtEmail.Text.Trim(), _txtFullName.Text.Trim(), Convert.ToInt32(_cmbRole.SelectedValue), isActive);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_grid.CurrentRow == null)
            {
                return;
            }
            try
            {
                var id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
                UserService.SoftDeleteUser(id);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnResetPass_Click(object sender, EventArgs e)
        {
            if (_grid.CurrentRow == null || string.IsNullOrWhiteSpace(_txtPassword.Text))
            {
                MessageBox.Show("Выберите пользователя и укажите новый пароль.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!UserService.IsStrongPassword(_txtPassword.Text))
            {
                MessageBox.Show("Пароль: минимум 8 символов, цифра, строчная и заглавная буква.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                var id = Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
                UserService.ForceResetPassword(id, _txtPassword.Text);
                MessageBox.Show("Пароль сброшен.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static Label LabelAt(string text, int left, int top, int width)
        {
            return ThemeHelper.FormFieldLabel(text, left, top, width);
        }
    }
}
