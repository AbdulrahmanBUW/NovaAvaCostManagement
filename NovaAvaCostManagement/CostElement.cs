using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Represents a complete NOVA AVA cost element with all 69+ columns
    /// Thread-safe implementation with improved validation
    /// </summary>
    public class CostElement : ICloneable, IEquatable<CostElement>
    {
        // Thread-safe ID generation
        private static int _idCounter = 0;
        private static readonly object _idLock = new object();

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

        // IFC-specific fields
        public string IfcType { get; set; } = "";
        public string Material { get; set; } = "";
        public string Dimension { get; set; } = "";
        public string SegmentType { get; set; } = "";

        // Additional data for flexibility
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();

        // Computed property for validation state
        public bool IsValid => Validate().Count == 0;

        /// <summary>
        /// Constructor with thread-safe ID generation
        /// </summary>
        public CostElement()
        {
            Id = GenerateThreadSafeId();
            Id2 = GenerateGuid();
            Id5 = GenerateGuid();
            Id6 = GenerateGuid();
            Ident = GenerateGuid();
            Created = DateTime.Now;
            Created3 = DateTime.Now;
            Criteria = GenerateDefaultCriteria();
        }

        /// <summary>
        /// Generate thread-safe sequential ID
        /// </summary>
        private string GenerateThreadSafeId()
        {
            lock (_idLock)
            {
                return (++_idCounter).ToString();
            }
        }

        /// <summary>
        /// Generate GUID for unique identifiers
        /// </summary>
        private string GenerateGuid()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper();
        }

        /// <summary>
        /// Generate default criteria string matching VBA macro format
        /// </summary>
        private string GenerateDefaultCriteria()
        {
            // PHP serialized array format
            return "a:2:{s:5:\"color\";s:7:\"#3498DB\";s:10:\"background\";s:7:\"#F1C40F\";}";
        }

        /// <summary>
        /// Calculate computed fields with validation
        /// </summary>
        public void CalculateFields()
        {
            // Ensure non-negative values
            Qty = Math.Max(0, Qty);
            Up = Math.Max(0, Up);
            Vat = Math.Max(0, Math.Min(100, Vat)); // VAT between 0-100%
            Tax = Math.Max(0, Math.Min(100, Tax)); // Tax between 0-100%

            // Calculate results
            QtyResult = Qty;
            UpResult = Up;
            Sum = Math.Round(Qty * Up, 2);

            // Calculate VAT and Tax values
            VatValue = Math.Round(Sum * Vat / 100, 2);
            TaxValue = Math.Round(Sum * Tax / 100, 2);

            // Calculate gross total
            ItGross = Math.Round(Sum + VatValue + TaxValue, 2);
            It = ItGross; // Total item value
        }

        /// <summary>
        /// Generate PHP-serialized properties string with error handling
        /// </summary>
        public void GenerateProperties()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(IfcType))
                {
                    Properties = PropertiesSerializer.SerializeProperties(
                        IfcType?.Trim() ?? "",
                        Material?.Trim() ?? "",
                        Dimension?.Trim() ?? "",
                        SegmentType?.Trim() ?? ""
                    );
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail
                Properties = "";
                AdditionalData["PropertyGenerationError"] = ex.Message;
            }
        }

        /// <summary>
        /// Comprehensive validation with categorized errors
        /// </summary>
        public List<ValidationError> Validate()
        {
            var errors = new List<ValidationError>();

            // Required fields
            if (string.IsNullOrWhiteSpace(Id))
                errors.Add(new ValidationError(ValidationSeverity.Critical, "ID", "ID is required"));

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add(new ValidationError(ValidationSeverity.Critical, "Name", "Name is required"));

            if (string.IsNullOrWhiteSpace(Text))
                errors.Add(new ValidationError(ValidationSeverity.Critical, "Text", "Text is required"));

            if (string.IsNullOrWhiteSpace(LongText))
                errors.Add(new ValidationError(ValidationSeverity.Critical, "LongText", "LongText is required"));

            // Length constraints
            if (Text?.Length > 255)
                errors.Add(new ValidationError(ValidationSeverity.Error, "Text", "Text must be 255 characters or less"));

            if (LongText?.Length > 2000)
                errors.Add(new ValidationError(ValidationSeverity.Error, "LongText", "LongText must be 2000 characters or less"));

            // Numeric validations
            if (Qty < 0)
                errors.Add(new ValidationError(ValidationSeverity.Warning, "Qty", "Quantity should be non-negative"));

            if (Up < 0)
                errors.Add(new ValidationError(ValidationSeverity.Warning, "Up", "Unit price should be non-negative"));

            if (Vat < 0 || Vat > 100)
                errors.Add(new ValidationError(ValidationSeverity.Warning, "Vat", "VAT should be between 0 and 100"));

            if (Tax < 0 || Tax > 100)
                errors.Add(new ValidationError(ValidationSeverity.Warning, "Tax", "Tax should be between 0 and 100"));

            // Format validations
            if (!string.IsNullOrWhiteSpace(Color) && !IsValidColorFormat(Color))
                errors.Add(new ValidationError(ValidationSeverity.Info, "Color", "Color should be in hex format (e.g., #RRGGBB)"));

            // IFC validations
            if (!string.IsNullOrWhiteSpace(IfcType) && !IsValidIfcType(IfcType))
                errors.Add(new ValidationError(ValidationSeverity.Info, "IfcType", "Unknown IFC type"));

            return errors;
        }

        /// <summary>
        /// Validate color format
        /// </summary>
        private bool IsValidColorFormat(string color)
        {
            if (string.IsNullOrWhiteSpace(color)) return true;

            // Allow common formats: #RGB, #RRGGBB, rgb(r,g,b)
            var trimmed = color.Trim();
            if (trimmed.StartsWith("#"))
            {
                var hex = trimmed.Substring(1);
                return (hex.Length == 3 || hex.Length == 6) &&
                       System.Text.RegularExpressions.Regex.IsMatch(hex, "^[0-9A-Fa-f]+$");
            }

            return trimmed.StartsWith("rgb(") || trimmed.StartsWith("rgba(");
        }

        /// <summary>
        /// Validate IFC type against known types
        /// </summary>
        private bool IsValidIfcType(string ifcType)
        {
            var validTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "IFCPIPESEGMENT", "IFCWALL", "IFCBEAM", "IFCSLAB",
                "IFCDOOR", "IFCWINDOW", "IFCCOLUMN", "IFCROOF",
                "IFCSTAIR", "IFCRAILING", "IFCFOOTING", "IFCPLATE"
            };

            return validTypes.Contains(ifcType?.ToUpper() ?? "");
        }

        /// <summary>
        /// Deep clone implementation
        /// </summary>
        public object Clone()
        {
            var clone = (CostElement)this.MemberwiseClone();
            clone.AdditionalData = new Dictionary<string, object>(this.AdditionalData);
            return clone;
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        public bool Equals(CostElement other)
        {
            if (other == null) return false;
            return this.Id == other.Id && this.Id2 == other.Id2;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CostElement);
        }

        public override int GetHashCode()
        {
            return (Id?.GetHashCode() ?? 0) ^ (Id2?.GetHashCode() ?? 0);
        }

        /// <summary>
        /// String representation for debugging
        /// </summary>
        public override string ToString()
        {
            return $"CostElement[{Id}]: {Name} - {Text} (Qty: {Qty}, Price: {Up}, Total: {Sum})";
        }
    }

    /// <summary>
    /// Validation error with severity levels
    /// </summary>
    public class ValidationError
    {
        public ValidationSeverity Severity { get; set; }
        public string Field { get; set; }
        public string Message { get; set; }

        public ValidationError(ValidationSeverity severity, string field, string message)
        {
            Severity = severity;
            Field = field;
            Message = message;
        }

        public override string ToString()
        {
            return $"{Severity}: {Field} - {Message}";
        }
    }

    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}