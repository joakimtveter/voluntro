import { queryOptions, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import type { VenuePayload } from "#/domains/venues/venue.schema.ts";
import type { Venue, VenueBrief } from "#/domains/venues/venue.types.ts";
import { ALL_VENUES, SINGLE_VENUE } from "#/shared/constants/query-keys.ts";
import { apiFetch } from "#/shared/lib/fetch/api-fetch.ts";
import type { PaginatedList, Pagination, QueryOptions } from "#/shared/types/api.types.ts";

async function getVenues(pagination?: Pagination) {
  return await apiFetch<PaginatedList<VenueBrief>>("/venues", { query: pagination });
}

export function useVenuesQueryOptions(pagination?: Pagination) {
  return queryOptions({
    queryKey: [ALL_VENUES, pagination],
    queryFn: () => getVenues(pagination),
  });
}

export function useVenues(pagination?: Pagination, options?: QueryOptions) {
  return useQuery({ ...useVenuesQueryOptions(pagination), ...options });
}

async function getVenueById(venueId: string) {
  return await apiFetch<Venue>(`/venues/${venueId}`);
}

export function useVenueByIdQueryOptions(venueId: string) {
  return queryOptions({
    queryKey: [SINGLE_VENUE, { venueId }],
    queryFn: () => getVenueById(venueId),
  });
}

export function useVenueById(venueId: string) {
  return useQuery(useVenueByIdQueryOptions(venueId));
}

async function createVenue(payload: VenuePayload) {
  return await apiFetch<Venue>("/venues", { method: "POST", body: payload });
}

export function useCreateVenue() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationKey: ["create-venue"],
    mutationFn: createVenue,
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: [ALL_VENUES],
      });
    },
  });
}

async function updateVenue(venueId: string, payload: VenuePayload) {
  return await apiFetch<Venue>(`/venues/${venueId}`, { method: "PUT", body: payload });
}

export function useUpdateVenue(venueId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationKey: ["update-venue", { venueId }],
    mutationFn: (data: VenuePayload) => updateVenue(venueId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: [ALL_VENUES],
      });
      queryClient.invalidateQueries({
        queryKey: [SINGLE_VENUE, { venueId }],
      });
    },
    onError: (error) => {
      toast.error(error.message);
    },
  });
}

async function deleteVenue(venueId: string) {
  return await apiFetch(`/venues/${venueId}`, { method: "DELETE" });
}

export function useDeleteVenue(venueId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationKey: ["delete-venue", { venueId }],
    mutationFn: () => deleteVenue(venueId),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: [ALL_VENUES],
      });
      queryClient.invalidateQueries({
        queryKey: [SINGLE_VENUE, { venueId }],
      });
      toast.success("Venue deleted");
    },
    onError: (error) => {
      toast.error("Unable to delete venue", { description: error.message });
    },
  });
}
