using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Enhanced XML importer with schema validation and better error handling
    /// </summary>
    public class XmlImporter
    {
        // Schema definitions for validation
        private static readonly Dictionary<string, string[]> RequiredFields = new Dictionary<string, string[]>
        {
            ["nova"] = new[] { "name", "text", "longtext" },
            ["gaeb"] = new[] { "Description", "LongText", "Unit" }
        };

        private static readonly Dictionary<string, string[]> OptionalFields = new Dictionary<string, string[]>
        {
            ["nova"] = new[] { "id", "id2", "type", "qty", "qu", "up", "properties", "bimkey", "note", "color" },
            ["gaeb"] = new[] { "RNoPart", "Qty", "UP", "Total", "Properties" }
        };

        /// <summary>
        /// Import result with detailed information
        /// </summary>
        public class ImportResult
        {
            public List<CostElement> Elements { get; set; } = new List<CostElement>();
            public List<string> Warnings { get; set; } = new List<string>();
            public List<string> Errors { get; set; } = new List<string>();
            public ImportFormat Format { get; set; }
            public int SkippedElements { get; set; }
            public bool Success => Errors.Count == 0 && Elements.Count > 0;
        }

        public enum ImportFormat
        {
            Unknown,
            NovaAva,
            Gaeb,
            Generic
        }

        /// <summary>
        /// Import cost elements from XML with validation
        /// </summary>
        public static ImportResult ImportFromXmlWithValidation(string filePath)
        {
            var result = new ImportResult();

            try
            {
                // Validate file exists and is readable
                if (!File.Exists(filePath))
                {
                    result.Errors.Add($"File not found: {filePath}");
                    return result;
                }

                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > 100 * 1024 * 1024) // 100MB limit
                {
                    result.Errors.Add("File too large (>100MB)");
                    return result;
                }

                // Load and validate XML
                var doc = LoadAndValidateXml(filePath, result);
                if (doc == null || !result.Success)
                {
                    return result;
                }

                // Detect format
                result.Format = DetectXmlFormat(doc);

                // Import based on format
                switch (result.Format)
                {
                    case ImportFormat.NovaAva:
                        ImportNovaAvaFormat(doc, result);
                        break;
                    case ImportFormat.Gaeb:
                        ImportGaebFormat(doc, result);
                        break;
                    default:
                        ImportGenericFormat(doc, result);
                        break;
                }

                // Post-process elements
                PostProcessElements(result);

                if (result.Elements.Count == 0)
                {
                    result.Errors.Add("No valid elements found in the XML file");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Import error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Load and validate XML structure
        /// </summary>
        private static XDocument LoadAndValidateXml(string filePath, ImportResult result)
        {
            try
            {
                // Load with settings to prevent XXE attacks
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    MaxCharactersFromEntities = 1024
                };

                using (var reader = XmlReader.Create(filePath, settings))
                {
                    var doc = XDocument.Load(reader);

                    // Basic structure validation
                    if (doc.Root == null)
                    {
                        result.Errors.Add("Invalid XML: No root element");
                        return null;
                    }

                    if (!doc.Root.HasElements)
                    {
                        result.Errors.Add("Invalid XML: Root element has no children");
                        return null;
                    }

                    return doc;
                }
            }
            catch (XmlException ex)
            {
                result.Errors.Add($"XML parsing error at line {ex.LineNumber}: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to load XML: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Detect XML format from structure
        /// </summary>
        private static ImportFormat DetectXmlFormat(XDocument doc)
        {
            var rootName = doc.Root.Name.LocalName.ToLower();

            // Check for NOVA/AVA format
            if (rootName.Contains("cost") || rootName.Contains("element") || rootName.Contains("nova") || rootName.Contains("ava"))
            {
                return ImportFormat.NovaAva;
            }

            // Check for GAEB format
            if (rootName.Contains("gaeb") || doc.Root.Descendants().Any(e => e.Name.LocalName.Equals("BoQ", StringComparison.OrdinalIgnoreCase)))
            {
                return ImportFormat.Gaeb;
            }

            // Check for presence of key elements
            var hasNovaElements = doc.Descendants().Any(e =>
                e.Element("name") != null || e.Element("text") != null || e.Element("longtext") != null);

            if (hasNovaElements)
            {
                return ImportFormat.NovaAva;
            }

            return ImportFormat.Generic;
        }

        /// <summary>
        /// Import NOVA/AVA format
        /// </summary>
        private static void ImportNovaAvaFormat(XDocument doc, ImportResult result)
        {
            var elementNodes = doc.Descendants()
                .Where(e => e.Name.LocalName.ToLower().Contains("element") ||
                           (e.Element("name") != null && e.Element("text") != null));

            foreach (var node in elementNodes)
            {
                try
                {
                    var element = ParseNovaAvaElement(node);
                    if (ValidateElement(element, result))
                    {
                        result.Elements.Add(element);
                    }
                    else
                    {
                        result.SkippedElements++;
                    }
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Failed to parse element: {ex.Message}");
                    result.SkippedElements++;
                }
            }
        }

        /// <summary>
        /// Parse NOVA/AVA element
        /// </summary>
        private static CostElement ParseNovaAvaElement(XElement node)
        {
            var element = new CostElement();

            // Map standard fields with case-insensitive lookup
            element.Id = GetElementValueCaseInsensitive(node, "id") ?? element.Id;
            element.Id2 = GetElementValueCaseInsensitive(node, "id2", "code") ?? element.Id2;
            element.Name = GetElementValueCaseInsensitive(node, "name") ?? "";
            element.Type = GetElementValueCaseInsensitive(node, "type") ?? "";
            element.Text = GetElementValueCaseInsensitive(node, "text") ?? "";
            element.LongText = GetElementValueCaseInsensitive(node, "longtext", "long_text", "description") ?? "";
            element.Label = GetElementValueCaseInsensitive(node, "label") ?? "";
            element.BimKey = GetElementValueCaseInsensitive(node, "bimkey", "bim_key") ?? "";
            element.Properties = GetElementValueCaseInsensitive(node, "properties") ?? "";
            element.Note = GetElementValueCaseInsensitive(node, "note", "notes") ?? "";
            element.Color = GetElementValueCaseInsensitive(node, "color", "colour") ?? "";
            element.Qu = GetElementValueCaseInsensitive(node, "qu", "unit") ?? "";
            element.ProcUnit = element.Qu;

            // Parse numeric fields with culture-invariant parsing
            element.Qty = ParseDecimal(GetElementValueCaseInsensitive(node, "qty", "quantity"));
            element.Up = ParseDecimal(GetElementValueCaseInsensitive(node, "up", "unitprice", "unit_price"));
            element.Sum = ParseDecimal(GetElementValueCaseInsensitive(node, "sum", "total"));
            element.Vat = ParseDecimal(GetElementValueCaseInsensitive(node, "vat"));
            element.Tax = ParseDecimal(GetElementValueCaseInsensitive(node, "tax"));

            // Parse IFC fields
            element.IfcType = GetElementValueCaseInsensitive(node, "ifc_type", "ifctype") ?? "";
            element.Material = GetElementValueCaseInsensitive(node, "material") ?? "";
            element.Dimension = GetElementValueCaseInsensitive(node, "dimension") ?? "";
            element.SegmentType = GetElementValueCaseInsensitive(node, "segment_type", "segmenttype") ?? "";

            // Parse dates
            element.Created = ParseDateTime(GetElementValueCaseInsensitive(node, "created"));
            element.Created3 = ParseDateTime(GetElementValueCaseInsensitive(node, "created3"));

            // Parse all remaining fields
            ParseAdditionalFields(node, element);

            // Calculate fields
            element.CalculateFields();

            return element;
        }

        /// <summary>
        /// Import GAEB format
        /// </summary>
        private static void ImportGaebFormat(XDocument doc, ImportResult result)
        {
            var itemNodes = doc.Descendants()
                .Where(e => e.Name.LocalName.Equals("Item", StringComparison.OrdinalIgnoreCase));

            foreach (var node in itemNodes)
            {
                try
                {
                    var element = ParseGaebElement(node);
                    if (ValidateElement(element, result))
                    {
                        result.Elements.Add(element);
                    }
                    else
                    {
                        result.SkippedElements++;
                    }
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Failed to parse GAEB item: {ex.Message}");
                    result.SkippedElements++;
                }
            }
        }

        /// <summary>
        /// Parse GAEB element
        /// </summary>
        private static CostElement ParseGaebElement(XElement node)
        {
            var element = new CostElement();

            // Get RNoPart attribute or element
            element.Id2 = node.Attribute("RNoPart")?.Value ??
                         GetElementValueCaseInsensitive(node, "RNoPart") ??
                         element.Id2;

            element.Text = GetElementValueCaseInsensitive(node, "Description") ?? "";
            element.LongText = GetElementValueCaseInsensitive(node, "LongText") ?? element.Text;
            element.Name = element.Text;
            element.Qu = GetElementValueCaseInsensitive(node, "Unit") ?? "";
            element.ProcUnit = element.Qu;

            element.Qty = ParseDecimal(GetElementValueCaseInsensitive(node, "Qty"));
            element.Up = ParseDecimal(GetElementValueCaseInsensitive(node, "UP"));
            element.Sum = ParseDecimal(GetElementValueCaseInsensitive(node, "Total"));

            element.Properties = GetElementValueCaseInsensitive(node, "Properties") ?? "";

            element.CalculateFields();

            return element;
        }

        /// <summary>
        /// Import generic XML format
        /// </summary>
        private static void ImportGenericFormat(XDocument doc, ImportResult result)
        {
            result.Warnings.Add("Unknown XML format - attempting generic import");

            // Find nodes that look like cost elements
            var potentialElements = doc.Descendants()
                .Where(e => e.HasElements &&
                           (ContainsKeyFields(e) || LooksLikeCostElement(e)));

            foreach (var node in potentialElements)
            {
                try
                {
                    var element = ParseGenericElement(node);
                    if (ValidateElement(element, result))
                    {
                        result.Elements.Add(element);
                    }
                    else
                    {
                        result.SkippedElements++;
                    }
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Failed to parse generic element: {ex.Message}");
                    result.SkippedElements++;
                }
            }
        }

        /// <summary>
        /// Parse generic element with fuzzy matching
        /// </summary>
        private static CostElement ParseGenericElement(XElement node)
        {
            var element = new CostElement();

            // Use fuzzy matching for common field names
            var fieldMappings = new Dictionary<string[], Action<string>>
            {
                [new[] { "id", "identifier", "key" }] = v => element.Id = v,
                [new[] { "code", "id2", "reference" }] = v => element.Id2 = v,
                [new[] { "name", "title", "designation" }] = v => element.Name = v,
                [new[] { "text", "shorttext", "description" }] = v => element.Text = v,
                [new[] { "longtext", "detailtext", "details" }] = v => element.LongText = v,
                [new[] { "qty", "quantity", "amount" }] = v => element.Qty = ParseDecimal(v),
                [new[] { "unit", "qu", "uom" }] = v => element.Qu = v,
                [new[] { "price", "unitprice", "up" }] = v => element.Up = ParseDecimal(v),
                [new[] { "total", "sum", "amount" }] = v => element.Sum = ParseDecimal(v)
            };

            foreach (var child in node.Elements())
            {
                var childName = child.Name.LocalName.ToLower();
                var value = child.Value?.Trim();

                if (string.IsNullOrEmpty(value)) continue;

                foreach (var mapping in fieldMappings)
                {
                    if (mapping.Key.Any(k => childName.Contains(k)))
                    {
                        mapping.Value(value);
                        break;
                    }
                }

                // Store unmapped fields in AdditionalData
                if (!fieldMappings.Keys.Any(keys => keys.Any(k => childName.Contains(k))))
                {
                    element.AdditionalData[child.Name.LocalName] = value;
                }
            }

            // Ensure required fields have values
            if (string.IsNullOrEmpty(element.Name) && !string.IsNullOrEmpty(element.Text))
                element.Name = element.Text;
            if (string.IsNullOrEmpty(element.Text) && !string.IsNullOrEmpty(element.Name))
                element.Text = element.Name;
            if (string.IsNullOrEmpty(element.LongText))
                element.LongText = element.Text;

            element.CalculateFields();

            return element;
        }

        /// <summary>
        /// Parse additional fields not explicitly mapped
        /// </summary>
        private static void ParseAdditionalFields(XElement node, CostElement element)
        {
            var mappedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "id", "id2", "name", "type", "text", "longtext", "qty", "qu", "up", "sum",
                "properties", "bimkey", "note", "color", "vat", "tax", "ifc_type", "material",
                "dimension", "segment_type", "created", "created3"
            };

            foreach (var child in node.Elements())
            {
                var fieldName = child.Name.LocalName;
                if (!mappedFields.Contains(fieldName) && !string.IsNullOrWhiteSpace(child.Value))
                {
                    element.AdditionalData[fieldName] = child.Value;
                }
            }
        }

        /// <summary>
        /// Validate element has minimum required fields
        /// </summary>
        private static bool ValidateElement(CostElement element, ImportResult result)
        {
            var errors = new List<string>();

            // Check required fields
            if (string.IsNullOrWhiteSpace(element.Name) &&
                string.IsNullOrWhiteSpace(element.Text) &&
                string.IsNullOrWhiteSpace(element.LongText))
            {
                errors.Add("Element has no name, text, or description");
            }

            // Validate numeric ranges
            if (element.Qty < 0)
                errors.Add("Negative quantity");
            if (element.Up < 0)
                errors.Add("Negative unit price");
            if (element.Vat < 0 || element.Vat > 100)
                errors.Add("Invalid VAT percentage");
            if (element.Tax < 0 || element.Tax > 100)
                errors.Add("Invalid tax percentage");

            if (errors.Any())
            {
                result.Warnings.Add($"Element validation failed: {string.Join(", ", errors)}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Post-process imported elements
        /// </summary>
        private static void PostProcessElements(ImportResult result)
        {
            // Remove duplicates based on ID
            var uniqueElements = new Dictionary<string, CostElement>();
            var duplicates = 0;

            foreach (var element in result.Elements)
            {
                var key = $"{element.Id}_{element.Id2}";
                if (!uniqueElements.ContainsKey(key))
                {
                    uniqueElements[key] = element;
                }
                else
                {
                    duplicates++;
                }
            }

            if (duplicates > 0)
            {
                result.Warnings.Add($"Removed {duplicates} duplicate elements");
                result.Elements = uniqueElements.Values.ToList();
            }

            // Sort by ID if possible
            result.Elements = result.Elements
                .OrderBy(e => int.TryParse(e.Id, out int id) ? id : int.MaxValue)
                .ThenBy(e => e.Id)
                .ThenBy(e => e.Name)
                .ToList();

            // Ensure all elements have required fields filled
            foreach (var element in result.Elements)
            {
                if (string.IsNullOrWhiteSpace(element.Text) && !string.IsNullOrWhiteSpace(element.Name))
                    element.Text = element.Name;
                if (string.IsNullOrWhiteSpace(element.LongText))
                    element.LongText = element.Text ?? element.Name ?? "No description";
                if (string.IsNullOrWhiteSpace(element.Name))
                    element.Name = element.Text ?? "Unnamed Element";
            }
        }

        // Helper methods
        private static string GetElementValueCaseInsensitive(XElement parent, params string[] possibleNames)
        {
            foreach (var name in possibleNames)
            {
                var element = parent.Elements()
                    .FirstOrDefault(e => e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (element != null && !string.IsNullOrWhiteSpace(element.Value))
                {
                    return element.Value.Trim();
                }
            }
            return null;
        }

        private static decimal ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            // Remove common formatting characters
            value = value.Replace(" ", "")
                        .Replace("€", "")
                        .Replace("$", "")
                        .Replace("£", "");

            // Try parsing with different cultures
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result;

            // Try with current culture
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
                return result;

            // Try German format (comma as decimal separator)
            if (decimal.TryParse(value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                return result;

            return 0;
        }

        private static DateTime ParseDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return DateTime.Now;

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                return result;

            if (DateTime.TryParse(value, out result))
                return result;

            return DateTime.Now;
        }

        private static bool ContainsKeyFields(XElement element)
        {
            var keyFields = new[] { "name", "text", "description", "qty", "quantity", "price", "code", "id" };
            return element.Elements()
                .Any(e => keyFields.Any(k => e.Name.LocalName.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static bool LooksLikeCostElement(XElement element)
        {
            // Check if element has numeric fields that suggest cost data
            var hasNumericFields = element.Elements()
                .Any(e => IsNumericField(e.Name.LocalName) && decimal.TryParse(e.Value, out _));

            // Check if element has text fields
            var hasTextFields = element.Elements()
                .Any(e => !string.IsNullOrWhiteSpace(e.Value) && e.Value.Length > 3);

            return hasNumericFields && hasTextFields;
        }

        private static bool IsNumericField(string fieldName)
        {
            var numericIndicators = new[] { "qty", "quantity", "price", "cost", "total", "sum", "amount", "value" };
            return numericIndicators.Any(n => fieldName.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Backward compatibility method
        /// </summary>
        [Obsolete("Use ImportFromXmlWithValidation for better error handling")]
        public static List<CostElement> ImportFromXml(string filePath)
        {
            var result = ImportFromXmlWithValidation(filePath);

            if (!result.Success)
            {
                throw new Exception(string.Join("; ", result.Errors));
            }

            return result.Elements;
        }
    }
}