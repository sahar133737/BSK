using System;
using System.Data;
using System.Drawing;
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
        private readonly TextBox _txtComment;

        public BackupForm()
        {
            ThemeHelper.ApplyForm(this, "Резервные копии");
            Width = 1100;
            Height = 700;

            var top = new Panel { Dock = DockStyle.Top, Height = 132 };
            var lblPath = ThemeHelper.FormFieldLabel("Путь к файлу резервной копии (.bak)", 12, 10, 560);
            _txtPath = new TextBox { Left = 12, Top = 34, Width = 730, Text = BackupService.GetDefaultBackupFilePath() };
            var btnFileMenu = new Button { Left = 748, Top = 32, Width = 100, Height = 28, Text = "Файл…" };
            var fileMenu = new ContextMenuStrip();
            fileMenu.Items.Add("Указать путь для новой копии…", null, (_, __) => PickSavePath());
            fileMenu.Items.Add("Открыть существующий .bak…", null, (_, __) => PickOpenPath());
            ThemeHelper.StyleButton(btnFileMenu, ThemeHelper.Secondary);
            btnFileMenu.Click += (_, __) => fileMenu.Show(btnFileMenu, new Point(0, btnFileMenu.Height));

            var lblComment = ThemeHelper.FormFieldLabel("Комментарий (необязательно)", 12, 68, 280);
            _txtComment = new TextBox { Left = 12, Top = 92, Width = 420 };
            var btnCreate = new Button { Left = 448, Top = 90, Width = 100, Height = 30, Text = "Создать" };
            var btnRestore = new Button { Left = 556, Top = 90, Width = 120, Height = 30, Text = "Восстановить" };
            var btnHelp = new Button { Left = 684, Top = 90, Width = 120, Height = 30, Text = "Справка" };
            ThemeHelper.StyleButton(btnCreate, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnRestore, ThemeHelper.Accent);
            ThemeHelper.StyleButton(btnHelp, ThemeHelper.Accent);
            btnCreate.Click += BtnCreate_Click;
            btnRestore.Click += BtnRestore_Click;
            btnHelp.Click += (s, e) => ModuleHelpProvider.ShowHelp("backup", this);
            top.Controls.AddRange(new Control[]
            {
                lblPath, _txtPath, btnFileMenu, lblComment, _txtComment, btnCreate, btnRestore, btnHelp
            });

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

            Controls.Add(top);
            Controls.Add(_grid);
            top.BringToFront();
            ModuleHelpProvider.BindF11(this, "backup");
            Load += (s, e) => LoadData();
        }

        private void LoadData()
        {
            _grid.DataSource = BackupService.GetBackups();
            GridHeaderMap.Apply(_grid, "backups", "Id");
        }

        private void PickSavePath()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Файлы резервных копий (*.bak)|*.bak";
                dialog.FileName = string.IsNullOrWhiteSpace(_txtPath.Text)
                    ? "BGSK1_manual.bak"
                    : Path.GetFileName(_txtPath.Text);
                var dir = Path.GetDirectoryName(_txtPath.Text);
                dialog.InitialDirectory = !string.IsNullOrEmpty(dir) && Directory.Exists(dir)
                    ? dir
                    : BackupService.GetRecommendedBackupDirectory();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _txtPath.Text = dialog.FileName;
                }
            }
        }

        private void PickOpenPath()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Файлы резервных копий (*.bak)|*.bak";
                dialog.FileName = string.IsNullOrWhiteSpace(_txtPath.Text)
                    ? string.Empty
                    : Path.GetFileName(_txtPath.Text);
                var dir = Path.GetDirectoryName(_txtPath.Text);
                dialog.InitialDirectory = !string.IsNullOrEmpty(dir) && Directory.Exists(dir)
                    ? dir
                    : BackupService.GetRecommendedBackupDirectory();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _txtPath.Text = dialog.FileName;
                }
            }
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtPath.Text))
            {
                MessageBox.Show("Укажите путь хранения резервных копий.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var fullPath = _txtPath.Text.Trim();
            if (!fullPath.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
            {
                fullPath += ".bak";
            }
            try
            {
                BackupService.CreateBackupToFile(fullPath, _txtComment.Text.Trim(), false);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Резервное копирование", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LoadData();
            MessageBox.Show("Резервная копия создана.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnRestore_Click(object sender, EventArgs e)
        {
            var path = _txtPath.Text.Trim();
            if (string.IsNullOrWhiteSpace(path) && _grid.CurrentRow != null)
            {
                path = _grid.CurrentRow.Cells["FilePath"]?.Value?.ToString();
            }
            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show("Укажите путь к файлу, выберите .bak через «Файл…» → «Открыть…» или выберите строку в таблице.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Восстановление перезапишет текущие данные. Продолжить?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                BackupService.RestoreBackup(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка восстановления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            MessageBox.Show("Восстановление завершено.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
        }
    }
}
