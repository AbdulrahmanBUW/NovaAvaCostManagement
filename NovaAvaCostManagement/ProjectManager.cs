using System;
using System.Collections.Generic;

namespace NovaAvaCostManagement
{
    /// <summary>
    /// Simplified validation result
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();

        public bool HasErrors => Errors.Count > 0;
        public bool HasWarnings => Warnings.Count > 0;
        public bool IsValid => !HasErrors;
    }

    /// <summary>
    /// Basic validator - validates only user-editable fields
    /// </summary>
    public static class SimpleValidator
    {
        public static ValidationResult ValidateElements(List<CostElement> elements)
        {
            var result = new ValidationResult();

            foreach (var element in elements)
            {
                var errors = element.Validate();
                foreach (var error in errors)
                {
                    result.Errors.Add($"Element {element.Id}: {error}");
                }

                // Check for duplicate IDs
                var duplicates = elements.FindAll(e => e.Id == element.Id);
                if (duplicates.Count > 1)
                {
                    result.Warnings.Add($"Duplicate ID found: {element.Id}");
                }

                // Check for duplicate GUIDs
                if (!string.IsNullOrEmpty(element.Ident))
                {
                    var guidDuplicates = elements.FindAll(e => e.Ident == element.Ident);
                    if (guidDuplicates.Count > 1)
                    {
                        result.Errors.Add($"Duplicate GUID found: {element.Ident}");
                    }
                }
            }

            return result;
        }
    }
}