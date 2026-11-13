using Voluntro.BuildingBlocks.Application;

namespace Voluntro.BuildingBlocks.Infrastructure;

/// <summary>
/// Acts as the System user. Registered with <c>TryAdd</c> until Identity registers the signed-in user instead.
/// </summary>
internal sealed class SystemCurrentUser : ICurrentUser
{
    public Guid UserId => SystemUser.Id;
}
