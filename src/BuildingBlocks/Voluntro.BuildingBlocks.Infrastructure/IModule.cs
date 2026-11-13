using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Voluntro.BuildingBlocks.Infrastructure;

/// <summary>
/// A module's entry point, implemented once in each module's Infrastructure project and registered
/// in <c>Program.cs</c> with <see cref="ModuleServiceCollectionExtensions.AddModules"/>.
/// The module's controllers are the ones in the same assembly as its <see cref="IModule"/>.
/// </summary>
public interface IModule
{
    /// <summary>Stable lowercase key, e.g. <c>"checkin"</c>; also the first segment of the module's error codes.</summary>
    string Key { get; }

    /// <summary>Core modules are always enabled; optional ones can be switched on and off by an Administrator.</summary>
    bool IsCore { get; }

    /// <summary>Keys of the modules this one needs enabled.</summary>
    IReadOnlyList<string> DependsOn { get; }

    /// <summary>Registers the module's services: handlers, DbContext, Contracts implementations, consumers.</summary>
    void Register(IServiceCollection services, IConfiguration config);
}
