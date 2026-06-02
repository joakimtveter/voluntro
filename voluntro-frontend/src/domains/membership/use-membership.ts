import { queryOptions, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import type { GroupBrief } from "#/domains/groups/group.types.ts";
import type { MemberBrief } from "#/domains/members/member.types.ts";
import type { MembershipPayload } from "#/domains/membership/membership.schema.ts";
import type { MembershipResponse } from "#/domains/membership/membership.types.ts";
import { ALL_GROUPS, GROUPS_MEMBERS, SINGLE_MEMBER } from "#/shared/constants/query-keys.ts";
import type { ApiError } from "#/shared/lib/fetch/api-error.ts";
import { apiFetch } from "#/shared/lib/fetch/api-fetch.ts";
import type { PaginatedList, Pagination } from "#/shared/types/api.types.ts";

async function getGroupMembers(groupId: string, pagination: Pagination) {
  return await apiFetch<PaginatedList<MemberBrief>>(`/memberships/group/${groupId}`, {
    query: pagination,
  });
}
export function useGroupMembersQueryOptions(
  groupId: string,
  pagination: Pagination,
  enabled: boolean,
) {
  return queryOptions({
    queryKey: [GROUPS_MEMBERS, { groupId }, pagination],
    queryFn: () => getGroupMembers(groupId, pagination),
    enabled,
  });
}
export function useGroupsMembers(groupId: string, pagination: Pagination, enabled: boolean) {
  return useQuery(useGroupMembersQueryOptions(groupId, pagination, enabled));
}

async function getMemberGroups(memberId: string) {
  return await apiFetch<GroupBrief[]>(`/memberships/member/${memberId}`);
}
export function useMemberGroupsQueryOptions(memberId: string) {
  return queryOptions({
    queryKey: [ALL_GROUPS, { memberId }],
    queryFn: () => getMemberGroups(memberId),
  });
}
export function useMemberGroups(memberId: string) {
  return useQuery(useMemberGroupsQueryOptions(memberId));
}

async function addMembership(payload: MembershipPayload) {
  return await apiFetch<MembershipResponse>(`/memberships`, { method: "POST", body: payload });
}
export function useAddMembership() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationKey: ["add-membership"],
    mutationFn: (payload: MembershipPayload) => addMembership(payload),
    onSuccess: (data) => {
      queryClient.invalidateQueries({
        queryKey: [SINGLE_MEMBER, { memberId: data.memberId }],
      });
      queryClient.invalidateQueries({
        queryKey: [GROUPS_MEMBERS, { groupId: data.groupId }],
      });
      toast.success("Member added to group.");
    },
    onError: (error: ApiError) => {
      toast.error("Member could not be added to group", {
        description: error.responseBody,
      });
    },
  });
}

async function removeMembership(payload: MembershipPayload) {
  return await apiFetch<MembershipResponse>(`/memberships`, { method: "DELETE", body: payload });
}
export function useRemoveMembership() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationKey: ["remove-membership"],
    mutationFn: (payload: MembershipPayload) => removeMembership(payload),
    onSuccess: (data) => {
      queryClient.invalidateQueries({
        queryKey: [SINGLE_MEMBER, { memberId: data.memberId }],
      });
      queryClient.invalidateQueries({
        queryKey: [GROUPS_MEMBERS, { groupId: data.groupId }],
      });
      toast.success("Member removed from group.");
    },
    onError: (error: ApiError) => {
      toast.error("Member could not be removed from group", {
        description: error.responseBody,
      });
    },
  });
}
