import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import type { MemberTypePayload } from "#/domains/member-types/member-type.schema.ts";
import type { MemberType } from "#/domains/member-types/member-type.types.ts";
import { ALL_MEMBER_TYPES } from "#/shared/constants/query-keys.ts";
import type { ApiError } from "#/shared/lib/fetch/api-error.ts";
import { apiFetch } from "#/shared/lib/fetch/api-fetch.ts";

async function getMemberTypes() {
  return await apiFetch<MemberType[]>("/admin/MemberTypes");
}
export function useGetMemberTypes() {
  return useQuery({
    queryKey: [ALL_MEMBER_TYPES],
    queryFn: getMemberTypes,
  });
}

async function createMemberType(payload: MemberTypePayload) {
  return await apiFetch("/admin/MemberTypes", { method: "POST", body: payload });
}
export function useCreateMemberType() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationKey: ["create-member-type"],
    mutationFn: (data: MemberTypePayload) => createMemberType(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [ALL_MEMBER_TYPES] });
      toast.success("Member type successfully created");
    },
    onError: (error: ApiError) => {
      toast.error("Unable to create member type", { description: error.message });
    },
  });
}

async function updateMemberType(memberTypeId: string, payload: MemberTypePayload) {
  return await apiFetch(`/admin/MemberTypes/${memberTypeId}`, { method: "PUT", body: payload });
}
export function useUpdateMemberType(memberTypeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationKey: ["update-member-type", { memberTypeId }],
    mutationFn: (data: MemberTypePayload) => updateMemberType(memberTypeId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [ALL_MEMBER_TYPES] });
      toast.success("Member type successfully updated");
    },
    onError: (error: ApiError) => {
      toast.error("Unable to update member type", { description: error.message });
    },
  });
}

async function setDefaultMemberType(memberTypeId: string) {
  return await apiFetch(`/admin/MemberTypes/${memberTypeId}/set-default`, { method: "PUT" });
}
export function useSetDefaultMemberType(memberTypeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationKey: ["set-default-member-type", { memberTypeId }],
    mutationFn: () => setDefaultMemberType(memberTypeId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [ALL_MEMBER_TYPES] });
      toast.success("Default member type updated");
    },
    onError: (error: ApiError) => {
      toast.error("Unable to set default member type", { description: error.message });
    },
  });
}

async function deleteMemberType(memberTypeId: string) {
  return await apiFetch(`/admin/MemberTypes/${memberTypeId}`, { method: "DELETE" });
}
export function useDeleteMemberType(memberTypeId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationKey: ["delete-member-type", { memberTypeId }],
    mutationFn: () => deleteMemberType(memberTypeId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [ALL_MEMBER_TYPES] });
      toast.success("Member type successfully deleted");
    },
    onError: (error: ApiError) => {
      toast.error("Unable to delete member type", { description: error.message });
    },
  });
}
