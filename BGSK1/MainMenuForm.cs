using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using BGSK1.Security;
using BGSK1.Services;
using BGSK1.UI;

namespace BGSK1
{
    public sealed class MainMenuForm : Form
    {
        private readonly Label _lblEquipmentCount;
        private readonly Label _lblOpenRequestsCount;
        private readonly Label _lblOverdueCount;
        private readonly Label _lblLowStockCount;
        private readonly DataGridView _gridRecentRequests;
        private readonly TextBox _txtQuickInventory;
        private readonly TextBox _txtQuickEquipmentName;
        private readonly ComboBox _cmbQuickEquipment;
        private readonly TextBox _txtQuickProblem;
        private readonly TextBox _txtQuickBackupPath;

        public MainMenuForm()
        {
            Width = 1380;
            Height = 860;
            StartPosition = FormStartPosition.CenterScreen;
            ThemeHelper.ApplyForm(this, "ИС ремонта техники - Главное меню");
            BackColor = ThemeHelper.DarkBg;

            var sidebar = new Panel { Dock = DockStyle.Left, Width = 330, BackColor = ThemeHelper.PanelBg };
            var content = new Panel { Dock = DockStyle.Fill, BackColor = ThemeHelper.Surface, Padding = new Padding(16) };

            var title = new Label
            {
                Left = 20,
                Top = 20,
                Width = 280,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold),
                Text = "Рабочее место оператора"
            };
            var user = new Label
            {
                Left = 20,
                Top = 66,
                Width = 290,
                ForeColor = Color.FromArgb(206, 224, 244),
                Text = $"Пользователь: {CurrentUserContext.FullName} ({CurrentUserContext.RoleName})"
            };
            sidebar.Controls.Add(title);
            sidebar.Controls.Add(user);

            AddMenuButton(sidebar, "Техника", 20, 120, 290, ThemeHelper.Primary, "module.equipment", () => OpenModule(new EquipmentForm()));
            AddMenuButton(sidebar, "Заявки на ремонт", 20, 176, 290, ThemeHelper.Secondary, "module.requests", () => OpenModule(new RequestsForm()));
            AddMenuButton(sidebar, "Плановое ТО", 20, 232, 290, Color.FromArgb(27, 111, 173), "module.maintenance", () => OpenModule(new MaintenanceForm()));
            AddMenuButton(sidebar, "Склад запчастей", 20, 288, 290, ThemeHelper.Success, "module.parts", () => OpenModule(new PartsForm()));
            AddMenuButton(sidebar, "Отчеты", 20, 344, 290, Color.FromArgb(24, 88, 146), "module.reports", () => OpenModule(new ReportsCenterForm()));
            AddMenuButton(sidebar, "Бэкапы", 20, 400, 290, ThemeHelper.Accent, "module.backups", () => OpenModule(new BackupForm()));
            AddMenuButton(sidebar, "Пользователи", 20, 456, 290, Color.FromArgb(46, 131, 201), "module.users", () => OpenModule(new UserManagementForm()));
            AddMenuButton(sidebar, "Администрирование прав", 20, 512, 290, ThemeHelper.Danger, "module.admin", () => OpenModule(new AdminPermissionsForm()));

            var pageTitle = new Label
            {
                Left = 16,
                Top = 10,
                Width = 980,
                Height = 30,
                Font = new Font("Segoe UI Semibold", 16f, FontStyle.Bold),
                ForeColor = ThemeHelper.Text,
                Text = "Панель мониторинга"
            };
            var subtitle = new Label
            {
                Left = 16,
                Top = 42,
                Width = 980,
                Height = 26,
                ForeColor = ThemeHelper.MutedText,
                Text = "Оперативная сводка по ремонту, обслуживанию и складу"
            };
            content.Controls.Add(pageTitle);
            content.Controls.Add(subtitle);

            var kpiPanel = new TableLayoutPanel
            {
                Left = 16,
                Top = 76,
                Width = 990,
                Height = 130,
                ColumnCount = 4,
                RowCount = 1
            };
            kpiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            kpiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            kpiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            kpiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            _lblEquipmentCount = AddKpiCard(kpiPanel, 0, "Единиц техники");
            _lblOpenRequestsCount = AddKpiCard(kpiPanel, 1, "Открытых заявок");
            _lblOverdueCount = AddKpiCard(kpiPanel, 2, "Просроченное ТО");
            _lblLowStockCount = AddKpiCard(kpiPanel, 3, "Дефицит запчастей");
            content.Controls.Add(kpiPanel);

            var quickPanel = new Panel
            {
                Left = 16,
                Top = 214,
                Width = 990,
                Height = 122,
                BackColor = ThemeHelper.Surface,
                BorderStyle = BorderStyle.FixedSingle
            };
            var quickTitle = new Label
            {
                Left = 10,
                Top = 8,
                Width = 300,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                ForeColor = ThemeHelper.Text,
                Text = "Быстрые действия"
            };
            quickPanel.Controls.Add(quickTitle);

            _cmbQuickEquipment = new ComboBox { Left = 12, Top = 34, Width = 238, DropDownStyle = ComboBoxStyle.DropDownList };
            _txtQuickProblem = new TextBox { Left = 256, Top = 34, Width = 230 };
            var btnQuickRequest = new Button { Left = 492, Top = 32, Width = 160, Height = 28, Text = "Создать заявку" };
            ThemeHelper.StyleButton(btnQuickRequest, ThemeHelper.Primary);
            btnQuickRequest.Click += BtnQuickRequest_Click;

            _txtQuickInventory = new TextBox { Left = 12, Top = 78, Width = 145 };
            _txtQuickEquipmentName = new TextBox { Left = 163, Top = 78, Width = 220 };
            var btnQuickEquipment = new Button { Left = 389, Top = 76, Width = 160, Height = 28, Text = "Добавить технику" };
            ThemeHelper.StyleButton(btnQuickEquipment, ThemeHelper.Secondary);
            btnQuickEquipment.Click += BtnQuickEquipment_Click;

            _txtQuickBackupPath = new TextBox { Left = 658, Top = 34, Width = 230, Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups") };
            var btnQuickBackup = new Button { Left = 894, Top = 32, Width = 88, Height = 28, Text = "Бэкап" };
            ThemeHelper.StyleButton(btnQuickBackup, ThemeHelper.Accent);
            btnQuickBackup.Click += BtnQuickBackup_Click;

            quickPanel.Controls.AddRange(new Control[]
            {
                LabelInline("Техника", 12, 60, 120), LabelInline("Инв. номер", 12, 104, 120), LabelInline("Наименование", 163, 104, 120),
                LabelInline("Неисправность", 256, 60, 120), LabelInline("Путь бэкапа", 658, 60, 120),
                _cmbQuickEquipment, _txtQuickProblem, btnQuickRequest, _txtQuickInventory, _txtQuickEquipmentName, btnQuickEquipment,
                _txtQuickBackupPath, btnQuickBackup
            });
            content.Controls.Add(quickPanel);

            var recentTitle = new Label
            {
                Left = 16,
                Top = 344,
                Width = 520,
                Height = 24,
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                ForeColor = ThemeHelper.Text,
                Text = "Последние заявки на ремонт"
            };
            content.Controls.Add(recentTitle);

            _gridRecentRequests = new DataGridView
            {
                Left = 16,
                Top = 370,
                Width = 990,
                Height = 408,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };
            ThemeHelper.StyleGrid(_gridRecentRequests);
            _gridRecentRequests.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            content.Controls.Add(_gridRecentRequests);

            Controls.Add(content);
            Controls.Add(sidebar);
            Load += MainMenuForm_Load;
        }

        private static void AddMenuButton(Panel parent, string text, int left, int top, int width, Color color, string permissionKey, Action onClick)
        {
            var btn = new Button
            {
                Left = left,
                Top = top,
                Width = width,
                Height = 44,
                Text = text,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                Enabled = RolePermissionService.HasPermission(permissionKey)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => onClick();
            parent.Controls.Add(btn);
        }

        private void MainMenuForm_Load(object sender, EventArgs e)
        {
            RefreshDashboard();
        }

        private void RefreshDashboard()
        {
            var equipmentCount = DashboardService.GetEquipmentCount();
            var openCount = DashboardService.GetOpenRequestsCount();
            var overdueCount = DashboardService.GetOverdueMaintenanceCount();
            var lowStockCount = DashboardService.GetLowStockCount();

            _lblEquipmentCount.Text = equipmentCount.ToString();
            _lblOpenRequestsCount.Text = openCount.ToString();
            _lblOverdueCount.Text = overdueCount.ToString();
            _lblLowStockCount.Text = lowStockCount.ToString();
            SetKpiSeverity(_lblEquipmentCount, false);
            SetKpiSeverity(_lblOpenRequestsCount, false);
            SetKpiSeverity(_lblOverdueCount, overdueCount > 0);
            SetKpiSeverity(_lblLowStockCount, lowStockCount > 0);

            _gridRecentRequests.DataSource = DashboardService.GetRecentRequests();
            GridHeaderMap.Apply(_gridRecentRequests, "dashboardRecentRequests");

            if (RolePermissionService.HasPermission("module.requests"))
            {
                _cmbQuickEquipment.DataSource = EquipmentService.GetEquipmentLookup();
                _cmbQuickEquipment.DisplayMember = "DisplayName";
                _cmbQuickEquipment.ValueMember = "Id";
            }
        }

        private void OpenModule(Form moduleForm)
        {
            using (moduleForm)
            {
                moduleForm.ShowDialog(this);
            }
            RefreshDashboard();
        }

        private static Label AddKpiCard(TableLayoutPanel parent, int colIndex, string title)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(8), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            var lblTitle = new Label
            {
                Left = 12,
                Top = 12,
                Width = 220,
                ForeColor = ThemeHelper.MutedText,
                Text = title
            };
            var lblValue = new Label
            {
                Left = 12,
                Top = 40,
                Width = 220,
                Height = 52,
                Font = new Font("Segoe UI Semibold", 24f, FontStyle.Bold),
                ForeColor = ThemeHelper.Text,
                Text = "0"
            };
            panel.Controls.Add(lblTitle);
            panel.Controls.Add(lblValue);
            parent.Controls.Add(panel, colIndex, 0);
            return lblValue;
        }

        private void BtnQuickRequest_Click(object sender, EventArgs e)
        {
            if (!RolePermissionService.HasPermission("module.requests"))
            {
                MessageBox.Show("Нет прав на создание заявок.", "Доступ запрещен", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_cmbQuickEquipment.SelectedValue == null || string.IsNullOrWhiteSpace(_txtQuickProblem.Text))
            {
                MessageBox.Show("Выберите технику и укажите неисправность.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RepairRequestService.CreateRequest(Convert.ToInt32(_cmbQuickEquipment.SelectedValue), _txtQuickProblem.Text.Trim(), "Средний", string.Empty);
            _txtQuickProblem.Clear();
            RefreshDashboard();
            MessageBox.Show("Заявка создана.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnQuickEquipment_Click(object sender, EventArgs e)
        {
            if (!RolePermissionService.HasPermission("module.equipment"))
            {
                MessageBox.Show("Нет прав на добавление техники.", "Доступ запрещен", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtQuickInventory.Text) || string.IsNullOrWhiteSpace(_txtQuickEquipmentName.Text))
            {
                MessageBox.Show("Укажите инвентарный номер и наименование.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            EquipmentService.AddEquipment(_txtQuickInventory.Text.Trim(), _txtQuickEquipmentName.Text.Trim(), string.Empty, string.Empty, string.Empty);
            _txtQuickInventory.Clear();
            _txtQuickEquipmentName.Clear();
            RefreshDashboard();
            MessageBox.Show("Техника добавлена.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnQuickBackup_Click(object sender, EventArgs e)
        {
            if (!RolePermissionService.HasPermission("module.backups"))
            {
                MessageBox.Show("Нет прав на создание бэкапа.", "Доступ запрещен", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtQuickBackupPath.Text))
            {
                MessageBox.Show("Укажите путь для сохранения бэкапа.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            BackupService.CreateBackup(_txtQuickBackupPath.Text.Trim(), "Быстрое действие из главного меню", false);
            RefreshDashboard();
            MessageBox.Show("Резервная копия создана.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static Label LabelInline(string text, int left, int top, int width)
        {
            return new Label { Left = left, Top = top, Width = width, Text = text, ForeColor = ThemeHelper.MutedText, Font = new Font("Segoe UI", 8.5f) };
        }

        private static void SetKpiSeverity(Label label, bool isCritical)
        {
            label.ForeColor = isCritical ? ThemeHelper.Danger : ThemeHelper.Text;
        }
    }
}
