using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;

namespace NovaAvaCostManagement
{
    public class XmlExporter
    {
        public static void ExportToXml(List<CostElement> elements, string filePath, bool useGaebFormat = false)
        {
            try
            {
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = " ",
                    Encoding = Encoding.UTF8
                };

                using (var writer = XmlWriter.Create(filePath, settings))
                {
                    if (useGaebFormat)
                    {
                        WriteGaebFormat(writer, elements);
                    }
                    else
                    {
                        WriteNovaAvaFormat(writer, elements);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error exporting XML: {ex.Message}", ex);
            }
        }

        private static void WriteNovaAvaFormat(XmlWriter writer, List<CostElement> elements)
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("cefexport");
            writer.WriteAttributeString("version", "2");

            writer.WriteStartElement("buildingfilters");
            writer.WriteEndElement();

            writer.WriteStartElement("costelements");

            var grouped = elements.GroupBy(e => e.Id)
                .OrderBy(g => int.TryParse(g.Key, out int id) ? id : 0)
                .ToList();

            foreach (var group in grouped)
            {
                WriteCostElement(writer, group);
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        private static void WriteCostElement(XmlWriter writer, IGrouping<string, CostElement> group)
        {
            var parentElement = group.FirstOrDefault(e => e.IsParentNode) ?? group.First();

            writer.WriteStartElement("costelement");
            writer.WriteAttributeString("id", parentElement.Id);

            WriteCostElementData(writer, parentElement);
            WriteCatalogAssignments(writer, parentElement);
            WriteCalculations(writer, group);

            writer.WriteEndElement();
        }

        private static void WriteCostElementData(XmlWriter writer, CostElement element)
        {
            WriteElement(writer, "type", element.ElementType.ToString());
            WriteElement(writer, "name", element.Name);
            WriteElement(writer, "description", element.Description);

            if (!string.IsNullOrEmpty(element.Properties))
            {
                writer.WriteStartElement("properties");
                writer.WriteCData(element.Properties);
                writer.WriteEndElement();
            }
            else
            {
                WriteElement(writer, "properties", "");
            }

            WriteElement(writer, "filter", element.FilterValue.ToString());
            WriteElement(writer, "children", element.Children);
            WriteElement(writer, "openings", element.Openings);
            WriteElement(writer, "created", element.Created.ToString("yyyy-MM-ddTHH:mm:ss"));
        }

        private static void WriteCatalogAssignments(XmlWriter writer, CostElement element)
        {
            if (!string.IsNullOrEmpty(element.CatalogName) ||
                !string.IsNullOrEmpty(element.CatalogType) ||
                !string.IsNullOrEmpty(element.CatalogItemName) ||
                !string.IsNullOrEmpty(element.CatalogNumber))
            {
                writer.WriteStartElement("cecatalogassigns");
                writer.WriteStartElement("cecatalogassign");
                WriteElement(writer, "catalogname", element.CatalogName);
                WriteElement(writer, "catalogtype", element.CatalogType);
                WriteElement(writer, "name", element.CatalogItemName);
                WriteElement(writer, "number", element.CatalogNumber);
                WriteElement(writer, "reference", "");
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }

        private static void WriteCalculations(XmlWriter writer, IGrouping<string, CostElement> group)
        {
            writer.WriteStartElement("cecalculations");

            foreach (var calc in group.OrderBy(e => e.Order))
            {
                WriteCalculation(writer, calc);
            }

            writer.WriteEndElement();
        }

        private static void WriteCalculation(XmlWriter writer, CostElement calc)
        {
            writer.WriteStartElement("cecalculation");

            WriteElement(writer, "id", calc.CalculationId.ToString());
            WriteElement(writer, "parent", calc.ParentCalcId.ToString());
            WriteElement(writer, "order", calc.Order.ToString());
            WriteElement(writer, "ident", calc.Ident);
            WriteElement(writer, "bimkey", calc.BimKey);

            WriteCDataElement(writer, "text", calc.Text);
            WriteCDataElement(writer, "longtext", calc.LongText);

            WriteElement(writer, "text_sys", calc.TextSys);
            WriteElement(writer, "text_key", calc.TextKey);
            WriteElement(writer, "stlno", calc.StlNo);
            WriteElement(writer, "outlinetext_free", calc.OutlineTextFree);

            WriteElement(writer, "qty", string.IsNullOrEmpty(calc.Vob) ? FormatDecimal(calc.Qty) : "DXQuantity");
            WriteElement(writer, "qty_result", FormatDecimal(calc.QtyResult));
            WriteElement(writer, "qu", calc.Qu);
            WriteElement(writer, "up", FormatDecimal(calc.Up));
            WriteElement(writer, "up_result", FormatDecimal(calc.UpResult));
            WriteElement(writer, "upbkdn", FormatDecimal(calc.UpBkdn));
            WriteElement(writer, "upcomp1", FormatDecimal(calc.UpComp1));
            WriteElement(writer, "upcomp2", FormatDecimal(calc.UpComp2));
            WriteElement(writer, "upcomp3", FormatDecimal(calc.UpComp3));
            WriteElement(writer, "upcomp4", FormatDecimal(calc.UpComp4));
            WriteElement(writer, "upcomp5", FormatDecimal(calc.UpComp5));
            WriteElement(writer, "upcomp6", FormatDecimal(calc.UpComp6));
            WriteElement(writer, "timequ", FormatDecimal(calc.TimeQu));
            WriteElement(writer, "it", FormatDecimal(calc.It));
            WriteElement(writer, "vat", FormatDecimal(calc.Vat, "0.00"));
            WriteElement(writer, "vatvalue", FormatDecimal(calc.VatValue));
            WriteElement(writer, "tax", FormatDecimal(calc.Tax, "0.00"));
            WriteElement(writer, "taxvalue", FormatDecimal(calc.TaxValue));
            WriteElement(writer, "itgross", FormatDecimal(calc.ItGross, "0.00"));
            WriteElement(writer, "sum", FormatDecimal(calc.Sum));
            WriteElement(writer, "vob", calc.Vob);
            WriteElement(writer, "vob_formula", calc.VobFormula);
            WriteElement(writer, "vob_condition", calc.VobCondition);
            WriteElement(writer, "vob_type", calc.VobType);
            WriteElement(writer, "vob_factor", FormatDecimal(calc.VobFactor));
            WriteElement(writer, "on", calc.On);

            if (!string.IsNullOrEmpty(calc.Additional))
            {
                writer.WriteStartElement("additional");
                writer.WriteCData(calc.Additional);
                writer.WriteEndElement();
            }

            WriteElement(writer, "perctotal", FormatDecimal(calc.PercTotal, "0.00"));
            WriteElement(writer, "marked", calc.Marked ? "1" : "0");
            WriteElement(writer, "percmarked", FormatDecimal(calc.PercMarked, "0.00"));
            WriteElement(writer, "procunit", calc.ProcUnit);
            WriteElement(writer, "color", calc.Color);
            WriteElement(writer, "note", calc.Note);

            writer.WriteEndElement();
        }

        private static void WriteGaebFormat(XmlWriter writer, List<CostElement> elements)
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("GAEB", "http://www.gaeb.de/GAEB_XML");
            writer.WriteAttributeString("version", "3.2");

            writer.WriteStartElement("GAEBInfo");
            WriteElement(writer, "Version", "3.2");
            WriteElement(writer, "Date", DateTime.Now.ToString("yyyy-MM-dd"));
            WriteElement(writer, "Time", DateTime.Now.ToString("HH:mm:ss"));
            writer.WriteEndElement();

            writer.WriteStartElement("Award");
            writer.WriteStartElement("BoQ");

            foreach (var element in elements)
            {
                writer.WriteStartElement("Item");
                writer.WriteAttributeString("RNoPart", element.Id);

                WriteElement(writer, "Description", element.Text);
                WriteElement(writer, "LongText", element.LongText);
                WriteElement(writer, "Unit", element.Qu);
                WriteElement(writer, "Qty", FormatDecimal(element.QtyResult));
                WriteElement(writer, "UP", FormatDecimal(element.Up));
                WriteElement(writer, "Total", FormatDecimal(element.UpResult));

                if (!string.IsNullOrEmpty(element.Properties))
                {
                    WriteElement(writer, "Properties", element.Properties);
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        private static void WriteElement(XmlWriter writer, string name, string value)
        {
            writer.WriteElementString(name, value ?? "");
        }

        private static void WriteCDataElement(XmlWriter writer, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                writer.WriteStartElement(name);
                writer.WriteCData(value);
                writer.WriteEndElement();
            }
            else
            {
                WriteElement(writer, name, "");
            }
        }

        private static string FormatDecimal(decimal value, string format = "0.000")
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static string FormatDecimal(string value, string format = "0.000")
        {
            if (string.IsNullOrWhiteSpace(value))
                return "0" + (format.Contains(".") ? ".000" : "");

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result.ToString(format, CultureInfo.InvariantCulture);

            return "0" + (format.Contains(".") ? ".000" : "");
        }
    }
}