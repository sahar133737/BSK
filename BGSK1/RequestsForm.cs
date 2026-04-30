using System;
using System.Data;
using System.Windows.Forms;
using BGSK1.Services;
using BGSK1.UI;

namespace BGSK1
{
    public sealed class RequestsForm : Form
    {
        private readonly DataGridView _gridRequests;
        private readonly DataGridView _gridParts;
        private readonly ComboBox _cmbEquipment;
        private readonly TextBox _txtProblem;
        private readonly ComboBox _cmbPriority;
        private readonly ComboBox _cmbStatus;
        private readonly TextBox _txtAssigned;
        private readonly ComboBox _cmbFilterStatus;
        private readonly TextBox _txtSearch;
        private readonly ComboBox _cmbRequest;
        private readonly ComboBox _cmbPart;
        private readonly NumericUpDown _numQty;

        public RequestsForm()
        {
            ThemeHelper.ApplyForm(this, "Заявки на ремонт");
            Width = 1280;
            Height = 760;
            if (!RolePermissionService.HasPermission("module.requests"))
            {
                Shown += (s, e) => { MessageBox.Show("Нет доступа к модулю.", "Доступ запрещен", MessageBoxButtons.OK, MessageBoxIcon.Warning); Close(); };
            }

            var card = new GroupBox { Dock = DockStyle.Top, Height = 118, Text = "  Карточка заявки  " };
            _cmbEquipment = new ComboBox { Left = 12, Top = 45, Width = 230, DropDownStyle = ComboBoxStyle.DropDownList };
            _txtProblem = new TextBox { Left = 248, Top = 45, Width = 310 };
            _cmbPriority = new ComboBox { Left = 564, Top = 45, Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbPriority.Items.AddRange(new[] { "Низкий", "Средний", "Высокий" });
            _cmbPriority.SelectedIndex = 1;
            _cmbStatus = new ComboBox { Left = 680, Top = 45, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbStatus.Items.AddRange(new[] { "Новая", "В работе", "Ожидание", "Завершена" });
            _cmbStatus.SelectedIndex = 0;
            _txtAssigned = new TextBox { Left = 806, Top = 45, Width = 160 };
            var btnCreate = new Button { Left = 972, Top = 42, Width = 90, Height = 30, Text = "Создать" };
            var btnUpdate = new Button { Left = 1066, Top = 42, Width = 95, Height = 30, Text = "Обновить" };
            var btnClose = new Button { Left = 1165, Top = 42, Width = 90, Height = 30, Text = "Закрыть" };
            ThemeHelper.StyleButton(btnCreate, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnUpdate, ThemeHelper.Secondary);
            ThemeHelper.StyleButton(btnClose, ThemeHelper.Success);
            btnCreate.Click += BtnCreateRequest_Click;
            btnUpdate.Click += BtnUpdateRequest_Click;
            btnClose.Click += BtnCloseRequest_Click;
            card.Controls.AddRange(new Control[]
            {
                LabelAt("Техника",12,27,230), LabelAt("Неисправность",248,27,310), LabelAt("Приоритет",564,27,110),
                LabelAt("Статус",680,27,120), LabelAt("Исполнитель",806,27,160),
                _cmbEquipment,_txtProblem,_cmbPriority,_cmbStatus,_txtAssigned,btnCreate,btnUpdate,btnClose
            });

            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 44 };
            _cmbFilterStatus = new ComboBox { Left = 12, Top = 9, Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbFilterStatus.Items.AddRange(new[] { "Все статусы", "Новая", "В работе", "Ожидание", "Завершена" });
            _cmbFilterStatus.SelectedIndex = 0;
            _txtSearch = new TextBox { Left = 188, Top = 10, Width = 260 };
            var btnFilter = new Button { Left = 454, Top = 8, Width = 110, Height = 28, Text = "Применить" };
            var btnReset = new Button { Left = 568, Top = 8, Width = 110, Height = 28, Text = "Сбросить" };
            btnFilter.Click += (s, e) => ApplyFilter();
            btnReset.Click += (s, e) => { _cmbFilterStatus.SelectedIndex = 0; _txtSearch.Clear(); LoadRequests(); };
            filterPanel.Controls.AddRange(new Control[] { _cmbFilterStatus, _txtSearch, btnFilter, btnReset });

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 410 };
            _gridRequests = CreateGrid();
            _gridRequests.SelectionChanged += GridRequests_SelectionChanged;
            _gridRequests.CellFormatting += GridRequests_CellFormatting;

            var bottomPanel = new Panel { Dock = DockStyle.Fill };
            _cmbRequest = new ComboBox { Left = 12, Top = 12, Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbPart = new ComboBox { Left = 340, Top = 12, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            _numQty = new NumericUpDown { Left = 608, Top = 12, Width = 90, Minimum = 1, Maximum = 500, Value = 1 };
            var btnAddPart = new Button { Left = 706, Top = 10, Width = 160, Height = 28, Text = "Добавить запчасть" };
            btnAddPart.Click += BtnAddPartToRequest_Click;
            _gridParts = CreateGrid();
            _gridParts.Top = 46;
            _gridParts.Left = 0;
            _gridParts.Width = 1240;
            _gridParts.Height = 240;
            _gridParts.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            bottomPanel.Controls.AddRange(new Control[] { _cmbRequest, _cmbPart, _numQty, btnAddPart, _gridParts });

            split.Panel1.Controls.Add(_gridRequests);
            split.Panel2.Controls.Add(bottomPanel);

            Controls.Add(split);
            Controls.Add(filterPanel);
            Controls.Add(card);
            Load += RequestsForm_Load;
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
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            ThemeHelper.StyleGrid(grid);
            return grid;
        }

        private void RequestsForm_Load(object sender, EventArgs e)
        {
            _cmbEquipment.DataSource = EquipmentService.GetEquipmentLookup();
            _cmbEquipment.DisplayMember = "DisplayName";
            _cmbEquipment.ValueMember = "Id";

            _cmbPart.DataSource = SparePartService.GetParts();
            _cmbPart.DisplayMember = "PartName";
            _cmbPart.ValueMember = "Id";

            LoadRequests();
        }

        private void LoadRequests()
        {
            _gridRequests.DataSource = RepairRequestService.GetRequests();
            ConfigureRequestsGrid();
            _cmbRequest.DataSource = RepairRequestService.GetRequestsLookup();
            _cmbRequest.DisplayMember = "DisplayName";
            _cmbRequest.ValueMember = "Id";
        }

        private void ConfigureRequestsGrid()
        {
            GridHeaderMap.Apply(_gridRequests, "requests", "Id");
        }

        private void ApplyFilter()
        {
            var table = RepairRequestService.GetRequests();
            var view = table.DefaultView;
            var filter = string.Empty;
            if (_cmbFilterStatus.SelectedIndex > 0)
            {
                filter = $"StatusName = '{_cmbFilterStatus.Text.Replace("'", "''")}'";
            }

            if (!string.IsNullOrWhiteSpace(_txtSearch.Text))
            {
                var s = _txtSearch.Text.Trim().Replace("'", "''");
                var query = $"(RequestNumber LIKE '%{s}%' OR EquipmentName LIKE '%{s}%' OR ProblemDescription LIKE '%{s}%' OR AssignedTo LIKE '%{s}%')";
                filter = string.IsNullOrWhiteSpace(filter) ? query : filter + " AND " + query;
            }

            view.RowFilter = filter;
            _gridRequests.DataSource = view.ToTable();
            ConfigureRequestsGrid();
        }

        private void GridRequests_SelectionChanged(object sender, EventArgs e)
        {
            LoadRequestParts();
            if (_gridRequests.CurrentRow == null) return;

            _txtProblem.Text = _gridRequests.CurrentRow.Cells["ProblemDescription"]?.Value?.ToString() ?? string.Empty;
            _cmbPriority.Text = _gridRequests.CurrentRow.Cells["PriorityName"]?.Value?.ToString() ?? "Средний";
            _cmbStatus.Text = _gridRequests.CurrentRow.Cells["StatusName"]?.Value?.ToString() ?? "Новая";
            _txtAssigned.Text = _gridRequests.CurrentRow.Cells["AssignedTo"]?.Value?.ToString() ?? string.Empty;
        }

        private void GridRequests_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (!_gridRequests.Columns.Contains("StatusName") || e.RowIndex < 0 || _gridRequests.Columns[e.ColumnIndex].Name != "StatusName")
            {
                return;
            }

            var status = (_gridRequests.Rows[e.RowIndex].Cells["StatusName"].Value ?? string.Empty).ToString();
            if (status == "Новая") e.CellStyle.BackColor = System.Drawing.Color.FromArgb(254, 240, 138);
            else if (status == "В работе") e.CellStyle.BackColor = System.Drawing.Color.FromArgb(191, 219, 254);
            else if (status == "Ожидание") e.CellStyle.BackColor = System.Drawing.Color.FromArgb(224, 231, 255);
            else if (status == "Завершена") e.CellStyle.BackColor = System.Drawing.Color.FromArgb(187, 247, 208);
        }

        private void BtnCreateRequest_Click(object sender, EventArgs e)
        {
            if (_cmbEquipment.SelectedValue == null || string.IsNullOrWhiteSpace(_txtProblem.Text))
            {
                MessageBox.Show("Выберите технику и заполните неисправность.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RepairRequestService.CreateRequest(Convert.ToInt32(_cmbEquipment.SelectedValue), _txtProblem.Text.Trim(), _cmbPriority.Text, _txtAssigned.Text.Trim());
            LoadRequests();
        }

        private void BtnUpdateRequest_Click(object sender, EventArgs e)
        {
            if (_gridRequests.CurrentRow == null)
            {
                MessageBox.Show("Выберите заявку для обновления.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = Convert.ToInt32(_gridRequests.CurrentRow.Cells["Id"].Value);
            RepairRequestService.UpdateRequest(id, Convert.ToInt32(_cmbEquipment.SelectedValue), _txtProblem.Text.Trim(), _cmbPriority.Text, _cmbStatus.Text, _txtAssigned.Text.Trim());
            LoadRequests();
        }

        private void BtnCloseRequest_Click(object sender, EventArgs e)
        {
            if (_gridRequests.CurrentRow == null) return;
            var id = Convert.ToInt32(_gridRequests.CurrentRow.Cells["Id"].Value);
            RepairRequestService.UpdateRequestStatus(id, "Завершена", _txtAssigned.Text.Trim());
            LoadRequests();
        }

        private void LoadRequestParts()
        {
            if (_gridRequests.CurrentRow == null) return;
            var requestId = Convert.ToInt32(_gridRequests.CurrentRow.Cells["Id"].Value);
            _gridParts.DataSource = RepairRequestPartsService.GetRequestParts(requestId);
            GridHeaderMap.Apply(_gridParts, "requestParts", "Id");
        }

        private void BtnAddPartToRequest_Click(object sender, EventArgs e)
        {
            if (_cmbRequest.SelectedValue == null || _cmbPart.SelectedValue == null) return;
            RepairRequestPartsService.AddPartToRequest(Convert.ToInt32(_cmbRequest.SelectedValue), Convert.ToInt32(_cmbPart.SelectedValue), Convert.ToInt32(_numQty.Value));
            LoadRequestParts();
            MessageBox.Show("Запчасть добавлена в заявку.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static Label LabelAt(string text, int left, int top, int width)
        {
            return new Label { Text = text, Left = left, Top = top, Width = width };
        }
    }
}
