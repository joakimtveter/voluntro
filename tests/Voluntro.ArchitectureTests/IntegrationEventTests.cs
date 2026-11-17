using Voluntro.BuildingBlocks.Contracts;

namespace Voluntro.ArchitectureTests;

/// <summary>The integration event rules against the real solution, and the loader that feeds them.</summary>
public sealed class IntegrationEventTests
{
    [Fact]
    public void Integration_event_names_are_well_formed_owned_by_their_module_and_unique()
    {
        var violations = IntegrationEventRules.Violations(
            SolutionIntegrationEvents.Events,
            SolutionIntegrationEvents.ModuleKeys);

        if (violations.Count > 0)
        {
            Assert.Fail(
                $"{violations.Count} integration event naming violation(s):"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
        }
    }

    // No module publishes yet, so the loader is proven on events declared here.
    [Fact]
    public void The_loader_reads_each_events_declared_name()
    {
        var declared = SolutionIntegrationEvents.Declared(typeof(IntegrationEventTests).Assembly).ToList();

        Assert.Contains(declared, e => e.TypeName == typeof(Implicit).FullName && e.EventType == "test.implicit_name.v1");
        Assert.Contains(declared, e => e.TypeName == typeof(Explicit).FullName && e.EventType == "test.explicit_name.v1");
        Assert.All(declared, e => Assert.Equal("Voluntro.ArchitectureTests", e.Assembly));
    }

    [Fact]
    public void The_loader_skips_abstract_events()
    {
        var declared = SolutionIntegrationEvents.Declared(typeof(IntegrationEventTests).Assembly);

        Assert.DoesNotContain(declared, e => e.TypeName == typeof(AbstractEvent).FullName);
    }

    private sealed record Implicit : IIntegrationEvent
    {
        public static string EventType => "test.implicit_name.v1";
    }

    private sealed record Explicit : IIntegrationEvent
    {
        static string IIntegrationEvent.EventType => "test.explicit_name.v1";
    }

    private abstract record AbstractEvent : IIntegrationEvent
    {
        public static string EventType => "test.abstract.v1";
    }
}
