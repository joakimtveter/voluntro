import { createFileRoute } from "@tanstack/react-router";
import { PencilIcon } from "lucide-react";

import { useVenueById, useVenueByIdQueryOptions } from "#/domains/venues/use-venues.ts";
import PageWrapper from "#/shared/components/page-wrapper.tsx";
import { LinkButton } from "#/shared/components/ui/link-button.tsx";
import ErrorPage from "#/shared/pages/error-page.tsx";
import LoadingPage from "#/shared/pages/loading-page.tsx";

export const Route = createFileRoute("/venues/$venueId/")({
  loader: ({ context: { queryClient }, params: { venueId } }) =>
    queryClient.ensureQueryData(useVenueByIdQueryOptions(venueId)),

  component: RouteComponent,
});

function RouteComponent() {
  const { venueId } = Route.useParams();
  const { data: venue, isPending, isError, error } = useVenueById(venueId);

  if (isPending) return <LoadingPage />;
  if (isError) return <ErrorPage error={error} />;

  return (
    <PageWrapper
      title={`Venue: ${venue.name}`}
      actions={
        <LinkButton to="/venues/$venueId/edit" params={{ venueId }} variant="outline">
          <PencilIcon />
          Edit venue
        </LinkButton>
      }
    >
      <pre>{JSON.stringify(venue, null, 2)}</pre>
    </PageWrapper>
  );
}
