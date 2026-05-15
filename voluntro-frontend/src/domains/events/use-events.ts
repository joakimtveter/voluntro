import { queryOptions, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";

import type { EventPayload } from "#/domains/events/event.schema.ts";
import type { Event, EventBrief } from "#/domains/events/events.types.ts";
import { ALL_EVENTS, SINGLE_EVENT } from "#/shared/constants/query-keys.ts";
import { apiFetch } from "#/shared/lib/fetch/api-fetch.ts";
import type { PaginatedList, Pagination } from "#/shared/types/api.types.ts";

async function getEvents(pagination?: Pagination) {
  return await apiFetch<PaginatedList<EventBrief>>("/events", { query: pagination });
}
export function useEventsQueryOptions(pagination?: Pagination) {
  return queryOptions({
    queryKey: [ALL_EVENTS, pagination],
    queryFn: () => getEvents(pagination),
  });
}
export function useEvents(pagination?: Pagination) {
  return useQuery(useEventsQueryOptions(pagination));
}

async function getEventById(eventId: string) {
  return await apiFetch<Event>(`/events/${eventId}`);
}
export function useEventByIdQueryOptions(eventId: string) {
  return queryOptions({
    queryKey: [SINGLE_EVENT, { eventId }],
    queryFn: () => getEventById(eventId),
  });
}
export function useEventById(memberId: string) {
  return useQuery(useEventByIdQueryOptions(memberId));
}

async function createEvent(payload: EventPayload) {
  return await apiFetch<Event>("/events", { method: "POST", body: payload });
}
export function useCreateEvent() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  return useMutation({
    mutationKey: ["create-event"],
    mutationFn: createEvent,
    onSuccess: (event) => {
      queryClient.invalidateQueries({
        queryKey: [ALL_EVENTS],
      });
      toast.success("Event created successfully.", {
        action: {
          label: "View event",
          onClick: () => navigate({ to: "/events/$eventId", params: { eventId: event.id } }),
        },
      });
    },
  });
}

async function updateEvent(eventId: string, payload: EventPayload) {
  return await apiFetch<Event>(`/events/${eventId}`, { method: "PUT", body: payload });
}
export function useUpdateEvent(eventId: string) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  return useMutation({
    mutationKey: ["update-event", { eventId }],
    mutationFn: (payload: EventPayload) => updateEvent(eventId, payload),
    onSuccess: (event) => {
      queryClient.invalidateQueries({
        queryKey: [ALL_EVENTS],
      });
      queryClient.invalidateQueries({
        queryKey: [SINGLE_EVENT, { eventId }],
      });
      toast.success("Event updated successfully.", {
        action: {
          label: "View event",
          onClick: () => navigate({ to: "/events/$eventId", params: { eventId: event.id } }),
        },
      });
    },
  });
}
