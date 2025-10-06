using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Exports to AVA NOVA XML format - EXACT structure as input
    /// DisplayNumber is NOT exported (display-only field)
    /// </summary>
    public class XmlExporter
    {
        public static void ExportToXml(List<CostElement> elements, string filePath, bool useGaebFormat = false)
        {
            try
            {
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
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

        /// <summary>
        /// Write in exact NOVA AVA format - matching input structure
        /// </summary>
        private static void WriteNovaAvaFormat(XmlWriter writer, List<CostElement> elements)
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("cefexport");
            writer.WriteAttributeString("version", "2");

            // Write buildingfilters section (if needed)
            writer.WriteStartElement("buildingfilters");
            writer.WriteEndElement();

            // Write costelements
            writer.WriteStartElement("costelements");

            // Group by element ID (parent level)
            var grouped = elements.GroupBy(e => e.Id).OrderBy(g => g.Key).ToList();

            foreach (var group in grouped)
            {
                // Get parent element (IsParentNode = true)
                var parentElement = group.FirstOrDefault(e => e.IsParentNode) ?? group.First();

                writer.WriteStartElement("costelement");
                writer.WriteAttributeString("id", parentElement.Id);

                // Write costelement-level fields
                WriteElement(writer, "type", parentElement.ElementType.ToString());
                WriteElement(writer, "name", parentElement.Name);
                WriteElement(writer, "description", parentElement.Description);

                // Write properties with CDATA
                if (!string.IsNullOrEmpty(parentElement.Properties))
                {
                    writer.WriteStartElement("properties");
                    writer.WriteCData(parentElement.Properties);
                    writer.WriteEndElement();
                }
                else
                {
                    WriteElement(writer, "properties", "");
                }

                WriteElement(writer, "children", parentElement.ChildrenCount.ToString());
                WriteElement(writer, "openings", parentElement.OpeningsCount.ToString());
                WriteElement(writer, "created", parentElement.Created.ToString("yyyy-MM-ddTHH:mm:ss"));

                // Write cecalculations section
                writer.WriteStartElement("cecalculations");

                // Write all calculations for this element, ordered by Order field
                foreach (var calc in group.OrderBy(e => e.Order))
                {
                    writer.WriteStartElement("cecalculation");

                    // CRITICAL: Write exact values from XML - NO DisplayNumber
                    WriteElement(writer, "id", calc.CalculationId.ToString());
                    WriteElement(writer, "parent", calc.ParentCalcId.ToString());
                    WriteElement(writer, "order", calc.Order.ToString());
                    WriteElement(writer, "ident", calc.Ident);
                    WriteElement(writer, "bimkey", calc.BimKey);

                    // Write text with CDATA
                    if (!string.IsNullOrEmpty(calc.Text))
                    {
                        writer.WriteStartElement("text");
                        writer.WriteCData(calc.Text);
                        writer.WriteEndElement();
                    }
                    else
                    {
                        WriteElement(writer, "text", "");
                    }

                    // Write longtext with CDATA
                    if (!string.IsNullOrEmpty(calc.LongText))
                    {
                        writer.WriteStartElement("longtext");
                        writer.WriteCData(calc.LongText);
                        writer.WriteEndElement();
                    }
                    else
                    {
                        WriteElement(writer, "longtext", "");
                    }

                    WriteElement(writer, "text_sys", calc.TextSys);
                    WriteElement(writer, "text_key", calc.TextKey);
                    WriteElement(writer, "stlno", calc.StlNo);
                    WriteElement(writer, "outlinetext_free", calc.OutlineTextFree);
                    WriteElement(writer, "qty", "DXQuantity");  // Keep formula if present
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
                    WriteElement(writer, "vat", FormatDecimal(calc.Vat));
                    WriteElement(writer, "vatvalue", FormatDecimal(calc.VatValue));
                    WriteElement(writer, "tax", FormatDecimal(calc.Tax));
                    WriteElement(writer, "taxvalue", FormatDecimal(calc.TaxValue));
                    WriteElement(writer, "itgross", FormatDecimal(calc.ItGross));
                    WriteElement(writer, "sum", FormatDecimal(calc.Sum));
                    WriteElement(writer, "vob", calc.Vob);
                    WriteElement(writer, "vob_formula", calc.VobFormula);
                    WriteElement(writer, "vob_condition", calc.VobCondition);
                    WriteElement(writer, "vob_type", calc.VobType);
                    WriteElement(writer, "vob_factor", FormatDecimal(calc.VobFactor));
                    WriteElement(writer, "on", calc.On);
                    WriteElement(writer, "perctotal", FormatDecimal(calc.PercTotal));
                    WriteElement(writer, "marked", calc.Marked ? "1" : "0");
                    WriteElement(writer, "percmarked", FormatDecimal(calc.PercMarked));
                    WriteElement(writer, "procunit", calc.ProcUnit);
                    WriteElement(writer, "color", calc.Color);
                    WriteElement(writer, "note", calc.Note);

                    writer.WriteEndElement(); // cecalculation
                }

                writer.WriteEndElement(); // cecalculations
                writer.WriteEndElement(); // costelement
            }

            writer.WriteEndElement(); // costelements
            writer.WriteEndElement(); // cefexport
            writer.WriteEndDocument();
        }

        /// <summary>
        /// Write GAEB format (alternative export)
        /// </summary>
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
                WriteElement(writer, "Qty", FormatDecimal(element.Qty));
                WriteElement(writer, "UP", FormatDecimal(element.Up));
                WriteElement(writer, "Total", FormatDecimal(element.Sum));

                if (!string.IsNullOrEmpty(element.Properties))
                {
                    WriteElement(writer, "Properties", element.Properties);
                }

                writer.WriteEndElement(); // Item
            }

            writer.WriteEndElement(); // BoQ
            writer.WriteEndElement(); // Award
            writer.WriteEndElement(); // GAEB
            writer.WriteEndDocument();
        }

        /// <summary>
        /// Write XML element safely
        /// </summary>
        private static void WriteElement(XmlWriter writer, string name, string value)
        {
            writer.WriteElementString(name, value ?? "");
        }

        /// <summary>
        /// Format decimal for XML output
        /// </summary>
        private static string FormatDecimal(decimal value)
        {
            return value.ToString("0.000", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Format decimal from string
        /// </summary>
        private static string FormatDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "0.000";

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result.ToString("0.000", CultureInfo.InvariantCulture);

            return "0.000";
        }
    }
}