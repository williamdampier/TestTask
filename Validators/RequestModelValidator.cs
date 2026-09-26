using FluentValidation;
using TestTask.Models;

namespace TestTask.Validators
{
    public class RequestModelValidator : AbstractValidator<RequestModel>
    {
        public RequestModelValidator() 
        {
            RuleFor(x => x.Selector)
                .NotEmpty()
                .WithMessage("Selector is required.");

            RuleFor(x => x.Attribute)
                .NotEmpty()
                .WithMessage("Attribute is required.");

            RuleFor(x => x.UrlBase64)
                .NotEmpty()
                .WithMessage("UrlBase64 is required.");

            RuleFor(x => x.EncryptedTextBytesBase64)
                .NotEmpty()
                .WithMessage("EncryptedTextBytesBase64 is required.");

            RuleFor(x => x.KeyBytesBase64)
                .NotEmpty()
                .WithMessage("KeyBytesBase64 is required.");

            RuleFor(x => x.PageBase64)
                .NotEmpty()
                .WithMessage("PageBase64 is required.");
        }
    }
}
