import { createFileRoute } from "@tanstack/react-router";

import GroupForm from "#/domains/groups/group.form.tsx";
import PageWrapper from "#/shared/components/page-wrapper.tsx";

export const Route = createFileRoute("/groups/add")({
  component: RouteComponent,
});

function RouteComponent() {
  return (
    <PageWrapper title="Create group">
      <GroupForm />
    </PageWrapper>
  );
}
