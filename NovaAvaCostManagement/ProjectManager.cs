using System;
using System.Collections.Generic;

namespace NovaAvaCostManagement
{
    public class ValidationResult
    {
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();

        public bool HasErrors => Errors.Count > 0;
        public bool HasWarnings => Warnings.Count > 0;
        public bool IsValid => !HasErrors;
    }

    public static class SimpleValidator
    {
        public static ValidationResult ValidateElements(List<CostElement> elements)
        {
            var result = new ValidationResult();

            foreach (var element in elements)
            {
                ValidateElement(element, elements, result);
            }

            return result;
        }

        private static void ValidateElement(CostElement element, List<CostElement> allElements, ValidationResult result)
        {
            var errors = element.Validate();
            foreach (var error in errors)
            {
                result.Errors.Add($"Element {element.Id}: {error}");
            }

            CheckForDuplicateIds(element, allElements, result);
            CheckForDuplicateGuids(element, allElements, result);
        }

        private static void CheckForDuplicateIds(CostElement element, List<CostElement> allElements, ValidationResult result)
        {
            var duplicates = allElements.FindAll(e => e.Id == element.Id);
            if (duplicates.Count > 1)
            {
                result.Warnings.Add($"Duplicate ID found: {element.Id}");
            }
        }

        private static void CheckForDuplicateGuids(CostElement element, List<CostElement> allElements, ValidationResult result)
        {
            if (!string.IsNullOrEmpty(element.Ident))
            {
                var guidDuplicates = allElements.FindAll(e => e.Ident == element.Ident);
                if (guidDuplicates.Count > 1)
                {
                    result.Errors.Add($"Duplicate GUID found: {element.Ident}");
                }
            }
        }
    }
}