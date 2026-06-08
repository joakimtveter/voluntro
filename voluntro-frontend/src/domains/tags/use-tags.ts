import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import type { Tag } from "#/domains/members/member.types.ts";
import type { TagPayload } from "#/domains/tags/tags.schema.ts";
import { ALL_TAGS } from "#/shared/constants/query-keys.ts";
import type { ApiError } from "#/shared/lib/fetch/api-error.ts";
import { apiFetch } from "#/shared/lib/fetch/api-fetch.ts";

async function getTags() {
  return await apiFetch<Tag[]>("/tags");
}
export function useGetTags() {
  return useQuery({
    queryKey: [ALL_TAGS],
    queryFn: getTags,
  });
}

async function createTag(payload: TagPayload) {
  return await apiFetch("/tags", { method: "POST", body: payload });
}
export function useCreateTag() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationKey: ["create-tag"],
    mutationFn: (data: TagPayload) => createTag(data),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: [ALL_TAGS],
      });
      toast.success("Tag successfully created");
    },
    onError: (error: ApiError) => {
      toast.error("Unable to create tag", { description: error.responseBody });
    },
  });
}

async function updateTag(tagId: string, payload: TagPayload) {
  return await apiFetch(`/tags/${tagId}`, { method: "PUT", body: payload });
}
export function useUpdateTag(tagId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationKey: ["update-tag", { tagId }],
    mutationFn: (data: TagPayload) => updateTag(tagId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: [ALL_TAGS],
      });
      toast.success("Tag successfully updated");
    },
    onError: (error: ApiError) => {
      toast.error("Unable to update tag", { description: error.responseBody });
    },
  });
}

async function deleteTag(tagId: string) {
  return await apiFetch(`/tags/${tagId}`, { method: "DELETE" });
}
export function useDeleteTag(tagId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationKey: ["delete-tag", { tagId }],
    mutationFn: () => deleteTag(tagId),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: [ALL_TAGS],
      });
      toast.success("Tag successfully deleted");
    },
    onError: (error: ApiError) => {
      toast.error("Unable to delete tag", { description: error.responseBody });
    },
  });
}
