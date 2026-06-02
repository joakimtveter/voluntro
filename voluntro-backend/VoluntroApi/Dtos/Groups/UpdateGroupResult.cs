namespace VoluntroApi.Dtos.Groups;

/// <summary>
/// Represents the outcome of a group update operation.
/// </summary>
public enum UpdateGroupResult
{
    /// <summary>The group was updated successfully.</summary>
    Success,

    /// <summary>No active group was found with the given identifier.</summary>
    NotFound,

    /// <summary>The specified parent group does not exist or has been deleted.</summary>
    ParentNotFound,

    /// <summary>Setting the specified parent would create a circular reference in the group hierarchy.</summary>
    CycleDetected
}