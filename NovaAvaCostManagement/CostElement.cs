using System;
using System.Collections.Generic;
using System.Linq;

namespace NovaAvaCostManagement
{
    public class CatalogAssignment
    {
        public string CatalogName { get; set; } = "";
        public string CatalogType { get; set; } = "";
        public string Name { get; set; } = "";
        public string Number { get; set; } = "";
        public string Reference { get; set; } = "";

        public override string ToString()
        {
            return $"{Number} - {Name}";
        }
    }

    public class CostElement
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Children { get; set; } = "";
        public string Properties { get; set; } = "";

        public string SpecFilter { get; set; } = "";
        public string SpecName { get; set; } = "";
        public string SpecSize { get; set; } = "";
        public string SpecType { get; set; } = "";
        public string SpecManufacturer { get; set; } = "";
        public string SpecMaterial { get; set; } = "";

        public List<CatalogAssignment> CatalogAssignments { get; set; } = new List<CatalogAssignment>();

        public string CatalogName { get; set; } = "";
        public string CatalogType { get; set; } = "";
        public string CatalogItemName { get; set; } = "";
        public string CatalogNumber { get; set; } = "";

        public string Ident { get; set; } = "";
        public string Text { get; set; } = "";
        public string LongText { get; set; } = "";
        public string Qty { get; set; } = "";
        public decimal QtyResult { get; set; } = 0;
        public string Qu { get; set; } = "";
        public decimal Up { get; set; } = 0;

        public decimal UpResult
        {
            get => QtyResult * Up;
            set { }
        }

        public string Version { get; set; } = "2";
        public string Id { get; set; } = "";
        public int CalculationId { get; set; } = 0;
        public string Id2 { get; set; } = "";
        public string Id5 { get; set; } = "";
        public string Id6 { get; set; } = "";
        public int ParentCalcId { get; set; } = 0;
        public int Order { get; set; } = 0;
        public bool IsParentNode { get; set; } = false;
        public int TreeLevel { get; set; } = 0;
        public int ElementId { get; set; } = 0;
        public int ElementType { get; set; } = 1;
        public string Title { get; set; } = "";
        public string Label { get; set; } = "";
        public string Type { get; set; } = "";
        public string BimKey { get; set; } = "";
        public string TextSys { get; set; } = "";
        public string TextKey { get; set; } = "";
        public string StlNo { get; set; } = "";
        public string OutlineTextFree { get; set; } = "";
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
        public string Vob { get; set; } = "";
        public string VobFormula { get; set; } = "";
        public string VobCondition { get; set; } = "";
        public string VobType { get; set; } = "";
        public decimal VobFactor { get; set; } = 0;
        public string On { get; set; } = "";
        public decimal PercTotal { get; set; } = 0;
        public bool Marked { get; set; } = false;
        public decimal PercMarked { get; set; } = 0;
        public string ProcUnit { get; set; } = "";
        public string Color { get; set; } = "";
        public string Note { get; set; } = "";
        public string Additional { get; set; } = "";
        public string Criteria { get; set; } = "";
        public string Parent { get; set; } = "";
        public string Openings { get; set; } = "";
        public int ChildrenCount { get; set; } = 0;
        public int OpeningsCount { get; set; } = 0;
        public string CatalogReference { get; set; } = "";
        public int FilterValue { get; set; } = 0;
        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Data { get; set; } = "";
        public string IfcType { get; set; } = "";
        public string Material { get; set; } = "";
        public string Dimension { get; set; } = "";
        public string SegmentType { get; set; } = "";
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime Created3 { get; set; } = DateTime.Now;
        public string Label4 { get; set; } = "";
        public string Name7 { get; set; } = "";
        public string Number { get; set; } = "";
        public string Reference { get; set; } = "";
        public string Filter { get; set; } = "";

        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();

        public Dictionary<string, string> ParseIfcParameters()
        {
            var result = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(Properties))
                return result;

            try
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(
                    Properties,
                    @"s:\d+:""([^""]+)"";s:\d+:""([^""]*)"";");

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (match.Groups.Count >= 3)
                    {
                        string key = match.Groups[1].Value;
                        string value = match.Groups[2].Value;
                        result[key] = value;

                        switch (key)
                        {
                            case "DX.SPEC_filter":
                                this.SpecFilter = value;
                                break;
                            case "DX.SPEC_Name":
                                this.SpecName = value;
                                break;
                            case "DX.SPEC_Size":
                                this.SpecSize = value;
                                break;
                            case "DX.SPEC_Type":
                                this.SpecType = value;
                                break;
                            case "DX.SPEC_Manufacturer":
                                this.SpecManufacturer = value;
                                break;
                            case "DX.SPEC_Material":
                                this.SpecMaterial = value;
                                break;
                        }
                    }
                }
            }
            catch
            {
            }

            return result;
        }

        public string GetCatalog(int index)
        {
            if (index < 1 || index > 4 || CatalogAssignments.Count < index)
                return "";

            return CatalogAssignments[index - 1].ToString();
        }

        public static List<string> GetEditableFields()
        {
            return new List<string>
            {
                "Name", "Description", "Children", "Properties",
                "SpecFilter", "SpecName", "SpecSize", "SpecType", "SpecManufacturer", "SpecMaterial",
                "Ident", "Text", "LongText", "Qty", "QtyResult", "Up", "Qu"
            };
        }

        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Name is required");

            if (string.IsNullOrWhiteSpace(Text))
                errors.Add("Text is required");

            if (Text != null && Text.Length > 255)
                errors.Add($"Text exceeds 255 characters (current: {Text.Length})");

            if (LongText != null && LongText.Length > 2000)
                errors.Add($"LongText exceeds 2000 characters (current: {LongText.Length})");

            if (QtyResult < 0)
                errors.Add("Quantity cannot be negative");

            if (Up < 0)
                errors.Add("Unit Price cannot be negative");

            return errors;
        }

        public CostElement Clone()
        {
            return new CostElement
            {
                Name = this.Name,
                Description = this.Description,
                Children = this.Children,
                Properties = this.Properties,
                SpecFilter = this.SpecFilter,
                SpecName = this.SpecName,
                SpecSize = this.SpecSize,
                SpecType = this.SpecType,
                SpecManufacturer = this.SpecManufacturer,
                SpecMaterial = this.SpecMaterial,
                Ident = this.Ident,
                Text = this.Text,
                LongText = this.LongText,
                Qty = this.Qty,
                QtyResult = this.QtyResult,
                Up = this.Up,
                Qu = this.Qu,
                CatalogAssignments = new List<CatalogAssignment>(
                    this.CatalogAssignments.Select(ca => new CatalogAssignment
                    {
                        CatalogName = ca.CatalogName,
                        CatalogType = ca.CatalogType,
                        Name = ca.Name,
                        Number = ca.Number,
                        Reference = ca.Reference
                    })
                ),
                CatalogName = this.CatalogName,
                CatalogType = this.CatalogType,
                CatalogItemName = this.CatalogItemName,
                CatalogNumber = this.CatalogNumber,
                Version = this.Version,
                Id = this.Id,
                CalculationId = this.CalculationId,
                Id2 = this.Id2,
                Id5 = this.Id5,
                Id6 = this.Id6,
                ParentCalcId = this.ParentCalcId,
                Order = this.Order,
                IsParentNode = this.IsParentNode,
                TreeLevel = this.TreeLevel,
                ElementId = this.ElementId,
                ElementType = this.ElementType,
                Title = this.Title,
                Label = this.Label,
                Type = this.Type,
                BimKey = this.BimKey,
                TextSys = this.TextSys,
                TextKey = this.TextKey,
                StlNo = this.StlNo,
                OutlineTextFree = this.OutlineTextFree,
                UpBkdn = this.UpBkdn,
                UpComp1 = this.UpComp1,
                UpComp2 = this.UpComp2,
                UpComp3 = this.UpComp3,
                UpComp4 = this.UpComp4,
                UpComp5 = this.UpComp5,
                UpComp6 = this.UpComp6,
                TimeQu = this.TimeQu,
                It = this.It,
                Vat = this.Vat,
                VatValue = this.VatValue,
                Tax = this.Tax,
                TaxValue = this.TaxValue,
                ItGross = this.ItGross,
                Sum = this.Sum,
                Vob = this.Vob,
                VobFormula = this.VobFormula,
                VobCondition = this.VobCondition,
                VobType = this.VobType,
                VobFactor = this.VobFactor,
                On = this.On,
                PercTotal = this.PercTotal,
                Marked = this.Marked,
                PercMarked = this.PercMarked,
                ProcUnit = this.ProcUnit,
                Color = this.Color,
                Note = this.Note,
                Additional = this.Additional,
                Criteria = this.Criteria,
                Parent = this.Parent,
                Openings = this.Openings,
                ChildrenCount = this.ChildrenCount,
                OpeningsCount = this.OpeningsCount,
                CatalogReference = this.CatalogReference,
                FilterValue = this.FilterValue,
                FilePath = this.FilePath,
                FileName = this.FileName,
                Data = this.Data,
                IfcType = this.IfcType,
                Material = this.Material,
                Dimension = this.Dimension,
                SegmentType = this.SegmentType,
                Created = this.Created,
                Created3 = this.Created3,
                Label4 = this.Label4,
                Name7 = this.Name7,
                Number = this.Number,
                Reference = this.Reference,
                Filter = this.Filter,
                AdditionalData = new Dictionary<string, object>(this.AdditionalData)
            };
        }
    }
}