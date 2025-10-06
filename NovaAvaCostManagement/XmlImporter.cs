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

                // Find all costelement nodes
                var costElementNodes = doc.Descendants()
                    .Where(x => x.Name.LocalName.ToLower() == "costelement");

                foreach (var elementNode in costElementNodes)
                {
                    // Get element-level data
                    var elementId = elementNode.Attribute("id")?.Value ?? "0";
                    var elementName = elementNode.Element("name")?.Value ?? "";
                    var elementType = elementNode.Element("type")?.Value ?? "1";
                    var created = elementNode.Element("created")?.Value ?? "";

                    // Parse child calculations
                    var calculationsNode = elementNode.Element("cecalculations");
                    if (calculationsNode != null)
                    {
                        var calculations = calculationsNode.Elements("cecalculation");

                        foreach (var calcNode in calculations)
                        {
                            var element = ParseCalculationNode(calcNode, elementId);
                            if (element != null)
                            {
                                elements.Add(element);
                            }
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

        private static CostElement ParseCalculationNode(XElement node, string parentElementId)
        {
            var element = new CostElement();

            // Parse all fields exactly as they are in the XML
            element.Id = GetElementValue(node, "id") ?? "0";
            element.Id2 = GetElementValue(node, "ident") ?? "";
            element.Ident = element.Id2;
            element.Order = int.Parse(GetElementValue(node, "order") ?? "0");
            element.BimKey = GetElementValue(node, "bimkey") ?? "";
            element.Text = GetElementValue(node, "text") ?? "";
            element.LongText = GetElementValue(node, "longtext") ?? "";
            element.Name = element.Text;
            element.Qu = GetElementValue(node, "qu") ?? "";
            element.ProcUnit = element.Qu;
            element.On = GetElementValue(node, "on") ?? "";
            element.StlNo = GetElementValue(node, "stlno") ?? "";
            element.Note = GetElementValue(node, "note") ?? "";
            element.Color = GetElementValue(node, "color") ?? "";
            element.Additional = GetElementValue(node, "additional") ?? "";

            // Parse numeric fields
            element.Qty = ParseDecimal(GetElementValue(node, "qty_result"));
            element.Up = ParseDecimal(GetElementValue(node, "up"));
            element.UpResult = ParseDecimal(GetElementValue(node, "up_result"));
            element.It = ParseDecimal(GetElementValue(node, "it"));
            element.Vat = ParseDecimal(GetElementValue(node, "vat"));
            element.VatValue = ParseDecimal(GetElementValue(node, "vatvalue"));
            element.Tax = ParseDecimal(GetElementValue(node, "tax"));
            element.TaxValue = ParseDecimal(GetElementValue(node, "taxvalue"));
            element.ItGross = ParseDecimal(GetElementValue(node, "itgross"));
            element.Sum = ParseDecimal(GetElementValue(node, "sum"));

            // Price components
            for (int i = 1; i <= 6; i++)
            {
                var compValue = ParseDecimal(GetElementValue(node, $"upcomp{i}"));
                typeof(CostElement).GetProperty($"UpComp{i}").SetValue(element, compValue);
            }

            element.CalculateFields();
            return element;
        }

        private static string GetElementValue(XElement parent, string elementName)
        {
            var element = parent.Element(elementName);
            if (element == null) return null;

            var cdata = element.Nodes().OfType<XCData>().FirstOrDefault();
            if (cdata != null)
                return cdata.Value.Trim();

            return element.Value?.Trim();
        }

        private static decimal ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0m;

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result;

            return 0m;
        }
    }
}