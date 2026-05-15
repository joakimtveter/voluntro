import { createFileRoute } from "@tanstack/react-router";

import VenueForm from "#/domains/venues/venue.form.tsx";
import PageWrapper from "#/shared/components/page-wrapper.tsx";

export const Route = createFileRoute("/venues/add")({
  component: RouteComponent,
});

function RouteComponent() {
  return (
    <PageWrapper title="Create venue">
      <VenueForm />
    </PageWrapper>
  );
}
