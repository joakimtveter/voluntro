import { createFileRoute } from "@tanstack/react-router";

import { useVenueById } from "#/domains/venues/use-venues.ts";
import VenueForm from "#/domains/venues/venue.form.tsx";
import PageWrapper from "#/shared/components/page-wrapper.tsx";
import ErrorPage from "#/shared/pages/error-page.tsx";
import LoadingPage from "#/shared/pages/loading-page.tsx";

export const Route = createFileRoute("/venues/$venueId/edit")({
  component: RouteComponent,
});

function RouteComponent() {
  const { venueId } = Route.useParams();
  const { data: venue, isPending, isError, error } = useVenueById(venueId);

  if (isPending) return <LoadingPage />;
  if (isError) return <ErrorPage error={error} />;

  return (
    <PageWrapper title="Edit venue">
      <VenueForm venueId={venueId} defaultValues={venue} />
    </PageWrapper>
  );
}
