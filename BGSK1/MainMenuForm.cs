using System;
using System.Drawing;
using System.Windows.Forms;
using BGSK1.Security;
using BGSK1.Services;
using BGSK1.UI;

namespace BGSK1
{
    public sealed class MainMenuForm : Form
    {
        private static bool _partsStockNotificationShown;

        private readonly Label _lblEquipmentCount;
        private readonly Label _lblOpenRequestsCount;
        private readonly Label _lblOverdueCount;
        private readonly Label _lblLowStockCount;
        private readonly ToolTip _lowStockKpiTip = new ToolTip { AutomaticDelay = 350 };
        private readonly DataGridView _gridRecentRequests;
        private readonly Label _lblLowStockAlert;

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
            AddMenuButton(sidebar, "Заявки на ремонт", 20, 176, 290, ThemeHelper.Primary, "module.requests", () => OpenModule(new RequestsForm()));
            AddMenuButton(sidebar, "Плановое ТО", 20, 232, 290, ThemeHelper.Primary, "module.maintenance", () => OpenModule(new MaintenanceForm()));
            AddMenuButton(sidebar, "Склад запчастей", 20, 288, 290, ThemeHelper.Primary, "module.parts", () => OpenModule(new PartsForm()));
            AddMenuButton(sidebar, "Отчеты", 20, 344, 290, ThemeHelper.Primary, "module.reports", () => OpenModule(new ReportsCenterForm()));
            AddMenuButton(sidebar, "Бэкапы", 20, 400, 290, ThemeHelper.Primary, "module.backups", () => OpenModule(new BackupForm()));
            AddMenuButton(sidebar, "Пользователи", 20, 456, 290, ThemeHelper.Primary, "module.users", () => OpenModule(new UserManagementForm()));
            AddMenuButton(sidebar, "Администрирование прав", 20, 512, 290, ThemeHelper.Secondary, "module.admin", () => OpenModule(new AdminPermissionsForm()));

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
                Height = 132,
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

            var btnQuickRequest = new Button { Left = 12, Top = 40, Width = 220, Height = 32, Text = "Открыть модуль заявок" };
            ThemeHelper.StyleButton(btnQuickRequest, ThemeHelper.Primary);
            btnQuickRequest.Click += (s, e) => OpenModule(new RequestsForm());

            var btnQuickEquipment = new Button { Left = 238, Top = 40, Width = 220, Height = 32, Text = "Открыть модуль техники" };
            ThemeHelper.StyleButton(btnQuickEquipment, ThemeHelper.Secondary);
            btnQuickEquipment.Click += (s, e) => OpenModule(new EquipmentForm());

            var btnQuickBackup = new Button { Left = 464, Top = 40, Width = 220, Height = 32, Text = "Открыть модуль бэкапов" };
            ThemeHelper.StyleButton(btnQuickBackup, ThemeHelper.Accent);
            btnQuickBackup.Click += (s, e) => OpenModule(new BackupForm());

            var btnQuickMaintenance = new Button { Left = 690, Top = 40, Width = 140, Height = 32, Text = "Плановое ТО" };
            ThemeHelper.StyleButton(btnQuickMaintenance, ThemeHelper.Primary);
            btnQuickMaintenance.Click += (s, e) => OpenModule(new MaintenanceForm());

            var btnQuickParts = new Button { Left = 836, Top = 40, Width = 146, Height = 32, Text = "Запчасти" };
            ThemeHelper.StyleButton(btnQuickParts, ThemeHelper.Secondary);
            btnQuickParts.Click += (s, e) => OpenModule(new PartsForm());

            quickPanel.Controls.AddRange(new Control[]
            {
                LabelInline("Создание записей выполняется внутри модулей.", 12, 82, 430),
                btnQuickRequest, btnQuickEquipment, btnQuickBackup, btnQuickMaintenance, btnQuickParts
            });
            content.Controls.Add(quickPanel);

            _lblLowStockAlert = new Label
            {
                Left = 16,
                Top = 342,
                Width = 990,
                Height = 56,
                ForeColor = ThemeHelper.Danger,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                Visible = false,
                AutoSize = false
            };
            content.Controls.Add(_lblLowStockAlert);

            var recentTitle = new Label
            {
                Left = 16,
                Top = 404,
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
                Top = 432,
                Width = 990,
                Height = 386,
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
            var lowStockCritical = DashboardService.GetLowStockCount();
            var lowStockSoon = DashboardService.GetLowStockSoonCount();

            _lblEquipmentCount.Text = equipmentCount.ToString();
            _lblOpenRequestsCount.Text = openCount.ToString();
            _lblOverdueCount.Text = overdueCount.ToString();
            _lblLowStockCount.Text = lowStockCritical.ToString();
            SetKpiSeverity(_lblEquipmentCount, false);
            SetKpiSeverity(_lblOpenRequestsCount, false);
            SetKpiSeverity(_lblOverdueCount, overdueCount > 0);
            SetLowStockKpiLook(lowStockCritical, lowStockSoon);

            _gridRecentRequests.DataSource = DashboardService.GetRecentRequests();
            GridHeaderMap.Apply(_gridRecentRequests, "dashboardRecentRequests");

            ApplyPartsStockAlerts(lowStockCritical, lowStockSoon);

        }

        private void ApplyPartsStockAlerts(int criticalCount, int soonCount)
        {
            if (criticalCount > 0)
            {
                _lblLowStockAlert.Visible = true;
                _lblLowStockAlert.ForeColor = ThemeHelper.Danger;
                var text = $"Критично или на минимуме: {criticalCount} поз. Рекомендуется пополнить склад немедленно.";
                if (soonCount > 0)
                {
                    text += Environment.NewLine + $"Низкий запас («скоро минимум»): ещё {soonCount} поз. Закажите заранее.";
                }

                _lblLowStockAlert.Text = text;
            }
            else if (soonCount > 0)
            {
                _lblLowStockAlert.Visible = true;
                _lblLowStockAlert.ForeColor = ThemeHelper.Accent;
                _lblLowStockAlert.Text = $"На складе {soonCount} поз. с низким остатком — минимальный порог недалеко, рекомендуется запланировать закупку.";
            }
            else
            {
                _lblLowStockAlert.Visible = false;
                _lblLowStockAlert.Text = string.Empty;
            }

            if ((criticalCount > 0 || soonCount > 0) && !_partsStockNotificationShown)
            {
                _partsStockNotificationShown = true;
                var dlgTitle = "Запасы запчастей";
                string dlgText;
                if (criticalCount > 0 && soonCount > 0)
                {
                    dlgText = $"На складе: {criticalCount} поз. на минимуме или ниже, и {soonCount} поз. с низким остатком (скоро минимум). Откройте модуль «Склад запчастей» для деталей.";
                }
                else if (criticalCount > 0)
                {
                    dlgText = $"На складе {criticalCount} поз. на минимальном или недостаточном остатке. Рекомендуется пополнение.";
                }
                else
                {
                    dlgText = $"На складе {soonCount} поз. с приближением к минимальному остатку — заранее спланируйте закупку.";
                }

                BeginInvoke(new Action(() =>
                    MessageBox.Show(this, dlgText, dlgTitle, MessageBoxButtons.OK,
                        criticalCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information)));
            }
        }

        private void SetLowStockKpiLook(int criticalCount, int soonCount)
        {
            string tip = string.Empty;
            if (criticalCount > 0 && soonCount > 0)
            {
                tip = $"На минимуме или ниже: {criticalCount}. Приближаются к минимуму: {soonCount}.";
                SetKpiSeverity(_lblLowStockCount, true);
            }
            else if (criticalCount > 0)
            {
                tip = $"На минимуме или ниже: {criticalCount}.";
                SetKpiSeverity(_lblLowStockCount, true);
            }
            else if (soonCount > 0)
            {
                tip = $"На минимуме: 0. Приближаются к минимуму и требуют планового пополнения: {soonCount}.";
                _lblLowStockCount.ForeColor = ThemeHelper.Accent;
            }
            else
            {
                SetKpiSeverity(_lblLowStockCount, false);
            }

            _lowStockKpiTip.SetToolTip(_lblLowStockCount, tip);
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
