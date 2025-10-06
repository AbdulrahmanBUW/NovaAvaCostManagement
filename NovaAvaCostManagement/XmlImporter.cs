using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Simple XML importer - reads data exactly as-is without interpretation
    /// </summary>
    public class XmlImporter
    {
        public static List<CostElement> ImportFromXml(string filePath)
        {
            var elements = new List<CostElement>();

            try
            {
                var doc = XDocument.Load(filePath);

                // Find ALL costelement nodes
                var costElementNodes = doc.Descendants()
                    .Where(x => x.Name.LocalName.ToLower() == "costelement");

                foreach (var elementNode in costElementNodes)
                {
                    // Get costelement ID
                    var elementId = elementNode.Attribute("id")?.Value ?? "";

                    // Find cecalculations node
                    var calculationsNode = elementNode.Element("cecalculations");
                    if (calculationsNode != null)
                    {
                        var calculations = calculationsNode.Elements("cecalculation");

                        foreach (var calcNode in calculations)
                        {
                            var element = new CostElement();

                            // READ EXACTLY AS-IS - NO AUTO-GENERATION
                            element.Id = elementId;  // costelement id
                            element.CalculationId = ParseInt(GetValue(calcNode, "id"));
                            element.Id2 = GetValue(calcNode, "ident") ?? "";  // Show this as Code
                            element.Ident = element.Id2;

                            // Parent/hierarchy
                            element.ParentCalcId = ParseInt(GetValue(calcNode, "parent"));
                            element.Order = ParseInt(GetValue(calcNode, "order"));

                            // Determine if parent or child
                            element.IsParentNode = element.ParentCalcId == 0;
                            element.TreeLevel = element.IsParentNode ? 0 : 1;

                            // Core fields - READ EXACTLY
                            element.BimKey = GetValue(calcNode, "bimkey") ?? "";
                            element.Text = GetValue(calcNode, "text") ?? "";
                            element.LongText = GetValue(calcNode, "longtext") ?? "";
                            element.Name = element.Text;  // Use text as name
                            element.TextSys = GetValue(calcNode, "text_sys") ?? "";
                            element.TextKey = GetValue(calcNode, "text_key") ?? "";
                            element.StlNo = GetValue(calcNode, "stlno") ?? "";
                            element.OutlineTextFree = GetValue(calcNode, "outlinetext_free") ?? "";

                            // Quantities and pricing - READ EXACTLY
                            element.Qty = ParseDecimal(GetValue(calcNode, "qty_result"));
                            element.QtyResult = element.Qty;
                            element.Qu = GetValue(calcNode, "qu") ?? "";
                            element.Up = ParseDecimal(GetValue(calcNode, "up"));
                            element.UpResult = ParseDecimal(GetValue(calcNode, "up_result"));
                            element.UpBkdn = ParseDecimal(GetValue(calcNode, "upbkdn"));

                            // Price components
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

                            // VOB fields
                            element.Vob = GetValue(calcNode, "vob") ?? "";
                            element.VobFormula = GetValue(calcNode, "vob_formula") ?? "";
                            element.VobCondition = GetValue(calcNode, "vob_condition") ?? "";
                            element.VobType = GetValue(calcNode, "vob_type") ?? "";
                            element.VobFactor = ParseDecimal(GetValue(calcNode, "vob_factor"));

                            // Additional fields
                            element.On = GetValue(calcNode, "on") ?? "";
                            element.PercTotal = ParseDecimal(GetValue(calcNode, "perctotal"));
                            element.Marked = GetValue(calcNode, "marked") == "1";
                            element.PercMarked = ParseDecimal(GetValue(calcNode, "percmarked"));
                            element.ProcUnit = GetValue(calcNode, "procunit") ?? "";
                            element.Color = GetValue(calcNode, "color") ?? "";
                            element.Note = GetValue(calcNode, "note") ?? "";

                            // Get properties from parent costelement
                            element.Properties = GetValue(elementNode, "properties") ?? "";

                            // Get name and type from parent costelement
                            var elementName = GetValue(elementNode, "name") ?? "";
                            if (string.IsNullOrEmpty(element.Name))
                                element.Name = elementName;

                            element.ElementType = ParseInt(GetValue(elementNode, "type"));

                            // DO NOT auto-generate anything
                            // DO NOT call CalculateFields()
                            // Just add the element as-is
                            elements.Add(element);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error importing XML: {ex.Message}", ex);
            }

            return elements;
        }

        private static string GetValue(XElement parent, string elementName)
        {
            var element = parent.Element(elementName);
            if (element == null) return null;

            // Handle CDATA
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

            // Handle both comma and period
            value = value.Replace(',', '.');

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result;

            return 0m;
        }
    }
}