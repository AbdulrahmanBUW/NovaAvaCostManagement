using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Manages template creation and conversion
    /// </summary>
    public class TemplateManager
    {
        /// <summary>
        /// Create data entry template CSV
        /// </summary>
        public static void CreateDataEntryTemplate(string filePath)
        {
            var headers = new[]
            {
                "ID", "Name", "Type", "Text", "LongText", "Qty", "Unit", "UnitPrice",
                "BIMKey", "Description", "Label", "Status", "Notes", "IfcType",
                "Material", "Dimension", "SegmentType", "Color"
            };

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers));

            // Add sample row
            sb.AppendLine("CAST_Pipe_DIN10216-2_DN125,\"C-Stahl Rohr DIN EN 10216-2 P235HTC1 DN125\",Pipe,\"C-Stahl Rohr DN125\",\"C-Stahl Rohr DIN EN 10216-2 P235HTC1 DN125\",37.09,m,0.11,PIPE_001,\"Steel pipe for construction\",\"DN125 Pipe\",Active,\"Sample pipe element\",IFCPIPESEGMENT,P235HTC1,DN125,\"DX_CarbonSteel_1.0345 - DIN EN 10216-2\",#3498DB");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Create IFC mapping template CSV
        /// </summary>
        public static void CreateIFCMappingTemplate(string filePath)
        {
            var headers = new[]
            {
                "IfcElementType", "DefaultCode", "DefaultText", "DefaultLongText",
                "DefaultUnit", "TypicalQty", "UnitPriceRange", "DefaultMaterial",
                "DefaultDimension", "DefaultSegmentType"
            };

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers));

            // Add sample mappings
            sb.AppendLine("IFCPIPESEGMENT,PIPE_STD,\"Standard Pipe\",\"Standard steel pipe segment\",m,1.0,\"50-200\",Steel,DN100,Standard");
            sb.AppendLine("IFCWALL,WALL_STD,\"Standard Wall\",\"Standard concrete wall\",m2,10.0,\"100-300\",Concrete,200mm,LoadBearing");
            sb.AppendLine("IFCBEAM,BEAM_STD,\"Standard Beam\",\"Standard steel beam\",m,2.0,\"200-500\",Steel,IPE200,Structural");
            sb.AppendLine("IFCSLAB,SLAB_STD,\"Standard Slab\",\"Standard concrete slab\",m2,20.0,\"80-150\",Concrete,200mm,Floor");
            sb.AppendLine("IFCDOOR,DOOR_STD,\"Standard Door\",\"Standard interior door\",pcs,1.0,\"200-800\",Wood,800x2000,Interior");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Convert template data to main sheet format
        /// </summary>
        public static List<CostElement> ConvertTemplateToMainSheet(string templateFilePath)
        {
            var elements = new List<CostElement>();

            try
            {
                var lines = File.ReadAllLines(templateFilePath);
                if (lines.Length < 2) return elements; // No data rows

                var headers = ParseCsvLine(lines[0]);

                for (int i = 1; i < lines.Length; i++)
                {
                    var values = ParseCsvLine(lines[i]);
                    if (values.Length == 0) continue;

                    var element = ConvertTemplateRowToElement(headers, values);
                    if (element != null)
                    {
                        elements.Add(element);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error converting template: {ex.Message}", ex);
            }

            return elements;
        }

        /// <summary>
        /// Convert single template row to CostElement
        /// </summary>
        private static CostElement ConvertTemplateRowToElement(string[] headers, string[] values)
        {
            var element = new CostElement();

            for (int i = 0; i < Math.Min(headers.Length, values.Length); i++)
            {
                var header = headers[i].Trim().ToLower();
                var value = values[i].Trim().Trim('"');

                switch (header)
                {
                    case "id":
                        if (!string.IsNullOrEmpty(value)) element.Id2 = value;
                        break;
                    case "name":
                        element.Name = value;
                        break;
                    case "type":
                        element.Type = value;
                        break;
                    case "text":
                        element.Text = value;
                        break;
                    case "longtext":
                        element.LongText = value;
                        break;
                    case "qty":
                        if (decimal.TryParse(value, out decimal qty)) element.Qty = qty;
                        break;
                    case "unit":
                        element.Qu = value;
                        element.ProcUnit = value;
                        break;
                    case "unitprice":
                        if (decimal.TryParse(value, out decimal up)) element.Up = up;
                        break;
                    case "bimkey":
                        element.BimKey = value;
                        break;
                    case "description":
                        element.Description = value;
                        break;
                    case "label":
                        element.Label = value;
                        break;
                    case "notes":
                        element.Note = value;
                        break;
                    case "ifctype":
                        element.IfcType = value;
                        break;
                    case "material":
                        element.Material = value;
                        break;
                    case "dimension":
                        element.Dimension = value;
                        break;
                    case "segmenttype":
                        element.SegmentType = value;
                        break;
                    case "color":
                        element.Color = value;
                        break;
                }
            }

            // Generate properties and calculate fields
            element.GenerateProperties();
            element.CalculateFields();

            return element;
        }

        /// <summary>
        /// Parse CSV line handling quotes and commas
        /// </summary>
        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result.ToArray();
        }
    }
}