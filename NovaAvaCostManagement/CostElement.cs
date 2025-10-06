using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Represents a complete NOVA AVA cost element with all 69+ columns
    /// </summary>
    public class CostElement
    {
        // Core identification fields
        public string Version { get; set; } = "2";
        public string Id { get; set; }
        public string Title { get; set; } = "";
        public string Label { get; set; } = "";
        public string Criteria { get; set; } = "";
        public DateTime Created { get; set; } = DateTime.Now;
        public string Id2 { get; set; }
        public string Type { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Properties { get; set; } = "";
        public string Children { get; set; } = "";
        public string Openings { get; set; } = "";
        public DateTime Created3 { get; set; } = DateTime.Now;
        public string Label4 { get; set; } = "";
        public string Id5 { get; set; }
        public string Parent { get; set; } = "";
        public int Order { get; set; } = 0;
        public string Ident { get; set; }
        public string BimKey { get; set; } = "";
        public int CalculationId { get; set; } = 0;  // The 'id' from cecalculation
        public int ParentCalcId { get; set; } = 0;   // Parent calculation
        public string CatalogReference { get; set; } = "";
        public int ElementType { get; set; } = 1;    // The 'type' from costelement
        public int FilterValue { get; set; } = 0;
        public int ChildrenCount { get; set; } = 0;
        public int OpeningsCount { get; set; } = 0;

        // Hierarchy fields
        public int ElementId { get; set; } = 0;
        public bool IsParentNode { get; set; } = false;
        public int TreeLevel { get; set; } = 0;
        // Text fields
        public string Text { get; set; } = "";
        public string LongText { get; set; } = "";
        public string TextSys { get; set; } = "";
        public string TextKey { get; set; } = "";
        public string StlNo { get; set; } = "";
        public string OutlineTextFree { get; set; } = "";

        // Quantity and pricing
        public decimal Qty { get; set; } = 0;
        public decimal QtyResult { get; set; } = 0;
        public string Qu { get; set; } = "";
        public decimal Up { get; set; } = 0;
        public decimal UpResult { get; set; } = 0;
        public decimal UpBkdn { get; set; } = 0;
        public decimal UpComp1 { get; set; } = 0;
        public decimal UpComp2 { get; set; } = 0;
        public decimal UpComp3 { get; set; } = 0;
        public decimal UpComp4 { get; set; } = 0;
        public decimal UpComp5 { get; set; } = 0;
        public decimal UpComp6 { get; set; } = 0;
        public string TimeQu { get; set; } = "";
        public decimal It { get; set; } = 0;
        public decimal Vat { get; set; } = 0;
        public decimal VatValue { get; set; } = 0;
        public decimal Tax { get; set; } = 0;
        public decimal TaxValue { get; set; } = 0;
        public decimal ItGross { get; set; } = 0;
        public decimal Sum { get; set; } = 0;

        // VOB fields
        public string Vob { get; set; } = "";
        public string VobFormula { get; set; } = "";
        public string VobCondition { get; set; } = "";
        public string VobType { get; set; } = "";
        public decimal VobFactor { get; set; } = 0;

        // Additional fields
        public string On { get; set; } = "";
        public decimal PercTotal { get; set; } = 0;
        public bool Marked { get; set; } = false;
        public decimal PercMarked { get; set; } = 0;
        public string ProcUnit { get; set; } = "";
        public string Color { get; set; } = "";
        public string Note { get; set; } = "";
        public string Additional { get; set; } = "";
        public string Id6 { get; set; }
        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Data { get; set; } = "";
        public string CatalogName { get; set; } = "";
        public string CatalogType { get; set; } = "";
        public string Name7 { get; set; } = "";
        public string Number { get; set; } = "";
        public string Reference { get; set; } = "";
        public string Filter { get; set; } = "";

        // IFC-specific fields for properties generation
        public string IfcType { get; set; } = "";
        public string Material { get; set; } = "";
        public string Dimension { get; set; } = "";
        public string SegmentType { get; set; } = "";

        // Additional data for unknown fields
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Constructor that initializes auto-generated fields WITHOUT properties
        /// </summary>
        public CostElement()
        {
            // Auto-generate ID as default, but user can override
            Id = GenerateId();
            Id2 = Guid.NewGuid().ToString();
            Id5 = Guid.NewGuid().ToString();
            Id6 = Guid.NewGuid().ToString();
            Ident = Guid.NewGuid().ToString();
            Created = DateTime.Now;
            Created3 = DateTime.Now;

            // Properties and Criteria remain empty until explicitly set
            Properties = "";
            Criteria = "";
        }

        /// <summary>
        /// Generate sequential ID
        /// </summary>
        private static int _lastId = 0;
        private string GenerateId()
        {
            return (++_lastId).ToString();
        }

        /// <summary>
        /// Calculate computed fields WITHOUT auto-generating properties
        /// </summary>
        public void CalculateFields()
        {
            QtyResult = Qty;
            UpResult = Up;
            Sum = Qty * Up;
            ItGross = Sum + (Sum * Vat / 100) + (Sum * Tax / 100);

            // REMOVED: Auto-properties generation
            // Properties will only be generated when explicitly called via GenerateProperties()
        }

        /// <summary>
        /// Generate PHP-serialized properties string - ONLY when explicitly called
        /// </summary>
        public void GenerateProperties()
        {
            if (!string.IsNullOrEmpty(IfcType))
            {
                Properties = PropertiesSerializer.SerializeProperties(IfcType, Material, Dimension, SegmentType);
            }
            // If no IFC type, leave Properties empty - no auto-generation
        }

        /// <summary>
        /// Enhanced validation with LongText syntax checking
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();

            // Critical fields
            if (string.IsNullOrWhiteSpace(Id))
                errors.Add("ID is required");

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Name is required");

            if (string.IsNullOrWhiteSpace(Text))
                errors.Add("Text is required");

            // LongText validation - not always required but must be valid when present
            if (!string.IsNullOrWhiteSpace(LongText))
            {
                if (LongText.Length > 2000)
                    errors.Add($"LongText exceeds maximum length of 2000 characters (current: {LongText.Length})");

                // Check for XML-invalid characters
                if (ContainsInvalidXmlCharacters(LongText))
                    errors.Add("LongText contains invalid XML characters");

                // Syntax validation
                var syntaxErrors = ValidateLongTextSyntax(LongText);
                errors.AddRange(syntaxErrors);
            }
            else if (RequiresLongText())
            {
                errors.Add("LongText is required for this element type");
            }

            // Text length validation
            if (!string.IsNullOrWhiteSpace(Text) && Text.Length > 255)
                errors.Add($"Text exceeds maximum length of 255 characters (current: {Text.Length})");

            // Numeric validation
            if (Qty < 0)
                errors.Add("Quantity cannot be negative");

            if (Up < 0)
                errors.Add("Unit price cannot be negative");

            // Calculated fields validation
            decimal expectedSum = Qty * Up;
            if (Math.Abs(Sum - expectedSum) > 0.01m)
                errors.Add($"Sum calculation error. Expected {expectedSum:F2}, got {Sum:F2}");

            // Properties validation when IFC type is specified
            if (!string.IsNullOrWhiteSpace(IfcType))
            {
                if (string.IsNullOrWhiteSpace(Properties))
                    errors.Add("Properties are required when IFC type is specified");
                else
                {
                    var propErrors = ValidatePropertiesFormat(Properties);
                    errors.AddRange(propErrors);
                }
            }

            return errors;
        }

        /// <summary>
        /// Check if this element requires LongText
        /// </summary>
        private bool RequiresLongText()
        {
            // LongText required for elements with:
            // - IFC type specified
            // - Properties defined
            // - Pricing information (Up > 0 or Qty > 0)
            return !string.IsNullOrWhiteSpace(IfcType) ||
                   !string.IsNullOrWhiteSpace(Properties) ||
                   Up > 0 ||
                   Qty > 0;
        }

        /// <summary>
        /// Validate LongText syntax for AVA NOVA compliance
        /// </summary>
        private List<string> ValidateLongTextSyntax(string longText)
        {
            var errors = new List<string>();

            // Check balanced brackets
            if (CountChar(longText, '(') != CountChar(longText, ')'))
                errors.Add("LongText has unbalanced parentheses");

            if (CountChar(longText, '[') != CountChar(longText, ']'))
                errors.Add("LongText has unbalanced square brackets");

            // Check for incomplete technical specifications
            if (System.Text.RegularExpressions.Regex.IsMatch(longText, @"DN\s*$|DIN\s*$|EN\s*$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                errors.Add("LongText contains incomplete technical specification (DN/DIN/EN without number)");

            // Check for trailing special characters that might cause parsing issues
            if (System.Text.RegularExpressions.Regex.IsMatch(longText, @"[,;\-]\s*$"))
                errors.Add("LongText has trailing punctuation that may cause issues");

            // Check for multiple consecutive spaces
            if (longText.Contains("  "))
                errors.Add("LongText contains multiple consecutive spaces");

            // Check consistency with Text field - FIXED for .NET Framework 4.8
            if (longText.IndexOf(Text, StringComparison.OrdinalIgnoreCase) < 0)
                errors.Add("LongText should typically contain the Text content");

            return errors;
        }

        /// <summary>
        /// Validate properties field format
        /// </summary>
        private List<string> ValidatePropertiesFormat(string properties)
        {
            var errors = new List<string>();

            if (!properties.StartsWith("a:") || !properties.Contains("{") || !properties.EndsWith("}"))
                errors.Add("Properties format is invalid (not proper PHP serialization)");

            if (CountChar(properties, '{') != CountChar(properties, '}'))
                errors.Add("Properties have unbalanced braces");

            // Validate array count
            var match = System.Text.RegularExpressions.Regex.Match(properties, @"^a:(\d+):");
            if (match.Success)
            {
                int declaredCount = int.Parse(match.Groups[1].Value);
                int actualCount = System.Text.RegularExpressions.Regex.Matches(
                    properties, @"s:\d+:""[^""]*"";s:\d+:""[^""]*"";").Count;

                if (declaredCount != actualCount)
                    errors.Add($"Properties array count mismatch (declared: {declaredCount}, actual: {actualCount})");
            }

            return errors;
        }

        /// <summary>
        /// Check for invalid XML characters
        /// </summary>
        private bool ContainsInvalidXmlCharacters(string text)
        {
            return text.Any(c => c < 0x20 && c != 0x09 && c != 0x0A && c != 0x0D);
        }

        /// <summary>
        /// Count occurrences of a character
        /// </summary>
        private int CountChar(string text, char c)
        {
            return text.Count(ch => ch == c);
        }
    }
}