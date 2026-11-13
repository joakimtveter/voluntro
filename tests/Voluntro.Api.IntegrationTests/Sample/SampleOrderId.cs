using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Sample;

public readonly record struct SampleOrderId(Guid Value) : IEntityId<SampleOrderId>
{
    public static SampleOrderId New() => new(Guid.CreateVersion7());

    public static SampleOrderId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
