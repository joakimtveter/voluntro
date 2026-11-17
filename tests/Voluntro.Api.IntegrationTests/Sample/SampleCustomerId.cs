using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Sample;

public readonly record struct SampleCustomerId(Guid Value) : IEntityId<SampleCustomerId>
{
    public static SampleCustomerId New() => new(Guid.CreateVersion7());

    public static SampleCustomerId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
