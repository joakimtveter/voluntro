import { createFileRoute } from "@tanstack/react-router";

import { useMemberById } from "#/domains/members/use-members.ts";
import PageWrapper from "#/shared/components/page-wrapper.tsx";

export const Route = createFileRoute("/members/$memberId/")({
  component: RouteComponent,
});

function RouteComponent() {
  const { memberId } = Route.useParams();
  const { data: member } = useMemberById(memberId);
  const names = [member?.firstName, member?.middleNames, member?.lastName];
  return (
    <PageWrapper title={names.join(" ")}>
      <pre>{JSON.stringify(member, null, 2)}</pre>
    </PageWrapper>
  );
}
