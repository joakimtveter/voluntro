namespace Voluntro.ArchitectureTests;

/// <summary>
/// The integration event rules on made-up declarations, so every rule is proven to fire before a real module publishes
/// anything.
/// </summary>
public sealed class IntegrationEventRulesTests
{
    private const string IdentityContracts = "Voluntro.Identity.Contracts";
    private const string MembershipContracts = "Voluntro.Membership.Contracts";

    private static readonly Dictionary<string, string> Keys = new()
    {
        ["Identity"] = "identity",
        ["Membership"] = "membership",
        ["Checkin"] = "checkin",
    };

    [Theory]
    [InlineData("identity.user_registered.v1")]
    [InlineData("identity.registered.v1")]
    [InlineData("identity.two_factor_reset.v12")]
    [InlineData("identity.user2_registered.v3")]
    public void A_well_formed_name_with_the_modules_key_is_accepted(string eventType)
    {
        var events = new[] { new DeclaredEvent(IdentityContracts, "UserRegistered", eventType) };

        Assert.Empty(IntegrationEventRules.Violations(events, Keys));
    }

    [Theory]
    [InlineData("identity.user_registered")] // no version
    [InlineData("identity.user_registered.v0")] // versions start at 1
    [InlineData("identity.user_registered.v01")]
    [InlineData("identity.user_registered.V1")]
    [InlineData("identity.UserRegistered.v1")]
    [InlineData("identity.user-registered.v1")]
    [InlineData("identity.user__registered.v1")]
    [InlineData("identity.user_registered_.v1")]
    [InlineData("identity.users.registered.v1")] // one name segment only
    [InlineData("user_registered.v1")] // no module key
    [InlineData("Identity.user_registered.v1")]
    [InlineData(" identity.user_registered.v1")]
    [InlineData("")]
    public void A_malformed_name_is_a_violation(string eventType)
    {
        var events = new[] { new DeclaredEvent(IdentityContracts, "UserRegistered", eventType) };

        var violation = Assert.Single(IntegrationEventRules.Violations(events, Keys));
        Assert.Contains("UserRegistered", violation, StringComparison.Ordinal);
    }

    [Fact]
    public void A_name_with_another_modules_key_is_a_violation()
    {
        var events = new[] { new DeclaredEvent(MembershipContracts, "PersonCreated", "identity.person_created.v1") };

        var violation = Assert.Single(IntegrationEventRules.Violations(events, Keys));
        Assert.Contains("\"membership.\"", violation, StringComparison.Ordinal);
    }

    [Fact]
    public void The_key_is_the_modules_own_not_its_lowercased_name()
    {
        var keys = new Dictionary<string, string> { ["Identity"] = "auth" };
        var events = new[] { new DeclaredEvent(IdentityContracts, "UserRegistered", "identity.user_registered.v1") };

        Assert.Single(IntegrationEventRules.Violations(events, keys));
    }

    [Fact]
    public void An_event_in_a_module_without_an_IModule_is_a_violation()
    {
        var events = new[] { new DeclaredEvent("Voluntro.Tickets.Contracts", "TicketSold", "tickets.ticket_sold.v1") };

        var violation = Assert.Single(IntegrationEventRules.Violations(events, Keys));
        Assert.Contains("Tickets", violation, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Voluntro.Identity.Domain")]
    [InlineData("Voluntro.Identity.Application")]
    [InlineData("Voluntro.Identity.Infrastructure")]
    [InlineData("Voluntro.BuildingBlocks.Contracts")]
    [InlineData("Voluntro.SharedKernel")]
    public void An_event_outside_a_modules_Contracts_is_a_violation(string assembly)
    {
        var events = new[] { new DeclaredEvent(assembly, "UserRegistered", "identity.user_registered.v1") };

        var violation = Assert.Single(IntegrationEventRules.Violations(events, Keys));
        Assert.Contains("Contracts", violation, StringComparison.Ordinal);
    }

    [Fact]
    public void Two_events_with_the_same_name_are_a_violation_naming_both()
    {
        var events = new[]
        {
            new DeclaredEvent(IdentityContracts, "UserRegistered", "identity.user_registered.v1"),
            new DeclaredEvent(IdentityContracts, "UserSignedUp", "identity.user_registered.v1"),
        };

        var violation = Assert.Single(IntegrationEventRules.Violations(events, Keys));
        Assert.Contains("UserRegistered", violation, StringComparison.Ordinal);
        Assert.Contains("UserSignedUp", violation, StringComparison.Ordinal);
    }

    // A new version is a new event next to the old one, not a clash.
    [Fact]
    public void Two_versions_of_one_event_are_accepted()
    {
        var events = new[]
        {
            new DeclaredEvent(IdentityContracts, "UserRegistered", "identity.user_registered.v1"),
            new DeclaredEvent(IdentityContracts, "UserRegisteredV2", "identity.user_registered.v2"),
        };

        Assert.Empty(IntegrationEventRules.Violations(events, Keys));
    }
}
