using System.ComponentModel.DataAnnotations;


namespace CosmicBot.DAL.Validation
{
    public class NotEmptyAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var fieldName = validationContext.MemberName;
            if (value is null || (value is string str && string.IsNullOrWhiteSpace(str)))
            {
                return new ValidationResult($"{fieldName} cannot be empty or whitespace.");
            }
            return ValidationResult.Success;
        }
    }
}
