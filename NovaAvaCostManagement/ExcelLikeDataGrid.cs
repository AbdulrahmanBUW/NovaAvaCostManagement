using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Enhanced DataGridView with Excel-like functionality
    /// </summary>
    public class ExcelLikeDataGrid : DataGridView
    {
        private UndoRedoManager undoRedoManager;
        private const int MAX_UNDO_LEVELS = 50;

        public ExcelLikeDataGrid()
        {
            InitializeExcelFeatures();
            undoRedoManager = new UndoRedoManager(MAX_UNDO_LEVELS);
        }

        private void InitializeExcelFeatures()
        {
            // Enable multi-cell selection
            this.MultiSelect = true;
            this.SelectionMode = DataGridViewSelectionMode.CellSelect;
            this.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;

            // Enable editing
            this.ReadOnly = false;
            this.EditMode = DataGridViewEditMode.EditOnEnter;

            // Visual settings
            this.AllowUserToAddRows = false;
            this.AllowUserToDeleteRows = false;
            this.RowHeadersWidth = 40;
            this.EnableHeadersVisualStyles = false;

            this.ColumnHeadersVisible = true;  // ← ADD IF MISSING
            this.ColumnHeadersHeight = 30;      // ← ADD IF MISSING
            this.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;  // ← ADD IF MISSING

            this.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            this.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            // Grid appearance
            this.GridColor = Color.FromArgb(220, 220, 220);
            this.BorderStyle = BorderStyle.Fixed3D;
            this.CellBorderStyle = DataGridViewCellBorderStyle.Single;

            // Performance optimization for large datasets
            this.DoubleBuffered = true;
            this.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;

            // Attach event handlers
            this.KeyDown += ExcelLikeDataGrid_KeyDown;
            this.CellBeginEdit += ExcelLikeDataGrid_CellBeginEdit;
            this.CellEndEdit += ExcelLikeDataGrid_CellEndEdit;
            this.CellPainting += ExcelLikeDataGrid_CellPainting;
        }

        /// <summary>
        /// Handle keyboard shortcuts
        /// </summary>
        private void ExcelLikeDataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                // Ctrl+C - Copy
                if (e.Control && e.KeyCode == Keys.C)
                {
                    CopyCells();
                    e.Handled = true;
                    return;
                }

                // Ctrl+X - Cut
                if (e.Control && e.KeyCode == Keys.X)
                {
                    CutCells();
                    e.Handled = true;
                    return;
                }

                // Ctrl+V - Paste
                if (e.Control && e.KeyCode == Keys.V)
                {
                    PasteCells();
                    e.Handled = true;
                    return;
                }

                // Delete or Backspace - Clear cells
                if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
                {
                    if (!this.IsCurrentCellInEditMode)
                    {
                        ClearSelectedCells();
                        e.Handled = true;
                        return;
                    }
                }

                // Ctrl+Z - Undo
                if (e.Control && e.KeyCode == Keys.Z)
                {
                    Undo();
                    e.Handled = true;
                    return;
                }

                // Ctrl+Y - Redo
                if (e.Control && e.KeyCode == Keys.Y)
                {
                    Redo();
                    e.Handled = true;
                    return;
                }

                // Ctrl+D - Fill Down
                if (e.Control && e.KeyCode == Keys.D)
                {
                    FillDown();
                    e.Handled = true;
                    return;
                }

                // Enter - Move down after edit
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.Handled = true;
                    SendKeys.Send("{DOWN}");
                }

                // Shift+Enter - Move up
                if (e.KeyCode == Keys.Enter && e.Shift)
                {
                    e.Handled = true;
                    SendKeys.Send("{UP}");
                }

                // Tab - Move right
                if (e.KeyCode == Keys.Tab && !e.Shift)
                {
                    e.Handled = true;
                    SendKeys.Send("{RIGHT}");
                }

                // Shift+Tab - Move left
                if (e.KeyCode == Keys.Tab && e.Shift)
                {
                    e.Handled = true;
                    SendKeys.Send("{LEFT}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Keyboard operation error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Copy selected cells to clipboard
        /// </summary>
        public void CopyCells()
        {
            try
            {
                if (this.SelectedCells.Count == 0)
                    return;

                // Get selected cells data
                var data = GetSelectedCellsData();

                // Convert to tab-delimited format (Excel compatible)
                var clipboardText = ConvertToClipboardFormat(data);

                // Copy to clipboard
                Clipboard.SetText(clipboardText);

                // Visual feedback
                FlashSelectedCells(Color.LightBlue);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Copy error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Cut selected cells to clipboard
        /// </summary>
        public void CutCells()
        {
            try
            {
                if (this.SelectedCells.Count == 0)
                    return;

                // Save state for undo
                SaveUndoState("Cut");

                // Copy first
                CopyCells();

                // Then clear
                ClearSelectedCells(false); // Don't save undo state again

                // Visual feedback
                FlashSelectedCells(Color.LightCoral);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cut error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Paste cells from clipboard
        /// </summary>
        public void PasteCells()
        {
            try
            {
                if (!Clipboard.ContainsText())
                    return;

                // Save state for undo
                SaveUndoState("Paste");

                var clipboardText = Clipboard.GetText();
                var data = ParseClipboardData(clipboardText);

                if (data == null || data.Count == 0)
                    return;

                // Get starting cell
                int startRow = this.CurrentCell?.RowIndex ?? 0;
                int startCol = this.CurrentCell?.ColumnIndex ?? 0;

                // Paste data
                for (int r = 0; r < data.Count && (startRow + r) < this.Rows.Count; r++)
                {
                    for (int c = 0; c < data[r].Count && (startCol + c) < this.Columns.Count; c++)
                    {
                        var cell = this.Rows[startRow + r].Cells[startCol + c];

                        if (!cell.ReadOnly && !this.Columns[startCol + c].ReadOnly)
                        {
                            cell.Value = data[r][c];
                        }
                    }
                }

                // Visual feedback
                FlashSelectedCells(Color.LightGreen);

                // Refresh the grid
                this.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Paste error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Clear selected cells
        /// </summary>
        public void ClearSelectedCells(bool saveUndo = true)
        {
            try
            {
                if (this.SelectedCells.Count == 0)
                    return;

                if (saveUndo)
                    SaveUndoState("Clear");

                foreach (DataGridViewCell cell in this.SelectedCells)
                {
                    if (!cell.ReadOnly && !this.Columns[cell.ColumnIndex].ReadOnly)
                    {
                        cell.Value = GetDefaultValueForColumn(cell.ColumnIndex);
                    }
                }

                this.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Clear error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Fill down - copy first cell value to all selected cells below
        /// </summary>
        public void FillDown()
        {
            try
            {
                if (this.SelectedCells.Count < 2)
                    return;

                SaveUndoState("Fill Down");

                // Get selected cells sorted by position
                var selectedCells = this.SelectedCells.Cast<DataGridViewCell>()
                    .OrderBy(c => c.ColumnIndex)
                    .ThenBy(c => c.RowIndex)
                    .ToList();

                // Group by column
                var columnGroups = selectedCells.GroupBy(c => c.ColumnIndex);

                foreach (var group in columnGroups)
                {
                    var cellsInColumn = group.OrderBy(c => c.RowIndex).ToList();
                    if (cellsInColumn.Count < 2)
                        continue;

                    var sourceValue = cellsInColumn[0].Value;

                    for (int i = 1; i < cellsInColumn.Count; i++)
                    {
                        if (!cellsInColumn[i].ReadOnly)
                        {
                            cellsInColumn[i].Value = sourceValue;
                        }
                    }
                }

                this.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fill down error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Undo last operation
        /// </summary>
        public void Undo()
        {
            try
            {
                if (!undoRedoManager.CanUndo)
                {
                    MessageBox.Show("Nothing to undo.", "Undo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var state = undoRedoManager.Undo();
                RestoreGridState(state);
                this.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Undo error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Redo last undone operation
        /// </summary>
        public void Redo()
        {
            try
            {
                if (!undoRedoManager.CanRedo)
                {
                    MessageBox.Show("Nothing to redo.", "Redo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var state = undoRedoManager.Redo();
                RestoreGridState(state);
                this.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Redo error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Save current grid state for undo
        /// </summary>
        private void SaveUndoState(string operation)
        {
            try
            {
                var state = new GridState
                {
                    Operation = operation,
                    Timestamp = DateTime.Now,
                    Data = new List<List<object>>()
                };

                // Save all cell values
                foreach (DataGridViewRow row in this.Rows)
                {
                    var rowData = new List<object>();
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        rowData.Add(cell.Value);
                    }
                    state.Data.Add(rowData);
                }

                undoRedoManager.AddState(state);
            }
            catch (Exception ex)
            {
                // Silent fail for undo - don't disrupt user workflow
                System.Diagnostics.Debug.WriteLine($"Undo save error: {ex.Message}");
            }
        }

        /// <summary>
        /// Restore grid state from undo/redo
        /// </summary>
        private void RestoreGridState(GridState state)
        {
            if (state == null || state.Data == null)
                return;

            for (int r = 0; r < Math.Min(state.Data.Count, this.Rows.Count); r++)
            {
                for (int c = 0; c < Math.Min(state.Data[r].Count, this.Columns.Count); c++)
                {
                    var cell = this.Rows[r].Cells[c];
                    if (!cell.ReadOnly)
                    {
                        cell.Value = state.Data[r][c];
                    }
                }
            }
        }

        /// <summary>
        /// Get selected cells data as 2D list
        /// </summary>
        private List<List<object>> GetSelectedCellsData()
        {
            var result = new List<List<object>>();

            if (this.SelectedCells.Count == 0)
                return result;

            // Get bounds of selection
            int minRow = this.SelectedCells.Cast<DataGridViewCell>().Min(c => c.RowIndex);
            int maxRow = this.SelectedCells.Cast<DataGridViewCell>().Max(c => c.RowIndex);
            int minCol = this.SelectedCells.Cast<DataGridViewCell>().Min(c => c.ColumnIndex);
            int maxCol = this.SelectedCells.Cast<DataGridViewCell>().Max(c => c.ColumnIndex);

            // Extract data in rectangular format
            for (int r = minRow; r <= maxRow; r++)
            {
                var rowData = new List<object>();
                for (int c = minCol; c <= maxCol; c++)
                {
                    rowData.Add(this.Rows[r].Cells[c].Value ?? "");
                }
                result.Add(rowData);
            }

            return result;
        }

        /// <summary>
        /// Convert data to tab-delimited clipboard format
        /// </summary>
        private string ConvertToClipboardFormat(List<List<object>> data)
        {
            var sb = new StringBuilder();

            foreach (var row in data)
            {
                sb.AppendLine(string.Join("\t", row.Select(v => v?.ToString() ?? "")));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Parse clipboard data to 2D list
        /// </summary>
        private List<List<object>> ParseClipboardData(string clipboardText)
        {
            var result = new List<List<object>>();

            var rows = clipboardText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var row in rows)
            {
                var cells = row.Split('\t').Select(c => (object)c).ToList();
                result.Add(cells);
            }

            return result;
        }

        /// <summary>
        /// Get default value for column based on type
        /// </summary>
        private object GetDefaultValueForColumn(int columnIndex)
        {
            var columnName = this.Columns[columnIndex].DataPropertyName?.ToLower() ?? "";

            if (columnName.Contains("qty") || columnName.Contains("up") ||
                columnName.Contains("sum") || columnName.Contains("price"))
            {
                return 0m;
            }

            return "";
        }

        /// <summary>
        /// Visual feedback - flash cells
        /// </summary>
        private async void FlashSelectedCells(Color color)
        {
            var originalColors = new Dictionary<DataGridViewCell, Color>();

            foreach (DataGridViewCell cell in this.SelectedCells)
            {
                originalColors[cell] = cell.Style.BackColor;
                cell.Style.BackColor = color;
            }

            this.Refresh();
            await System.Threading.Tasks.Task.Delay(200);

            foreach (var kvp in originalColors)
            {
                kvp.Key.Style.BackColor = kvp.Value;
            }

            this.Refresh();
        }

        /// <summary>
        /// Track cell edit start
        /// </summary>
        private void ExcelLikeDataGrid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            // Save state before editing
            SaveUndoState("Edit");
        }

        /// <summary>
        /// Handle cell edit completion
        /// </summary>
        private void ExcelLikeDataGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // Auto-calculate if editing quantity or price
            var cell = this.Rows[e.RowIndex].Cells[e.ColumnIndex];
            var columnName = this.Columns[e.ColumnIndex].DataPropertyName?.ToLower() ?? "";

            if (columnName.Contains("qty") || columnName.Contains("up"))
            {
                RecalculateRow(e.RowIndex);
            }
        }

        /// <summary>
        /// Recalculate sum for a row
        /// </summary>
        private void RecalculateRow(int rowIndex)
        {
            try
            {
                var row = this.Rows[rowIndex];

                // Find Qty, Up, and Sum columns
                int qtyColIndex = -1, upColIndex = -1, sumColIndex = -1;

                for (int i = 0; i < this.Columns.Count; i++)
                {
                    var propName = this.Columns[i].DataPropertyName?.ToLower() ?? "";
                    if (propName == "qty") qtyColIndex = i;
                    if (propName == "up") upColIndex = i;
                    if (propName == "sum") sumColIndex = i;
                }

                if (qtyColIndex >= 0 && upColIndex >= 0 && sumColIndex >= 0)
                {
                    decimal qty = 0, up = 0;

                    if (decimal.TryParse(row.Cells[qtyColIndex].Value?.ToString(), out qty) &&
                        decimal.TryParse(row.Cells[upColIndex].Value?.ToString(), out up))
                    {
                        row.Cells[sumColIndex].Value = qty * up;
                    }
                }
            }
            catch
            {
                // Silent fail - don't interrupt editing
            }
        }

        /// <summary>
        /// Custom cell painting for better appearance
        /// </summary>
        private void ExcelLikeDataGrid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            // Paint row headers with row numbers
            if (e.ColumnIndex < 0 && e.RowIndex >= 0)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All);

                var headerText = (e.RowIndex + 1).ToString();
                var textSize = TextRenderer.MeasureText(headerText, this.RowHeadersDefaultCellStyle.Font);
                var x = e.CellBounds.Left + (e.CellBounds.Width - textSize.Width) / 2;
                var y = e.CellBounds.Top + (e.CellBounds.Height - textSize.Height) / 2;

                TextRenderer.DrawText(e.Graphics, headerText, this.RowHeadersDefaultCellStyle.Font,
                    new Point(x, y), this.RowHeadersDefaultCellStyle.ForeColor);

                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// Grid state for undo/redo
    /// </summary>
    public class GridState
    {
        public string Operation { get; set; }
        public DateTime Timestamp { get; set; }
        public List<List<object>> Data { get; set; }
    }

    /// <summary>
    /// Undo/Redo manager
    /// </summary>
    public class UndoRedoManager
    {
        private Stack<GridState> undoStack;
        private Stack<GridState> redoStack;
        private int maxLevels;

        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;

        public UndoRedoManager(int maxUndoLevels = 50)
        {
            maxLevels = maxUndoLevels;
            undoStack = new Stack<GridState>();
            redoStack = new Stack<GridState>();
        }

        public void AddState(GridState state)
        {
            undoStack.Push(state);

            // Clear redo stack when new action is performed
            redoStack.Clear();

            // Limit undo stack size
            if (undoStack.Count > maxLevels)
            {
                var temp = undoStack.ToList();
                temp.RemoveAt(temp.Count - 1);
                undoStack = new Stack<GridState>(temp.AsEnumerable().Reverse());
            }
        }

        public GridState Undo()
        {
            if (!CanUndo)
                return null;

            var state = undoStack.Pop();
            redoStack.Push(state);
            return state;
        }

        public GridState Redo()
        {
            if (!CanRedo)
                return null;

            var state = redoStack.Pop();
            undoStack.Push(state);
            return state;
        }

        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
        }
    }
}