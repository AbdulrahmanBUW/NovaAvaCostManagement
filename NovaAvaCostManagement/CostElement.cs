using System;
using System.Collections.Generic;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Cost element - pure data storage with display-only hierarchy
    /// </summary>
    public class CostElement
    {
        // DISPLAY-ONLY - NOT exported to XML
        public string DisplayNumber { get; set; } = "";  // e.g., "1", "1.1", "1.2", "2", "2.1"

        // IDs from XML - NEVER auto-generate
        public string Version { get; set; } = "";
        public string Id { get; set; } = "";  // From costelement
        public int CalculationId { get; set; } = 0;  // From cecalculation id
        public string Id2 { get; set; } = "";  // From ident - show as Code
        public string Ident { get; set; } = "";
        public string Id5 { get; set; } = "";
        public string Id6 { get; set; } = "";

        // Hierarchy - from XML
        public int ParentCalcId { get; set; } = 0;  // 0 = parent, >0 = child
        public int Order { get; set; } = 0;
        public bool IsParentNode { get; set; } = false;
        public int TreeLevel { get; set; } = 0;
        public int ElementId { get; set; } = 0;
        public int ElementType { get; set; } = 1;

        // Basic info
        public string Title { get; set; } = "";
        public string Label { get; set; } = "";
        public string Type { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string BimKey { get; set; } = "";

        // Text fields
        public string Text { get; set; } = "";
        public string LongText { get; set; } = "";
        public string TextSys { get; set; } = "";
        public string TextKey { get; set; } = "";
        public string StlNo { get; set; } = "";
        public string OutlineTextFree { get; set; } = "";

        // Quantities and pricing
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

        // Properties and criteria
        public string Properties { get; set; } = "";
        public string Criteria { get; set; } = "";

        // Relationships
        public string Parent { get; set; } = "";
        public string Children { get; set; } = "";
        public string Openings { get; set; } = "";
        public int ChildrenCount { get; set; } = 0;
        public int OpeningsCount { get; set; } = 0;

        // Catalog references
        public string CatalogReference { get; set; } = "";
        public string CatalogName { get; set; } = "";
        public string CatalogType { get; set; } = "";
        public int FilterValue { get; set; } = 0;

        // File references
        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Data { get; set; } = "";

        // IFC fields
        public string IfcType { get; set; } = "";
        public string Material { get; set; } = "";
        public string Dimension { get; set; } = "";
        public string SegmentType { get; set; } = "";

        // Dates
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime Created3 { get; set; } = DateTime.Now;

        // Extra fields
        public string Label4 { get; set; } = "";
        public string Name7 { get; set; } = "";
        public string Number { get; set; } = "";
        public string Reference { get; set; } = "";
        public string Filter { get; set; } = "";

        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Constructor - NO auto-generation
        /// </summary>
        public CostElement()
        {
            // Empty - all data comes from XML
        }

        /// <summary>
        /// Calculate Sum when user edits Qty or Up
        /// </summary>
        public void RecalculateSum()
        {
            Sum = Qty * Up;
        }

        /// <summary>
        /// Basic validation
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Id))
                errors.Add("Element ID is required");

            if (string.IsNullOrWhiteSpace(Id2))
                errors.Add("Code (ident) is required");

            return errors;
        }
    }
}