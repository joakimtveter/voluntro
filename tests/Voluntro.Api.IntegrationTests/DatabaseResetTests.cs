using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voluntro.Api.IntegrationTests.Database;
using Voluntro.Api.IntegrationTests.Sample;
using Voluntro.Api.IntegrationTests.Sample.Persistence;

namespace Voluntro.Api.IntegrationTests;

/// <summary>
/// Tests don't leak data into each other. Both tests save one order and expect to find exactly one:
/// whichever runs second fails unless the database is emptied in between.
/// </summary>
public sealed class DatabaseResetTests(PostgresDatabase database) : DatabaseTests(database)
{
    [Fact]
    public Task A_test_does_not_see_rows_from_earlier_tests() => SaveOneOrderAndExpectOnlyIt();

    [Fact]
    public Task Another_test_does_not_see_them_either() => SaveOneOrderAndExpectOnlyIt();

    private async Task SaveOneOrderAndExpectOnlyIt()
    {
        await SaveAsync<SampleDbContext>(SampleOrder.Create("Kari Nordmann", note: null));

        await using var scope = CreateScope();
        var count = await scope.ServiceProvider.GetRequiredService<SampleDbContext>().Orders.CountAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, count);
    }
}
