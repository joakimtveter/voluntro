import { createFileRoute } from "@tanstack/react-router";

import { useEventById } from "#/domains/events/use-events.ts";
import PageWrapper from "#/shared/components/page-wrapper.tsx";

export const Route = createFileRoute("/events/$eventId/")({
  component: RouteComponent,
});

function RouteComponent() {
  const { eventId } = Route.useParams();
  const { data: event } = useEventById(eventId);
  return (
    <PageWrapper title="Evnets">
      <pre>{JSON.stringify(event, null, 2)}</pre>
    </PageWrapper>
  );
}
