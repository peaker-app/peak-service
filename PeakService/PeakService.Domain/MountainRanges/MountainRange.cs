using Common.Domain.Abstractions;
using Common.Domain.Results;

namespace PeakService.Domain.MountainRanges;

public sealed class MountainRange : AggregateRoot
{
    public const int MaxNameLength = 200;

    private MountainRange()
    {
    }

    private MountainRange(Guid id, string name, Guid? parentRangeId) : base(id)
    {
        Name = name;
        ParentRangeId = parentRangeId;
    }

    public string Name { get; private set; } = null!;

    public Guid? ParentRangeId { get; private set; }

    public static Result<MountainRange> Create(string name, Guid? parentRangeId)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLength)
        {
            return MountainRangeErrors.NameInvalid;
        }

        return new MountainRange(Guid.CreateVersion7(), name.Trim(), parentRangeId);
    }
}
