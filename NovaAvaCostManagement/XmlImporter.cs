using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace NovaAvaCostManagement
{
    public class XmlImporter
    {
        public static List<CostElement> ImportFromXml(string filePath)
        {
            var elements = new List<CostElement>();

            try
            {
                var doc = XDocument.Load(filePath);
                var costElementNodes = doc.Descendants()
                    .Where(x => x.Name.LocalName.ToLower() == "costelement");

                foreach (var elementNode in costElementNodes)
                {
                    ProcessCostElement(elementNode, elements);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error importing XML: {ex.Message}", ex);
            }

            return elements;
        }

        private static void ProcessCostElement(XElement elementNode, List<CostElement> elements)
        {
            var elementId = elementNode.Attribute("id")?.Value ?? "";
            var costElementData = ExtractCostElementData(elementNode);
            var catalogAssignments = ExtractCatalogAssignments(elementNode);

            var calculationsNode = elementNode.Element("cecalculations");
            if (calculationsNode != null)
            {
                var calculations = calculationsNode.Elements("cecalculation");

                foreach (var calcNode in calculations)
                {
                    var element = CreateCostElement(elementId, costElementData, catalogAssignments, calcNode);
                    elements.Add(element);
                }
            }
        }

        private static CostElementData ExtractCostElementData(XElement elementNode)
        {
            return new CostElementData
            {
                Name = GetValue(elementNode, "name") ?? "",
                Description = GetValue(elementNode, "description") ?? "",
                ElementType = ParseInt(GetValue(elementNode, "type")),
                Properties = GetValue(elementNode, "properties") ?? "",
                Children = GetValue(elementNode, "children") ?? "",
                Openings = GetValue(elementNode, "openings") ?? "",
                Created = ParseDateTime(GetValue(elementNode, "created"))
            };
        }

        private static List<CatalogAssignment> ExtractCatalogAssignments(XElement elementNode)
        {
            var catalogAssignments = new List<CatalogAssignment>();
            var catalogAssignsNode = elementNode.Element("cecatalogassigns");

            if (catalogAssignsNode != null)
            {
                foreach (var catalogAssign in catalogAssignsNode.Elements("cecatalogassign"))
                {
                    catalogAssignments.Add(new CatalogAssignment
                    {
                        CatalogName = GetValue(catalogAssign, "catalogname") ?? "",
                        CatalogType = GetValue(catalogAssign, "catalogtype") ?? "",
                        Name = GetValue(catalogAssign, "name") ?? "",
                        Number = GetValue(catalogAssign, "number") ?? "",
                        Reference = GetValue(catalogAssign, "reference") ?? ""
                    });
                }
            }

            return catalogAssignments;
        }

        private static CostElement CreateCostElement(string elementId, CostElementData data,
            List<CatalogAssignment> catalogAssignments, XElement calcNode)
        {
            var element = new CostElement
            {
                Id = elementId,
                Name = data.Name,
                Description = data.Description,
                ElementType = data.ElementType,
                Properties = data.Properties,
                Children = data.Children,
                Openings = data.Openings,
                Created = data.Created,
                CatalogAssignments = catalogAssignments
            };

            if (catalogAssignments.Count > 0)
            {
                element.CatalogName = catalogAssignments[0].CatalogName;
                element.CatalogType = catalogAssignments[0].CatalogType;
                element.CatalogItemName = catalogAssignments[0].Name;
                element.CatalogNumber = catalogAssignments[0].Number;
            }

            if (!string.IsNullOrEmpty(data.Properties))
            {
                element.ParseIfcParameters();
            }

            PopulateCalculationData(element, calcNode);

            return element;
        }

        private static void PopulateCalculationData(CostElement element, XElement calcNode)
        {
            element.CalculationId = ParseInt(GetValue(calcNode, "id"));
            element.Ident = GetValue(calcNode, "ident") ?? "";
            element.Id2 = element.Ident;
            element.ParentCalcId = ParseInt(GetValue(calcNode, "parent"));
            element.Order = ParseInt(GetValue(calcNode, "order"));
            element.IsParentNode = element.ParentCalcId == 0;
            element.TreeLevel = element.IsParentNode ? 0 : 1;
            element.BimKey = GetValue(calcNode, "bimkey") ?? "";
            element.Text = GetValue(calcNode, "text") ?? "";
            element.LongText = GetValue(calcNode, "longtext") ?? "";
            element.TextSys = GetValue(calcNode, "text_sys") ?? "";
            element.TextKey = GetValue(calcNode, "text_key") ?? "";
            element.StlNo = GetValue(calcNode, "stlno") ?? "";
            element.OutlineTextFree = GetValue(calcNode, "outlinetext_free") ?? "";
            element.Qty = GetValue(calcNode, "qty") ?? "";
            element.QtyResult = ParseDecimal(GetValue(calcNode, "qty_result"));
            element.Qu = GetValue(calcNode, "qu") ?? "";
            element.Up = ParseDecimal(GetValue(calcNode, "up"));
            element.UpBkdn = ParseDecimal(GetValue(calcNode, "upbkdn"));
            element.UpComp1 = ParseDecimal(GetValue(calcNode, "upcomp1"));
            element.UpComp2 = ParseDecimal(GetValue(calcNode, "upcomp2"));
            element.UpComp3 = ParseDecimal(GetValue(calcNode, "upcomp3"));
            element.UpComp4 = ParseDecimal(GetValue(calcNode, "upcomp4"));
            element.UpComp5 = ParseDecimal(GetValue(calcNode, "upcomp5"));
            element.UpComp6 = ParseDecimal(GetValue(calcNode, "upcomp6"));
            element.TimeQu = GetValue(calcNode, "timequ") ?? "";
            element.It = ParseDecimal(GetValue(calcNode, "it"));
            element.Vat = ParseDecimal(GetValue(calcNode, "vat"));
            element.VatValue = ParseDecimal(GetValue(calcNode, "vatvalue"));
            element.Tax = ParseDecimal(GetValue(calcNode, "tax"));
            element.TaxValue = ParseDecimal(GetValue(calcNode, "taxvalue"));
            element.ItGross = ParseDecimal(GetValue(calcNode, "itgross"));
            element.Sum = ParseDecimal(GetValue(calcNode, "sum"));
            element.Vob = GetValue(calcNode, "vob") ?? "";
            element.VobFormula = GetValue(calcNode, "vob_formula") ?? "";
            element.VobCondition = GetValue(calcNode, "vob_condition") ?? "";
            element.VobType = GetValue(calcNode, "vob_type") ?? "";
            element.VobFactor = ParseDecimal(GetValue(calcNode, "vob_factor"));
            element.On = GetValue(calcNode, "on") ?? "";
            element.PercTotal = ParseDecimal(GetValue(calcNode, "perctotal"));
            element.Marked = GetValue(calcNode, "marked") == "1";
            element.PercMarked = ParseDecimal(GetValue(calcNode, "percmarked"));
            element.ProcUnit = GetValue(calcNode, "procunit") ?? "";
            element.Color = GetValue(calcNode, "color") ?? "";
            element.Note = GetValue(calcNode, "note") ?? "";
        }

        private static string GetValue(XElement parent, string elementName)
        {
            var element = parent.Element(elementName);
            if (element == null) return null;

            var cdata = element.Nodes().OfType<XCData>().FirstOrDefault();
            if (cdata != null)
                return cdata.Value?.Trim();

            return element.Value?.Trim();
        }

        private static int ParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            if (int.TryParse(value, out int result))
                return result;

            return 0;
        }

        private static decimal ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0m;

            value = value.Replace(',', '.');

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result;

            return 0m;
        }

        private static DateTime ParseDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return DateTime.Now;

            if (DateTime.TryParse(value, out DateTime result))
                return result;

            return DateTime.Now;
        }

        private class CostElementData
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public int ElementType { get; set; }
            public string Properties { get; set; }
            public string Children { get; set; }
            public string Openings { get; set; }
            public DateTime Created { get; set; }
        }
    }
}