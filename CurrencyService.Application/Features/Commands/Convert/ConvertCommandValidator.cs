using FluentValidation;

namespace CurrencyService.Application.Features.Commands.Convert;

public sealed class ConvertCommandValidator : AbstractValidator<ConvertCommand>
{
    public ConvertCommandValidator()
    {

    }
}
