using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Voluntro.Api.IntegrationTests.Database;

/// <summary>
/// Base class for every test that touches the database. It puts the test in the <see cref="DatabaseCollection"/>
/// (xUnit inherits the attribute) and empties the module tables <em>before</em> each test, so every test starts
/// clean and a failed test's rows stay for inspection until the next one starts.
/// </summary>
[Collection<DatabaseCollection>]
public abstract class DatabaseTests(PostgresDatabase database) : IAsyncLifetime
{
    /// <summary>The API, connected to the test database.</summary>
    protected VoluntroApiFactory Api => database.Api;

    public async ValueTask InitializeAsync() => await database.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    /// <summary>A scope of its own, as one request would get; resolve a DbContext from it.</summary>
    protected AsyncServiceScope CreateScope() => Api.Services.CreateAsyncScope();

    /// <summary>Adds the entities and saves them in a scope of their own, as an earlier request would have.</summary>
    protected async Task SaveAsync<TContext>(params object[] entities)
        where TContext : DbContext
    {
        await using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();

        context.AddRange(entities);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
