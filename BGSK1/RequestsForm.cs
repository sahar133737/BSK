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
        private readonly ComboBox _cmbAssigned;
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
            MinimumSize = new System.Drawing.Size(1100, 680);
            if (!RolePermissionService.HasPermission("module.requests"))
            {
                Shown += (s, e) => { MessageBox.Show("Нет доступа к модулю.", "Доступ запрещен", MessageBoxButtons.OK, MessageBoxIcon.Warning); Close(); };
            }

            var card = new GroupBox { Dock = DockStyle.Top, Height = 124, Text = "  Карточка заявки  " };
            _cmbEquipment = new ComboBox { Left = 12, Top = 48, Width = 230, DropDownStyle = ComboBoxStyle.DropDownList };
            _txtProblem = new TextBox { Left = 248, Top = 48, Width = 310 };
            _cmbPriority = new ComboBox { Left = 564, Top = 48, Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbPriority.Items.AddRange(new[] { "Низкий", "Средний", "Высокий" });
            _cmbPriority.SelectedIndex = 1;
            _cmbStatus = new ComboBox { Left = 680, Top = 48, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbStatus.Items.AddRange(new[] { "Новая", "В работе", "Ожидание", "Завершена" });
            _cmbStatus.SelectedIndex = 0;
            _cmbAssigned = new ComboBox { Left = 806, Top = 48, Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            var btnCreate = new Button { Left = 972, Top = 46, Width = 90, Height = 30, Text = "Создать" };
            var btnUpdate = new Button { Left = 1066, Top = 46, Width = 95, Height = 30, Text = "Обновить" };
            var btnClose = new Button { Left = 1165, Top = 46, Width = 90, Height = 30, Text = "Закрыть" };
            var btnDelete = new Button { Left = 1066, Top = 80, Width = 189, Height = 28, Text = "Удалить запись" };
            var btnHelp = new Button { Left = 972, Top = 80, Width = 90, Height = 28, Text = "Справка" };
            ThemeHelper.StyleButton(btnCreate, ThemeHelper.Primary);
            ThemeHelper.StyleButton(btnUpdate, ThemeHelper.Secondary);
            ThemeHelper.StyleButton(btnClose, ThemeHelper.Success);
            ThemeHelper.StyleButton(btnDelete, ThemeHelper.Danger);
            ThemeHelper.StyleButton(btnHelp, ThemeHelper.Accent);
            btnCreate.Click += BtnCreateRequest_Click;
            btnUpdate.Click += BtnUpdateRequest_Click;
            btnClose.Click += BtnCloseRequest_Click;
            btnDelete.Click += BtnDeleteRequest_Click;
            btnHelp.Click += (s, e) => ModuleHelpProvider.ShowHelp("requests", this);
            card.Controls.AddRange(new Control[]
            {
                LabelAt("Техника",12,20,230), LabelAt("Неисправность",248,20,310), LabelAt("Приоритет",564,20,110),
                LabelAt("Статус",680,20,120), LabelAt("Исполнитель",806,20,160),
                _cmbEquipment,_txtProblem,_cmbPriority,_cmbStatus,_cmbAssigned,btnCreate,btnUpdate,btnClose,btnDelete,btnHelp
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
            var btnRemovePart = new Button { Left = 872, Top = 10, Width = 180, Height = 28, Text = "Убрать из заявки" };
            btnAddPart.Click += BtnAddPartToRequest_Click;
            btnRemovePart.Click += BtnRemovePartFromRequest_Click;
            _gridParts = CreateGrid();
            _gridParts.Top = 46;
            _gridParts.Left = 0;
            _gridParts.Width = 1240;
            _gridParts.Height = 240;
            _gridParts.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            bottomPanel.Controls.AddRange(new Control[] { _cmbRequest, _cmbPart, _numQty, btnAddPart, btnRemovePart, _gridParts });

            split.Panel1.Controls.Add(_gridRequests);
            split.Panel2.Controls.Add(bottomPanel);

            Controls.Add(split);
            Controls.Add(filterPanel);
            Controls.Add(card);
            ModuleHelpProvider.BindF11(this, "requests");
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
            BindAssignees();

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
            _cmbAssigned.Text = _gridRequests.CurrentRow.Cells["AssignedTo"]?.Value?.ToString() ?? string.Empty;
        }

        private void GridRequests_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (!_gridRequests.Columns.Contains("StatusName") || e.RowIndex < 0 || _gridRequests.Columns[e.ColumnIndex].Name != "StatusName")
            {
                return;
            }

            var status = (_gridRequests.Rows[e.RowIndex].Cells["StatusName"].Value ?? string.Empty).ToString();
            if (status == "Новая") e.CellStyle.BackColor = System.Drawing.Color.FromArgb(236, 245, 255);
            else if (status == "В работе") e.CellStyle.BackColor = System.Drawing.Color.FromArgb(221, 236, 252);
            else if (status == "Ожидание") e.CellStyle.BackColor = System.Drawing.Color.FromArgb(243, 247, 253);
            else if (status == "Завершена") e.CellStyle.BackColor = System.Drawing.Color.FromArgb(226, 242, 235);
        }

        private void BtnCreateRequest_Click(object sender, EventArgs e)
        {
            using (var dialog = new RequestCreateForm())
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    LoadRequests();
                }
            }
        }

        private void BtnUpdateRequest_Click(object sender, EventArgs e)
        {
            if (_gridRequests.CurrentRow == null)
            {
                MessageBox.Show("Выберите заявку для обновления.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = Convert.ToInt32(_gridRequests.CurrentRow.Cells["Id"].Value);
            var equipmentId = ResolveEquipmentIdForCurrentRow();
            using (var dialog = new RequestEditForm(
                id,
                equipmentId,
                _gridRequests.CurrentRow.Cells["ProblemDescription"]?.Value?.ToString() ?? string.Empty,
                _gridRequests.CurrentRow.Cells["PriorityName"]?.Value?.ToString() ?? "Средний",
                _gridRequests.CurrentRow.Cells["StatusName"]?.Value?.ToString() ?? "Новая",
                _gridRequests.CurrentRow.Cells["AssignedTo"]?.Value?.ToString() ?? string.Empty))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    LoadRequests();
                }
            }
        }

        private void BtnCloseRequest_Click(object sender, EventArgs e)
        {
            if (_gridRequests.CurrentRow == null) return;
            var id = Convert.ToInt32(_gridRequests.CurrentRow.Cells["Id"].Value);
            RepairRequestService.UpdateRequestStatus(id, "Завершена", _cmbAssigned.Text.Trim());
            LoadRequests();
        }

        private void BtnDeleteRequest_Click(object sender, EventArgs e)
        {
            if (_gridRequests.CurrentRow == null) return;
            if (MessageBox.Show("Удалить выбранную заявку и связанные запчасти?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            var id = Convert.ToInt32(_gridRequests.CurrentRow.Cells["Id"].Value);
            RepairRequestService.DeleteRequest(id);
            LoadRequests();
        }

        private void LoadRequestParts()
        {
            if (_gridRequests.CurrentRow == null) return;
            var requestId = Convert.ToInt32(_gridRequests.CurrentRow.Cells["Id"].Value);
            _gridParts.DataSource = RepairRequestPartsService.GetRequestParts(requestId);
            GridHeaderMap.Apply(_gridParts, "requestParts", "Id", "SparePartId");
        }

        private void BtnAddPartToRequest_Click(object sender, EventArgs e)
        {
            if (_cmbRequest.SelectedValue == null || _cmbPart.SelectedValue == null) return;
            RepairRequestPartsService.AddPartToRequest(Convert.ToInt32(_cmbRequest.SelectedValue), Convert.ToInt32(_cmbPart.SelectedValue), Convert.ToInt32(_numQty.Value));
            LoadRequestParts();
            MessageBox.Show("Запчасть добавлена в заявку.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnRemovePartFromRequest_Click(object sender, EventArgs e)
        {
            if (_gridParts.CurrentRow == null || !_gridParts.Columns.Contains("Id"))
            {
                MessageBox.Show("Выберите строку с запчастью в нижней таблице.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Убрать запчасть из заявки и вернуть ее на склад?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            var requestPartId = Convert.ToInt32(_gridParts.CurrentRow.Cells["Id"].Value);
            RepairRequestPartsService.RemovePartFromRequest(requestPartId);
            LoadRequestParts();
            MessageBox.Show("Запчасть удалена из заявки и возвращена на склад.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static Label LabelAt(string text, int left, int top, int width)
        {
            return ThemeHelper.FormFieldLabel(text, left, top, width);
        }

        private int ResolveEquipmentIdForCurrentRow()
        {
            var displayInv = _gridRequests.CurrentRow.Cells["InventoryNumber"]?.Value?.ToString() ?? string.Empty;
            var lookup = EquipmentService.GetEquipmentLookup();
            foreach (DataRow row in lookup.Rows)
            {
                var display = row["DisplayName"]?.ToString() ?? string.Empty;
                if (display.StartsWith(displayInv + " - ", StringComparison.OrdinalIgnoreCase))
                {
                    return Convert.ToInt32(row["Id"]);
                }
            }
            return Convert.ToInt32(_cmbEquipment.SelectedValue);
        }

        private void BindAssignees()
        {
            var table = UserService.GetActiveUsersLookup();
            _cmbAssigned.DataSource = table;
            _cmbAssigned.DisplayMember = "FullName";
            _cmbAssigned.ValueMember = "FullName";
        }
    }
}
