using System.Text.RegularExpressions;

namespace Voluntro.ArchitectureTests;

/// <summary>An integration event type found in an assembly, with the name it declares.</summary>
public sealed record DeclaredEvent(string Assembly, string TypeName, string EventType);

/// <summary>
/// The rules for integration event names (ADR-0005), as pure functions over made-up declarations, like the layering
/// rules: every rule is proven to fire before any real module publishes an event.
/// </summary>
public static partial class IntegrationEventRules
{
    /// <summary>
    /// Every violation among <paramref name="events"/>. An event must be declared in a module's Contracts, be named
    /// <c>{moduleKey}.{snake_case}.v{N}</c> with the key of the module that declares it, and have a name no other event
    /// has.
    /// </summary>
    /// <param name="moduleKeys">Module name (the assembly segment, e.g. <c>Membership</c>) → its <c>IModule.Key</c>.</param>
    public static IReadOnlyList<string> Violations(
        IReadOnlyCollection<DeclaredEvent> events,
        IReadOnlyDictionary<string, string> moduleKeys)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(moduleKeys);

        var violations = new List<string>();

        foreach (var declared in events)
        {
            var where = $"{declared.TypeName} ({declared.Assembly}) is \"{declared.EventType}\"";

            if (AssemblyClassifier.Classify(declared.Assembly) is not ModuleAssembly { Layer: Layer.Contracts } owner)
            {
                violations.Add($"{where}: integration events belong in a module's Contracts.");
                continue;
            }

            var match = EventTypeFormat().Match(declared.EventType);
            if (!match.Success)
            {
                violations.Add($"{where}: not {{moduleKey}}.{{snake_case}}.v{{N}}, e.g. identity.user_registered.v1.");
                continue;
            }

            if (!moduleKeys.TryGetValue(owner.Module, out var key))
            {
                violations.Add($"{where}: module {owner.Module} has no IModule to take its key from.");
            }
            else if (match.Groups["module"].Value != key)
            {
                violations.Add($"{where}: must start with its module's key, \"{key}.\".");
            }
        }

        violations.AddRange(events
            .GroupBy(declared => declared.EventType, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group =>
                $"\"{group.Key}\" is declared more than once: {string.Join(", ", group.Select(declared => declared.TypeName))}."));

        return violations;
    }

    // The key and the name are snake_case words; the version is a positive number without leading zeros.
    [GeneratedRegex(@"^(?<module>[a-z][a-z0-9]*(_[a-z0-9]+)*)\.[a-z][a-z0-9]*(_[a-z0-9]+)*\.v[1-9][0-9]*$")]
    private static partial Regex EventTypeFormat();
}
