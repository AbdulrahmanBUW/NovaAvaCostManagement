using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Handles importing AVA/NOVA XML files
    /// </summary>
    public class XmlImporter
    {
        /// <summary>
        /// Import cost elements from AVA/NOVA XML
        /// </summary>
        public static List<CostElement> ImportFromXml(string filePath)
        {
            var elements = new List<CostElement>();

            try
            {
                var doc = XDocument.Load(filePath);

                // Debug: Show what was loaded
                MessageBox.Show($"XML loaded. Root element: {doc.Root?.Name}");

                // Try different possible root structures
                var itemNodes = doc.Descendants().Where(x =>
                x.Name.LocalName.ToLower().Contains("element") ||
                x.Name.LocalName.ToLower().Contains("item") ||
                x.Name.LocalName.ToLower().Contains("cost") ||
                x.Elements().Any(e => e.Name.LocalName.ToLower() == "name" || e.Name.LocalName.ToLower() == "text"));

                // Debug: Show how many nodes found
                MessageBox.Show($"Found {itemNodes.Count()} potential element nodes");

                foreach (var node in itemNodes)
                {
                    var element = ParseXmlNode(node);
                    if (element != null)
                    {
                        elements.Add(element);
                    }
                }

                // Debug: Show final count
                MessageBox.Show($"Successfully parsed {elements.Count} elements");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import error: {ex.Message}");
                throw new Exception($"Error importing XML: {ex.Message}", ex);
            }

            return elements;
        }

        /// <summary>
        /// Parse individual XML node to CostElement
        /// </summary>
        private static CostElement ParseXmlNode(XElement node)
        {
            var element = new CostElement();

            try
            {
                // Map common XML elements to properties
                element.Id = GetElementValue(node, "id") ?? element.Id;
                element.Id2 = GetElementValue(node, "id2") ??
                              GetElementValue(node, "code") ??
                              GetElementValue(node, "Code") ??
                              element.Id2;
                element.Name = GetElementValue(node, "name") ??
                               GetElementValue(node, "Name") ??
                               GetElementValue(node, "title") ??
                               GetElementValue(node, "code") ?? "";
                element.Type = GetElementValue(node, "type") ?? "";
                element.Text = GetElementValue(node, "text") ??
                               GetElementValue(node, "Text") ??
                               GetElementValue(node, "name") ??
                               GetElementValue(node, "code") ?? "";
                element.LongText = GetElementValue(node, "longtext") ??
                               GetElementValue(node, "LongText") ??
                               GetElementValue(node, "description") ??
                               GetElementValue(node, "Description") ??
                               GetElementValue(node, "name") ?? "";
                element.Label = GetElementValue(node, "label") ?? "";
                element.BimKey = GetElementValue(node, "bimkey") ?? "";
                element.Properties = GetElementValue(node, "properties") ?? "";
                element.Note = GetElementValue(node, "note") ?? "";
                element.Color = GetElementValue(node, "color") ?? "";
                element.Qu = GetElementValue(node, "unit") ?? GetElementValue(node, "qu") ?? "";
                element.ProcUnit = element.Qu;

                // Parse numeric fields
                if (decimal.TryParse(GetElementValue(node, "quantity") ?? GetElementValue(node, "qty"), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal qty))
                    element.Qty = qty;

                if (decimal.TryParse(GetElementValue(node, "unitprice") ?? GetElementValue(node, "up"), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal up))
                    element.Up = up;

                if (decimal.TryParse(GetElementValue(node, "total") ?? GetElementValue(node, "sum"), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal total))
                    element.Sum = total;

                // Parse IFC-specific fields if present
                element.IfcType = GetElementValue(node, "ifc_type") ?? GetElementValue(node, "ifctype") ?? "";
                element.Material = GetElementValue(node, "material") ?? "";
                element.Dimension = GetElementValue(node, "dimension") ?? "";
                element.SegmentType = GetElementValue(node, "segment_type") ?? GetElementValue(node, "segmenttype") ?? "";

                // Parse timestamps
                if (DateTime.TryParse(GetElementValue(node, "created"), out DateTime created))
                    element.Created = created;

                // Store unknown elements in AdditionalData
                foreach (var childNode in node.Elements())
                {
                    var name = childNode.Name.LocalName;
                    if (!IsKnownElement(name))
                    {
                        element.AdditionalData[name] = childNode.Value;
                    }
                }

                element.CalculateFields();

                return element;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing XML node: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get element value safely
        /// </summary>
        private static string GetElementValue(XElement parent, string elementName)
        {
            return parent.Element(elementName)?.Value;
        }

        /// <summary>
        /// Check if element name is known/mapped
        /// </summary>
        private static bool IsKnownElement(string name)
        {
            var knownElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "id", "id2", "code", "name", "type", "text", "longtext", "description", "label",
                "bimkey", "properties", "note", "color", "unit", "qu", "quantity", "qty",
                "unitprice", "up", "total", "sum", "ifc_type", "ifctype", "material",
                "dimension", "segment_type", "segmenttype", "created"
            };

            return knownElements.Contains(name);
        }
    }
}