import { createFileRoute } from "@tanstack/react-router";

import GroupForm from "#/domains/groups/group.form.tsx";
import { useGroupById } from "#/domains/groups/use-groups.ts";
import PageWrapper from "#/shared/components/page-wrapper.tsx";
import ErrorPage from "#/shared/pages/error-page.tsx";
import LoadingPage from "#/shared/pages/loading-page.tsx";

export const Route = createFileRoute("/groups/$groupId/edit")({
  component: EditGroupPage,
});

function EditGroupPage() {
  const { groupId } = Route.useParams();
  const { data: group, isPending, isError, error } = useGroupById(groupId);

  if (isPending) return <LoadingPage />;
  if (isError) return <ErrorPage error={error} />;

  return (
    <PageWrapper title={`Edit group: ${group.name}`}>
      <GroupForm groupId={groupId} defaultValues={group} />
    </PageWrapper>
  );
}
