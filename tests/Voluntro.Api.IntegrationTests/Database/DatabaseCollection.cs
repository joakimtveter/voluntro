using System.Diagnostics.CodeAnalysis;

namespace Voluntro.Api.IntegrationTests.Database;

/// <summary>
/// The tests that share <see cref="PostgresDatabase"/>. xUnit runs collections in parallel but the tests inside one
/// collection one at a time, so database tests can't empty the tables under each other. Join it by deriving from
/// <see cref="DatabaseTests"/>.
/// </summary>
[CollectionDefinition]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit's naming for collection definitions.")]
public sealed class DatabaseCollection : ICollectionFixture<PostgresDatabase>;
