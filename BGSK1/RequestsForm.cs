using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using BGSK1.Services;
using BGSK1.UI;

namespace BGSK1
{
    public sealed class RequestsForm : Form
    {
        public static int? PendingEditRequestId { get; set; }

        /// <summary>Одна заявка для панели запчастей; грид и комбо только отражают её (без «кто важнее»).</summary>
        private int? _activeRequestId;

        private bool _suppressRequestUiSync;

        private readonly DataGridView _gridRequests;
        private readonly DataGridView _gridParts;
        private readonly ComboBox _cmbFilterStatus;
        private readonly TextBox _txtSearch;
        private readonly ComboBox _cmbRequest;
        private readonly ComboBox _cmbPart;
        private readonly NumericUpDown _numQty;
        private readonly Label _lblRequestPartsSummary;

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

            var toolbar = new GroupBox { Dock = DockStyle.Top, Height = 100, Text = "  Заявки  " };
            var lblHint = new Label
            {
                Left = 12,
                Top = 22,
                Width = 980,
                Height = 36,
                ForeColor = ThemeHelper.MutedText,
                Text = "Заявка: двойной щелчок или «Изменить» — карточка заявки. Ниже: запчасти по выбранной заявке; двойной щелчок по строке запчасти — карточка номенклатуры на складе (нужен доступ к модулю «Склад запчастей»)."
            };
            var btnCreate = new Button { Left = 12, Top = 58, Width = 100, Height = 30, Text = "Создать" };
            var btnUpdate = new Button { Left = 118, Top = 58, Width = 100, Height = 30, Text = "Изменить" };
            var btnClose = new Button { Left = 224, Top = 58, Width = 110, Height = 30, Text = "Закрыть" };
            var btnDelete = new Button { Left = 340, Top = 58, Width = 120, Height = 30, Text = "Удалить" };
            var btnHelp = new Button { Left = 466, Top = 58, Width = 100, Height = 30, Text = "Справка" };
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
            toolbar.Controls.AddRange(new Control[] { lblHint, btnCreate, btnUpdate, btnClose, btnDelete, btnHelp });

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
            _gridRequests.CellDoubleClick += GridRequests_CellDoubleClick;
            _gridRequests.CellFormatting += GridRequests_CellFormatting;

            var bottomLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0),
                Margin = new Padding(0),
                BackColor = ThemeHelper.Surface
            };
            bottomLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
            bottomLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            bottomLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var partsTools = new Panel { Dock = DockStyle.Fill, BackColor = ThemeHelper.Surface };
            _cmbRequest = new ComboBox { Left = 12, Top = 10, Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbRequest.SelectionChangeCommitted += CmbRequest_SelectionChangeCommitted;
            _cmbPart = new ComboBox { Left = 340, Top = 10, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            _numQty = new NumericUpDown { Left = 608, Top = 10, Width = 90, Minimum = 1, Maximum = 500, Value = 1 };
            var btnAddPart = new Button { Left = 706, Top = 8, Width = 160, Height = 28, Text = "Добавить запчасть" };
            var btnRemovePart = new Button { Left = 872, Top = 8, Width = 180, Height = 28, Text = "Убрать из заявки" };
            btnAddPart.Click += BtnAddPartToRequest_Click;
            btnRemovePart.Click += BtnRemovePartFromRequest_Click;
            partsTools.Controls.AddRange(new Control[] { _cmbRequest, _cmbPart, _numQty, btnAddPart, btnRemovePart });

            _lblRequestPartsSummary = new Label
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 4, 8, 0),
                ForeColor = ThemeHelper.MutedText,
                Text = "Запчасти: выберите заявку.",
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                BackColor = ThemeHelper.Surface
            };
            _gridParts = CreateGrid();
            _gridParts.Dock = DockStyle.Fill;
            _gridParts.CellDoubleClick += GridParts_CellDoubleClick;

            bottomLayout.Controls.Add(partsTools, 0, 0);
            bottomLayout.Controls.Add(_lblRequestPartsSummary, 0, 1);
            bottomLayout.Controls.Add(_gridParts, 0, 2);

            split.Panel1.Controls.Add(_gridRequests);
            split.Panel2.Controls.Add(bottomLayout);

            Controls.Add(split);
            Controls.Add(filterPanel);
            Controls.Add(toolbar);
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

        private static DataTable CreateEmptyRequestPartsTable()
        {
            var t = new DataTable();
            t.Columns.Add("Id", typeof(int));
            t.Columns.Add("PartName", typeof(string));
            t.Columns.Add("PartNumber", typeof(string));
            t.Columns.Add("QuantityUsed", typeof(int));
            t.Columns.Add("SparePartId", typeof(int));
            return t;
        }

        /// <summary>Привязка к DataTable — строки всегда соответствуют данным после добавления/обновления.</summary>
        private void BindRequestPartsData(DataTable table)
        {
            if (table == null)
            {
                table = CreateEmptyRequestPartsTable();
            }

            _gridParts.SuspendLayout();
            try
            {
                _gridParts.DataSource = null;
                _gridParts.AutoGenerateColumns = false;
                _gridParts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                _gridParts.ScrollBars = ScrollBars.Vertical;
                _gridParts.Columns.Clear();

                var colPart = new DataGridViewTextBoxColumn
                {
                    Name = "PartName",
                    DataPropertyName = "PartName",
                    HeaderText = "Запчасть",
                    ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    FillWeight = 100f,
                    MinimumWidth = 120,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };
                var colNum = new DataGridViewTextBoxColumn
                {
                    Name = "PartNumber",
                    DataPropertyName = "PartNumber",
                    HeaderText = "Артикул",
                    ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    Width = 200,
                    MinimumWidth = 100,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };
                var colQty = new DataGridViewTextBoxColumn
                {
                    Name = "QuantityUsed",
                    DataPropertyName = "QuantityUsed",
                    HeaderText = "Количество",
                    ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    Width = 120,
                    MinimumWidth = 90,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };
                colQty.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                var colLineId = new DataGridViewTextBoxColumn
                {
                    Name = "Id",
                    DataPropertyName = "Id",
                    Visible = false,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };
                var colSpId = new DataGridViewTextBoxColumn
                {
                    Name = "SparePartId",
                    DataPropertyName = "SparePartId",
                    Visible = false,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };

                _gridParts.Columns.AddRange(colPart, colNum, colQty, colLineId, colSpId);
                _gridParts.DataSource = table;
            }
            finally
            {
                _gridParts.ResumeLayout(true);
            }
        }

        private void RequestsForm_Load(object sender, EventArgs e)
        {
            BindPartsCombo(SparePartService.GetParts());

            BindRequestPartsData(CreateEmptyRequestPartsTable());
            LoadRequests();
            TryOpenPendingRequestEdit();
        }

        private void LoadRequests()
        {
            var keepId = _activeRequestId;
            _gridRequests.DataSource = RepairRequestService.GetRequests();
            ConfigureRequestsGrid();
            _cmbRequest.DataSource = RepairRequestService.GetRequestsLookup();
            _cmbRequest.DisplayMember = "DisplayName";
            _cmbRequest.ValueMember = "Id";

            _suppressRequestUiSync = true;
            try
            {
                if (keepId.HasValue && RowExistsInRequestsGrid(keepId.Value))
                {
                    _activeRequestId = keepId.Value;
                    SelectRequestRowById(_activeRequestId.Value);
                }
                else if (_gridRequests.Rows.Count > 0 && _gridRequests.Columns.Contains("Id"))
                {
                    var id = Convert.ToInt32(_gridRequests.Rows[0].Cells["Id"].Value);
                    _activeRequestId = id;
                    SelectRequestRowById(id);
                }
                else
                {
                    _activeRequestId = null;
                }

                SyncComboFromActiveRequest();
            }
            finally
            {
                _suppressRequestUiSync = false;
            }

            RefreshRequestParts();
        }

        private bool RowExistsInRequestsGrid(int requestId)
        {
            if (!_gridRequests.Columns.Contains("Id"))
            {
                return false;
            }

            foreach (DataGridViewRow row in _gridRequests.Rows)
            {
                if (row.IsNewRow)
                {
                    continue;
                }

                var v = row.Cells["Id"].Value;
                if (v == null || v == DBNull.Value)
                {
                    continue;
                }

                if (Convert.ToInt32(v) == requestId)
                {
                    return true;
                }
            }

            return false;
        }

        private void TryOpenPendingRequestEdit()
        {
            if (!PendingEditRequestId.HasValue)
            {
                return;
            }

            var id = PendingEditRequestId.Value;
            PendingEditRequestId = null;
            var opened = false;
            _suppressRequestUiSync = true;
            try
            {
                if (!SelectRequestRowById(id))
                {
                    return;
                }

                _activeRequestId = id;
                SyncComboFromActiveRequest();
                opened = true;
            }
            finally
            {
                _suppressRequestUiSync = false;
            }

            if (!opened)
            {
                return;
            }

            RefreshRequestParts();
            BtnUpdateRequest_Click(this, EventArgs.Empty);
        }

        private bool SelectRequestRowById(int requestId)
        {
            foreach (DataGridViewRow row in _gridRequests.Rows)
            {
                if (row.IsNewRow || !_gridRequests.Columns.Contains("Id"))
                {
                    continue;
                }

                var cell = row.Cells["Id"].Value;
                if (cell == null || cell == DBNull.Value)
                {
                    continue;
                }

                if (Convert.ToInt32(cell) == requestId)
                {
                    _gridRequests.ClearSelection();
                    row.Selected = true;
                    SetCurrentCellToFirstVisible(_gridRequests, row);
                    return true;
                }
            }

            return false;
        }

        /// <summary>Первая колонка грида часто скрыта (Id) — CurrentCell только в видимую ячейку.</summary>
        private static void SetCurrentCellToFirstVisible(DataGridView grid, DataGridViewRow row)
        {
            if (grid == null || row == null)
            {
                return;
            }

            DataGridViewCell pick = null;
            foreach (DataGridViewCell c in row.Cells)
            {
                if (c.Visible)
                {
                    pick = c;
                    break;
                }
            }

            if (pick != null)
            {
                grid.CurrentCell = pick;
            }
        }

        private void ConfigureRequestsGrid()
        {
            GridHeaderMap.Apply(_gridRequests, "requests", "Id", "EquipmentId");
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
            var keepId = _activeRequestId;
            _gridRequests.DataSource = view.ToTable();
            ConfigureRequestsGrid();

            _suppressRequestUiSync = true;
            try
            {
                if (keepId.HasValue && RowExistsInRequestsGrid(keepId.Value))
                {
                    _activeRequestId = keepId.Value;
                    SelectRequestRowById(_activeRequestId.Value);
                }
                else if (_gridRequests.Rows.Count > 0 && _gridRequests.Columns.Contains("Id"))
                {
                    var id = Convert.ToInt32(_gridRequests.Rows[0].Cells["Id"].Value);
                    _activeRequestId = id;
                    SelectRequestRowById(id);
                }
                else
                {
                    _activeRequestId = null;
                }

                SyncComboFromActiveRequest();
            }
            finally
            {
                _suppressRequestUiSync = false;
            }

            RefreshRequestParts();
        }

        private void GridRequests_SelectionChanged(object sender, EventArgs e)
        {
            if (_suppressRequestUiSync)
            {
                return;
            }

            if (_gridRequests.CurrentRow == null || _gridRequests.CurrentRow.IsNewRow)
            {
                _activeRequestId = null;
                RefreshRequestParts();
                return;
            }

            if (!_gridRequests.Columns.Contains("Id"))
            {
                return;
            }

            try
            {
                _activeRequestId = Convert.ToInt32(_gridRequests.CurrentRow.Cells["Id"].Value);
            }
            catch
            {
                _activeRequestId = null;
                RefreshRequestParts();
                return;
            }

            _suppressRequestUiSync = true;
            try
            {
                SyncComboFromActiveRequest();
            }
            finally
            {
                _suppressRequestUiSync = false;
            }

            RefreshRequestParts();
        }

        private void GridRequests_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            BtnUpdateRequest_Click(sender, e);
        }

        private void GridParts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            if (!RolePermissionService.HasPermission("module.parts"))
            {
                MessageBox.Show("Для редактирования карточки запчасти нужен доступ к модулю «Склад запчастей».", "Доступ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!(_gridParts.Rows[e.RowIndex].DataBoundItem is DataRowView drv))
            {
                return;
            }

            var sparePartId = Convert.ToInt32(drv.Row["SparePartId"]);
            var row = SparePartService.TryGetPartRow(sparePartId);
            if (row == null)
            {
                MessageBox.Show("Запчасть не найдена в справочнике склада.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dialog = new PartEditForm(
                sparePartId,
                row["PartName"]?.ToString() ?? string.Empty,
                row["PartNumber"]?.ToString() ?? string.Empty,
                Convert.ToInt32(row["QuantityInStock"] ?? 0),
                Convert.ToInt32(row["MinQuantity"] ?? 0)))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    ReloadPartsComboKeepingSelection();
                    RefreshRequestParts();
                }
            }
        }

        private void SyncComboFromActiveRequest()
        {
            if (!_activeRequestId.HasValue || _cmbRequest.DataSource == null)
            {
                return;
            }

            var dt = _cmbRequest.DataSource as DataTable;
            if (dt == null)
            {
                return;
            }

            for (var i = 0; i < dt.Rows.Count; i++)
            {
                if (Convert.ToInt32(dt.Rows[i]["Id"]) != _activeRequestId.Value)
                {
                    continue;
                }

                if (_cmbRequest.SelectedIndex != i)
                {
                    _cmbRequest.SelectedIndex = i;
                }

                return;
            }
        }

        /// <summary>Id заявки из комбо без SelectedValue (стабильнее для DataTable + ValueMember).</summary>
        private int? TryGetRequestIdFromCombo()
        {
            if (_cmbRequest.SelectedIndex < 0 || _cmbRequest.DataSource == null)
            {
                return null;
            }

            var dt = _cmbRequest.DataSource as DataTable;
            if (dt == null || _cmbRequest.SelectedIndex >= dt.Rows.Count)
            {
                return null;
            }

            return Convert.ToInt32(dt.Rows[_cmbRequest.SelectedIndex]["Id"]);
        }

        /// <summary>Колонка списка с артикулом — уникальнее, чем одно PartName (дубли имён ломали SelectedValue).</summary>
        private static void PreparePartsComboTable(DataTable parts)
        {
            if (parts == null)
            {
                return;
            }

            if (!parts.Columns.Contains("PartListDisplay"))
            {
                parts.Columns.Add("PartListDisplay", typeof(string));
            }

            foreach (DataRow r in parts.Rows)
            {
                var name = r["PartName"]?.ToString() ?? string.Empty;
                var num = r["PartNumber"]?.ToString() ?? string.Empty;
                r["PartListDisplay"] = string.IsNullOrEmpty(num) ? name : name + " · " + num;
            }
        }

        private void BindPartsCombo(DataTable parts)
        {
            PreparePartsComboTable(parts);
            _cmbPart.DataSource = parts;
            _cmbPart.DisplayMember = "PartListDisplay";
            _cmbPart.ValueMember = "Id";
        }

        /// <summary>Id запчасти по выбранной строке списка — без SelectedValue.</summary>
        private int? TryGetSparePartIdFromCombo()
        {
            if (_cmbPart.SelectedIndex < 0 || _cmbPart.DataSource == null)
            {
                return null;
            }

            var dt = _cmbPart.DataSource as DataTable;
            if (dt == null || _cmbPart.SelectedIndex >= dt.Rows.Count)
            {
                return null;
            }

            return Convert.ToInt32(dt.Rows[_cmbPart.SelectedIndex]["Id"]);
        }

        private void ReloadPartsComboKeepingSelection()
        {
            var keepId = TryGetSparePartIdFromCombo();
            BindPartsCombo(SparePartService.GetParts());
            if (!keepId.HasValue)
            {
                return;
            }

            var dt = _cmbPart.DataSource as DataTable;
            if (dt == null)
            {
                return;
            }

            for (var i = 0; i < dt.Rows.Count; i++)
            {
                if (Convert.ToInt32(dt.Rows[i]["Id"]) == keepId.Value)
                {
                    _cmbPart.SelectedIndex = i;
                    return;
                }
            }
        }

        private void CmbRequest_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (_suppressRequestUiSync)
            {
                return;
            }

            var idOpt = TryGetRequestIdFromCombo();
            if (!idOpt.HasValue)
            {
                return;
            }

            _activeRequestId = idOpt.Value;
            _suppressRequestUiSync = true;
            try
            {
                if (!SelectRequestRowById(_activeRequestId.Value))
                {
                    _gridRequests.ClearSelection();
                }
            }
            finally
            {
                _suppressRequestUiSync = false;
            }

            RefreshRequestParts();
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
            var assigned = _gridRequests.CurrentRow.Cells["AssignedTo"]?.Value?.ToString() ?? string.Empty;
            RepairRequestService.UpdateRequestStatus(id, "Завершена", assigned.Trim());
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

        private void LoadRequestPartsForRequestId(int requestId)
        {
            RepairRequestPartsService.MergeDuplicateLinesForRequest(requestId);
            var table = RepairRequestPartsService.GetRequestParts(requestId);
            BindRequestPartsData(table);
            UpdateRequestPartsSummary(table);
        }

        private void RefreshRequestParts()
        {
            if (!_activeRequestId.HasValue)
            {
                BindRequestPartsData(CreateEmptyRequestPartsTable());
                _lblRequestPartsSummary.Text = "Запчасти: выберите заявку.";
                return;
            }

            LoadRequestPartsForRequestId(_activeRequestId.Value);
        }

        private void UpdateRequestPartsSummary(DataTable table)
        {
            if (table == null || table.Rows.Count == 0)
            {
                _lblRequestPartsSummary.Text = "Запчасти в заявке: позиций 0, всего единиц 0.";
                return;
            }

            var positions = table.Rows.Count;
            var totalUnits = 0;
            foreach (DataRow row in table.Rows)
            {
                totalUnits += Convert.ToInt32(row["QuantityUsed"] ?? 0);
            }

            _lblRequestPartsSummary.Text = $"Запчасти в заявке: позиций {positions}, всего единиц {totalUnits}. Повторное добавление той же запчасти увеличивает количество в строке.";
        }

        private void BtnAddPartToRequest_Click(object sender, EventArgs e)
        {
            if (!_activeRequestId.HasValue)
            {
                return;
            }

            var sparePartIdOpt = TryGetSparePartIdFromCombo();
            if (!sparePartIdOpt.HasValue)
            {
                return;
            }

            var sparePartId = sparePartIdOpt.Value;
            var addQty = Convert.ToInt32(_numQty.Value);
            if (addQty < 1)
            {
                return;
            }

            RepairRequestPartsService.AddPartToRequest(_activeRequestId.Value, sparePartId, addQty);
            LoadRequestPartsForRequestId(_activeRequestId.Value);
        }

        private void BtnRemovePartFromRequest_Click(object sender, EventArgs e)
        {
            if (_gridParts.CurrentRow == null)
            {
                MessageBox.Show("Выберите строку с запчастью в нижней таблице.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!(_gridParts.CurrentRow.DataBoundItem is DataRowView drv))
            {
                MessageBox.Show("Выберите строку с запчастью в нижней таблице.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Убрать запчасть из заявки и вернуть ее на склад?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            var requestPartId = Convert.ToInt32(drv.Row["Id"]);
            RepairRequestPartsService.RemovePartFromRequest(requestPartId);
            RefreshRequestParts();
            MessageBox.Show("Запчасть удалена из заявки и возвращена на склад.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private int ResolveEquipmentIdForCurrentRow()
        {
            if (_gridRequests.Columns.Contains("EquipmentId"))
            {
                var v = _gridRequests.CurrentRow.Cells["EquipmentId"].Value;
                if (v != null && v != DBNull.Value)
                {
                    return Convert.ToInt32(v);
                }
            }

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

            return 0;
        }
    }
}
