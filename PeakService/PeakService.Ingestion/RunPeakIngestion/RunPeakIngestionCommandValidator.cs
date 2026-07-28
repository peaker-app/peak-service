using FluentValidation;

namespace PeakService.Ingestion.RunPeakIngestion;

internal sealed class RunPeakIngestionCommandValidator : AbstractValidator<RunPeakIngestionCommand>
{
    public RunPeakIngestionCommandValidator() => RuleFor(command => command.Mode).IsInEnum();
}
