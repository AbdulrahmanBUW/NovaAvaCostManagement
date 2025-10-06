using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Manages project operations: save, load, validate
    /// </summary>
    public class ProjectManager
    {
        public List<CostElement> Elements { get; set; } = new List<CostElement>();
        public string ProjectFilePath { get; set; }
        public List<string> LogMessages { get; set; } = new List<string>();
        /// <summary>
        /// Import from AVA XML with schema tracking
        /// </summary>
        public void ImportAvaXmlWithSchemaTracking(string filePath)
        {
            try
            {
                // Store the original file path
                EnhancedValidator.OriginalXmlFilePath = filePath;

                // Load and analyze the XML schema
                CaptureXmlSchema(filePath);

                // Perform the actual import using existing method
                ImportAvaXml(filePath);

                AddLogMessage($"Imported with schema tracking from {filePath}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error importing AVA XML with schema tracking: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Capture XML schema structure for later comparison
        /// </summary>
        private void CaptureXmlSchema(string filePath)
        {
            try
            {
                var doc = System.Xml.Linq.XDocument.Load(filePath);
                var schema = new Dictionary<string, HashSet<string>>();

                // Find all element nodes
                var elements = doc.Descendants().Where(x =>
                    x.Name.LocalName.ToLower().Contains("element") ||
                    x.Name.LocalName.ToLower().Contains("item"));

                foreach (var element in elements)
                {
                    // Get all child element names
                    var fields = element.Elements()
                        .Select(e => e.Name.LocalName.ToLower())
                        .ToHashSet();

                    // Store field names for each element
                    string elementId = element.Element("id")?.Value ??
                                     element.Element("id2")?.Value ??
                                     Guid.NewGuid().ToString();

                    schema[elementId] = fields;
                }

                EnhancedValidator.OriginalXmlSchema = schema;
                AddLogMessage($"Captured schema with {schema.Count} elements");
            }
            catch (Exception ex)
            {
                AddLogMessage($"Warning: Could not capture XML schema: {ex.Message}");
            }
        }

        /// <summary>
        /// Enhanced validation using the new validator
        /// </summary>
        public ValidationResult ValidateForExportEnhanced()
        {
            var result = EnhancedValidator.ValidateForExport(Elements,
                compareWithOriginal: EnhancedValidator.OriginalXmlSchema.Any());

            // Generate and log detailed report
            var report = EnhancedValidator.GenerateValidationReport(result);
            AddLogMessage("Enhanced validation completed");

            return result;
        }

        /// <summary>
        /// Pre-export validation check
        /// </summary>
        public bool CanExport(out string reason)
        {
            var result = ValidateForExportEnhanced();

            if (result.HasErrors)
            {
                reason = $"Export blocked: {result.Errors.Count} critical error(s) must be fixed first.";
                return false;
            }

            if (result.HasWarnings)
            {
                reason = $"Export ready with {result.Warnings.Count} warning(s). Review recommended.";
                return true;
            }

            reason = "Export ready. All validations passed.";
            return true;
        }

        /// <summary>
        /// Export with automatic pre-validation
        /// </summary>
        public void ExportAvaXmlSafe(string filePath, bool useGaebFormat = false, bool forceExport = false)
        {
            // Always validate before export
            var validationResult = ValidateForExportEnhanced();

            if (validationResult.HasErrors && !forceExport)
            {
                throw new InvalidOperationException(
                    $"Cannot export: {validationResult.Errors.Count} critical error(s) found. " +
                    "Fix errors or use force export option.");
            }

            try
            {
                ExportAvaXml(filePath, useGaebFormat);
                AddLogMessage($"Successfully exported {Elements.Count} elements to {filePath}");

                if (validationResult.HasWarnings)
                {
                    AddLogMessage($"Note: Export completed with {validationResult.Warnings.Count} warning(s)");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error exporting AVA XML: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Compare current data with original imported data
        /// </summary>
        public ComparisonResult CompareWithOriginal()
        {
            var result = new ComparisonResult();

            if (!EnhancedValidator.OriginalXmlSchema.Any())
            {
                result.Message = "No original XML data available for comparison";
                return result;
            }

            // Compare element counts
            result.OriginalElementCount = EnhancedValidator.OriginalXmlSchema.Count;
            result.CurrentElementCount = Elements.Count;

            // Compare fields
            var originalFields = EnhancedValidator.OriginalXmlSchema
                .SelectMany(kvp => kvp.Value)
                .Distinct()
                .ToHashSet();

            var currentFields = new HashSet<string>();
            foreach (var element in Elements)
            {
                var elementFields = GetElementFieldNames(element);
                currentFields.UnionWith(elementFields);
            }

            result.MissingFields = originalFields.Except(currentFields).ToList();
            result.NewFields = currentFields.Except(originalFields).ToList();

            return result;
        }

        /// <summary>
        /// Get field names from an element that have non-default values
        /// </summary>
        private HashSet<string> GetElementFieldNames(CostElement element)
        {
            var fields = new HashSet<string>();
            var properties = typeof(CostElement).GetProperties();

            foreach (var prop in properties)
            {
                var value = prop.GetValue(element);
                if (value != null && !IsDefaultValueInternal(value))
                {
                    fields.Add(prop.Name.ToLower());
                }
            }

            return fields;
        }

        private bool IsDefaultValueInternal(object value)
        {
            if (value is string s)
                return string.IsNullOrWhiteSpace(s);
            if (value is decimal d)
                return d == 0;
            if (value is int i)
                return i == 0;
            if (value is bool b)
                return b == false;
            return false;
        }

        // ADD THIS NEW CLASS AT THE END OF THE FILE, OUTSIDE THE ProjectManager CLASS

        /// <summary>
        /// Result of comparing current data with original
        /// </summary>
        public class ComparisonResult
        {
            public string Message { get; set; } = "";
            public int OriginalElementCount { get; set; }
            public int CurrentElementCount { get; set; }
            public List<string> MissingFields { get; set; } = new List<string>();
            public List<string> NewFields { get; set; } = new List<string>();

            public bool HasDifferences => MissingFields.Any() || NewFields.Any() ||
                OriginalElementCount != CurrentElementCount;

            public string GetSummary()
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("═══════════════════════════════════════════════════════");
                sb.AppendLine("     COMPARISON WITH ORIGINAL XML");
                sb.AppendLine("═══════════════════════════════════════════════════════");
                sb.AppendLine();

                if (!string.IsNullOrEmpty(Message))
                {
                    sb.AppendLine(Message);
                    return sb.ToString();
                }

                sb.AppendLine($"Original Elements: {OriginalElementCount}");
                sb.AppendLine($"Current Elements:  {CurrentElementCount}");
                sb.AppendLine();

                if (MissingFields.Any())
                {
                    sb.AppendLine("Missing Fields (present in original, absent in current):");
                    sb.AppendLine("───────────────────────────────────────────────────────");
                    foreach (var field in MissingFields)
                    {
                        sb.AppendLine($"  • {field}");
                    }
                    sb.AppendLine();
                }

                if (NewFields.Any())
                {
                    sb.AppendLine("New Fields (absent in original, present in current):");
                    sb.AppendLine("───────────────────────────────────────────────────────");
                    foreach (var field in NewFields)
                    {
                        sb.AppendLine($"  • {field}");
                    }
                    sb.AppendLine();
                }

                if (!HasDifferences)
                {
                    sb.AppendLine("✓ No significant differences found");
                }

                return sb.ToString();
            }
        }
        /// <summary>
        /// Create new project with sample data
        /// </summary>
        public void CreateNewProject()
        {
            Elements.Clear();
            LogMessages.Clear();
            ProjectFilePath = null;
            AddLogMessage("New project created");
        }

        /// <summary>
        /// Save project to XML file
        /// </summary>
        public void SaveProject(string filePath)
        {
            try
            {
                var doc = new XDocument(
                    new XElement("NovaAvaProject",
                        new XAttribute("version", "1.0"),
                        new XAttribute("created", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")),
                        new XElement("Elements",
                            Elements.Select(e => SerializeElementToXml(e))
                        )
                    )
                );

                doc.Save(filePath);
                ProjectFilePath = filePath;
                AddLogMessage($"Project saved to {filePath}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving project: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Load project from XML file
        /// </summary>
        public void LoadProject(string filePath)
        {
            try
            {
                var doc = XDocument.Load(filePath);
                Elements.Clear();

                var elementsNode = doc.Root?.Element("Elements");
                if (elementsNode != null)
                {
                    foreach (var elementNode in elementsNode.Elements("Element"))
                    {
                        var element = DeserializeElementFromXml(elementNode);
                        if (element != null)
                        {
                            Elements.Add(element);
                        }
                    }
                }

                ProjectFilePath = filePath;
                AddLogMessage($"Project loaded from {filePath}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading project: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Import from AVA XML
        /// </summary>
        /// <summary>
        /// Import from AVA XML
        /// </summary>
        public void ImportAvaXml(string filePath)
        {
            try
            {
                var importedElements = XmlImporter.ImportFromXml(filePath);
                Elements.Clear();
                Elements.AddRange(importedElements);
                AddLogMessage($"Imported {importedElements.Count} elements from {filePath}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error importing AVA XML: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Export to AVA XML - DisplayNumber is NOT exported
        /// </summary>
        public void ExportAvaXml(string filePath, bool useGaebFormat = false)
        {
            try
            {
                XmlExporter.ExportToXml(Elements, filePath, useGaebFormat);
                AddLogMessage($"Exported {Elements.Count} elements to {filePath}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error exporting AVA XML: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validate all elements for export
        /// </summary>
        public ValidationResult ValidateForExport()
        {
            var result = new ValidationResult();

            foreach (var element in Elements)
            {
                var errors = element.Validate();
                foreach (var error in errors)
                {
                    if (IsErrorCritical(error))
                    {
                        result.Errors.Add($"Element {element.Id}: {error}");
                    }
                    else
                    {
                        result.Warnings.Add($"Element {element.Id}: {error}");
                    }
                }
            }

            AddLogMessage($"Validation completed: {result.Errors.Count} errors, {result.Warnings.Count} warnings");
            return result;
        }

        /// <summary>
        /// Check if validation error is critical
        /// </summary>
        private bool IsErrorCritical(string error)
        {
            var criticalErrors = new[] { "ID is required", "Name is required", "Text is required", "LongText is required" };
            return criticalErrors.Any(ce => error.Contains(ce));
        }

        /// <summary>
        /// Generate quick diagnostics
        /// </summary>
        public string GenerateQuickDiagnostics()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== NOVA AVA Project Diagnostics ===");
            sb.AppendLine($"Total Elements: {Elements.Count}");
            sb.AppendLine($"Elements with Properties: {Elements.Count(e => !string.IsNullOrEmpty(e.Properties))}");
            sb.AppendLine($"Elements with Text: {Elements.Count(e => !string.IsNullOrEmpty(e.Text))}");
            sb.AppendLine($"Elements with LongText: {Elements.Count(e => !string.IsNullOrEmpty(e.LongText))}");
            sb.AppendLine($"Max ID: {Elements.Max(e => int.TryParse(e.Id, out int id) ? id : 0)}");
            sb.AppendLine($"Total Quantity: {Elements.Sum(e => e.Qty):F2}");
            sb.AppendLine($"Total Value: {Elements.Sum(e => e.Sum):F2}");
            sb.AppendLine($"IFC Types: {string.Join(", ", Elements.Select(e => e.IfcType).Where(t => !string.IsNullOrEmpty(t)).Distinct())}");
            sb.AppendLine($"Log Messages: {LogMessages.Count}");

            return sb.ToString();
        }

        /// <summary>
        /// Add log message with timestamp
        /// </summary>
        public void AddLogMessage(string message)
        {
            LogMessages.Add($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
        }

        /// <summary>
        /// Serialize CostElement to XML
        /// </summary>
        private XElement SerializeElementToXml(CostElement element)
        {
            return new XElement("Element",
                new XElement("Version", element.Version),
                new XElement("Id", element.Id),
                new XElement("Title", element.Title),
                new XElement("Label", element.Label),
                new XElement("Criteria", element.Criteria),
                new XElement("Created", element.Created.ToString("yyyy-MM-ddTHH:mm:ss")),
                new XElement("Id2", element.Id2),
                new XElement("Type", element.Type),
                new XElement("Name", element.Name),
                new XElement("Description", element.Description),
                new XElement("Properties", element.Properties),
                new XElement("Text", element.Text),
                new XElement("LongText", element.LongText),
                new XElement("Qty", element.Qty),
                new XElement("Qu", element.Qu),
                new XElement("Up", element.Up),
                new XElement("Sum", element.Sum),
                new XElement("BimKey", element.BimKey),
                new XElement("Note", element.Note),
                new XElement("Color", element.Color),
                new XElement("IfcType", element.IfcType),
                new XElement("Material", element.Material),
                new XElement("Dimension", element.Dimension),
                new XElement("SegmentType", element.SegmentType),
                new XElement("AdditionalData",
                    element.AdditionalData.Select(kvp => new XElement(kvp.Key, kvp.Value))
                )
            );
        }

        /// <summary>
        /// Deserialize CostElement from XML
        /// </summary>
        private CostElement DeserializeElementFromXml(XElement node)
        {
            var element = new CostElement();

            element.Version = node.Element("Version")?.Value ?? "2";
            element.Id = node.Element("Id")?.Value ?? element.Id;
            element.Title = node.Element("Title")?.Value ?? "";
            element.Label = node.Element("Label")?.Value ?? "";
            element.Criteria = node.Element("Criteria")?.Value ?? "";
            if (DateTime.TryParse(node.Element("Created")?.Value, out DateTime created))
                element.Created = created;
            element.Id2 = node.Element("Id2")?.Value ?? element.Id2;
            element.Type = node.Element("Type")?.Value ?? "";
            element.Name = node.Element("Name")?.Value ?? "";
            element.Description = node.Element("Description")?.Value ?? "";
            element.Properties = node.Element("Properties")?.Value ?? "";
            element.Text = node.Element("Text")?.Value ?? "";
            element.LongText = node.Element("LongText")?.Value ?? "";
            if (decimal.TryParse(node.Element("Qty")?.Value, out decimal qty))
                element.Qty = qty;
            element.Qu = node.Element("Qu")?.Value ?? "";
            if (decimal.TryParse(node.Element("Up")?.Value, out decimal up))
                element.Up = up;
            if (decimal.TryParse(node.Element("Sum")?.Value, out decimal sum))
                element.Sum = sum;
            element.BimKey = node.Element("BimKey")?.Value ?? "";
            element.Note = node.Element("Note")?.Value ?? "";
            element.Color = node.Element("Color")?.Value ?? "";
            element.IfcType = node.Element("IfcType")?.Value ?? "";
            element.Material = node.Element("Material")?.Value ?? "";
            element.Dimension = node.Element("Dimension")?.Value ?? "";
            element.SegmentType = node.Element("SegmentType")?.Value ?? "";

            // Load additional data
            var additionalDataNode = node.Element("AdditionalData");
            if (additionalDataNode != null)
            {
                foreach (var child in additionalDataNode.Elements())
                {
                    element.AdditionalData[child.Name.LocalName] = child.Value;
                }
            }

            return element;
        }
    }

    /// <summary>
    /// Validation result container
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();

        public bool HasErrors => Errors.Count > 0;
        public bool HasWarnings => Warnings.Count > 0;
        public bool IsValid => !HasErrors;
    }
}