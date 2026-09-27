using FluentValidation;
using TestJob.Models;

namespace TestJob.Validators;

public class RequestDtoValidator : AbstractValidator<RequestDto>
{
    public RequestDtoValidator()
    {
        RuleFor(x => x.Selector).NotEmpty();
        RuleFor(x => x.Attribute).NotEmpty();
        RuleFor(x => x.UrlB64).NotEmpty();
        RuleFor(x => x.EncryptedTextBytesB64).NotEmpty();
        RuleFor(x => x.KeyBytesB64).NotEmpty();
        RuleFor(x => x.PageB64).NotEmpty();
    }
}
