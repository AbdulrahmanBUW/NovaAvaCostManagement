using System;
using System.Collections.Generic;
using System.Linq;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Represents a single change to a cell
    /// </summary>
    public class CellChange
    {
        public int RowIndex { get; set; }
        public string ColumnName { get; set; }
        public string PropertyName { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
        public CostElement Element { get; set; }

        public CellChange(int rowIndex, string columnName, string propertyName, object oldValue, object newValue, CostElement element)
        {
            RowIndex = rowIndex;
            ColumnName = columnName;
            PropertyName = propertyName;
            OldValue = oldValue;
            NewValue = newValue;
            Element = element;
        }
    }

    /// <summary>
    /// Represents a batch of changes (for multi-cell operations)
    /// </summary>
    public class ChangeSet
    {
        public List<CellChange> Changes { get; set; } = new List<CellChange>();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Description { get; set; }

        public ChangeSet(string description = "Change")
        {
            Description = description;
        }

        public void AddChange(CellChange change)
        {
            Changes.Add(change);
        }
    }

    /// <summary>
    /// Manages Undo/Redo operations
    /// </summary>
    public class UndoRedoManager
    {
        private Stack<ChangeSet> undoStack = new Stack<ChangeSet>();
        private Stack<ChangeSet> redoStack = new Stack<ChangeSet>();
        private const int MaxUndoLevels = 50;

        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;

        /// <summary>
        /// Record a change set
        /// </summary>
        public void RecordChange(ChangeSet changeSet)
        {
            if (changeSet.Changes.Count == 0)
                return;

            undoStack.Push(changeSet);
            redoStack.Clear(); // Clear redo stack when new change is made

            // Limit undo stack size
            if (undoStack.Count > MaxUndoLevels)
            {
                var oldStack = undoStack.ToList();
                oldStack.RemoveAt(oldStack.Count - 1); // Remove oldest
                undoStack = new Stack<ChangeSet>(oldStack.AsEnumerable().Reverse());
            }
        }

        /// <summary>
        /// Undo last change
        /// </summary>
        public ChangeSet Undo()
        {
            if (!CanUndo)
                return null;

            var changeSet = undoStack.Pop();
            redoStack.Push(changeSet);

            // Apply undo - revert to old values
            foreach (var change in changeSet.Changes)
            {
                ApplyValue(change.Element, change.PropertyName, change.OldValue);
            }

            return changeSet;
        }

        /// <summary>
        /// Redo last undone change
        /// </summary>
        public ChangeSet Redo()
        {
            if (!CanRedo)
                return null;

            var changeSet = redoStack.Pop();
            undoStack.Push(changeSet);

            // Apply redo - reapply new values
            foreach (var change in changeSet.Changes)
            {
                ApplyValue(change.Element, change.PropertyName, change.NewValue);
            }

            return changeSet;
        }

        /// <summary>
        /// Clear all undo/redo history
        /// </summary>
        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
        }

        /// <summary>
        /// Apply a value to an element property
        /// </summary>
        private void ApplyValue(CostElement element, string propertyName, object value)
        {
            var property = typeof(CostElement).GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                // Handle type conversion
                if (value != null && property.PropertyType != value.GetType())
                {
                    try
                    {
                        if (property.PropertyType == typeof(decimal))
                        {
                            value = Convert.ToDecimal(value);
                        }
                        else if (property.PropertyType == typeof(int))
                        {
                            value = Convert.ToInt32(value);
                        }
                        else if (property.PropertyType == typeof(string))
                        {
                            value = value.ToString();
                        }
                    }
                    catch
                    {
                        // If conversion fails, skip this change
                        return;
                    }
                }

                property.SetValue(element, value);
            }
        }

        /// <summary>
        /// Get undo stack info for debugging
        /// </summary>
        public string GetUndoStackInfo()
        {
            return $"Undo: {undoStack.Count} | Redo: {redoStack.Count}";
        }
    }
}