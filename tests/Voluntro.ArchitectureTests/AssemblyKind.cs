namespace Voluntro.ArchitectureTests;

/// <summary>What role a Voluntro assembly plays in the layering, derived from its name.</summary>
public abstract record AssemblyKind;

/// <summary><c>Voluntro.SharedKernel</c>.</summary>
public sealed record SharedKernelAssembly : AssemblyKind;

/// <summary><c>Voluntro.BuildingBlocks.{Layer}</c>; there is no BuildingBlocks Domain layer.</summary>
public sealed record BuildingBlockAssembly(Layer Layer) : AssemblyKind;

/// <summary><c>Voluntro.{Module}.{Layer}</c> with a single-segment PascalCase module name.</summary>
public sealed record ModuleAssembly(string Module, Layer Layer) : AssemblyKind;

/// <summary>A name that fits none of the conventions above.</summary>
public sealed record UnclassifiedAssembly : AssemblyKind;
