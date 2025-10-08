using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// XML importer - reads data exactly as-is, properly separating Name and Text
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

                    // IMPORTANT: Get Name from <name> tag at costelement level (NOT from <text>!)
                    var elementName = GetValue(elementNode, "name") ?? "";  // ← <name> tag, not <n>
                    var elementDescription = GetValue(elementNode, "description") ?? "";
                    var elementType = ParseInt(GetValue(elementNode, "type"));
                    var elementProperties = GetValue(elementNode, "properties") ?? "";
                    var elementChildren = GetValue(elementNode, "children") ?? "";
                    var elementOpenings = GetValue(elementNode, "openings") ?? "";
                    var elementCreated = ParseDateTime(GetValue(elementNode, "created"));

                    // Get catalog information from cecatalogassigns
                    string catalogName = "";
                    string catalogType = "";
                    var catalogAssignsNode = elementNode.Element("cecatalogassigns");
                    if (catalogAssignsNode != null)
                    {
                        var catalogAssign = catalogAssignsNode.Element("cecatalogassign");
                        if (catalogAssign != null)
                        {
                            catalogName = GetValue(catalogAssign, "catalogname") ?? "";
                            catalogType = GetValue(catalogAssign, "catalogtype") ?? "";
                        }
                    }

                    // Find cecalculations node
                    var calculationsNode = elementNode.Element("cecalculations");
                    if (calculationsNode != null)
                    {
                        var calculations = calculationsNode.Elements("cecalculation");

                        foreach (var calcNode in calculations)
                        {
                            var element = new CostElement();

                            // ============================================
                            // COSTELEMENT LEVEL DATA (from parent node)
                            // ============================================
                            element.Id = elementId;
                            element.Name = elementName;  // ← From <name> at costelement level
                            element.Description = elementDescription;
                            element.ElementType = elementType;
                            element.Properties = elementProperties;
                            element.Children = elementChildren;
                            element.Openings = elementOpenings;
                            element.Created = elementCreated;
                            element.CatalogName = catalogName;  // ← From cecatalogassigns
                            element.CatalogType = catalogType;  // ← From cecatalogassigns

                            // ============================================
                            // CECALCULATION LEVEL DATA (from this node)
                            // ============================================
                            element.CalculationId = ParseInt(GetValue(calcNode, "id"));
                            element.Ident = GetValue(calcNode, "ident") ?? "";
                            element.Id2 = element.Ident;

                            // Parent/hierarchy
                            element.ParentCalcId = ParseInt(GetValue(calcNode, "parent"));
                            element.Order = ParseInt(GetValue(calcNode, "order"));
                            element.IsParentNode = element.ParentCalcId == 0;
                            element.TreeLevel = element.IsParentNode ? 0 : 1;

                            // Core fields - Text is DIFFERENT from Name!
                            element.BimKey = GetValue(calcNode, "bimkey") ?? "";
                            element.Text = GetValue(calcNode, "text") ?? "";  // ← From <text> at cecalculation level (DIFFERENT!)
                            element.LongText = GetValue(calcNode, "longtext") ?? "";
                            element.TextSys = GetValue(calcNode, "text_sys") ?? "";
                            element.TextKey = GetValue(calcNode, "text_key") ?? "";
                            element.StlNo = GetValue(calcNode, "stlno") ?? "";
                            element.OutlineTextFree = GetValue(calcNode, "outlinetext_free") ?? "";

                            // Quantities and pricing
                            var qtyValue = GetValue(calcNode, "qty");
                            element.Qty = qtyValue == "DXQuantity" || qtyValue == "Count" ? 0 : ParseDecimal(qtyValue);
                            element.QtyResult = ParseDecimal(GetValue(calcNode, "qty_result"));
                            element.Qu = GetValue(calcNode, "qu") ?? "";
                            element.Up = ParseDecimal(GetValue(calcNode, "up"));
                            element.UpResult = ParseDecimal(GetValue(calcNode, "up_result"));  // ← Total price
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

        private static DateTime ParseDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return DateTime.Now;

            if (DateTime.TryParse(value, out DateTime result))
                return result;

            return DateTime.Now;
        }
    }
}