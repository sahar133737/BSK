using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using BGSK1.Services;
using BGSK1.UI;

namespace BGSK1
{
    public sealed class BackupForm : Form
    {
        private readonly DataGridView _grid;
        private readonly TextBox _txtPath;

        public BackupForm()
        {
            ThemeHelper.ApplyForm(this, "Резервные копии");
            Width = 1100;
            Height = 680;

            var top = new Panel { Dock = DockStyle.Top, Height = 52 };
            _txtPath = new TextBox { Left = 12, Top = 12, Width = 560, Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups") };
            var btnCreate = new Button { Left = 580, Top = 10, Width = 130, Height = 30, Text = "Создать" };
            var btnRestore = new Button { Left = 716, Top = 10, Width = 130, Height = 30, Text = "Восстановить" };
            var btnRefresh = new Button { Left = 852, Top = 10, Width = 130, Height = 30, Text = "Обновить" };
            ThemeHelper.StyleButton(btnCreate, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnRestore, ThemeHelper.Accent);
            ThemeHelper.StyleButton(btnRefresh, ThemeHelper.Secondary);
            btnCreate.Click += BtnCreate_Click;
            btnRestore.Click += BtnRestore_Click;
            btnRefresh.Click += (s, e) => LoadData();
            top.Controls.AddRange(new Control[] { _txtPath, btnCreate, btnRestore, btnRefresh });

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

            Controls.Add(_grid);
            Controls.Add(top);
            Load += (s, e) => LoadData();
        }

        private void LoadData()
        {
            _grid.DataSource = BackupService.GetBackups();
            GridHeaderMap.Apply(_grid, "backups", "Id");
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtPath.Text))
            {
                MessageBox.Show("Укажите путь хранения резервных копий.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            BackupService.CreateBackup(_txtPath.Text.Trim(), "Ручной запуск", false);
            LoadData();
            MessageBox.Show("Резервная копия создана.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnRestore_Click(object sender, EventArgs e)
        {
            if (_grid.CurrentRow == null)
            {
                MessageBox.Show("Выберите резервную копию в таблице.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var path = _grid.CurrentRow.Cells["FilePath"]?.Value?.ToString();
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (MessageBox.Show("Восстановление перезапишет текущие данные. Продолжить?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            BackupService.RestoreBackup(path);
            MessageBox.Show("Восстановление завершено.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
        }
    }
}
