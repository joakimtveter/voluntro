using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Voluntro.BuildingBlocks.Application;
using Voluntro.BuildingBlocks.Contracts;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// Publishing through a module context's <see cref="IOutbox"/>: the message it adds to the change tracker, and the
/// tables every module context maps. No database: the context's save only records, so the audit interceptor, which
/// stamps the message's time, is covered by <c>OutboxPublishingTests</c>. The message type is internal, so it is read
/// through the change tracker and the model, by property name.
/// </summary>
public sealed class OutboxTests
{
    [Fact]
    public void A_published_event_is_added_as_an_outbox_message_with_its_type()
    {
        using var provider = Build();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();

        ((IOutbox)context).Add(new Happened(Guid.Empty, "Kari", Mood.VeryHappy));

        var message = Assert.Single(context.ChangeTracker.Entries());
        Assert.Equal("outbox", message.Metadata.GetTableName());
        Assert.Equal(EntityState.Added, message.State);
        Assert.Equal(Happened.EventType, message.Property("Type").CurrentValue);
    }

    [Fact]
    public void The_message_id_is_a_new_version_7_guid()
    {
        using var provider = Build();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();

        ((IOutbox)context).Add(new Happened(Guid.Empty, "Kari", Mood.VeryHappy));
        ((IOutbox)context).Add(new Happened(Guid.Empty, "Kari", Mood.VeryHappy));

        var ids = context.ChangeTracker.Entries().Select(entry => (Guid)entry.Property("Id").CurrentValue!).ToList();
        Assert.All(ids, id => Assert.Equal(7, id.Version));
        Assert.NotEqual(ids[0], ids[1]);
    }

    // The outbox's own fixed format, not the API's JsonConventions.
    [Fact]
    public void The_payload_is_camelCase_json_with_enums_as_camelCase_strings()
    {
        using var provider = Build();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
        var id = Guid.CreateVersion7();

        ((IOutbox)context).Add(new Happened(id, "Kari", Mood.VeryHappy));

        var payload = (string)Assert.Single(context.ChangeTracker.Entries()).Property("Payload").CurrentValue!;
        using var json = JsonDocument.Parse(payload);
        Assert.Equal(id, json.RootElement.GetProperty("personId").GetGuid());
        Assert.Equal("Kari", json.RootElement.GetProperty("name").GetString());
        Assert.Equal("veryHappy", json.RootElement.GetProperty("mood").GetString());
    }

    [Fact]
    public void The_message_carries_the_trace_of_the_activity_that_published_it()
    {
        using var provider = Build();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
        using var activity = new Activity("publish").SetIdFormat(ActivityIdFormat.W3C).Start();

        ((IOutbox)context).Add(new Happened(Guid.Empty, "Kari", Mood.VeryHappy));

        var traceParent = (string?)Assert.Single(context.ChangeTracker.Entries()).Property("TraceParent").CurrentValue;
        Assert.Equal(activity.Id, traceParent);
        Assert.StartsWith($"00-{activity.TraceId}-", traceParent, StringComparison.Ordinal);
    }

    // The most common way to publish: a domain event handler reacting to the command's change. Its message is in the
    // tracker before the one save, so it commits with the change.
    [Fact]
    public async Task An_event_published_by_a_domain_event_handler_is_saved_with_the_command()
    {
        using var provider = Build(handlers => handlers.AddDomainEventHandler<Noticed, PublishingHandler>());
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
        var person = new Person();
        context.Add(person);
        person.Notice();

        await UnitOfWork(scope).SaveChangesAsync(TestContext.Current.CancellationToken);

        var message = Assert.Single(OutboxEntries(context));
        Assert.Equal(Happened.EventType, message.Property("Type").CurrentValue);
        Assert.Equal(EntityState.Added, message.State);
        Assert.Equal(["save"], context.Saves);
    }

    // An event published before a failure is discarded with the rest of the command (TransactionDecorator).
    [Fact]
    public async Task A_failed_dispatch_saves_nothing()
    {
        using var provider = Build(handlers => handlers
            .AddDomainEventHandler<Noticed, PublishingHandler>()
            .AddDomainEventHandler<Noticed, RefusingHandler>());
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
        var person = new Person();
        context.Add(person);
        person.Notice();

        var result = await UnitOfWork(scope).SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Empty(context.Saves);
    }

    [Fact]
    public void Every_module_context_maps_an_outbox_table_in_its_schema()
    {
        using var provider = Build();
        using var scope = provider.CreateScope();
        var outbox = Table(scope, "outbox");

        Assert.Equal("outbox_schema", outbox.GetSchema());
        Assert.Equal(["id"], outbox.FindPrimaryKey()!.Properties.Select(ColumnName));
        Assert.Equal(ValueGenerated.Never, outbox.FindPrimaryKey()!.Properties[0].ValueGenerated);
        Assert.Equal("jsonb", outbox.FindProperty("Payload")!.GetColumnType());
        Assert.Equal(
            ["attempts", "failed_at", "id", "next_attempt_at", "occurred_at", "payload", "processed_at", "trace_parent", "type"],
            outbox.GetProperties().Select(ColumnName).Order());

        var index = Assert.Single(outbox.GetIndexes());
        Assert.Equal(["next_attempt_at"], index.Properties.Select(ColumnName));
        Assert.Equal("processed_at IS NULL AND failed_at IS NULL", index.GetFilter());
    }

    [Fact]
    public void Every_module_context_maps_an_inbox_table_keyed_by_message_and_consumer()
    {
        using var provider = Build();
        using var scope = provider.CreateScope();
        var inbox = Table(scope, "inbox");

        Assert.Equal("outbox_schema", inbox.GetSchema());
        Assert.Equal(["message_id", "consumer"], inbox.FindPrimaryKey()!.Properties.Select(ColumnName));
        Assert.Equal(["consumer", "message_id", "processed_at"], inbox.GetProperties().Select(ColumnName).Order());
    }

    private static string ColumnName(IReadOnlyProperty property) =>
        property.GetColumnName(StoreObjectIdentifier.Table(property.DeclaringType.GetTableName()!, property.DeclaringType.GetSchema()))!;

    private static IReadOnlyEntityType Table(IServiceScope scope, string name)
    {
        var context = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
        var model = context.GetService<IDesignTimeModel>().Model;
        return Assert.Single(model.GetEntityTypes(), entityType => entityType.GetTableName() == name);
    }

    private static IEnumerable<EntityEntry> OutboxEntries(DbContext context) =>
        context.ChangeTracker.Entries().Where(entry => entry.Metadata.GetTableName() == "outbox");

    private static ServiceProvider Build(Action<ModuleHandlerRegistrar<OutboxDbContext>>? register = null)
    {
        var services = new ServiceCollection();
        services.AddDbContext<OutboxDbContext>(options => options.UseNpgsql("Host=never-connected"));
        var handlers = services.AddHandlers<OutboxDbContext>();
        register?.Invoke(handlers);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    private static IUnitOfWork UnitOfWork(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(typeof(OutboxDbContext));

    private enum Mood
    {
        Calm,
        VeryHappy,
    }

    private sealed record Happened(Guid PersonId, string Name, Mood Mood) : IIntegrationEvent
    {
        public static string EventType => "test.something_happened.v1";
    }

    private sealed record Noticed : IDomainEvent;

    private sealed class PublishingHandler(OutboxDbContext db) : IDomainEventHandler<Noticed>
    {
        public Task<Result> HandleAsync(Noticed domainEvent, CancellationToken cancellationToken)
        {
            ((IOutbox)db).Add(new Happened(Guid.Empty, "Kari", Mood.Calm));
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class RefusingHandler : IDomainEventHandler<Noticed>
    {
        public Task<Result> HandleAsync(Noticed domainEvent, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Failure(Error.Conflict("test.person.refused")));
    }

    private readonly record struct PersonId(Guid Value) : IEntityId<PersonId>
    {
        public static PersonId New() => new(Guid.CreateVersion7());

        public static PersonId From(Guid value) => new(value);
    }

    private sealed class Person : AggregateRoot<PersonId>
    {
        public Person()
            : base(PersonId.New())
        {
        }

        public void Notice() => Raise(new Noticed());
    }

    /// <summary>A module context whose save records instead of reaching a database.</summary>
    private sealed class OutboxDbContext(DbContextOptions<OutboxDbContext> options) : ModuleDbContext(options)
    {
        public List<string> Saves { get; } = [];

        public DbSet<Person> People => Set<Person>();

        protected override string Schema => "outbox_schema";

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            Saves.Add("save");
            return Task.FromResult(0);
        }

        protected override void ConfigureModel(ModelBuilder modelBuilder) => modelBuilder.Entity<Person>();
    }
}
