import { queryOptions, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import type { GroupBrief } from "#/domains/groups/group.types.ts";
import type { AddGroupToMemberPayload } from "#/domains/members/add-group-to-member.form.tsx";
import type { Member, MemberBrief } from "#/domains/members/member.types.ts";
import type { MemberPayload } from "#/domains/members/members.schema.ts";
import {
  ALL_GROUPS,
  ALL_MEMBERS,
  GROUPS_MEMBERS,
  MEMBER_QUERY,
  SINGLE_GROUP,
  SINGLE_MEMBER,
} from "#/shared/constants/query-keys.ts";
import type { ApiError } from "#/shared/lib/fetch/api-error.ts";
import { apiFetch } from "#/shared/lib/fetch/api-fetch.ts";
import type { PaginatedList, Pagination, SelectOption } from "#/shared/types/api.types.ts";

async function getMembers(pagination: Pagination) {
  return await apiFetch<PaginatedList<MemberBrief>>("/members", { query: pagination });
}
export function useMembersQueryOptions(pagination: Pagination) {
  return queryOptions({
    queryKey: [ALL_MEMBERS, pagination],
    queryFn: () => getMembers(pagination),
  });
}
export function useMembers(pagination: Pagination) {
  return useQuery(useMembersQueryOptions(pagination));
}

async function getMemberById(memberId: string) {
  return await apiFetch<Member>(`/members/${memberId}`);
}
export function useMemberByIdQueryOptions(memberId: string) {
  return queryOptions({
    queryKey: [SINGLE_MEMBER, { memberId }],
    queryFn: () => getMemberById(memberId),
  });
}
export function useMemberById(memberId: string) {
  return useQuery(useMemberByIdQueryOptions(memberId));
}

async function getMemberGroups(memberId: string) {
  return await apiFetch<GroupBrief[]>(`/members/${memberId}/groups`);
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

async function addGroupToMember(memberId: string, payload: AddGroupToMemberPayload) {
  return await apiFetch(`/member/${memberId}/group`, { method: "POST", body: payload });
}
export function useAddGroupToMember(memberId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationKey: ["add-group-to-member", { memberId }],
    mutationFn: (payload: AddGroupToMemberPayload) => addGroupToMember(memberId, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: [SINGLE_GROUP, {}],
      });
      queryClient.invalidateQueries({
        queryKey: [GROUPS_MEMBERS, { groupId }],
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

async function createMember(payload: MemberPayload) {
  return await apiFetch<Member>("/members", { method: "POST", body: payload });
}
export function useCreateMember() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationKey: ["create-member"],
    mutationFn: createMember,
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: [ALL_MEMBERS],
      });
      toast.success("Member successfully created.");
    },
    onError: (error: ApiError) => {
      toast.error("Unable to update member", { description: error.responseBody });
    },
  });
}

async function updateMember(memberId: string, payload: MemberPayload) {
  return await apiFetch<Member>(`/members/${memberId}`, { method: "PUT", body: payload });
}
export function useUpdateMember(memberId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationKey: ["update-member", { memberId }],
    mutationFn: (data: MemberPayload) => updateMember(memberId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: [ALL_MEMBERS],
      });
      queryClient.invalidateQueries({
        queryKey: [SINGLE_MEMBER, { memberId }],
      });
      toast.success("Member updated");
    },
    onError: (error: ApiError) => {
      toast.error("Unable to update member", { description: error.responseBody });
    },
  });
}

async function memberComboboxQuery(query: string) {
  return await apiFetch<SelectOption[]>(`/query/members?${query}`);
}
export function useMemberComboboxQuery(query: string) {
  return useQuery({
    queryKey: [ALL_MEMBERS, MEMBER_QUERY, query],
    queryFn: () => memberComboboxQuery(query),
  });
}
