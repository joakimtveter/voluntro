namespace Voluntro.BuildingBlocks.Infrastructure;

/// <summary>
/// The body of every <c>201 Created</c>: the new resource's ID. Its address is in the <c>Location</c> header.
/// Returned through <see cref="ApiController.CreatedAt(string, Guid)"/> (or its overload for a child resource), so
/// the shape can't drift between modules.
/// </summary>
public sealed record CreatedResponse(Guid Id);
