using Xunit;

namespace PeakService.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class PeakServiceCollection : ICollectionFixture<PeakServiceApiFactory>
{
    public const string Name = "PeakService";
}
