using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using BGSK1.Services;
using BGSK1.UI;

namespace BGSK1
{
    public sealed class AdminPermissionsForm : Form
    {
        private readonly ComboBox _cmbRoles;
        private readonly DataGridView _grid;

        public AdminPermissionsForm()
        {
            ThemeHelper.ApplyForm(this, "Настройка ролей и прав доступа");
            Width = 900;
            Height = 620;
            if (!RolePermissionService.HasPermission("module.admin"))
            {
                Shown += (s, e) => { MessageBox.Show("Нет доступа к администрированию прав.", "Доступ запрещен", MessageBoxButtons.OK, MessageBoxIcon.Warning); Close(); };
            }

            var top = new Panel { Dock = DockStyle.Top, Height = 52 };
            _cmbRoles = new ComboBox { Left = 12, Top = 12, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            var btnSave = new Button { Left = 278, Top = 10, Width = 180, Height = 30, Text = "Сохранить права" };
            var btnHelp = new Button { Left = 464, Top = 10, Width = 100, Height = 30, Text = "Справка" };
            ThemeHelper.StyleButton(btnSave, ThemeHelper.Success);
            ThemeHelper.StyleButton(btnHelp, ThemeHelper.Accent);
            btnSave.Click += BtnSave_Click;
            btnHelp.Click += (s, e) => ModuleHelpProvider.ShowHelp("admin", this);
            _cmbRoles.SelectedIndexChanged += (s, e) => LoadPermissions();
            top.Controls.AddRange(new Control[] { _cmbRoles, btnSave, btnHelp });

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };
            ThemeHelper.StyleGrid(_grid);

            Controls.Add(_grid);
            Controls.Add(top);
            ModuleHelpProvider.BindF11(this, "admin");
            Load += AdminPermissionsForm_Load;
        }

        private void AdminPermissionsForm_Load(object sender, EventArgs e)
        {
            _cmbRoles.DataSource = RolePermissionService.GetRoles();
            _cmbRoles.DisplayMember = "Name";
            _cmbRoles.ValueMember = "Id";
            LoadPermissions();
        }

        private void LoadPermissions()
        {
            if (_cmbRoles.SelectedValue == null || _cmbRoles.SelectedValue is DataRowView)
            {
                return;
            }

            int roleId;
            if (!int.TryParse(_cmbRoles.SelectedValue.ToString(), out roleId))
            {
                return;
            }
            var table = RolePermissionService.GetPermissionsByRole(roleId);

            _grid.DataSource = table;
            if (_grid.Columns.Contains("PermissionKey"))
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.IsNewRow) continue;
                    var key = row.Cells["PermissionKey"].Value?.ToString() ?? string.Empty;
                    row.Cells["PermissionKey"].Value = ToRussianPermissionName(key);
                    row.Tag = key;
                }
            }
            if (_grid.Columns.Contains("PermissionKey")) _grid.Columns["PermissionKey"].HeaderText = "Раздел";
            if (_grid.Columns.Contains("IsAllowed")) _grid.Columns["IsAllowed"].HeaderText = "Разрешено";
            if (_grid.Columns.Contains("PermissionKey")) _grid.Columns["PermissionKey"].ReadOnly = true;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_cmbRoles.SelectedValue == null)
            {
                return;
            }

            var roleId = Convert.ToInt32(_cmbRoles.SelectedValue);
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow)
                {
                    continue;
                }

                var key = row.Tag?.ToString();
                var allowed = Convert.ToBoolean(row.Cells["IsAllowed"]?.Value ?? false);
                if (!string.IsNullOrWhiteSpace(key))
                {
                    RolePermissionService.SavePermission(roleId, key, allowed);
                }
            }

            MessageBox.Show("Права сохранены.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static string ToRussianPermissionName(string key)
        {
            switch (key)
            {
                case "module.equipment": return "Техника";
                case "module.requests": return "Заявки";
                case "module.maintenance": return "Плановое ТО";
                case "module.parts": return "Склад запчастей";
                case "module.reports": return "Отчеты";
                case "module.backups": return "Резервные копии";
                case "module.users": return "Пользователи";
                case "module.admin": return "Настройка ролей и прав";
                default: return key;
            }
        }
    }
}
