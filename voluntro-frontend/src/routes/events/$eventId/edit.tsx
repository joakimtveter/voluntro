import { createFileRoute } from "@tanstack/react-router";

import EventForm from "#/domains/events/event.form.tsx";
import { useEventById } from "#/domains/events/use-events.ts";
import PageWrapper from "#/shared/components/page-wrapper.tsx";
import ErrorPage from "#/shared/pages/error-page.tsx";
import LoadingPage from "#/shared/pages/loading-page.tsx";

export const Route = createFileRoute("/events/$eventId/edit")({
  component: RouteComponent,
});

function RouteComponent() {
  const { eventId } = Route.useParams();
  const { data, isPending, isError, error } = useEventById(eventId);

  if (isPending) return <LoadingPage />;
  if (isError) return <ErrorPage error={error} />;

  return (
    <PageWrapper title={`Edit event: ${data.title}`}>
      <EventForm eventId={eventId} defaultValues={data} />
    </PageWrapper>
  );
}
