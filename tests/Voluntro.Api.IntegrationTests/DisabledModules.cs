using Voluntro.BuildingBlocks.Infrastructure;

namespace Voluntro.Api.IntegrationTests;

/// <summary>An activation source that reports the given modules as disabled and every other module as enabled.</summary>
public sealed class DisabledModules(params string[] disabledKeys) : IModuleActivation
{
    public bool IsEnabled(string moduleKey) => !disabledKeys.Contains(moduleKey, StringComparer.Ordinal);
}
