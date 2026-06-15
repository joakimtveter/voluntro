import { queryOptions, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import type { LegalGenderEnum, MemberBrief } from "#/domains/members/member.types.ts";
import { ALL_MEMBERS } from "#/shared/constants/query-keys.ts";
import type { ApiError } from "#/shared/lib/fetch/api-error.ts";
import { apiFetch } from "#/shared/lib/fetch/api-fetch.ts";
import type { PaginatedList, Pagination } from "#/shared/types/api.types.ts";

type AdminMembersQuery = Pagination & {
  includeDeleted?: boolean;
  tagIds?: string[];
  tagFilterMode?: "any" | "all";
  legalGender?: LegalGenderEnum;
  sortBy?: "lastName" | "firstName" | "dateOfBirth";
  sortOrder?: "asc" | "desc";
};

async function getAdminMembers(query: AdminMembersQuery) {
  return await apiFetch<PaginatedList<MemberBrief>>("/admin/members", { query });
}
export function useAdminMembersQueryOptions(query: AdminMembersQuery) {
  return queryOptions({
    queryKey: ["admin", ALL_MEMBERS, query],
    queryFn: () => getAdminMembers(query),
  });
}
export function useAdminMembers(query: AdminMembersQuery) {
  return useQuery(useAdminMembersQueryOptions(query));
}

async function restoreMember(memberId: string) {
  return await apiFetch(`/admin/members/${memberId}/restore`, { method: "POST" });
}
export function useRestoreMember(memberId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationKey: ["restore-member", { memberId }],
    mutationFn: () => restoreMember(memberId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", ALL_MEMBERS] });
      queryClient.invalidateQueries({ queryKey: [ALL_MEMBERS] });
      toast.success("Member restored");
    },
    onError: (error: ApiError) => {
      toast.error("Unable to restore member", { description: error.message });
    },
  });
}

async function gdprDeleteMember(memberId: string) {
  return await apiFetch(`/admin/members/${memberId}/gdpr`, { method: "DELETE" });
}
export function useGdprDeleteMember(memberId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationKey: ["gdpr-delete-member", { memberId }],
    mutationFn: () => gdprDeleteMember(memberId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin", ALL_MEMBERS] });
      queryClient.invalidateQueries({ queryKey: [ALL_MEMBERS] });
      toast.success("Member data permanently erased");
    },
    onError: (error: ApiError) => {
      toast.error("Unable to erase member data", { description: error.message });
    },
  });
}
