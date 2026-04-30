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
            ThemeHelper.ApplyForm(this, "Администрирование прав");
            Width = 900;
            Height = 620;
            if (!RolePermissionService.HasPermission("module.admin"))
            {
                Shown += (s, e) => { MessageBox.Show("Нет доступа к администрированию прав.", "Доступ запрещен", MessageBoxButtons.OK, MessageBoxIcon.Warning); Close(); };
            }

            var top = new Panel { Dock = DockStyle.Top, Height = 52 };
            _cmbRoles = new ComboBox { Left = 12, Top = 12, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            var btnLoad = new Button { Left = 278, Top = 10, Width = 120, Height = 30, Text = "Загрузить" };
            var btnSave = new Button { Left = 404, Top = 10, Width = 120, Height = 30, Text = "Сохранить" };
            ThemeHelper.StyleButton(btnLoad, ThemeHelper.Secondary);
            ThemeHelper.StyleButton(btnSave, ThemeHelper.Success);
            btnLoad.Click += (s, e) => LoadPermissions();
            btnSave.Click += BtnSave_Click;
            top.Controls.AddRange(new Control[] { _cmbRoles, btnLoad, btnSave });

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
            if (_cmbRoles.SelectedValue == null)
            {
                return;
            }

            var roleId = Convert.ToInt32(_cmbRoles.SelectedValue);
            var table = RolePermissionService.GetPermissionsByRole(roleId);

            if (table.Rows.Count == 0)
            {
                table.Rows.Add("module.equipment", true);
                table.Rows.Add("module.requests", true);
                table.Rows.Add("module.maintenance", true);
                table.Rows.Add("module.parts", true);
                table.Rows.Add("module.reports", false);
                table.Rows.Add("module.backups", false);
                table.Rows.Add("module.admin", false);
            }

            _grid.DataSource = table;
            if (_grid.Columns.Contains("PermissionKey")) _grid.Columns["PermissionKey"].HeaderText = "Ключ права";
            if (_grid.Columns.Contains("IsAllowed")) _grid.Columns["IsAllowed"].HeaderText = "Разрешено";
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

                var key = row.Cells["PermissionKey"]?.Value?.ToString();
                var allowed = Convert.ToBoolean(row.Cells["IsAllowed"]?.Value ?? false);
                if (!string.IsNullOrWhiteSpace(key))
                {
                    RolePermissionService.SavePermission(roleId, key, allowed);
                }
            }

            MessageBox.Show("Права сохранены.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
