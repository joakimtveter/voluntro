namespace VoluntroApi.Dtos.Groups;

public enum RestoreGroupResult
{
    Success,
    NotFound,
    AlreadyRestored,
    ParentDoesNotExist
}