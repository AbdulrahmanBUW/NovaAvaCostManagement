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
        /// Create new project with sample data
        /// </summary>
        public void CreateNewProject()
        {
            Elements.Clear();
            LogMessages.Clear();
            ProjectFilePath = null;

            // Add sample element
            var sampleElement = new CostElement
            {
                Id2 = "CAST_Pipe_DIN10216-2_DN125",
                Name = "C-Stahl Rohr DIN EN 10216-2 P235HTC1 DN125",
                Type = "Pipe",
                Text = "C-Stahl Rohr DN125",
                LongText = "C-Stahl Rohr DIN EN 10216-2 P235HTC1 DN125",
                Qty = 37.09m,
                Qu = "m",
                Up = 0.11m,
                BimKey = "PIPE_001",
                Description = "Steel pipe for construction",
                Label = "DN125 Pipe",
                Note = "Sample pipe element",
                IfcType = "IFCPIPESEGMENT",
                Material = "P235HTC1",
                Dimension = "DN125",
                SegmentType = "DX_CarbonSteel_1.0345 - DIN EN 10216-2",
                Color = "#3498DB"
            };

            sampleElement.GenerateProperties();
            sampleElement.CalculateFields();
            Elements.Add(sampleElement);

            AddLogMessage("New project created with sample data");
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
        public void ImportAvaXml(string filePath)
        {
            try
            {
                var importedElements = XmlImporter.ImportFromXml(filePath);
                Elements.AddRange(importedElements);
                AddLogMessage($"Imported {importedElements.Count} elements from AVA XML");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error importing AVA XML: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Export to AVA XML
        /// </summary>
        public void ExportAvaXml(string filePath, bool useGaebFormat = false)
        {
            try
            {
                XmlExporter.ExportToXml(Elements, filePath, useGaebFormat);
                AddLogMessage($"Exported {Elements.Count} elements to AVA XML");
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