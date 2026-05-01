using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using BGSK1.Services;
using BGSK1.UI;

namespace BGSK1
{
    public sealed class ReportsCenterForm : Form
    {
        private readonly DataGridView _grid;
        private readonly DataGridView _gridHistory;
        private readonly Label _lblTitle;
        private readonly DateTimePicker _dtFrom;
        private readonly DateTimePicker _dtTo;
        private readonly CheckBox _chkShowJournal;
        private DataTable _current;
        private string _currentReportCode;
        private string _currentReportTitle;
        private string _currentReportSubtitle;
        private int _printRow;

        public ReportsCenterForm()
        {
            ThemeHelper.ApplyForm(this, "Отчетный центр");
            Width = 1320;
            Height = 700;
            if (!RolePermissionService.HasPermission("module.reports"))
            {
                Shown += (s, e) => { MessageBox.Show("Нет доступа к модулю.", "Доступ запрещен", MessageBoxButtons.OK, MessageBoxIcon.Warning); Close(); };
            }

            var top = new Panel { Dock = DockStyle.Top, Height = 136 };
            _lblTitle = new Label { Left = 12, Top = 10, Width = 1000, Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold) };
            _dtFrom = new DateTimePicker { Left = 12, Top = 44, Width = 150, Value = DateTime.Today.AddDays(-30) };
            _dtTo = new DateTimePicker { Left = 166, Top = 44, Width = 150, Value = DateTime.Today };
            var lblPeriod = new Label { Left = 320, Top = 49, Width = 420, Text = "Период используется для расчета динамики, SLA и потребности в закупке." };

            var b1 = new Button { Left = 12, Top = 86, Width = 220, Height = 32, Text = "Паспорт техсостояния" };
            var b2 = new Button { Left = 236, Top = 86, Width = 220, Height = 32, Text = "Анализ заявок и SLA" };
            var b3 = new Button { Left = 460, Top = 86, Width = 220, Height = 32, Text = "План-график ТО и риски" };
            var b4 = new Button { Left = 684, Top = 86, Width = 220, Height = 32, Text = "Запчасти и закупка" };
            var be = new Button { Left = 908, Top = 86, Width = 120, Height = 32, Text = "Excel" };
            var bf = new Button { Left = 1032, Top = 86, Width = 100, Height = 32, Text = "PDF" };
            var bp = new Button { Left = 1136, Top = 86, Width = 84, Height = 32, Text = "Печать" };
            var bh = new Button { Left = 1224, Top = 52, Width = 90, Height = 28, Text = "Справка" };
            _chkShowJournal = new CheckBox { Left = 1224, Top = 92, Width = 90, Height = 24, Text = "Журнал", Checked = false };
            ThemeHelper.StyleButton(b1, ThemeHelper.Primary);
            ThemeHelper.StyleButton(b2, ThemeHelper.Primary);
            ThemeHelper.StyleButton(b3, ThemeHelper.Primary);
            ThemeHelper.StyleButton(b4, ThemeHelper.Primary);
            ThemeHelper.StyleButton(be, ThemeHelper.Secondary);
            ThemeHelper.StyleButton(bf, ThemeHelper.Secondary);
            ThemeHelper.StyleButton(bp, ThemeHelper.Secondary);
            ThemeHelper.StyleButton(bh, ThemeHelper.Accent);
            b1.Click += (s, e) => LoadEquipmentPassport();
            b2.Click += (s, e) => LoadSlaAnalytics();
            b3.Click += (s, e) => LoadMaintenanceCompliance();
            b4.Click += (s, e) => LoadPartsForecast();
            be.Click += ExportExcel_Click;
            bf.Click += ExportPdf_Click;
            bp.Click += Print_Click;
            bh.Click += (s, e) => ModuleHelpProvider.ShowHelp("reports", this);
            _chkShowJournal.CheckedChanged += (s, e) => ToggleHistory();
            top.Controls.AddRange(new Control[] { _lblTitle, _dtFrom, _dtTo, lblPeriod, b1, b2, b3, b4, be, bf, bp, bh, _chkShowJournal });

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 540 };
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
            _gridHistory = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };
            ThemeHelper.StyleGrid(_gridHistory);
            split.Panel1.Controls.Add(_grid);
            split.Panel2.Controls.Add(_gridHistory);

            Controls.Add(split);
            Controls.Add(top);
            ModuleHelpProvider.BindF11(this, "reports");
            Load += (s, e) => { LoadEquipmentPassport(); LoadHistory(); ToggleHistory(); };
        }

        private void SetReport(string code, string title, string subtitle, DataTable table)
        {
            _current = table;
            _currentReportCode = code;
            _currentReportTitle = title;
            _currentReportSubtitle = subtitle;
            _lblTitle.Text = title + $" | Строк: {table.Rows.Count}";
            _grid.DataSource = table;
            GridHeaderMap.Apply(_grid, "reportData", "Id");
        }

        private void LoadEquipmentPassport()
        {
            var from = _dtFrom.Value.Date;
            var to = _dtTo.Value.Date.AddDays(1).AddSeconds(-1);
            var subtitle = $"Период анализа заявок: {from:dd.MM.yyyy} - {_dtTo.Value:dd.MM.yyyy}";
            SetReport("equipment-passport", "Отчет 1. Паспорт технического состояния оборудования", subtitle, DomainReportService.GetEquipmentTechnicalPassport(from, to));
        }

        private void LoadSlaAnalytics()
        {
            var from = _dtFrom.Value.Date;
            var to = _dtTo.Value.Date.AddDays(1).AddSeconds(-1);
            var subtitle = $"Период SLA: {from:dd.MM.yyyy} - {_dtTo.Value:dd.MM.yyyy}";
            SetReport("sla-analytics", "Отчет 2. Анализ заявок и SLA по исполнителям", subtitle, DomainReportService.GetRepairSlaAnalytics(from, to));
        }

        private void LoadMaintenanceCompliance()
        {
            var from = _dtFrom.Value.Date;
            var to = _dtTo.Value.Date.AddDays(1).AddSeconds(-1);
            var subtitle = $"Контроль выполнения ТО, период истории: {from:dd.MM.yyyy} - {_dtTo.Value:dd.MM.yyyy}";
            SetReport("maintenance-compliance", "Отчет 3. План-график ТО, просрочки и риски", subtitle, DomainReportService.GetMaintenanceComplianceReport(from, to));
        }

        private void LoadPartsForecast()
        {
            var from = _dtFrom.Value.Date;
            var to = _dtTo.Value.Date.AddDays(1).AddSeconds(-1);
            var subtitle = $"Период расхода запчастей: {from:dd.MM.yyyy} - {_dtTo.Value:dd.MM.yyyy}";
            SetReport("parts-forecast", "Отчет 4. Движение запчастей и потребность в закупке", subtitle, DomainReportService.GetPartsProcurementForecast(from, to));
        }

        private void LoadHistory()
        {
            _gridHistory.DataSource = ReportService.GetSavedReports();
            GridHeaderMap.Apply(_gridHistory, "reportHistory", "Id");
        }

        private void ToggleHistory()
        {
            _gridHistory.Visible = _chkShowJournal.Checked;
        }

        private DataTable BuildPrintableTable()
        {
            var prepared = _current.Copy();
            foreach (DataColumn column in prepared.Columns)
            {
                if (_grid.Columns.Contains(column.ColumnName))
                {
                    column.ColumnName = _grid.Columns[column.ColumnName].HeaderText;
                }
            }
            return prepared;
        }

        private string BuildParametersJson(string format)
        {
            return
                "{"
                + "\"reportCode\":\"" + _currentReportCode + "\","
                + "\"reportTitle\":\"" + _currentReportTitle.Replace("\"", "'") + "\","
                + "\"periodFrom\":\"" + _dtFrom.Value.ToString("yyyy-MM-dd") + "\","
                + "\"periodTo\":\"" + _dtTo.Value.ToString("yyyy-MM-dd") + "\","
                + "\"format\":\"" + format + "\""
                + "}";
        }

        private void ExportExcel_Click(object sender, EventArgs e)
        {
            if (_current == null) return;
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Title = "Экспорт отчета в Excel";
                saveDialog.Filter = "Excel 2003 XML (*.xls)|*.xls";
                saveDialog.FileName = "Отчет_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xls";
                saveDialog.AddExtension = true;
                saveDialog.OverwritePrompt = true;
                if (saveDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                var savedPath = ReportService.ExportFormalExcel(
                    BuildPrintableTable(),
                    saveDialog.FileName,
                    _currentReportTitle,
                    _currentReportSubtitle,
                    BuildParametersJson("excel"));
                MessageBox.Show("Отчет сохранен:\n" + savedPath, "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadHistory();
            }
        }

        private void ExportPdf_Click(object sender, EventArgs e)
        {
            if (_current == null) return;
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Title = "Экспорт отчета в PDF";
                saveDialog.Filter = "PDF (*.pdf)|*.pdf";
                saveDialog.FileName = "Отчет_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf";
                saveDialog.AddExtension = true;
                saveDialog.OverwritePrompt = true;
                if (saveDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                var savedPath = ReportService.ExportFormalPdf(
                    BuildPrintableTable(),
                    saveDialog.FileName,
                    _currentReportTitle,
                    _currentReportSubtitle,
                    BuildParametersJson("pdf"));
                MessageBox.Show("Отчет сохранен:\n" + savedPath, "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadHistory();
            }
        }

        private void Print_Click(object sender, EventArgs e)
        {
            if (_current == null || _current.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для печати.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var doc = new PrintDocument();
            _printRow = 0;
            doc.PrintPage += Doc_PrintPage;
            using (var dlg = new PrintDialog())
            {
                dlg.Document = doc;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    doc.Print();
                }
            }
        }

        private void Doc_PrintPage(object sender, PrintPageEventArgs e)
        {
            var y = 40;
            e.Graphics.DrawString("ГБПОУ \"Брянский строительный колледж им. Н.Е. Жуковского\"", new Font("Segoe UI", 11, FontStyle.Bold), System.Drawing.Brushes.Black, 40, y);
            y += 24;
            e.Graphics.DrawString(_currentReportTitle, new Font("Segoe UI", 10, FontStyle.Bold), System.Drawing.Brushes.Black, 40, y);
            y += 26;
            e.Graphics.DrawString(_currentReportSubtitle, new Font("Segoe UI", 9), System.Drawing.Brushes.Black, 40, y);
            y += 18;
            y += 12;

            var cols = Math.Min(5, _current.Columns.Count);
            var w = (e.MarginBounds.Width - 20) / cols;
            for (var c = 0; c < cols; c++)
            {
                e.Graphics.DrawRectangle(System.Drawing.Pens.Black, 40 + c * w, y, w, 24);
                e.Graphics.DrawString(_current.Columns[c].ColumnName, new Font("Segoe UI", 8, FontStyle.Bold), System.Drawing.Brushes.Black, new System.Drawing.RectangleF(42 + c * w, y + 4, w - 4, 20));
            }

            y += 24;
            while (_printRow < _current.Rows.Count)
            {
                if (y > e.MarginBounds.Bottom - 24)
                {
                    e.HasMorePages = true;
                    return;
                }
                for (var c = 0; c < cols; c++)
                {
                    var txt = _current.Rows[_printRow][c]?.ToString() ?? string.Empty;
                    e.Graphics.DrawRectangle(System.Drawing.Pens.Black, 40 + c * w, y, w, 22);
                    e.Graphics.DrawString(txt, new Font("Segoe UI", 8), System.Drawing.Brushes.Black, new System.Drawing.RectangleF(42 + c * w, y + 4, w - 4, 18));
                }
                y += 22;
                _printRow++;
            }

            y += 22;
            e.Graphics.DrawString("Ответственный за формирование отчета: ____________________", new Font("Segoe UI", 9), System.Drawing.Brushes.Black, 40, y);
            y += 16;
            e.Graphics.DrawString("Зав. кабинетом/подразделением: ___________________________", new Font("Segoe UI", 9), System.Drawing.Brushes.Black, 40, y);
            e.HasMorePages = false;
        }
    }
}
