using System;
using System.Collections.Generic;
using System.Globalization;

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
        /// Constructor that initializes auto-generated fields
        /// </summary>
        public CostElement()
        {
            Id = GenerateId();
            Id2 = Guid.NewGuid().ToString();
            Id5 = Guid.NewGuid().ToString();
            Id6 = Guid.NewGuid().ToString();
            Ident = Guid.NewGuid().ToString();
            Created = DateTime.Now;
            Created3 = DateTime.Now;
            Criteria = GenerateDefaultCriteria();
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
        /// Generate default criteria string matching VBA macro format
        /// </summary>
        private string GenerateDefaultCriteria()
        {
            return "a:2:{s:5:\"color\";s:7:\"#3498DB\";s:10:\"background\";s:7:\"#F1C40F\";}";
        }

        /// <summary>
        /// Calculate computed fields
        /// </summary>
        public void CalculateFields()
        {
            QtyResult = Qty;
            UpResult = Up;
            Sum = Qty * Up;
            ItGross = Sum + (Sum * Vat / 100) + (Sum * Tax / 100);
        }

        /// <summary>
        /// Generate PHP-serialized properties string
        /// </summary>
        public void GenerateProperties()
        {
            if (!string.IsNullOrEmpty(IfcType))
            {
                Properties = PropertiesSerializer.SerializeProperties(IfcType, Material, Dimension, SegmentType);
            }
        }

        /// <summary>
        /// Validate the cost element
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Id))
                errors.Add("ID is required");

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Name is required");

            if (string.IsNullOrWhiteSpace(Text))
                errors.Add("Text is required");

            if (string.IsNullOrWhiteSpace(LongText))
                errors.Add("LongText is required");

            if (Text.Length > 255)
                errors.Add("Text must be 255 characters or less");

            if (LongText.Length > 2000)
                errors.Add("LongText must be 2000 characters or less");

            if (Qty < 0)
                errors.Add("Quantity must be non-negative");

            if (Up < 0)
                errors.Add("Unit price must be non-negative");

            return errors;
        }
    }
}