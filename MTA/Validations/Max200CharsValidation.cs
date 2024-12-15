using System.ComponentModel.DataAnnotations;

namespace MTA.Validations
{
    public class Max200CharsValidation : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is string stringValue && stringValue.Length < 100)
            {
                return ValidationResult.Success;
            }
            return new ValidationResult("The content should less than 100");
        }
    }
}
