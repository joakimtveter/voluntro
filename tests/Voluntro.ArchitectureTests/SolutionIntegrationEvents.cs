using System.Reflection;
using Voluntro.BuildingBlocks.Contracts;
using Voluntro.BuildingBlocks.Infrastructure;

namespace Voluntro.ArchitectureTests;

/// <summary>
/// The integration events and module keys in the solution's assemblies. Unlike the reference graph, this needs the
/// assemblies loaded: an event's name is the value of a static property, and a module's key the value of an instance
/// property, which metadata alone can't give.
/// </summary>
public static class SolutionIntegrationEvents
{
    private static readonly MethodInfo _eventTypeOf =
        typeof(SolutionIntegrationEvents).GetMethod(nameof(EventTypeOf), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>Every integration event declared in the solution's Voluntro assemblies.</summary>
    public static IReadOnlyList<DeclaredEvent> Events =>
        SolutionAssemblies.Graph.Keys.Select(Load).SelectMany(Declared).ToList();

    /// <summary>Module name → <c>IModule.Key</c>, from each module's Infrastructure assembly.</summary>
    public static IReadOnlyDictionary<string, string> ModuleKeys =>
        SolutionAssemblies.Graph.Keys
            .Select(name => (Name: name, Kind: AssemblyClassifier.Classify(name)))
            .Where(assembly => assembly.Kind is ModuleAssembly { Layer: Layer.Infrastructure })
            .SelectMany(assembly => Load(assembly.Name).GetTypes()
                .Where(type => type is { IsAbstract: false, IsGenericTypeDefinition: false }
                    && typeof(IModule).IsAssignableFrom(type)
                    && type.GetConstructor(Type.EmptyTypes) is not null)
                .Select(type => (((ModuleAssembly)assembly.Kind).Module, Key: ((IModule)Activator.CreateInstance(type)!).Key)))
            .ToDictionary(module => module.Module, module => module.Key);

    /// <summary>The integration events declared in one assembly, with the names they declare.</summary>
    public static IEnumerable<DeclaredEvent> Declared(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsGenericTypeDefinition: false }
                && typeof(IIntegrationEvent).IsAssignableFrom(type))
            .Select(type => new DeclaredEvent(
                assembly.GetName().Name!,
                type.FullName!,
                (string)_eventTypeOf.MakeGenericMethod(type).Invoke(null, null)!));
    }

    private static Assembly Load(string name) => Assembly.Load(new AssemblyName(name));

    // Reads the static abstract member the way the compiler does, so an explicit implementation works too.
    private static string EventTypeOf<TEvent>()
        where TEvent : IIntegrationEvent =>
        TEvent.EventType;
}
