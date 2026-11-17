using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Sample;

public static class SampleErrors
{
    public static readonly Error ItemNotFound = Error.NotFound("sample.item.not_found");

    public static readonly Error OrderNotFound = Error.NotFound("sample.order.not_found");

    public static readonly Error OrderEmpty = Error.Conflict("sample.order.empty", "An order without lines can't be placed.");
}
