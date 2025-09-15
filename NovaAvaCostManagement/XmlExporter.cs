using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Handles exporting cost elements to AVA/NOVA XML
    /// </summary>
    public class XmlExporter
    {
        /// <summary>
        /// Export cost elements to NOVA/AVA compatible XML
        /// </summary>
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
        /// Write NOVA/AVA format XML
        /// </summary>
        private static void WriteNovaAvaFormat(XmlWriter writer, List<CostElement> elements)
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("CostElements");
            writer.WriteAttributeString("version", "2");
            writer.WriteAttributeString("title", "Cost Elements_NotAssigned");
            writer.WriteAttributeString("created", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));

            foreach (var element in elements)
            {
                writer.WriteStartElement("Element");

                // Write all standard fields
                WriteElementField(writer, "version", element.Version);
                WriteElementField(writer, "id", element.Id);
                WriteElementField(writer, "title", element.Title);
                WriteElementField(writer, "label", element.Label);
                WriteElementField(writer, "criteria", element.Criteria);
                WriteElementField(writer, "created", element.Created.ToString("yyyy-MM-ddTHH:mm:ss"));
                WriteElementField(writer, "id2", element.Id2);
                WriteElementField(writer, "type", element.Type);
                WriteElementField(writer, "name", element.Name);
                WriteElementField(writer, "description", element.Description);
                WriteElementField(writer, "properties", element.Properties);
                WriteElementField(writer, "children", element.Children);
                WriteElementField(writer, "openings", element.Openings);
                WriteElementField(writer, "created3", element.Created3.ToString("yyyy-MM-ddTHH:mm:ss"));
                WriteElementField(writer, "label4", element.Label4);
                WriteElementField(writer, "id5", element.Id5);
                WriteElementField(writer, "parent", element.Parent);
                WriteElementField(writer, "order", element.Order.ToString());
                WriteElementField(writer, "ident", element.Ident);
                WriteElementField(writer, "bimkey", element.BimKey);
                WriteElementField(writer, "text", element.Text);
                WriteElementField(writer, "longtext", element.LongText);
                WriteElementField(writer, "text_sys", element.TextSys);
                WriteElementField(writer, "text_key", element.TextKey);
                WriteElementField(writer, "stlno", element.StlNo);
                WriteElementField(writer, "outlinetext_free", element.OutlineTextFree);
                WriteElementField(writer, "qty", element.Qty.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "qty_result", element.QtyResult.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "qu", element.Qu);
                WriteElementField(writer, "up", element.Up.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "up_result", element.UpResult.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "upbkdn", element.UpBkdn.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "upcomp1", element.UpComp1.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "upcomp2", element.UpComp2.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "upcomp3", element.UpComp3.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "upcomp4", element.UpComp4.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "upcomp5", element.UpComp5.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "upcomp6", element.UpComp6.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "timequ", element.TimeQu);
                WriteElementField(writer, "it", element.It.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "vat", element.Vat.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "vatvalue", element.VatValue.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "tax", element.Tax.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "taxvalue", element.TaxValue.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "itgross", element.ItGross.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "sum", element.Sum.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "vob", element.Vob);
                WriteElementField(writer, "vob_formula", element.VobFormula);
                WriteElementField(writer, "vob_condition", element.VobCondition);
                WriteElementField(writer, "vob_type", element.VobType);
                WriteElementField(writer, "vob_factor", element.VobFactor.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "on", element.On);
                WriteElementField(writer, "perctotal", element.PercTotal.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "marked", element.Marked.ToString().ToLower());
                WriteElementField(writer, "percmarked", element.PercMarked.ToString(CultureInfo.InvariantCulture));
                WriteElementField(writer, "procunit", element.ProcUnit);
                WriteElementField(writer, "color", element.Color);
                WriteElementField(writer, "note", element.Note);
                WriteElementField(writer, "additional", element.Additional);
                WriteElementField(writer, "id6", element.Id6);
                WriteElementField(writer, "filepath", element.FilePath);
                WriteElementField(writer, "filename", element.FileName);
                WriteElementField(writer, "data", element.Data);
                WriteElementField(writer, "catalogname", element.CatalogName);
                WriteElementField(writer, "catalogtype", element.CatalogType);
                WriteElementField(writer, "name7", element.Name7);
                WriteElementField(writer, "number", element.Number);
                WriteElementField(writer, "reference", element.Reference);
                WriteElementField(writer, "filter", element.Filter);

                // Write IFC-specific fields
                WriteElementField(writer, "ifc_type", element.IfcType);
                WriteElementField(writer, "material", element.Material);
                WriteElementField(writer, "dimension", element.Dimension);
                WriteElementField(writer, "segment_type", element.SegmentType);

                // Write additional data
                foreach (var kvp in element.AdditionalData)
                {
                    WriteElementField(writer, kvp.Key, kvp.Value?.ToString() ?? "");
                }

                writer.WriteEndElement(); // Element
            }

            writer.WriteEndElement(); // CostElements
            writer.WriteEndDocument();
        }

        /// <summary>
        /// Write GAEB format XML
        /// </summary>
        private static void WriteGaebFormat(XmlWriter writer, List<CostElement> elements)
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("GAEB", "http://www.gaeb.de/GAEB_XML");
            writer.WriteAttributeString("version", "3.2");

            writer.WriteStartElement("GAEBInfo");
            writer.WriteElementString("Version", "3.2");
            writer.WriteElementString("Date", DateTime.Now.ToString("yyyy-MM-dd"));
            writer.WriteElementString("Time", DateTime.Now.ToString("HH:mm:ss"));
            writer.WriteEndElement(); // GAEBInfo

            writer.WriteStartElement("Award");
            writer.WriteStartElement("BoQ");

            foreach (var element in elements)
            {
                writer.WriteStartElement("Item");
                writer.WriteAttributeString("RNoPart", element.Id);

                writer.WriteElementString("Description", element.Text);
                writer.WriteElementString("LongText", element.LongText);
                writer.WriteElementString("Unit", element.Qu);
                writer.WriteElementString("Qty", element.Qty.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("UP", element.Up.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("Total", element.Sum.ToString(CultureInfo.InvariantCulture));

                if (!string.IsNullOrEmpty(element.Properties))
                {
                    writer.WriteElementString("Properties", element.Properties);
                }

                writer.WriteEndElement(); // Item
            }

            writer.WriteEndElement(); // BoQ
            writer.WriteEndElement(); // Award
            writer.WriteEndElement(); // GAEB
            writer.WriteEndDocument();
        }

        /// <summary>
        /// Write XML element field safely
        /// </summary>
        private static void WriteElementField(XmlWriter writer, string name, string value)
        {
            writer.WriteElementString(name, value ?? "");
        }

        /// <summary>
        /// Generate unique output file path
        /// </summary>
        public static string GenerateUniqueOutputPath(string directory, string baseName, string extension)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"{baseName}_{timestamp}.{extension}";
            return Path.Combine(directory, fileName);
        }
    }
}