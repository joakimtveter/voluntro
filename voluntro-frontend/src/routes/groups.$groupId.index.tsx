import { createFileRoute } from "@tanstack/react-router";
import { PencilIcon } from "lucide-react";

import { useGroupById } from "#/domains/groups/use-groups.ts";
import PageWrapper from "#/shared/components/page-wrapper.tsx";
import { LinkButton } from "#/shared/components/ui/link-button.tsx";
import { formatDateTime } from "#/shared/lib/datetime.ts";
import ErrorPage from "#/shared/pages/error-page.tsx";
import LoadingPage from "#/shared/pages/loading-page.tsx";

export const Route = createFileRoute("/groups/$groupId/")({
  component: RouteComponent,
});

function RouteComponent() {
  const { groupId } = Route.useParams();
  const { data: group, isPending, isError, error } = useGroupById(groupId);

  if (isPending) return <LoadingPage />;
  if (isError) return <ErrorPage error={error} />;

  const notEdited = group.createdAt === group.updatedAt;

  return (
    <PageWrapper
      title={`Group: ${group.name}`}
      subTitle={
        notEdited
          ? `Created: ${formatDateTime(group?.createdAt, "precise")}`
          : `Created: ${formatDateTime(group?.createdAt, "precise")}, Updated: ${formatDateTime(group?.updatedAt, "precise")}`
      }
      actions={
        <LinkButton to="/groups/$groupId/edit">
          <PencilIcon />
          Edit group
        </LinkButton>
      }
    >
      <pre>{JSON.stringify(group, null, 2)}</pre>
    </PageWrapper>
  );
}
