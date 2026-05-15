import { createFileRoute } from "@tanstack/react-router";

import EventForm from "#/domains/events/event.form.tsx";
import PageWrapper from "#/shared/components/page-wrapper.tsx";

export const Route = createFileRoute("/events/add")({
  component: RouteComponent,
});

function RouteComponent() {
  return (
    <PageWrapper title="Create event">
      <EventForm action="create" />
    </PageWrapper>
  );
}
