using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Enhanced validation with XML schema comparison and LongText syntax checking
    /// </summary>
    public class EnhancedValidator
    {
        // Store original XML schema for comparison
        public static Dictionary<string, HashSet<string>> OriginalXmlSchema { get; set; }
            = new Dictionary<string, HashSet<string>>();

        public static string OriginalXmlFilePath { get; set; }

        /// <summary>
        /// Comprehensive validation for export
        /// </summary>
        public static ValidationResult ValidateForExport(List<CostElement> elements,
            bool compareWithOriginal = true)
        {
            var result = new ValidationResult();

            // 1. Validate each element
            foreach (var element in elements)
            {
                ValidateElement(element, result);
            }

            // 2. Compare with original XML schema if available
            if (compareWithOriginal && OriginalXmlSchema.Any())
            {
                ValidateAgainstOriginalSchema(elements, result);
            }

            // 3. Validate overall data consistency
            ValidateDataConsistency(elements, result);

            return result;
        }

        /// <summary>
        /// Validate individual element with enhanced rules
        /// </summary>
        private static void ValidateElement(CostElement element, ValidationResult result)
        {
            string elementId = $"Element {element.Id} ({element.Id2})";

            // Critical fields - MUST be present
            if (string.IsNullOrWhiteSpace(element.Id))
                result.Errors.Add($"{elementId}: ID is required");

            if (string.IsNullOrWhiteSpace(element.Id2))
                result.Errors.Add($"{elementId}: Code (ID2) is required");

            if (string.IsNullOrWhiteSpace(element.Name))
                result.Errors.Add($"{elementId}: Name is required");

            if (string.IsNullOrWhiteSpace(element.Text))
                result.Errors.Add($"{elementId}: Text is required");

            // Text length validation
            if (element.Text != null && element.Text.Length > 255)
                result.Errors.Add($"{elementId}: Text exceeds maximum length of 255 characters (current: {element.Text.Length})");

            // LongText validation with syntax checking
            ValidateLongText(element, elementId, result);

            // Numeric validation
            if (element.Qty < 0)
                result.Errors.Add($"{elementId}: Quantity cannot be negative");

            if (element.Up < 0)
                result.Errors.Add($"{elementId}: Unit price cannot be negative");

            // Sum calculation validation
            decimal expectedSum = element.Qty * element.Up;
            if (Math.Abs(element.Sum - expectedSum) > 0.01m)
                result.Warnings.Add($"{elementId}: Sum mismatch. Expected {expectedSum:F2}, got {element.Sum:F2}");

            // Unit validation
            if (element.Qty > 0 && string.IsNullOrWhiteSpace(element.Qu))
                result.Warnings.Add($"{elementId}: Quantity specified but unit is missing");

            // GUID validation
            ValidateGuidFields(element, elementId, result);

            // Properties validation
            ValidateProperties(element, elementId, result);

            // Color validation
            if (!string.IsNullOrWhiteSpace(element.Color))
            {
                if (!IsValidColorFormat(element.Color))
                    result.Warnings.Add($"{elementId}: Color format may be invalid: {element.Color}");
            }

            // IFC data validation
            ValidateIfcData(element, elementId, result);
        }

        /// <summary>
        /// Enhanced LongText validation with syntax checking
        /// </summary>
        private static void ValidateLongText(CostElement element, string elementId, ValidationResult result)
        {
            // LongText is not always required, but when present must be valid
            if (string.IsNullOrWhiteSpace(element.LongText))
            {
                // Only require LongText in specific cases
                if (RequiresLongText(element))
                {
                    result.Errors.Add($"{elementId}: LongText is required for this element type");
                }
                return;
            }

            // Check length
            if (element.LongText.Length > 2000)
            {
                result.Errors.Add($"{elementId}: LongText exceeds maximum length of 2000 characters (current: {element.LongText.Length})");
            }

            // Check for proper syntax based on AVA NOVA requirements
            var syntaxIssues = CheckLongTextSyntax(element.LongText);
            foreach (var issue in syntaxIssues)
            {
                result.Warnings.Add($"{elementId}: LongText syntax issue - {issue}");
            }

            // Check for special characters that might cause XML issues
            if (ContainsInvalidXmlCharacters(element.LongText))
            {
                result.Errors.Add($"{elementId}: LongText contains invalid XML characters");
            }

            // Verify consistency with Text field
            if (element.LongText.IndexOf(element.Text, StringComparison.OrdinalIgnoreCase) < 0)
            {
                result.Warnings.Add($"{elementId}: LongText should typically contain the Text content");
            }
        }

        /// <summary>
        /// Check if element requires LongText based on type and context
        /// </summary>
        private static bool RequiresLongText(CostElement element)
        {
            if (!string.IsNullOrWhiteSpace(element.Properties))
                return true;

            if (!string.IsNullOrWhiteSpace(element.IfcType))
                return true;

            if (element.Up > 0 || element.Qty > 0)
                return true;

            return false;
        }

        /// <summary>
        /// Check LongText syntax for AVA NOVA compliance
        /// </summary>
        private static List<string> CheckLongTextSyntax(string longText)
        {
            var issues = new List<string>();

            // Check for unbalanced brackets/parentheses
            if (CountChar(longText, '(') != CountChar(longText, ')'))
                issues.Add("Unbalanced parentheses");

            if (CountChar(longText, '[') != CountChar(longText, ']'))
                issues.Add("Unbalanced square brackets");

            // Check for incomplete technical specifications
            if (Regex.IsMatch(longText, @"DN\s*$|DIN\s*$|EN\s*$", RegexOptions.IgnoreCase))
                issues.Add("Incomplete technical specification (DN/DIN/EN without number)");

            // Check for proper dimension format
            if (Regex.IsMatch(longText, @"\d+\s*x\s*(?!\d)|\d+\s*/\s*(?!\d)", RegexOptions.IgnoreCase))
                issues.Add("Incomplete dimension specification");

            // Check for trailing special characters
            if (Regex.IsMatch(longText, @"[,;\-]\s*$"))
                issues.Add("Trailing punctuation");

            // Check for multiple consecutive spaces
            if (longText.Contains("  "))
                issues.Add("Contains multiple consecutive spaces");

            // Check for proper unit formatting
            if (Regex.IsMatch(longText, @"\d+(?:mm|cm|m|kg|g)\w", RegexOptions.IgnoreCase))
                issues.Add("Unit appears to be concatenated with following text");

            return issues;
        }

        /// <summary>
        /// Validate against original XML schema
        /// </summary>
        private static void ValidateAgainstOriginalSchema(List<CostElement> elements, ValidationResult result)
        {
            if (!OriginalXmlSchema.Any())
            {
                result.Warnings.Add("No original XML schema available for comparison");
                return;
            }

            // Check for missing required fields based on original
            var requiredFields = OriginalXmlSchema
                .Where(kvp => kvp.Value.Count == OriginalXmlSchema.First().Value.Count)
                .Select(kvp => kvp.Key)
                .ToHashSet();

            foreach (var element in elements)
            {
                var elementFields = GetElementFields(element);
                var missingFields = requiredFields.Except(elementFields).ToList();

                if (missingFields.Any())
                {
                    result.Warnings.Add($"Element {element.Id2}: Missing fields from original schema: {string.Join(", ", missingFields)}");
                }
            }
        }

        /// <summary>
        /// Validate overall data consistency
        /// </summary>
        private static void ValidateDataConsistency(List<CostElement> elements, ValidationResult result)
        {
            // Check for duplicate IDs
            var duplicateIds = elements.GroupBy(e => e.Id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var id in duplicateIds)
            {
                result.Errors.Add($"Duplicate ID found: {id}");
            }

            // Check for duplicate ID2 (codes)
            var duplicateCodes = elements.GroupBy(e => e.Id2)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var code in duplicateCodes)
            {
                result.Warnings.Add($"Duplicate code found: {code}");
            }

            // Validate parent-child relationships
            var allIds = elements.Select(e => e.Id).ToHashSet();
            foreach (var element in elements.Where(e => !string.IsNullOrWhiteSpace(e.Parent)))
            {
                if (!allIds.Contains(element.Parent))
                {
                    result.Warnings.Add($"Element {element.Id2}: Parent ID '{element.Parent}' not found");
                }
            }

            // Check for orphaned children references
            foreach (var element in elements.Where(e => !string.IsNullOrWhiteSpace(e.Children)))
            {
                var childIds = element.Children.Split(',').Select(s => s.Trim());
                foreach (var childId in childIds)
                {
                    if (!allIds.Contains(childId))
                    {
                        result.Warnings.Add($"Element {element.Id2}: Child ID '{childId}' not found");
                    }
                }
            }

            // Validate total calculations
            decimal totalValue = elements.Sum(e => e.Sum);
            if (totalValue == 0 && elements.Any(e => e.Qty > 0))
            {
                result.Warnings.Add("Total project value is zero despite having quantities");
            }
        }

        /// <summary>
        /// Validate GUID format fields
        /// </summary>
        private static void ValidateGuidFields(CostElement element, string elementId, ValidationResult result)
        {
            if (!string.IsNullOrWhiteSpace(element.Id2) && !IsValidGuidOrCode(element.Id2))
                result.Warnings.Add($"{elementId}: ID2 format may be invalid");

            if (!string.IsNullOrWhiteSpace(element.Id5) && !IsValidGuid(element.Id5))
                result.Warnings.Add($"{elementId}: ID5 should be a valid GUID");

            if (!string.IsNullOrWhiteSpace(element.Id6) && !IsValidGuid(element.Id6))
                result.Warnings.Add($"{elementId}: ID6 should be a valid GUID");

            if (!string.IsNullOrWhiteSpace(element.Ident) && !IsValidGuid(element.Ident))
                result.Warnings.Add($"{elementId}: Ident should be a valid GUID");
        }

        /// <summary>
        /// Validate properties field
        /// </summary>
        private static void ValidateProperties(CostElement element, string elementId, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(element.Properties))
            {
                if (!string.IsNullOrWhiteSpace(element.IfcType))
                {
                    result.Warnings.Add($"{elementId}: IFC type specified but properties are empty");
                }
                return;
            }

            // Validate PHP serialization format
            if (!element.Properties.StartsWith("a:") || !element.Properties.Contains("{") || !element.Properties.EndsWith("}"))
            {
                result.Errors.Add($"{elementId}: Properties format is invalid (not proper PHP serialization)");
            }

            // Check for balanced braces
            if (CountChar(element.Properties, '{') != CountChar(element.Properties, '}'))
            {
                result.Errors.Add($"{elementId}: Properties have unbalanced braces");
            }

            // Validate array count matches content
            var match = Regex.Match(element.Properties, @"^a:(\d+):");
            if (match.Success)
            {
                int declaredCount = int.Parse(match.Groups[1].Value);
                int actualCount = Regex.Matches(element.Properties, @"s:\d+:""[^""]*"";s:\d+:""[^""]*"";").Count;

                if (declaredCount != actualCount)
                {
                    result.Errors.Add($"{elementId}: Properties array count mismatch (declared: {declaredCount}, actual: {actualCount})");
                }
            }
        }

        /// <summary>
        /// Validate IFC-specific data
        /// </summary>
        private static void ValidateIfcData(CostElement element, string elementId, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(element.IfcType))
                return;

            // Validate IFC type format
            if (!element.IfcType.StartsWith("IFC", StringComparison.OrdinalIgnoreCase))
            {
                result.Warnings.Add($"{elementId}: IFC type should start with 'IFC'");
            }

            // Check for required IFC data
            if (string.IsNullOrWhiteSpace(element.Material))
                result.Warnings.Add($"{elementId}: IFC type specified but material is missing");

            if (string.IsNullOrWhiteSpace(element.Dimension))
                result.Warnings.Add($"{elementId}: IFC type specified but dimension is missing");

            // Validate dimension format
            if (!string.IsNullOrWhiteSpace(element.Dimension))
            {
                if (!Regex.IsMatch(element.Dimension, @"^(DN\d+|IPE\d+|\d+mm|\d+x\d+)$", RegexOptions.IgnoreCase))
                {
                    result.Warnings.Add($"{elementId}: Dimension format may be non-standard: {element.Dimension}");
                }
            }
        }

        // Helper methods
        private static int CountChar(string text, char c)
        {
            return text.Count(ch => ch == c);
        }

        private static bool IsValidGuid(string value)
        {
            return Guid.TryParse(value, out _);
        }

        private static bool IsValidGuidOrCode(string value)
        {
            return Guid.TryParse(value, out _) ||
                   Regex.IsMatch(value, @"^[A-Z0-9_\-]+$", RegexOptions.IgnoreCase);
        }

        private static bool IsValidColorFormat(string color)
        {
            return Regex.IsMatch(color, @"^#[0-9A-Fa-f]{6}$");
        }

        private static bool ContainsInvalidXmlCharacters(string text)
        {
            return text.Any(c => c < 0x20 && c != 0x09 && c != 0x0A && c != 0x0D);
        }

        private static HashSet<string> GetElementFields(CostElement element)
        {
            var fields = new HashSet<string>();
            var properties = typeof(CostElement).GetProperties();

            foreach (var prop in properties)
            {
                var value = prop.GetValue(element);
                if (value != null && !IsDefaultValue(value))
                {
                    fields.Add(prop.Name.ToLower());
                }
            }

            return fields;
        }

        private static bool IsDefaultValue(object value)
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

        /// <summary>
        /// Generate detailed validation report
        /// </summary>
        public static string GenerateValidationReport(ValidationResult result)
        {
            var report = new System.Text.StringBuilder();

            report.AppendLine("═══════════════════════════════════════════════════════");
            report.AppendLine("     NOVA AVA EXPORT VALIDATION REPORT");
            report.AppendLine("═══════════════════════════════════════════════════════");
            report.AppendLine();

            if (result.IsValid)
            {
                report.AppendLine("✓ VALIDATION PASSED");
                report.AppendLine();
                report.AppendLine("All critical validations passed. The data is ready for export.");
            }
            else
            {
                report.AppendLine("✗ VALIDATION FAILED");
                report.AppendLine();
                report.AppendLine($"Found {result.Errors.Count} critical error(s) that MUST be fixed.");
            }

            if (result.HasWarnings)
            {
                report.AppendLine($"Found {result.Warnings.Count} warning(s) that should be reviewed.");
            }

            report.AppendLine();
            report.AppendLine("═══════════════════════════════════════════════════════");

            if (result.HasErrors)
            {
                report.AppendLine();
                report.AppendLine("CRITICAL ERRORS (Must Fix):");
                report.AppendLine("───────────────────────────────────────────────────────");
                for (int i = 0; i < result.Errors.Count; i++)
                {
                    report.AppendLine($"{i + 1}. {result.Errors[i]}");
                }
            }

            if (result.HasWarnings)
            {
                report.AppendLine();
                report.AppendLine("WARNINGS (Should Review):");
                report.AppendLine("───────────────────────────────────────────────────────");
                for (int i = 0; i < result.Warnings.Count; i++)
                {
                    report.AppendLine($"{i + 1}. {result.Warnings[i]}");
                }
            }

            report.AppendLine();
            report.AppendLine("═══════════════════════════════════════════════════════");
            report.AppendLine($"Report generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            return report.ToString();
        }
    }
}