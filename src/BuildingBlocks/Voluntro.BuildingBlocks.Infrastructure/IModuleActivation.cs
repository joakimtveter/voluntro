namespace Voluntro.BuildingBlocks.Infrastructure;

/// <summary>
/// Says whether a module is enabled. The single authority: an implementation reports core modules as enabled itself.
/// Called on every request to a module's endpoint, so it reads an in-memory cache and is synchronous.
/// </summary>
public interface IModuleActivation
{
    bool IsEnabled(string moduleKey);
}

/// <summary>The default until Platform stores real activation settings: every module is enabled.</summary>
internal sealed class AllModulesEnabled : IModuleActivation
{
    public bool IsEnabled(string moduleKey) => true;
}
