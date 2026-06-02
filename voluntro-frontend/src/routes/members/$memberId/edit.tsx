import { createFileRoute } from "@tanstack/react-router";

import MemberForm from "#/domains/members/member.form.tsx";
import { useMemberById } from "#/domains/members/use-members.ts";
import PageWrapper from "#/shared/components/page-wrapper.tsx";
import { formatName } from "#/shared/lib/formatName.ts";
import ErrorPage from "#/shared/pages/error-page.tsx";
import LoadingPage from "#/shared/pages/loading-page.tsx";

export const Route = createFileRoute("/members/$memberId/edit")({
  component: RouteComponent,
});

function RouteComponent() {
  const { memberId } = Route.useParams();
  const { data: member, isPending, isError, error } = useMemberById(memberId);

  if (isPending) return <LoadingPage />;
  if (isError) return <ErrorPage error={error} />;

  return (
    <PageWrapper
      title={`Edit member: ${formatName(
        member.firstName,
        member.middleNames,
        member.lastName,
        "fl",
      )}`}
    >
      <MemberForm defaultValues={member} memberId={memberId} />
    </PageWrapper>
  );
}
