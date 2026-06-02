import { createFileRoute } from "@tanstack/react-router";

import MemberForm from "#/domains/members/member.form.tsx";
import PageWrapper from "#/shared/components/page-wrapper.tsx";

export const Route = createFileRoute("/members/add")({
  component: RouteComponent,
});

function RouteComponent() {
  return (
    <PageWrapper title="Create new member">
      <MemberForm />
    </PageWrapper>
  );
}
