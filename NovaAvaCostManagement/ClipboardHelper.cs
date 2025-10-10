using System;
using System.Globalization;
using System.Windows.Forms;

namespace NovaAvaCostManagement
{
    public static class ClipboardHelper
    {
        public static void CopyCellsToClipboard(DataGridView dataGridView)
        {
            try
            {
                var content = dataGridView.GetClipboardContent();
                if (content != null)
                {
                    Clipboard.SetDataObject(content);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Copy error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public static int PasteCellsFromClipboard(DataGridView dataGridView, ViewMode currentViewMode,
            UndoRedoManager undoRedoManager, ref bool isRecordingChanges)
        {
            try
            {
                string clipboardText = Clipboard.GetText();
                if (string.IsNullOrEmpty(clipboardText))
                    return 0;

                if (dataGridView.CurrentCell == null)
                    return 0;

                int startRow = dataGridView.CurrentCell.RowIndex;
                int startCol = dataGridView.CurrentCell.ColumnIndex;

                if (dataGridView.SelectedCells.Count > 0)
                {
                    int minRow = int.MaxValue;
                    int minCol = int.MaxValue;

                    foreach (DataGridViewCell cell in dataGridView.SelectedCells)
                    {
                        if (cell.RowIndex < minRow) minRow = cell.RowIndex;
                        if (cell.ColumnIndex < minCol) minCol = cell.ColumnIndex;
                    }

                    startRow = minRow;
                    startCol = minCol;
                }

                string[] rows = clipboardText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                var changeSet = new ChangeSet("Paste cells");
                int pastedCells = 0;

                bool wasRecording = isRecordingChanges;
                isRecordingChanges = false;

                try
                {
                    for (int i = 0; i < rows.Length; i++)
                    {
                        if (string.IsNullOrEmpty(rows[i]) && i == rows.Length - 1)
                            continue;

                        int targetRow = startRow + i;
                        if (targetRow >= dataGridView.Rows.Count)
                            break;

                        string[] cells = rows[i].Split('\t');

                        for (int j = 0; j < cells.Length; j++)
                        {
                            int targetCol = startCol + j;
                            if (targetCol >= dataGridView.Columns.Count)
                                break;

                            var column = dataGridView.Columns[targetCol];
                            if (column.ReadOnly)
                                continue;

                            var element = GridHelper.GetElementAtRow(dataGridView, targetRow, currentViewMode);
                            if (element == null)
                                continue;

                            string propertyName = column.DataPropertyName;
                            if (string.IsNullOrEmpty(propertyName))
                                continue;

                            var property = typeof(CostElement).GetProperty(propertyName);
                            if (property == null || !property.CanWrite)
                                continue;

                            object oldValue = property.GetValue(element);
                            string newValueStr = cells[j];

                            try
                            {
                                object newValue = ConvertValue(newValueStr, property.PropertyType);

                                if (!Equals(oldValue, newValue))
                                {
                                    changeSet.AddChange(new CellChange(
                                        targetRow,
                                        column.Name,
                                        propertyName,
                                        oldValue,
                                        newValue,
                                        element
                                    ));

                                    property.SetValue(element, newValue);
                                    pastedCells++;
                                }
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }
                }
                finally
                {
                    isRecordingChanges = wasRecording;
                }

                if (changeSet.Changes.Count > 0)
                {
                    undoRedoManager.RecordChange(changeSet);
                }

                dataGridView.Refresh();
                return pastedCells;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Paste error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 0;
            }
        }

        public static object ConvertValue(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (targetType == typeof(string)) return "";
                if (targetType == typeof(decimal)) return 0m;
                if (targetType == typeof(int)) return 0;
                if (targetType == typeof(bool)) return false;
                return null;
            }

            if (targetType == typeof(string))
                return value;
            else if (targetType == typeof(decimal))
                return decimal.Parse(value.Replace(',', '.'), CultureInfo.InvariantCulture);
            else if (targetType == typeof(int))
                return int.Parse(value);
            else if (targetType == typeof(bool))
                return bool.Parse(value);

            return Convert.ChangeType(value, targetType);
        }

        public static void ClearSelectedCells(DataGridView dataGridView, ViewMode currentViewMode,
            UndoRedoManager undoRedoManager)
        {
            if (dataGridView.SelectedCells.Count == 0)
                return;

            var changeSet = new ChangeSet("Clear cells");

            foreach (DataGridViewCell cell in dataGridView.SelectedCells)
            {
                if (cell.ReadOnly)
                    continue;

                var column = dataGridView.Columns[cell.ColumnIndex];
                var propertyName = column.DataPropertyName;
                if (string.IsNullOrEmpty(propertyName))
                    continue;

                var element = GridHelper.GetElementAtRow(dataGridView, cell.RowIndex, currentViewMode);
                if (element == null)
                    continue;

                var property = typeof(CostElement).GetProperty(propertyName);
                if (property == null || !property.CanWrite)
                    continue;

                object oldValue = property.GetValue(element);
                object newValue = GetDefaultValue(property.PropertyType);

                changeSet.AddChange(new CellChange(
                    cell.RowIndex,
                    column.Name,
                    propertyName,
                    oldValue,
                    newValue,
                    element
                ));

                property.SetValue(element, newValue);
                cell.Value = newValue;
            }

            if (changeSet.Changes.Count > 0)
            {
                undoRedoManager.RecordChange(changeSet);
            }
        }

        private static object GetDefaultValue(Type propertyType)
        {
            if (propertyType == typeof(string))
                return "";
            else if (propertyType == typeof(decimal))
                return 0m;
            else if (propertyType == typeof(int))
                return 0;
            else if (propertyType == typeof(bool))
                return false;

            return null;
        }
    }
}