import { createFileRoute, useNavigate } from "@tanstack/react-router";
import * as z from "zod";

import MemberForm from "#/domains/members/components/member.form.tsx";
import { useMemberById } from "#/domains/members/use-members.ts";
import PageWrapper from "#/shared/components/page-wrapper.tsx";
import { formatName } from "#/shared/lib/formatName.ts";
import ErrorPage from "#/shared/pages/error-page.tsx";
import LoadingPage from "#/shared/pages/loading-page.tsx";

export const Route = createFileRoute("/members/$memberId/edit")({
  validateSearch: z.object({
    returnTo: z.string().optional().catch(undefined),
  }),
  component: RouteComponent,
});

function RouteComponent() {
  const { memberId } = Route.useParams();
  const { returnTo } = Route.useSearch();
  const navigate = useNavigate();
  const { data: member, isPending, isError, error } = useMemberById(memberId);

  function handleSuccess() {
    if (returnTo) {
      void navigate({ to: returnTo as "/" });
    } else {
      void navigate({ to: "/members/$memberId", params: { memberId } });
    }
  }

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
      <MemberForm defaultValues={member} memberId={memberId} onSuccess={handleSuccess} />
    </PageWrapper>
  );
}
