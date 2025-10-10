using System;
using System.Collections.Generic;
using System.Linq;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// View mode for the main grid
    /// </summary>
    public enum ViewMode
    {
        FlatList,
        WbsHierarchy
    }

    /// <summary>
    /// Represents a node in the WBS hierarchy
    /// </summary>
    public class WbsNode
    {
        public string Id { get; set; }
        public string Number { get; set; }
        public string Name { get; set; }
        public string CatalogType { get; set; }
        public int Level { get; set; }
        public bool IsGroup { get; set; }
        public CostElement Element { get; set; }
        public List<WbsNode> Children { get; set; } = new List<WbsNode>();

        public WbsNode(string id, string number, string name, string catalogType, int level, bool isGroup = true)
        {
            Id = id;
            Number = number;
            Name = name;
            CatalogType = catalogType;
            Level = level;
            IsGroup = isGroup;
        }

        /// <summary>
        /// Creates a flat display list from WBS tree
        /// </summary>
        public static List<WbsDisplayItem> FlattenToDisplayList(List<WbsNode> nodes)
        {
            var displayList = new List<WbsDisplayItem>();
            foreach (var node in nodes)
            {
                AddNodeToDisplayList(node, displayList);
            }
            return displayList;
        }

        private static void AddNodeToDisplayList(WbsNode node, List<WbsDisplayItem> displayList)
        {
            displayList.Add(new WbsDisplayItem(node));

            foreach (var child in node.Children.OrderBy(c => c.Number).ThenBy(c => c.Name))
            {
                AddNodeToDisplayList(child, displayList);
            }
        }
    }

    /// <summary>
    /// Flattened display item for DataGridView
    /// </summary>
    public class WbsDisplayItem
    {
        public int Level { get; set; }
        public bool IsGroup { get; set; }
        public string DisplayNumber { get; set; }
        public string DisplayName { get; set; }
        public string CatalogType { get; set; }
        public CostElement Element { get; set; }

        // Properties for grid binding with indentation
        public string Id => Element?.Id ?? "";
        public string Name => GetIndentedText(DisplayName);
        public string Number => DisplayNumber;
        public string Text => Element?.Text ?? "";
        public string LongText => Element?.LongText ?? "";
        public decimal QtyResult => Element?.QtyResult ?? 0;
        public string Qu => Element?.Qu ?? "";
        public decimal Up => Element?.Up ?? 0;
        public decimal UpResult => Element?.UpResult ?? 0;
        public string Properties => Element?.Properties ?? "";
        public string Children => Element?.Children ?? "";
        public string Ident => Element?.Ident ?? "";
        public string CatalogName => Element?.CatalogName ?? "";
        public string CatalogItemName => Element?.CatalogItemName ?? "";
        public string CatalogNumber => Element?.CatalogNumber ?? "";

        public WbsDisplayItem(WbsNode node)
        {
            Level = node.Level;
            IsGroup = node.IsGroup;
            DisplayNumber = node.Number;
            DisplayName = node.Name;
            CatalogType = node.CatalogType;
            Element = node.Element;
        }

        /// <summary>
        /// Add indentation based on level
        /// </summary>
        private string GetIndentedText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            // Use spaces for indentation (4 spaces per level)
            string indent = new string(' ', Level * 4);
            return indent + text;
        }

        /// <summary>
        /// Get font style indicator for group rows
        /// </summary>
        public bool IsBold => IsGroup;
    }

    /// <summary>
    /// Builds WBS hierarchy from cost elements
    /// </summary>
    public static class WbsBuilder
    {
        public static List<WbsNode> BuildWbsTree(List<CostElement> elements)
        {
            var rootNodes = new List<WbsNode>();
            var catalogGroups = new Dictionary<string, WbsNode>();
            var numberGroups = new Dictionary<string, WbsNode>();

            foreach (var element in elements.OrderBy(e => int.TryParse(e.Id, out int id) ? id : 0))
            {
                // Skip if no catalog data
                if (string.IsNullOrEmpty(element.CatalogNumber))
                    continue;

                // Level 1: Catalog Type Group
                string catalogTypeKey = element.CatalogType ?? "Uncategorized";
                if (!catalogGroups.ContainsKey(catalogTypeKey))
                {
                    var catalogNode = new WbsNode(
                        $"CAT_{catalogTypeKey}",
                        "",
                        catalogTypeKey,
                        catalogTypeKey,
                        0,
                        true
                    );
                    catalogGroups[catalogTypeKey] = catalogNode;
                    rootNodes.Add(catalogNode);
                }

                var parentCatalogNode = catalogGroups[catalogTypeKey];

                // Level 2: Number Group
                string numberKey = $"{catalogTypeKey}_{element.CatalogNumber}";
                if (!numberGroups.ContainsKey(numberKey))
                {
                    var numberNode = new WbsNode(
                        $"NUM_{numberKey}",
                        element.CatalogNumber,
                        element.CatalogItemName ?? "Unnamed Category",
                        catalogTypeKey,
                        1,
                        true
                    );
                    numberGroups[numberKey] = numberNode;
                    parentCatalogNode.Children.Add(numberNode);
                }

                var parentNumberNode = numberGroups[numberKey];

                // Level 3: Individual Element
                var elementNode = new WbsNode(
                    element.Id,
                    element.CatalogNumber,
                    element.Name,
                    element.CatalogType,
                    2,
                    false
                )
                {
                    Element = element
                };

                parentNumberNode.Children.Add(elementNode);
            }

            // Sort root nodes
            return rootNodes.OrderBy(n => n.Name).ToList();
        }

        /// <summary>
        /// Build WBS tree handling multiple catalog assignments
        /// </summary>
        public static List<WbsNode> BuildMultiCatalogWbsTree(List<CostElement> elements)
        {
            // Group elements by their catalog assignments
            // Since each element can have multiple catalogs, we need to handle duplicates

            var rootNodes = new List<WbsNode>();
            var catalogGroups = new Dictionary<string, WbsNode>();
            var numberGroups = new Dictionary<string, WbsNode>();

            // First pass: Create unique catalog type nodes
            var allCatalogTypes = elements
                .Where(e => !string.IsNullOrEmpty(e.CatalogType))
                .Select(e => e.CatalogType)
                .Distinct()
                .OrderBy(t => t);

            foreach (var catalogType in allCatalogTypes)
            {
                var catalogNode = new WbsNode(
                    $"CAT_{catalogType}",
                    "",
                    catalogType,
                    catalogType,
                    0,
                    true
                );
                catalogGroups[catalogType] = catalogNode;
                rootNodes.Add(catalogNode);
            }

            // Second pass: Add number groups and elements
            foreach (var element in elements.OrderBy(e => int.TryParse(e.Id, out int id) ? id : 0))
            {
                if (string.IsNullOrEmpty(element.CatalogNumber) || string.IsNullOrEmpty(element.CatalogType))
                    continue;

                string catalogType = element.CatalogType;
                if (!catalogGroups.ContainsKey(catalogType))
                    continue;

                var parentCatalogNode = catalogGroups[catalogType];

                // Level 2: Number Group
                string numberKey = $"{catalogType}_{element.CatalogNumber}";
                if (!numberGroups.ContainsKey(numberKey))
                {
                    var numberNode = new WbsNode(
                        $"NUM_{numberKey}",
                        element.CatalogNumber,
                        element.CatalogItemName ?? "Unnamed Category",
                        catalogType,
                        1,
                        true
                    );
                    numberGroups[numberKey] = numberNode;
                    parentCatalogNode.Children.Add(numberNode);
                }

                var parentNumberNode = numberGroups[numberKey];

                // Level 3: Individual Element
                var elementNode = new WbsNode(
                    element.Id,
                    element.CatalogNumber,
                    element.Name,
                    element.CatalogType,
                    2,
                    false
                )
                {
                    Element = element
                };

                parentNumberNode.Children.Add(elementNode);
            }

            return rootNodes;
        }
    }
}