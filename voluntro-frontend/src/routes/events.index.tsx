import { createFileRoute } from "@tanstack/react-router";
import { PlusSquareIcon } from "lucide-react";
import * as z from "zod";

import EventList from "#/domains/events/components/event-list.tsx";
import { useEvents, useEventsQueryOptions } from "#/domains/events/use-events.ts";
import PageWrapper from "#/shared/components/page-wrapper.tsx";
import { LinkButton } from "#/shared/components/ui/link-button.tsx";

const eventsSearchSchema = z.object({
  page: z.number().default(1),
  pageSize: z.number().default(12),
});

export const Route = createFileRoute("/events/")({
  component: EventsPage,
  validateSearch: eventsSearchSchema,
  loaderDeps: ({ search: { page, pageSize } }) => ({ page, pageSize }),
  loader: ({ context: { queryClient }, deps: { page, pageSize } }) =>
    queryClient.ensureQueryData(
      useEventsQueryOptions({ page: Number(page), pageSize: Number(pageSize) }),
    ),
});

function EventsPage() {
  const { page, pageSize } = Route.useSearch();
  const { data } = useEvents({ page, pageSize });

  if (data) {
    return (
      <PageWrapper
        title="Events"
        actions={
          <LinkButton to="/events/add">
            <PlusSquareIcon />
            Create new event
          </LinkButton>
        }
      >
        <EventList events={data.items} />
      </PageWrapper>
    );
  }
}
