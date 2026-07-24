using FluentValidation;

namespace PeakService.Application.Ingestion.RunPeakIngestion;

internal sealed class RunPeakIngestionCommandValidator : AbstractValidator<RunPeakIngestionCommand>
{
    public RunPeakIngestionCommandValidator() => RuleFor(command => command.Mode).IsInEnum();
}
