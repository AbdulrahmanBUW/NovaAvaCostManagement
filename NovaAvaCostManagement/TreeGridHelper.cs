using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Helper class to manage tree hierarchy in DataGridView
    /// </summary>
    public class TreeGridHelper
    {
        private ExcelLikeDataGrid grid;
        private Dictionary<int, bool> expandedState = new Dictionary<int, bool>();
        private const string EXPAND_SYMBOL = "▶";
        private const string COLLAPSE_SYMBOL = "▼";

        public TreeGridHelper(ExcelLikeDataGrid dataGrid)
        {
            grid = dataGrid;
        }

        /// <summary>
        /// Convert flat list to hierarchical display
        /// </summary>
        public List<TreeNode<CostElement>> BuildTree(List<CostElement> flatElements)
        {
            var tree = new List<TreeNode<CostElement>>();
            var lookup = new Dictionary<string, TreeNode<CostElement>>();

            // Group by element (parent level)
            var grouped = flatElements.GroupBy(e => e.Id).ToList();

            foreach (var group in grouped)
            {
                // Create parent node
                var parentElement = group.First();
                var parentNode = new TreeNode<CostElement>
                {
                    Data = parentElement,
                    IsExpanded = true,
                    Level = 0
                };

                // Add child calculations
                foreach (var calc in group)
                {
                    if (calc != parentElement || group.Count() == 1)
                    {
                        var childNode = new TreeNode<CostElement>
                        {
                            Data = calc,
                            Parent = parentNode,
                            Level = 1,
                            IsExpanded = false
                        };
                        parentNode.Children.Add(childNode);
                    }
                }

                tree.Add(parentNode);
                lookup[parentElement.Id] = parentNode;
            }

            return tree;
        }

        /// <summary>
        /// Flatten tree for display
        /// </summary>
        public List<CostElement> FlattenTree(List<TreeNode<CostElement>> tree)
        {
            var result = new List<CostElement>();

            foreach (var node in tree)
            {
                // Add parent
                result.Add(node.Data);

                // Add children if expanded
                if (node.IsExpanded && node.Children.Any())
                {
                    foreach (var child in node.Children)
                    {
                        result.Add(child.Data);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Get tree level for an element
        /// </summary>
        public int GetLevel(CostElement element)
        {
            // Parent elements have ParentCalcId = 0 or empty Parent field
            if (string.IsNullOrEmpty(element.Parent) || element.ParentCalcId == 0)
                return 0;
            return 1;
        }

        /// <summary>
        /// Check if element is a parent node
        /// </summary>
        public bool IsParentNode(CostElement element)
        {
            return GetLevel(element) == 0;
        }

        /// <summary>
        /// Apply tree formatting to grid
        /// </summary>
        public void FormatTreeGrid()
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                var element = row.DataBoundItem as CostElement;
                if (element == null) continue;

                int level = GetLevel(element);
                bool isParent = IsParentNode(element);

                // Apply indentation and styling
                if (isParent)
                {
                    // Parent row styling
                    row.DefaultCellStyle.BackColor = Color.FromArgb(240, 248, 255);
                    row.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

                    // Add expand/collapse symbol to text
                    var textCell = row.Cells["colText"];
                    if (textCell != null && !string.IsNullOrEmpty(textCell.Value?.ToString()))
                    {
                        string symbol = COLLAPSE_SYMBOL; // Expanded by default
                        textCell.Value = $"{symbol} {element.Text}";
                    }
                }
                else
                {
                    // Child row styling
                    row.DefaultCellStyle.BackColor = Color.White;
                    row.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

                    // Indent text
                    var textCell = row.Cells["colText"];
                    if (textCell != null && !string.IsNullOrEmpty(textCell.Value?.ToString()))
                    {
                        textCell.Value = $"    {element.Text}";
                    }
                }
            }
        }
    }

    /// <summary>
    /// Tree node for hierarchy
    /// </summary>
    public class TreeNode<T>
    {
        public T Data { get; set; }
        public TreeNode<T> Parent { get; set; }
        public List<TreeNode<T>> Children { get; set; } = new List<TreeNode<T>>();
        public bool IsExpanded { get; set; } = true;
        public int Level { get; set; } = 0;

        public bool HasChildren => Children.Any();
    }
}