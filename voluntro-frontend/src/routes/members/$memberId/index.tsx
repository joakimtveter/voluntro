import { createFileRoute } from "@tanstack/react-router";
import { PencilIcon } from "lucide-react";

import { useMemberById } from "#/domains/members/use-members.ts";
import { useMemberGroups } from "#/domains/membership/use-membership.ts";
import PageWrapper from "#/shared/components/page-wrapper.tsx";
import { LinkButton } from "#/shared/components/ui/link-button.tsx";
import { formatName } from "#/shared/lib/formatName.ts";
import ErrorPage from "#/shared/pages/error-page.tsx";
import LoadingPage from "#/shared/pages/loading-page.tsx";

export const Route = createFileRoute("/members/$memberId/")({
  component: RouteComponent,
});

function RouteComponent() {
  const { memberId } = Route.useParams();
  const { data: member, isError, error } = useMemberById(memberId);
  const { data: groups } = useMemberGroups(memberId);

  if (member) {
    return (
      <PageWrapper
        title={formatName(member.firstName, member.middleNames, member.lastName)}
        actions={
          <LinkButton to="/members/$memberId/edit" params={{ memberId }}>
            <PencilIcon />
            Edit member{" "}
          </LinkButton>
        }
      >
        <pre>{JSON.stringify(member, null, 2)}</pre>
        <pre>{JSON.stringify(groups, null, 2)}</pre>
      </PageWrapper>
    );
  }

  if (isError) return <ErrorPage error={error} />;

  return <LoadingPage title={`Member with ID: ${memberId}`} />;
}
