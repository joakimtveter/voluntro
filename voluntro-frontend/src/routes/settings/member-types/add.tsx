import { createFileRoute } from "@tanstack/react-router";

import MemberTypeForm from "#/domains/member-types/member-type.form.tsx";
import PageWrapper from "#/shared/components/page-wrapper.tsx";

export const Route = createFileRoute("/settings/member-types/add")({
  component: RouteComponent,
});

function RouteComponent() {
  return (
    <PageWrapper title="Add member type">
      <MemberTypeForm />
    </PageWrapper>
  );
}
