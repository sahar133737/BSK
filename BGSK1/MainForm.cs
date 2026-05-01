using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using BGSK1.Security;
using BGSK1.Services;
using BGSK1.UI;

namespace BGSK1
{
    public sealed class MainForm : Form
    {
        private readonly TabControl _tabs;
        private readonly DataGridView _gridEquipment;
        private readonly DataGridView _gridRequests;
        private readonly DataGridView _gridMaintenance;
        private readonly DataGridView _gridParts;
        private readonly DataGridView _gridAudit;
        private readonly DataGridView _gridBackups;
        private readonly DataGridView _gridReports;
        private readonly DataGridView _gridReportHistory;
        private readonly Label _lblUser;
        private readonly Label _lblReportTitle;
        private readonly Label _lblReportDescription;

        private readonly TextBox _txtInv;
        private readonly TextBox _txtEqName;
        private readonly TextBox _txtEqType;
        private readonly TextBox _txtEqLocation;
        private readonly TextBox _txtEqResp;
        private readonly TextBox _txtEquipmentSearch;

        private readonly ComboBox _cmbEquipmentForRequest;
        private readonly TextBox _txtProblem;
        private readonly ComboBox _cmbPriority;
        private readonly ComboBox _cmbRequestStatus;
        private readonly TextBox _txtAssigned;
        private readonly ComboBox _cmbRequestFilterStatus;
        private readonly ComboBox _cmbRequestFilterPriority;
        private readonly TextBox _txtRequestSearch;

        private readonly ComboBox _cmbEquipmentForPlan;
        private readonly TextBox _txtMaintenanceType;
        private readonly NumericUpDown _numPeriod;
        private readonly DateTimePicker _dtNext;
        private readonly TextBox _txtPlanResp;
        private readonly CheckBox _chkPlanActive;
        private readonly CheckBox _chkOnlyOverduePlans;
        private readonly TextBox _txtMaintenanceSearch;

        private readonly TextBox _txtPartName;
        private readonly TextBox _txtPartNumber;
        private readonly NumericUpDown _numPartQty;
        private readonly NumericUpDown _numPartMin;
        private readonly CheckBox _chkOnlyLowStock;
        private readonly TextBox _txtPartSearch;
        private readonly TextBox _txtBackupPath;

        private DataTable _currentReportTable;
        private string _currentReportTitle;
        private int _printRowIndex;

        public MainForm()
        {
            Text = "ИС учета ремонта и обслуживания компьютерной техники - ГБПОУ БСК";
            WindowState = FormWindowState.Maximized;
            Font = new Font("Segoe UI", 10f);
            BackColor = ThemeHelper.Surface;

            var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = ThemeHelper.DarkBg };
            _lblUser = new Label { Left = 14, Top = 17, ForeColor = Color.White, AutoSize = true, Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold) };
            header.Controls.Add(_lblUser);

            _tabs = new TabControl { Dock = DockStyle.Fill };
            _tabs.TabPages.Add(CreateEquipmentTab(out _txtInv, out _txtEqName, out _txtEqType, out _txtEqLocation, out _txtEqResp, out _txtEquipmentSearch, out _gridEquipment));
            _tabs.TabPages.Add(CreateRequestsTab(out _cmbEquipmentForRequest, out _txtProblem, out _cmbPriority, out _cmbRequestStatus, out _txtAssigned, out _cmbRequestFilterStatus, out _cmbRequestFilterPriority, out _txtRequestSearch, out _gridRequests));
            _tabs.TabPages.Add(CreateMaintenanceTab(out _cmbEquipmentForPlan, out _txtMaintenanceType, out _numPeriod, out _dtNext, out _txtPlanResp, out _chkPlanActive, out _chkOnlyOverduePlans, out _txtMaintenanceSearch, out _gridMaintenance));
            _tabs.TabPages.Add(CreatePartsTab(out _txtPartName, out _txtPartNumber, out _numPartQty, out _numPartMin, out _chkOnlyLowStock, out _txtPartSearch, out _gridParts));
            _tabs.TabPages.Add(CreateAuditTab(out _gridAudit));
            _tabs.TabPages.Add(CreateBackupsTab(out _txtBackupPath, out _gridBackups));
            _tabs.TabPages.Add(CreateReportsTab(out _gridReports, out _lblReportTitle, out _lblReportDescription));
            _tabs.TabPages.Add(CreateReportHistoryTab(out _gridReportHistory));

            Controls.Add(_tabs);
            Controls.Add(header);
            Load += MainForm_Load;
            Load += (s, e) => ThemeHelper.ApplyMinimalistTheme(this);
        }

        private static TabPage Page(string title)
        {
            return new TabPage(title) { Name = title, BackColor = ThemeHelper.Surface, Padding = new Padding(10) };
        }

        private static DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            ThemeHelper.StyleGrid(grid);
            return grid;
        }

        private static GroupBox CreateCard(string title)
        {
            return new GroupBox
            {
                Dock = DockStyle.Top,
                Height = 128,
                Text = "  " + title + "  ",
                ForeColor = ThemeHelper.Text,
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                Padding = new Padding(10)
            };
        }

        private static Label L(string text, int left, int top, int width)
        {
            return ThemeHelper.FormFieldLabel(text, left, top, width);
        }

        private TabPage CreateEquipmentTab(out TextBox txtInv, out TextBox txtName, out TextBox txtType, out TextBox txtLoc, out TextBox txtResp, out TextBox txtSearch, out DataGridView grid)
        {
            var p = Page("Техника");
            var card = CreateCard("Карточка техники");
            txtInv = new TextBox { Left = 20, Top = 48, Width = 150 };
            txtName = new TextBox { Left = 180, Top = 48, Width = 260 };
            txtType = new TextBox { Left = 450, Top = 48, Width = 160 };
            txtLoc = new TextBox { Left = 620, Top = 48, Width = 180 };
            txtResp = new TextBox { Left = 810, Top = 48, Width = 180 };
            var add = new Button { Left = 1000, Top = 46, Width = 120, Height = 30, Text = "Добавить" };
            var upd = new Button { Left = 1128, Top = 46, Width = 120, Height = 30, Text = "Обновить" };
            add.Click += BtnAddEquipment_Click;
            upd.Click += BtnUpdateEquipment_Click;

            card.Controls.AddRange(new Control[]
            {
                L("Инв. номер", 20, 20, 150), L("Наименование", 180, 20, 260), L("Тип", 450, 20, 160),
                L("Локация", 620, 20, 180), L("Ответственный", 810, 20, 180),
                txtInv, txtName, txtType, txtLoc, txtResp, add, upd
            });

            var searchPanel = new Panel { Dock = DockStyle.Top, Height = 42 };
            txtSearch = new TextBox { Left = 12, Top = 9, Width = 320 };
            var btnSearch = new Button { Left = 338, Top = 8, Width = 110, Height = 28, Text = "Поиск" };
            var btnReset = new Button { Left = 452, Top = 8, Width = 110, Height = 28, Text = "Сброс" };
            btnSearch.Click += (s, e) => ApplyEquipmentSearch();
            btnReset.Click += (s, e) =>
            {
                _txtEquipmentSearch.Clear();
                _gridEquipment.DataSource = EquipmentService.GetEquipment();
                ConfigureGrid(_gridEquipment, new Dictionary<string, string>
                {
                    { "InventoryNumber", "Инвентарный номер" }, { "Name", "Наименование" }, { "TypeName", "Тип оборудования" },
                    { "LocationName", "Локация/кабинет" }, { "ResponsiblePerson", "Ответственный" }, { "PurchaseDate", "Дата покупки" },
                    { "WarrantyUntil", "Гарантия до" }, { "StatusName", "Статус" }
                });
            };
            searchPanel.Controls.AddRange(new Control[] { L("Поиск:", 12, 10, 55), txtSearch, btnSearch, btnReset });

            grid = CreateGrid();
            grid.SelectionChanged += GridEquipment_SelectionChanged;
            p.Controls.Add(grid);
            p.Controls.Add(searchPanel);
            p.Controls.Add(card);
            return p;
        }

        private TabPage CreateRequestsTab(
            out ComboBox cmbEquipment,
            out TextBox txtProblem,
            out ComboBox cmbPriority,
            out ComboBox cmbStatus,
            out TextBox txtAssigned,
            out ComboBox cmbFilterStatus,
            out ComboBox cmbFilterPriority,
            out TextBox txtSearch,
            out DataGridView grid)
        {
            var p = Page("Заявки на ремонт");
            var card = CreateCard("Карточка заявки на ремонт");
            cmbEquipment = new ComboBox { Left = 20, Top = 48, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            txtProblem = new TextBox { Left = 290, Top = 48, Width = 380 };
            cmbPriority = new ComboBox { Left = 680, Top = 48, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbPriority.Items.AddRange(new[] { "Низкий", "Средний", "Высокий" });
            cmbPriority.SelectedIndex = 1;
            cmbStatus = new ComboBox { Left = 810, Top = 48, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new[] { "Новая", "В работе", "Ожидание", "Завершена" });
            cmbStatus.SelectedIndex = 0;
            txtAssigned = new TextBox { Left = 940, Top = 48, Width = 160 };
            var create = new Button { Left = 1110, Top = 46, Width = 95, Height = 30, Text = "Создать" };
            var update = new Button { Left = 1210, Top = 46, Width = 95, Height = 30, Text = "Обновить" };
            var close = new Button { Left = 1310, Top = 46, Width = 95, Height = 30, Text = "Закрыть" };
            create.Click += BtnCreateRequest_Click;
            update.Click += BtnUpdateRequest_Click;
            close.Click += BtnCloseRequest_Click;

            card.Controls.AddRange(new Control[]
            {
                L("Техника", 20, 20, 260), L("Описание неисправности", 290, 20, 380),
                L("Приоритет", 680, 20, 120), L("Статус", 810, 20, 120), L("Исполнитель", 940, 20, 160),
                cmbEquipment, txtProblem, cmbPriority, cmbStatus, txtAssigned, create, update, close
            });

            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 46 };
            cmbFilterStatus = new ComboBox { Left = 12, Top = 10, Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFilterStatus.Items.AddRange(new[] { "Все статусы", "Новая", "В работе", "Ожидание", "Завершена" });
            cmbFilterStatus.SelectedIndex = 0;
            cmbFilterPriority = new ComboBox { Left = 188, Top = 10, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFilterPriority.Items.AddRange(new[] { "Все приоритеты", "Низкий", "Средний", "Высокий" });
            cmbFilterPriority.SelectedIndex = 0;
            txtSearch = new TextBox { Left = 344, Top = 10, Width = 220 };
            var btnApply = new Button { Left = 570, Top = 9, Width = 120, Height = 28, Text = "Применить" };
            var btnReset = new Button { Left = 694, Top = 9, Width = 120, Height = 28, Text = "Сбросить" };
            btnApply.Click += (s, e) => ApplyRequestFilters();
            btnReset.Click += (s, e) =>
            {
                _cmbRequestFilterStatus.SelectedIndex = 0;
                _cmbRequestFilterPriority.SelectedIndex = 0;
                _gridRequests.DataSource = RepairRequestService.GetRequests();
                ConfigureGrid(_gridRequests, new Dictionary<string, string>
                {
                    { "RequestNumber", "Номер заявки" }, { "CreatedAt", "Дата создания" }, { "InventoryNumber", "Инв. номер" },
                    { "EquipmentName", "Оборудование" }, { "ProblemDescription", "Неисправность" }, { "PriorityName", "Приоритет" },
                    { "StatusName", "Статус" }, { "AssignedTo", "Исполнитель" }, { "CompletedAt", "Завершено" }
                });
            };
            filterPanel.Controls.AddRange(new Control[] { L("Фильтры:", 12, -2, 60), cmbFilterStatus, cmbFilterPriority, L("Поиск:", 344, -2, 60), txtSearch, btnApply, btnReset });

            grid = CreateGrid();
            grid.SelectionChanged += GridRequests_SelectionChanged;
            grid.CellFormatting += GridRequests_CellFormatting;
            p.Controls.Add(grid);
            p.Controls.Add(filterPanel);
            p.Controls.Add(card);
            return p;
        }

        private TabPage CreateMaintenanceTab(out ComboBox cmbEquipment, out TextBox txtType, out NumericUpDown numPeriod, out DateTimePicker dtNext, out TextBox txtResp, out CheckBox chkActive, out CheckBox chkOverdue, out TextBox txtSearch, out DataGridView grid)
        {
            var p = Page("Плановое ТО");
            var card = CreateCard("Карточка планового обслуживания");
            cmbEquipment = new ComboBox { Left = 20, Top = 48, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            txtType = new TextBox { Left = 290, Top = 48, Width = 200 };
            numPeriod = new NumericUpDown { Left = 500, Top = 48, Width = 110, Minimum = 1, Maximum = 365, Value = 30 };
            dtNext = new DateTimePicker { Left = 620, Top = 48, Width = 170 };
            txtResp = new TextBox { Left = 800, Top = 48, Width = 190 };
            chkActive = new CheckBox { Left = 1000, Top = 51, Width = 80, Text = "Активен", Checked = true };
            var add = new Button { Left = 1084, Top = 46, Width = 100, Height = 30, Text = "Добавить" };
            var update = new Button { Left = 1188, Top = 46, Width = 100, Height = 30, Text = "Обновить" };
            var done = new Button { Left = 1292, Top = 46, Width = 110, Height = 30, Text = "Отметить" };
            add.Click += BtnAddPlan_Click;
            update.Click += BtnUpdatePlan_Click;
            done.Click += BtnDonePlan_Click;

            card.Controls.AddRange(new Control[]
            {
                L("Техника", 20, 20, 260), L("Вид ТО", 290, 20, 200), L("Период (дн.)", 500, 20, 110),
                L("Следующая дата", 620, 20, 170), L("Ответственный", 800, 20, 190),
                cmbEquipment, txtType, numPeriod, dtNext, txtResp, chkActive, add, update, done
            });

            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 42 };
            chkOverdue = new CheckBox { Left = 12, Top = 12, Width = 260, Text = "Показывать только просроченные ТО" };
            txtSearch = new TextBox { Left = 280, Top = 10, Width = 260 };
            var btnSearch = new Button { Left = 546, Top = 9, Width = 100, Height = 28, Text = "Поиск" };
            btnSearch.Click += (s, e) => ApplyMaintenanceFilter();
            chkOverdue.CheckedChanged += (s, e) => ApplyMaintenanceFilter();
            filterPanel.Controls.AddRange(new Control[] { chkOverdue, L("Поиск:", 280, 10, 60), txtSearch, btnSearch });

            grid = CreateGrid();
            grid.SelectionChanged += GridMaintenance_SelectionChanged;
            p.Controls.Add(grid);
            p.Controls.Add(filterPanel);
            p.Controls.Add(card);
            return p;
        }

        private TabPage CreatePartsTab(out TextBox txtName, out TextBox txtNumber, out NumericUpDown numQty, out NumericUpDown numMin, out CheckBox chkLowStock, out TextBox txtSearch, out DataGridView grid)
        {
            var p = Page("Склад запчастей");
            var card = CreateCard("Карточка запчасти");
            txtName = new TextBox { Left = 20, Top = 48, Width = 300 };
            txtNumber = new TextBox { Left = 330, Top = 48, Width = 180 };
            numQty = new NumericUpDown { Left = 520, Top = 48, Width = 120, Minimum = 0, Maximum = 100000, Value = 1 };
            numMin = new NumericUpDown { Left = 650, Top = 48, Width = 120, Minimum = 0, Maximum = 100000, Value = 1 };
            var add = new Button { Left = 780, Top = 46, Width = 120, Height = 30, Text = "Добавить" };
            var update = new Button { Left = 908, Top = 46, Width = 120, Height = 30, Text = "Обновить" };
            var writeOff = new Button { Left = 1036, Top = 46, Width = 150, Height = 30, Text = "Списать 1 ед." };
            add.Click += BtnAddPart_Click;
            update.Click += BtnUpdatePart_Click;
            writeOff.Click += BtnWriteOffPart_Click;

            card.Controls.AddRange(new Control[]
            {
                L("Наименование", 20, 20, 300), L("Артикул", 330, 20, 180), L("Количество", 520, 20, 120), L("Мин. остаток", 650, 20, 120),
                txtName, txtNumber, numQty, numMin, add, update, writeOff
            });

            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 42 };
            chkLowStock = new CheckBox { Left = 12, Top = 12, Width = 330, Text = "Показывать только позиции ниже минимума" };
            txtSearch = new TextBox { Left = 348, Top = 10, Width = 260 };
            var btnSearch = new Button { Left = 614, Top = 9, Width = 100, Height = 28, Text = "Поиск" };
            btnSearch.Click += (s, e) => ApplyPartFilter();
            chkLowStock.CheckedChanged += (s, e) => ApplyPartFilter();
            filterPanel.Controls.AddRange(new Control[] { chkLowStock, L("Поиск:", 348, 10, 60), txtSearch, btnSearch });

            grid = CreateGrid();
            grid.SelectionChanged += GridParts_SelectionChanged;
            p.Controls.Add(grid);
            p.Controls.Add(filterPanel);
            p.Controls.Add(card);
            return p;
        }

        private TabPage CreateAuditTab(out DataGridView grid)
        {
            var p = Page("Аудит");
            var top = new Panel { Dock = DockStyle.Top, Height = 48 };
            var btn = new Button { Text = "Обновить журнал", Left = 10, Top = 10, Width = 150, Height = 30 };
            btn.Click += (s, e) => ReloadAudit();
            top.Controls.Add(btn);
            grid = CreateGrid();
            p.Controls.Add(grid);
            p.Controls.Add(top);
            return p;
        }

        private TabPage CreateBackupsTab(out TextBox txtPath, out DataGridView grid)
        {
            var p = Page("Резервные копии");
            var card = CreateCard("Управление резервными копиями");
            txtPath = new TextBox { Left = 20, Top = 48, Width = 620, Text = BackupService.GetDefaultBackupFilePath() };
            var pathInput = txtPath;
            var btnFile = new Button { Text = "Файл…", Left = 646, Top = 46, Width = 90, Height = 30 };
            var fileMenu = new ContextMenuStrip();
            fileMenu.Items.Add("Путь для новой копии…", null, (_, __) => PickBackupSaveFile(pathInput));
            fileMenu.Items.Add("Открыть .bak…", null, (_, __) => PickBackupOpenFile(pathInput));
            ThemeHelper.StyleButton(btnFile, ThemeHelper.Secondary);
            btnFile.Click += (_, __) => fileMenu.Show(btnFile, new Point(0, btnFile.Height));
            var create = new Button { Text = "Создать бэкап", Left = 744, Top = 46, Width = 120, Height = 30 };
            var restore = new Button { Text = "Восстановить", Left = 870, Top = 46, Width = 120, Height = 30 };
            var export = new Button { Text = "Экспорт аудита", Left = 996, Top = 46, Width = 130, Height = 30 };
            create.Click += BtnCreateBackup_Click;
            restore.Click += BtnRestoreBackup_Click;
            export.Click += BtnExportAudit_Click;
            card.Controls.AddRange(new Control[] { L("Путь к файлу .bak", 20, 20, 620), txtPath, btnFile, create, restore, export });
            grid = CreateGrid();
            p.Controls.Add(grid);
            p.Controls.Add(card);
            return p;
        }

        private TabPage CreateReportsTab(out DataGridView grid, out Label lblTitle, out Label lblDescription)
        {
            var p = Page("Отчеты");
            var top = new Panel { Dock = DockStyle.Top, Height = 122 };
            lblTitle = new Label { Left = 10, Top = 8, Width = 800, Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold), ForeColor = ThemeHelper.Text };
            lblDescription = new Label { Left = 10, Top = 34, Width = 1100, Height = 36, ForeColor = ThemeHelper.MutedText };

            var btnOpenRequests = new Button { Text = "Открытые заявки", Left = 10, Top = 76, Width = 145, Height = 30 };
            var btnOverdue = new Button { Text = "Просроченное ТО", Left = 160, Top = 76, Width = 145, Height = 30 };
            var btnLowStock = new Button { Text = "Дефицит запчастей", Left = 310, Top = 76, Width = 155, Height = 30 };
            var btnWorkload = new Button { Text = "Нагрузка исполнителей", Left = 470, Top = 76, Width = 175, Height = 30 };
            var btnExport = new Button { Text = "Экспорт текущего отчета", Left = 650, Top = 76, Width = 190, Height = 30 };
            var btnPrint = new Button { Text = "Печать", Left = 846, Top = 76, Width = 110, Height = 30 };

            btnOpenRequests.Click += (s, e) => LoadReport("Открытые заявки на ремонт", "Список всех заявок, которые еще не переведены в статус 'Завершена'.", DomainReportService.GetOpenRepairRequests());
            btnOverdue.Click += (s, e) => LoadReport("Просроченное плановое обслуживание", "Оборудование, у которого дата следующего ТО уже прошла.", DomainReportService.GetOverdueMaintenance());
            btnLowStock.Click += (s, e) => LoadReport("Дефицит запчастей", "Позиции склада, где остаток меньше или равен минимально допустимому.", DomainReportService.GetLowStockReport());
            btnWorkload.Click += (s, e) => LoadReport("Нагрузка исполнителей за 30 дней", "Количество заявок в работе по ответственным сотрудникам.", DomainReportService.GetEngineerWorkload(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow));
            btnExport.Click += BtnExportCurrentReport_Click;
            btnPrint.Click += BtnPrintCurrentReport_Click;

            top.Controls.AddRange(new Control[] { lblTitle, lblDescription, btnOpenRequests, btnOverdue, btnLowStock, btnWorkload, btnExport, btnPrint });
            grid = CreateGrid();
            p.Controls.Add(grid);
            p.Controls.Add(top);
            return p;
        }

        private TabPage CreateReportHistoryTab(out DataGridView grid)
        {
            var p = Page("Журнал отчетов");
            var top = new Panel { Dock = DockStyle.Top, Height = 46 };
            var btnRefresh = new Button { Left = 12, Top = 9, Width = 170, Height = 28, Text = "Обновить журнал отчетов" };
            btnRefresh.Click += (s, e) =>
            {
                _gridReportHistory.DataSource = ReportService.GetSavedReports();
                ConfigureGrid(_gridReportHistory, new Dictionary<string, string>
                {
                    { "ReportName", "Название отчета" }, { "ReportType", "Формат" }, { "CreatedAt", "Создан" },
                    { "CreatedBy", "Пользователь" }, { "FilePath", "Путь к файлу" }
                });
            };
            top.Controls.Add(btnRefresh);

            grid = CreateGrid();
            p.Controls.Add(grid);
            p.Controls.Add(top);
            return p;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            _lblUser.Text = $"Пользователь: {CurrentUserContext.FullName} ({CurrentUserContext.RoleName})";
            ReloadAll();
        }

        private void ReloadAll()
        {
            _gridEquipment.DataSource = EquipmentService.GetEquipment();
            _gridRequests.DataSource = RepairRequestService.GetRequests();
            _gridMaintenance.DataSource = MaintenanceService.GetPlans();
            _gridParts.DataSource = SparePartService.GetParts();
            _gridAudit.DataSource = AuditService.GetAuditLog(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
            _gridBackups.DataSource = BackupService.GetBackups();
            _gridReportHistory.DataSource = ReportService.GetSavedReports();

            var eq = EquipmentService.GetEquipmentLookup();
            _cmbEquipmentForRequest.DataSource = eq.Copy();
            _cmbEquipmentForRequest.DisplayMember = "DisplayName";
            _cmbEquipmentForRequest.ValueMember = "Id";
            _cmbEquipmentForPlan.DataSource = eq;
            _cmbEquipmentForPlan.DisplayMember = "DisplayName";
            _cmbEquipmentForPlan.ValueMember = "Id";

            LoadReport("Открытые заявки на ремонт", "Список всех заявок, которые еще не переведены в статус 'Завершена'.", DomainReportService.GetOpenRepairRequests());
            ApplyAllGridStyles();
            _cmbRequestStatus.SelectedIndex = 0;
        }

        private void ApplyRequestFilters()
        {
            var table = RepairRequestService.GetRequests();
            var view = table.DefaultView;
            var filters = new List<string>();

            if (_cmbRequestFilterStatus.SelectedIndex > 0)
            {
                var status = _cmbRequestFilterStatus.Text.Replace("'", "''");
                filters.Add($"StatusName = '{status}'");
            }

            if (_cmbRequestFilterPriority.SelectedIndex > 0)
            {
                var priority = _cmbRequestFilterPriority.Text.Replace("'", "''");
                filters.Add($"PriorityName = '{priority}'");
            }

            if (!string.IsNullOrWhiteSpace(_txtRequestSearch.Text))
            {
                var search = _txtRequestSearch.Text.Replace("'", "''");
                filters.Add($"(RequestNumber LIKE '%{search}%' OR EquipmentName LIKE '%{search}%' OR ProblemDescription LIKE '%{search}%' OR AssignedTo LIKE '%{search}%')");
            }

            view.RowFilter = string.Join(" AND ", filters.ToArray());
            _gridRequests.DataSource = view.ToTable();
            ConfigureGrid(_gridRequests, new Dictionary<string, string>
            {
                { "RequestNumber", "Номер заявки" }, { "CreatedAt", "Дата создания" }, { "InventoryNumber", "Инв. номер" },
                { "EquipmentName", "Оборудование" }, { "ProblemDescription", "Неисправность" }, { "PriorityName", "Приоритет" },
                { "StatusName", "Статус" }, { "AssignedTo", "Исполнитель" }, { "CompletedAt", "Завершено" }
            });
        }

        private void ApplyMaintenanceFilter()
        {
            if (_chkOnlyOverduePlans.Checked)
            {
                _gridMaintenance.DataSource = DomainReportService.GetOverdueMaintenance();
                ConfigureGrid(_gridMaintenance, new Dictionary<string, string>
                {
                    { "InventoryNumber", "Инв. номер" }, { "EquipmentName", "Оборудование" }, { "MaintenanceType", "Вид ТО" },
                    { "NextDate", "Следующее ТО" }, { "ResponsiblePerson", "Ответственный" }
                }, "IsActive");
                return;
            }

            var table = MaintenanceService.GetPlans();
            if (!string.IsNullOrWhiteSpace(_txtMaintenanceSearch.Text))
            {
                var view = table.DefaultView;
                var search = _txtMaintenanceSearch.Text.Replace("'", "''");
                view.RowFilter = $"EquipmentName LIKE '%{search}%' OR InventoryNumber LIKE '%{search}%' OR MaintenanceType LIKE '%{search}%' OR ResponsiblePerson LIKE '%{search}%'";
                _gridMaintenance.DataSource = view.ToTable();
            }
            else
            {
                _gridMaintenance.DataSource = table;
            }
            ConfigureGrid(_gridMaintenance, new Dictionary<string, string>
            {
                { "InventoryNumber", "Инв. номер" }, { "EquipmentName", "Оборудование" }, { "MaintenanceType", "Вид ТО" },
                { "PeriodDays", "Период (дни)" }, { "NextDate", "Следующее ТО" }, { "ResponsiblePerson", "Ответственный" }
            }, "IsActive");
        }

        private void ApplyPartFilter()
        {
            var table = _chkOnlyLowStock.Checked ? SparePartService.GetLowStock() : SparePartService.GetParts();
            if (!string.IsNullOrWhiteSpace(_txtPartSearch.Text))
            {
                var view = table.DefaultView;
                var search = _txtPartSearch.Text.Replace("'", "''");
                view.RowFilter = $"PartName LIKE '%{search}%' OR PartNumber LIKE '%{search}%'";
                _gridParts.DataSource = view.ToTable();
            }
            else
            {
                _gridParts.DataSource = table;
            }
            ConfigureGrid(_gridParts, new Dictionary<string, string>
            {
                { "PartName", "Запчасть" }, { "PartNumber", "Артикул" }, { "QuantityInStock", "Остаток" },
                { "MinQuantity", "Минимум" }, { "UnitName", "Ед. изм." }, { "LastUpdated", "Обновлено" }
            });
        }

        private void ApplyEquipmentSearch()
        {
            var table = EquipmentService.GetEquipment();
            if (!string.IsNullOrWhiteSpace(_txtEquipmentSearch.Text))
            {
                var view = table.DefaultView;
                var search = _txtEquipmentSearch.Text.Replace("'", "''");
                view.RowFilter = $"InventoryNumber LIKE '%{search}%' OR Name LIKE '%{search}%' OR TypeName LIKE '%{search}%' OR LocationName LIKE '%{search}%' OR ResponsiblePerson LIKE '%{search}%'";
                _gridEquipment.DataSource = view.ToTable();
            }
            else
            {
                _gridEquipment.DataSource = table;
            }

            ConfigureGrid(_gridEquipment, new Dictionary<string, string>
            {
                { "InventoryNumber", "Инвентарный номер" }, { "Name", "Наименование" }, { "TypeName", "Тип оборудования" },
                { "LocationName", "Локация/кабинет" }, { "ResponsiblePerson", "Ответственный" }, { "PurchaseDate", "Дата покупки" },
                { "WarrantyUntil", "Гарантия до" }, { "StatusName", "Статус" }
            });
        }

        private void ApplyAllGridStyles()
        {
            ConfigureGrid(_gridEquipment, new Dictionary<string, string>
            {
                { "InventoryNumber", "Инвентарный номер" }, { "Name", "Наименование" }, { "TypeName", "Тип оборудования" },
                { "LocationName", "Локация/кабинет" }, { "ResponsiblePerson", "Ответственный" }, { "PurchaseDate", "Дата покупки" },
                { "WarrantyUntil", "Гарантия до" }, { "StatusName", "Статус" }
            });

            ConfigureGrid(_gridRequests, new Dictionary<string, string>
            {
                { "RequestNumber", "Номер заявки" }, { "CreatedAt", "Дата создания" }, { "InventoryNumber", "Инв. номер" },
                { "EquipmentName", "Оборудование" }, { "ProblemDescription", "Неисправность" }, { "PriorityName", "Приоритет" },
                { "StatusName", "Статус" }, { "AssignedTo", "Исполнитель" }, { "CompletedAt", "Завершено" }
            });

            ConfigureGrid(_gridMaintenance, new Dictionary<string, string>
            {
                { "InventoryNumber", "Инв. номер" }, { "EquipmentName", "Оборудование" }, { "MaintenanceType", "Вид ТО" },
                { "PeriodDays", "Период (дни)" }, { "NextDate", "Следующее ТО" }, { "ResponsiblePerson", "Ответственный" }
            }, "IsActive");

            ConfigureGrid(_gridParts, new Dictionary<string, string>
            {
                { "PartName", "Запчасть" }, { "PartNumber", "Артикул" }, { "QuantityInStock", "Остаток" },
                { "MinQuantity", "Минимум" }, { "UnitName", "Ед. изм." }, { "LastUpdated", "Обновлено" }
            });

            ConfigureGrid(_gridAudit, new Dictionary<string, string>
            {
                { "Timestamp", "Дата/время" }, { "Email", "Пользователь" }, { "TableName", "Таблица" },
                { "OperationType", "Операция" }, { "RecordId", "ID записи" }, { "IPAddress", "IP адрес" }
            });

            ConfigureGrid(_gridBackups, new Dictionary<string, string>
            {
                { "FileName", "Имя файла" }, { "FilePath", "Путь" }, { "SizeBytes", "Размер (байт)" },
                { "CreationDate", "Дата создания" }, { "Comment", "Комментарий" }, { "IsAuto", "Авто" }
            });

            ConfigureGrid(_gridReports, null);
            ConfigureGrid(_gridReportHistory, new Dictionary<string, string>
            {
                { "ReportName", "Название отчета" }, { "ReportType", "Формат" }, { "CreatedAt", "Создан" },
                { "CreatedBy", "Пользователь" }, { "FilePath", "Путь к файлу" }
            });
        }

        private static void ConfigureGrid(DataGridView grid, Dictionary<string, string> headers, params string[] hideColumns)
        {
            if (grid.DataSource == null)
            {
                return;
            }

            if (grid.Columns.Contains("Id"))
            {
                grid.Columns["Id"].Visible = false;
            }

            foreach (var name in hideColumns)
            {
                if (!string.IsNullOrEmpty(name) && grid.Columns.Contains(name))
                {
                    grid.Columns[name].Visible = false;
                }
            }

            if (headers != null)
            {
                foreach (DataGridViewColumn col in grid.Columns)
                {
                    if (headers.ContainsKey(col.Name))
                    {
                        col.HeaderText = headers[col.Name];
                    }
                }
            }
        }

        private void ReloadAudit()
        {
            _gridAudit.DataSource = AuditService.GetAuditLog(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
            ConfigureGrid(_gridAudit, new Dictionary<string, string>
            {
                { "Timestamp", "Дата/время" }, { "Email", "Пользователь" }, { "TableName", "Таблица" },
                { "OperationType", "Операция" }, { "RecordId", "ID записи" }, { "IPAddress", "IP адрес" }
            });
        }

        private void LoadReport(string title, string description, DataTable data)
        {
            _lblReportTitle.Text = title;
            _lblReportDescription.Text = description + $"  |  Строк: {data.Rows.Count}";
            _gridReports.DataSource = data;
            _currentReportTable = data;
            _currentReportTitle = title;
            ConfigureGrid(_gridReports, GetReportHeaders(data));
        }

        private static Dictionary<string, string> GetReportHeaders(DataTable data)
        {
            var map = new Dictionary<string, string>
            {
                { "RequestNumber", "Номер заявки" }, { "CreatedAt", "Дата создания" }, { "InventoryNumber", "Инв. номер" },
                { "EquipmentName", "Оборудование" }, { "PriorityName", "Приоритет" }, { "StatusName", "Статус" },
                { "AssignedTo", "Исполнитель" }, { "MaintenanceType", "Вид ТО" }, { "NextDate", "Следующая дата ТО" },
                { "ResponsiblePerson", "Ответственный" }, { "PartName", "Запчасть" }, { "PartNumber", "Артикул" },
                { "QuantityInStock", "Остаток" }, { "MinQuantity", "Минимум" }, { "RequestsCount", "Количество заявок" }
            };
            return map;
        }

        private static void PickBackupSaveFile(TextBox txtPath)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Файлы резервных копий (*.bak)|*.bak";
                dialog.FileName = string.IsNullOrWhiteSpace(txtPath.Text)
                    ? "BGSK1_manual.bak"
                    : Path.GetFileName(txtPath.Text);
                var dir = Path.GetDirectoryName(txtPath.Text);
                dialog.InitialDirectory = !string.IsNullOrEmpty(dir) && Directory.Exists(dir)
                    ? dir
                    : BackupService.GetRecommendedBackupDirectory();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = dialog.FileName;
                }
            }
        }

        private static void PickBackupOpenFile(TextBox txtPath)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Файлы резервных копий (*.bak)|*.bak";
                dialog.FileName = string.IsNullOrWhiteSpace(txtPath.Text)
                    ? string.Empty
                    : Path.GetFileName(txtPath.Text);
                var dir = Path.GetDirectoryName(txtPath.Text);
                dialog.InitialDirectory = !string.IsNullOrEmpty(dir) && Directory.Exists(dir)
                    ? dir
                    : BackupService.GetRecommendedBackupDirectory();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = dialog.FileName;
                }
            }
        }

        private void BtnCreateBackup_Click(object sender, EventArgs e)
        {
            RunAction(() =>
            {
                var backupPath = _txtBackupPath.Text.Trim();
                if (string.IsNullOrWhiteSpace(backupPath))
                {
                    throw new Exception("Укажите путь к файлу резервной копии.");
                }

                if (!backupPath.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                {
                    backupPath += ".bak";
                }

                BackupService.CreateBackupToFile(backupPath, "Ручной запуск из интерфейса", false);
                _gridBackups.DataSource = BackupService.GetBackups();
                ConfigureGrid(_gridBackups, new Dictionary<string, string>
                {
                    { "FileName", "Имя файла" }, { "FilePath", "Путь" }, { "SizeBytes", "Размер (байт)" },
                    { "CreationDate", "Дата создания" }, { "Comment", "Комментарий" }, { "IsAuto", "Авто" }
                });
                MessageBox.Show("Бэкап успешно создан.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        }

        private void BtnRestoreBackup_Click(object sender, EventArgs e)
        {
            if (_gridBackups.CurrentRow == null)
            {
                MessageBox.Show("Выберите резервную копию в таблице.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var filePath = _gridBackups.CurrentRow.Cells["FilePath"]?.Value?.ToString();
            if (string.IsNullOrWhiteSpace(filePath))
            {
                MessageBox.Show("У выбранной записи отсутствует путь к файлу.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MessageBox.Show("Восстановление перезапишет текущие данные. Продолжить?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            RunAction(() =>
            {
                BackupService.RestoreBackup(filePath);
                MessageBox.Show("Восстановление завершено.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ReloadAll();
            });
        }

        private void BtnAddEquipment_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtInv.Text) || string.IsNullOrWhiteSpace(_txtEqName.Text))
            {
                MessageBox.Show("Заполните поля 'Инв. номер' и 'Наименование'.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RunAction(() =>
            {
                EquipmentService.AddEquipment(_txtInv.Text.Trim(), _txtEqName.Text.Trim(), _txtEqType.Text.Trim(), _txtEqLocation.Text.Trim(), _txtEqResp.Text.Trim());
                ReloadAll();
            });
        }

        private void BtnUpdateEquipment_Click(object sender, EventArgs e)
        {
            if (_gridEquipment.CurrentRow == null)
            {
                MessageBox.Show("Выберите запись техники для обновления.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = Convert.ToInt32(_gridEquipment.CurrentRow.Cells["Id"].Value);
            var status = _gridEquipment.CurrentRow.Cells["StatusName"].Value?.ToString() ?? "В эксплуатации";
            RunAction(() =>
            {
                EquipmentService.UpdateEquipment(id, _txtInv.Text.Trim(), _txtEqName.Text.Trim(), _txtEqType.Text.Trim(), _txtEqLocation.Text.Trim(), _txtEqResp.Text.Trim(), status);
                ReloadAll();
            });
        }

        private void BtnCreateRequest_Click(object sender, EventArgs e)
        {
            if (_cmbEquipmentForRequest.SelectedValue == null || string.IsNullOrWhiteSpace(_txtProblem.Text))
            {
                MessageBox.Show("Выберите технику и заполните описание неисправности.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RunAction(() =>
            {
                RepairRequestService.CreateRequest(Convert.ToInt32(_cmbEquipmentForRequest.SelectedValue), _txtProblem.Text.Trim(), _cmbPriority.Text, _txtAssigned.Text.Trim());
                ReloadAll();
            });
        }

        private void BtnCloseRequest_Click(object sender, EventArgs e)
        {
            if (_gridRequests.CurrentRow == null)
            {
                MessageBox.Show("Выберите заявку для закрытия.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = Convert.ToInt32(_gridRequests.CurrentRow.Cells["Id"].Value);
            RunAction(() =>
            {
                RepairRequestService.UpdateRequestStatus(id, "Завершена", _txtAssigned.Text.Trim());
                ReloadAll();
            });
        }

        private void BtnUpdateRequest_Click(object sender, EventArgs e)
        {
            if (_gridRequests.CurrentRow == null)
            {
                MessageBox.Show("Выберите заявку для редактирования.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = Convert.ToInt32(_gridRequests.CurrentRow.Cells["Id"].Value);
            RunAction(() =>
            {
                RepairRequestService.UpdateRequest(
                    id,
                    Convert.ToInt32(_cmbEquipmentForRequest.SelectedValue),
                    _txtProblem.Text.Trim(),
                    _cmbPriority.Text,
                    _cmbRequestStatus.Text,
                    _txtAssigned.Text.Trim());
                ReloadAll();
            });
        }

        private void BtnAddPlan_Click(object sender, EventArgs e)
        {
            if (_cmbEquipmentForPlan.SelectedValue == null || string.IsNullOrWhiteSpace(_txtMaintenanceType.Text))
            {
                MessageBox.Show("Выберите технику и заполните вид ТО.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RunAction(() =>
            {
                MaintenanceService.AddPlan(Convert.ToInt32(_cmbEquipmentForPlan.SelectedValue), _txtMaintenanceType.Text.Trim(), Convert.ToInt32(_numPeriod.Value), _dtNext.Value.Date, _txtPlanResp.Text.Trim());
                ReloadAll();
            });
        }

        private void BtnDonePlan_Click(object sender, EventArgs e)
        {
            if (_gridMaintenance.CurrentRow == null)
            {
                MessageBox.Show("Выберите план ТО для отметки выполнения.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = Convert.ToInt32(_gridMaintenance.CurrentRow.Cells["Id"].Value);
            RunAction(() =>
            {
                MaintenanceService.MarkCompleted(id, "Выполнено по регламенту");
                ReloadAll();
            });
        }

        private void BtnUpdatePlan_Click(object sender, EventArgs e)
        {
            if (_gridMaintenance.CurrentRow == null || _gridMaintenance.CurrentRow.Cells["Id"] == null)
            {
                MessageBox.Show("Выберите активный план ТО для обновления.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = Convert.ToInt32(_gridMaintenance.CurrentRow.Cells["Id"].Value);
            RunAction(() =>
            {
                MaintenanceService.UpdatePlan(
                    id,
                    Convert.ToInt32(_cmbEquipmentForPlan.SelectedValue),
                    _txtMaintenanceType.Text.Trim(),
                    Convert.ToInt32(_numPeriod.Value),
                    _dtNext.Value.Date,
                    _txtPlanResp.Text.Trim(),
                    _chkPlanActive.Checked);
                ReloadAll();
            });
        }

        private void BtnAddPart_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtPartName.Text))
            {
                MessageBox.Show("Укажите наименование запчасти.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RunAction(() =>
            {
                SparePartService.AddPart(_txtPartName.Text.Trim(), _txtPartNumber.Text.Trim(), Convert.ToInt32(_numPartQty.Value), Convert.ToInt32(_numPartMin.Value), "шт");
                ReloadAll();
            });
        }

        private void BtnWriteOffPart_Click(object sender, EventArgs e)
        {
            if (_gridParts.CurrentRow == null)
            {
                MessageBox.Show("Выберите запчасть для списания.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = Convert.ToInt32(_gridParts.CurrentRow.Cells["Id"].Value);
            RunAction(() =>
            {
                SparePartService.WriteOffPart(id, 1);
                ReloadAll();
            });
        }

        private void BtnUpdatePart_Click(object sender, EventArgs e)
        {
            if (_gridParts.CurrentRow == null || !_gridParts.Columns.Contains("Id"))
            {
                MessageBox.Show("Выберите запись запчасти из полного списка.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = Convert.ToInt32(_gridParts.CurrentRow.Cells["Id"].Value);
            RunAction(() =>
            {
                SparePartService.UpdatePart(
                    id,
                    _txtPartName.Text.Trim(),
                    _txtPartNumber.Text.Trim(),
                    Convert.ToInt32(_numPartQty.Value),
                    Convert.ToInt32(_numPartMin.Value),
                    "шт");
                ReloadAll();
            });
        }

        private void BtnExportAudit_Click(object sender, EventArgs e)
        {
            RunAction(() =>
            {
                var data = AuditService.GetAuditLog(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
                var exportDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                var filePath = ReportService.ExportDataTableToCsv(data, "Аудит_РемонтИС", exportDirectory);
                MessageBox.Show($"Файл сохранен:\n{filePath}", "Экспорт завершен", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        }

        private void BtnExportCurrentReport_Click(object sender, EventArgs e)
        {
            var table = _gridReports.DataSource as DataTable;
            if (table == null)
            {
                MessageBox.Show("Нет данных для экспорта.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            RunAction(() =>
            {
                var exportDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                var filePath = ReportService.ExportDataTableToCsv(table, "Отчет_РемонтИС", exportDirectory);
                var htmlPath = ReportService.ExportDataTableToHtml(table, "Отчет_РемонтИС", exportDirectory, _lblReportTitle.Text, _lblReportDescription.Text);
                var xlsPath = ReportService.ExportDataTableToExcelCompatible(table, "Отчет_РемонтИС", exportDirectory);
                var docPath = ReportService.ExportDataTableToWordCompatible(table, "Отчет_РемонтИС", exportDirectory);
                MessageBox.Show($"Файлы сохранены:\nCSV: {filePath}\nHTML: {htmlPath}\nXLS: {xlsPath}\nDOC: {docPath}", "Экспорт завершен", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        }

        private void BtnPrintCurrentReport_Click(object sender, EventArgs e)
        {
            if (_currentReportTable == null || _currentReportTable.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для печати.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var printDoc = new PrintDocument();
            _printRowIndex = 0;
            printDoc.PrintPage += PrintDoc_PrintPage;

            using (var dialog = new PrintDialog())
            {
                dialog.Document = printDoc;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    printDoc.Print();
                }
            }
        }

        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            var y = 40;
            e.Graphics.DrawString("ГБПОУ \"Брянский строительный колледж им. Н.Е. Жуковского\"", new Font("Segoe UI", 11, FontStyle.Bold), Brushes.Black, 40, y);
            y += 26;
            e.Graphics.DrawString(_currentReportTitle ?? "Отчет", new Font("Segoe UI", 10, FontStyle.Bold), Brushes.Black, 40, y);
            y += 22;
            e.Graphics.DrawString("Дата печати: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm"), new Font("Segoe UI", 9), Brushes.Black, 40, y);
            y += 28;

            var cols = Math.Min(5, _currentReportTable.Columns.Count);
            var colWidth = (e.MarginBounds.Width - 20) / cols;
            for (var c = 0; c < cols; c++)
            {
                e.Graphics.DrawRectangle(Pens.Black, 40 + c * colWidth, y, colWidth, 24);
                e.Graphics.DrawString(_currentReportTable.Columns[c].ColumnName, new Font("Segoe UI", 8, FontStyle.Bold), Brushes.Black, new RectangleF(42 + c * colWidth, y + 4, colWidth - 4, 20));
            }

            y += 24;
            while (_printRowIndex < _currentReportTable.Rows.Count)
            {
                if (y > e.MarginBounds.Bottom - 24)
                {
                    e.HasMorePages = true;
                    return;
                }

                for (var c = 0; c < cols; c++)
                {
                    var text = _currentReportTable.Rows[_printRowIndex][c]?.ToString() ?? string.Empty;
                    e.Graphics.DrawRectangle(Pens.Black, 40 + c * colWidth, y, colWidth, 22);
                    e.Graphics.DrawString(text, new Font("Segoe UI", 8), Brushes.Black, new RectangleF(42 + c * colWidth, y + 4, colWidth - 4, 18));
                }

                y += 22;
                _printRowIndex++;
            }

            e.HasMorePages = false;
        }

        private static void RunAction(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Операция завершилась ошибкой:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GridEquipment_SelectionChanged(object sender, EventArgs e)
        {
            if (_gridEquipment.CurrentRow == null)
            {
                return;
            }

            _txtInv.Text = _gridEquipment.CurrentRow.Cells["InventoryNumber"]?.Value?.ToString() ?? string.Empty;
            _txtEqName.Text = _gridEquipment.CurrentRow.Cells["Name"]?.Value?.ToString() ?? string.Empty;
            _txtEqType.Text = _gridEquipment.CurrentRow.Cells["TypeName"]?.Value?.ToString() ?? string.Empty;
            _txtEqLocation.Text = _gridEquipment.CurrentRow.Cells["LocationName"]?.Value?.ToString() ?? string.Empty;
            _txtEqResp.Text = _gridEquipment.CurrentRow.Cells["ResponsiblePerson"]?.Value?.ToString() ?? string.Empty;
        }

        private void GridRequests_SelectionChanged(object sender, EventArgs e)
        {
            if (_gridRequests.CurrentRow == null)
            {
                return;
            }

            _txtProblem.Text = _gridRequests.CurrentRow.Cells["ProblemDescription"]?.Value?.ToString() ?? string.Empty;
            _cmbPriority.Text = _gridRequests.CurrentRow.Cells["PriorityName"]?.Value?.ToString() ?? "Средний";
            _cmbRequestStatus.Text = _gridRequests.CurrentRow.Cells["StatusName"]?.Value?.ToString() ?? "Новая";
            _txtAssigned.Text = _gridRequests.CurrentRow.Cells["AssignedTo"]?.Value?.ToString() ?? string.Empty;
        }

        private void GridRequests_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (!_gridRequests.Columns.Contains("StatusName") || e.RowIndex < 0)
            {
                return;
            }

            if (_gridRequests.Columns[e.ColumnIndex].Name != "StatusName")
            {
                return;
            }

            var status = (_gridRequests.Rows[e.RowIndex].Cells["StatusName"].Value ?? string.Empty).ToString();
            if (status == "Новая")
            {
                e.CellStyle.BackColor = Color.FromArgb(236, 245, 255);
            }
            else if (status == "В работе")
            {
                e.CellStyle.BackColor = Color.FromArgb(221, 236, 252);
            }
            else if (status == "Ожидание")
            {
                e.CellStyle.BackColor = Color.FromArgb(243, 247, 253);
            }
            else if (status == "Завершена")
            {
                e.CellStyle.BackColor = Color.FromArgb(226, 242, 235);
            }
        }

        private void GridMaintenance_SelectionChanged(object sender, EventArgs e)
        {
            if (_gridMaintenance.CurrentRow == null)
            {
                return;
            }

            if (_gridMaintenance.Columns.Contains("MaintenanceType"))
            {
                _txtMaintenanceType.Text = _gridMaintenance.CurrentRow.Cells["MaintenanceType"]?.Value?.ToString() ?? string.Empty;
            }

            if (_gridMaintenance.Columns.Contains("PeriodDays"))
            {
                _numPeriod.Value = Convert.ToDecimal(_gridMaintenance.CurrentRow.Cells["PeriodDays"]?.Value ?? 30);
            }

            if (_gridMaintenance.Columns.Contains("NextDate"))
            {
                DateTime date;
                if (DateTime.TryParse(_gridMaintenance.CurrentRow.Cells["NextDate"]?.Value?.ToString(), out date))
                {
                    _dtNext.Value = date;
                }
            }

            _txtPlanResp.Text = _gridMaintenance.Columns.Contains("ResponsiblePerson")
                ? _gridMaintenance.CurrentRow.Cells["ResponsiblePerson"]?.Value?.ToString() ?? string.Empty
                : string.Empty;
            _chkPlanActive.Checked = !_gridMaintenance.Columns.Contains("IsActive") || Convert.ToBoolean(_gridMaintenance.CurrentRow.Cells["IsActive"]?.Value ?? true);
        }

        private void GridParts_SelectionChanged(object sender, EventArgs e)
        {
            if (_gridParts.CurrentRow == null)
            {
                return;
            }

            _txtPartName.Text = _gridParts.CurrentRow.Cells["PartName"]?.Value?.ToString() ?? string.Empty;
            _txtPartNumber.Text = _gridParts.CurrentRow.Cells["PartNumber"]?.Value?.ToString() ?? string.Empty;

            if (_gridParts.Columns.Contains("QuantityInStock"))
            {
                _numPartQty.Value = Convert.ToDecimal(_gridParts.CurrentRow.Cells["QuantityInStock"]?.Value ?? 0);
            }

            if (_gridParts.Columns.Contains("MinQuantity"))
            {
                _numPartMin.Value = Convert.ToDecimal(_gridParts.CurrentRow.Cells["MinQuantity"]?.Value ?? 0);
            }
        }
    }
}
