using System;
using System.Text;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Handles PHP-style serialization for properties field using SPEC parameters
    /// </summary>
    public static class PropertiesSerializer
    {
        /// <summary>
        /// Serialize SPEC properties to PHP format with exact byte-length calculations
        /// </summary>
        public static string SerializeSpecProperties(
            string specFilter,
            string specName,
            string specSize,
            string specType,
            string specManufacturer,
            string specMaterial)
        {
            var sb = new StringBuilder();

            // Count non-empty fields
            int fieldCount = 0;
            if (!string.IsNullOrEmpty(specFilter)) fieldCount++;
            if (!string.IsNullOrEmpty(specName)) fieldCount++;
            if (!string.IsNullOrEmpty(specSize)) fieldCount++;
            if (!string.IsNullOrEmpty(specType)) fieldCount++;
            if (!string.IsNullOrEmpty(specManufacturer)) fieldCount++;
            if (!string.IsNullOrEmpty(specMaterial)) fieldCount++;

            if (fieldCount == 0)
                return "";

            sb.Append($"a:{fieldCount}:{{");

            // Add each field if not empty
            if (!string.IsNullOrEmpty(specName))
                AppendSerializedString(sb, "DX.SPEC_Name", specName);

            if (!string.IsNullOrEmpty(specSize))
                AppendSerializedString(sb, "DX.SPEC_Size", specSize);

            if (!string.IsNullOrEmpty(specType))
                AppendSerializedString(sb, "DX.SPEC_Type", specType);

            if (!string.IsNullOrEmpty(specFilter))
                AppendSerializedString(sb, "DX.SPEC_filter", specFilter);

            if (!string.IsNullOrEmpty(specManufacturer))
                AppendSerializedString(sb, "DX.SPEC_Manufacturer", specManufacturer);

            if (!string.IsNullOrEmpty(specMaterial))
                AppendSerializedString(sb, "DX.SPEC_Material", specMaterial);

            sb.Append("}");

            return sb.ToString();
        }

        /// <summary>
        /// Serialize properties from CostElement
        /// </summary>
        public static string SerializeFromElement(CostElement element)
        {
            return SerializeSpecProperties(
                element.SpecFilter ?? "",
                element.SpecName ?? "",
                element.SpecSize ?? "",
                element.SpecType ?? "",
                element.SpecManufacturer ?? "",
                element.SpecMaterial ?? ""
            );
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
        /// Test serialization with sample data
        /// </summary>
        public static string TestSerialization()
        {
            return SerializeSpecProperties(
                "NO",
                "3-Piece Ball Valve",
                "1/4\"",
                "H6800",
                "HAM-LET",
                "SS316L"
            );
        }
    }
}