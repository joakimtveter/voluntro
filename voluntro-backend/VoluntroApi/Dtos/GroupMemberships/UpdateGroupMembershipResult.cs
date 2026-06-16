namespace VoluntroApi.Dtos.GroupMemberships;

public enum UpdateGroupMembershipResult
{
    Success,
    GroupNotFound,
    MemberNotFound,
    AlreadyMember,
    NotMember,
}
