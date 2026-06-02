namespace VoluntroApi.Dtos.Groups;

/// <summary>
/// Represents the outcome of a group delete operation.
/// </summary>
public enum DeleteGroupResult
{
    /// <summary>The group was soft-deleted successfully.</summary>
    Success,

    /// <summary>No active group was found with the given identifier.</summary>
    NotFound,

    /// <summary>The group has active child groups and cannot be deleted until they are removed or re-parented.</summary>
    HasChildren
}