using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Sample;

public static class SampleErrors
{
    public static readonly Error ItemNotFound = Error.NotFound("sample.item.not_found");
}
