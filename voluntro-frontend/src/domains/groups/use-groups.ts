import { queryOptions, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";

import type { GroupPayload } from "#/domains/groups/group.schema.ts";
import type { Group, GroupBrief } from "#/domains/groups/group.types.ts";
import { ALL_GROUPS, GROUP_QUERY, SINGLE_GROUP } from "#/shared/constants/query-keys.ts";
import { ApiError } from "#/shared/lib/fetch/api-error.ts";
import { apiFetch } from "#/shared/lib/fetch/api-fetch.ts";
import type { PaginatedList, Pagination, SelectOption } from "#/shared/types/api.types.ts";

async function getGroups(pagination: Pagination) {
  return await apiFetch<PaginatedList<GroupBrief>>("/groups", { query: pagination });
}
export function useGroupsQueryOptions(pagination: Pagination) {
  return queryOptions({
    queryKey: [ALL_GROUPS, pagination],
    queryFn: () => getGroups(pagination),
  });
}
export function useGroups(pagination: Pagination) {
  return useQuery(useGroupsQueryOptions(pagination));
}

async function getGroupById(groupId: string) {
  return await apiFetch<Group>(`/groups/${groupId}`);
}
export function useGetGroupByIdQueryOptions(groupId: string) {
  return queryOptions({
    queryKey: [SINGLE_GROUP, { groupId }],
    queryFn: () => getGroupById(groupId),
  });
}
export function useGroupById(groupId: string) {
  return useQuery(useGetGroupByIdQueryOptions(groupId));
}

async function createGroup(payload: GroupPayload) {
  return await apiFetch<Group>("/groups", { method: "POST", body: payload });
}
export function useCreateGroup() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  return useMutation({
    mutationKey: ["create-group"],
    mutationFn: createGroup,
    onSuccess: (group) => {
      queryClient.invalidateQueries({
        queryKey: [ALL_GROUPS],
      });
      toast.success("Group successfully created.", {
        action: {
          label: "View group",
          onClick: () => navigate({ to: "/groups/$groupId", params: { groupId: group.id } }),
        },
      });
    },
  });
}

async function updateGroup(groupId: string, payload: GroupPayload) {
  return await apiFetch<Group>(`/groups/${groupId}`, { method: "PUT", body: payload });
}
export function useUpdateGroup(groupId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationKey: ["update-group", groupId],
    mutationFn: (payload: GroupPayload) => updateGroup(groupId, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: [ALL_GROUPS],
      });
      queryClient.invalidateQueries({
        queryKey: [SINGLE_GROUP, { groupId }],
      });
      toast.success("Group updated successfully.");
    },
    onError: (error: ApiError) =>
      toast.error("Could not update group", {
        description: error.responseBody,
      }),
  });
}

async function deleteGroup(groupId: string) {
  return await apiFetch<void>(`/groups/${groupId}`, { method: "DELETE" });
}
export function useDeleteEvent(groupId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationKey: ["delete-group", groupId],
    mutationFn: deleteGroup,
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: [ALL_GROUPS],
      });
      queryClient.invalidateQueries({
        queryKey: [SINGLE_GROUP, { groupId }],
      });
      toast.success("Group successfully deleted.");
    },
    onError: (error: ApiError) => {
      toast.error("Could not delete group", {
        description: error.responseBody,
      });
    },
  });
}

async function groupComboboxQuery(query: string) {
  return await apiFetch<SelectOption[]>(`/query/groups?${query}`);
}
export function useGroupQuery(query: string) {
  return useQuery({
    queryKey: [ALL_GROUPS, GROUP_QUERY, query],
    queryFn: () => groupComboboxQuery(query),
  });
}
