namespace Voluntro.ArchitectureTests;

/// <summary>
/// Classifies an assembly by name alone, so the layering rules can be tested on made-up names
/// before any real module exists. Matching is exact and case-sensitive.
/// </summary>
public static class AssemblyClassifier
{
    private const string Root = "Voluntro";
    private const string SharedKernel = "SharedKernel";
    private const string BuildingBlocks = "BuildingBlocks";

    // Names that belong to other parts of the solution. They match the module-name shape,
    // so without this list "Voluntro.Providers.Infrastructure" would classify as a module.
    private static readonly HashSet<string> _reservedOwners =
        [SharedKernel, BuildingBlocks, "Providers", "Api", "AppHost", "ServiceDefaults"];

    public static AssemblyKind Classify(string assemblyName)
    {
        ArgumentNullException.ThrowIfNull(assemblyName);

        var segments = assemblyName.Split('.');

        if (segments is [Root, SharedKernel])
        {
            return new SharedKernelAssembly();
        }

        if (segments is not [Root, var owner, var layerName] || ParseLayer(layerName) is not { } layer)
        {
            return new UnclassifiedAssembly();
        }

        // Checked before the module branch: "BuildingBlocks" would otherwise pass as a module name.
        if (owner == BuildingBlocks)
        {
            return layer == Layer.Domain ? new UnclassifiedAssembly() : new BuildingBlockAssembly(layer);
        }

        return IsModuleName(owner) ? new ModuleAssembly(owner, layer) : new UnclassifiedAssembly();
    }

    // A switch rather than Enum.TryParse, which would also accept numbers ("1") and ignore surrounding whitespace.
    private static Layer? ParseLayer(string name) => name switch
    {
        nameof(Layer.Domain) => Layer.Domain,
        nameof(Layer.Application) => Layer.Application,
        nameof(Layer.Infrastructure) => Layer.Infrastructure,
        nameof(Layer.Contracts) => Layer.Contracts,
        _ => null,
    };

    private static bool IsModuleName(string name) =>
        name.Length > 0
        && char.IsAsciiLetterUpper(name[0])
        && name.All(char.IsAsciiLetterOrDigit)
        && !_reservedOwners.Contains(name);
}
