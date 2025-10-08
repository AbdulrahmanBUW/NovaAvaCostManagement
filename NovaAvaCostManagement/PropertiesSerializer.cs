// Add this new file: NovaAvaCostManagement/PropertiesSerializer.cs
using System;
using System.Text;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Handles PHP-style serialization for properties field
    /// </summary>
    public static class PropertiesSerializer
    {
        /// <summary>
        /// Serialize properties to PHP format with exact byte-length calculations
        /// </summary>
        public static string SerializeProperties(string ifcType, string material, string dimension, string segmentType)
        {
            if (string.IsNullOrEmpty(ifcType))
                return "";

            var sb = new StringBuilder();
            sb.Append("a:4:{");

            // IFC Type
            AppendSerializedString(sb, "ifc_type", ifcType);

            // Material with DX_Pset prefix
            var materialKey = GetMaterialKey(ifcType);
            AppendSerializedString(sb, materialKey, material);

            // Dimension with DX_Pset prefix  
            var dimensionKey = GetDimensionKey(ifcType);
            AppendSerializedString(sb, dimensionKey, dimension);

            // Segment Type with DX_Pset prefix
            var segmentTypeKey = GetSegmentTypeKey(ifcType);
            AppendSerializedString(sb, segmentTypeKey, segmentType);

            sb.Append("}");

            return sb.ToString();
        }

        /// <summary>
        /// Append a serialized string with accurate UTF-8 byte length
        /// </summary>
        private static void AppendSerializedString(StringBuilder sb, string key, string value)
        {
            if (value == null) value = "";

            var keyBytes = Encoding.UTF8.GetByteCount(key);
            var valueBytes = Encoding.UTF8.GetByteCount(value);

            sb.Append($"s:{keyBytes}:\"{key}\";s:{valueBytes}:\"{value}\";");
        }

        /// <summary>
        /// Get material key based on IFC type
        /// </summary>
        private static string GetMaterialKey(string ifcType)
        {
            switch (ifcType.ToUpper())
            {
                case "IFCPIPESEGMENT":
                    return "DX_Pset_Pipe_Data.DX_Material";
                case "IFCWALL":
                    return "DX_Pset_Wall_Data.DX_Material";
                case "IFCBEAM":
                    return "DX_Pset_Beam_Data.DX_Material";
                case "IFCSLAB":
                    return "DX_Pset_Slab_Data.DX_Material";
                case "IFCDOOR":
                    return "DX_Pset_Door_Data.DX_Material";
                default:
                    return "DX_Pset_Element_Data.DX_Material";
            }
        }

        /// <summary>
        /// Get dimension key based on IFC type
        /// </summary>
        private static string GetDimensionKey(string ifcType)
        {
            switch (ifcType.ToUpper())
            {
                case "IFCPIPESEGMENT":
                    return "DX_Pset_Pipe_Data.DX_Dimension";
                case "IFCWALL":
                    return "DX_Pset_Wall_Data.DX_Dimension";
                case "IFCBEAM":
                    return "DX_Pset_Beam_Data.DX_Dimension";
                case "IFCSLAB":
                    return "DX_Pset_Slab_Data.DX_Dimension";
                case "IFCDOOR":
                    return "DX_Pset_Door_Data.DX_Dimension";
                default:
                    return "DX_Pset_Element_Data.DX_Dimension";
            }
        }

        /// <summary>
        /// Get segment type key based on IFC type
        /// </summary>
        private static string GetSegmentTypeKey(string ifcType)
        {
            switch (ifcType.ToUpper())
            {
                case "IFCPIPESEGMENT":
                    return "DX_Pset_Pipe_Data.DX_SegmentType";
                case "IFCWALL":
                    return "DX_Pset_Wall_Data.DX_WallType";
                case "IFCBEAM":
                    return "DX_Pset_Beam_Data.DX_BeamType";
                case "IFCSLAB":
                    return "DX_Pset_Slab_Data.DX_SlabType";
                case "IFCDOOR":
                    return "DX_Pset_Door_Data.DX_DoorType";
                default:
                    return "DX_Pset_Element_Data.DX_ElementType";
            }
        }

        /// <summary>
        /// Test serialization with sample data
        /// </summary>
        public static string TestSerialization()
        {
            return SerializeProperties("IFCPIPESEGMENT", "P235HTC1", "DN125", "DX_CarbonSteel_1.0345 - DIN EN 10216-2");
        }
    }
}