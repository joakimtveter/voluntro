import { createFileRoute } from "@tanstack/react-router";
import { PencilIcon } from "lucide-react";

import MemberGroupsSection from "#/domains/members/components/member-groups-section.tsx";
import MemberTagsSection from "#/domains/members/components/member-tags-section.tsx";
import { useMemberById } from "#/domains/members/use-members.ts";
import PageWrapper from "#/shared/components/page-wrapper.tsx";
import { LinkButton } from "#/shared/components/ui/link-button.tsx";
import { formatDate, formatDateTime } from "#/shared/lib/datetime.ts";
import { formatName } from "#/shared/lib/formatName.ts";
import ErrorPage from "#/shared/pages/error-page.tsx";
import LoadingPage from "#/shared/pages/loading-page.tsx";

export const Route = createFileRoute("/members/$memberId/")({
  component: RouteComponent,
});

function RouteComponent() {
  const { memberId } = Route.useParams();
  const { data: member, isError, error } = useMemberById(memberId);

  if (member) {
    return (
      <PageWrapper
        title={formatName(member.firstName, member.middleNames, member.lastName)}
        subTitle={
          member.createdAt === member.updatedAt
            ? `Created: ${formatDateTime(member.createdAt, "precise")}`
            : `Created: ${formatDateTime(member.createdAt, "precise")}, Updated: ${formatDateTime(member.updatedAt, "precise")}`
        }
        actions={
          <LinkButton
            variant="outline"
            to="/members/$memberId/edit"
            params={{ memberId }}
            search={{ returnTo: `/members/${memberId}` }}
          >
            <PencilIcon />
            Edit member
          </LinkButton>
        }
      >
        <section className="my-6">
          <dl className="grid grid-cols-[auto_1fr] gap-x-6 gap-y-3 text-base">
            <dt className="text-muted-foreground font-medium">Email</dt>
            <dd>
              {member.email ? (
                <a href={`mailto:${member.email}`} className="hover:underline">
                  {member.email}
                </a>
              ) : (
                "Unknown"
              )}
            </dd>
            <dt className="text-muted-foreground font-medium">Date of birth</dt>
            <dd>
              {member.dateOfBirth != null
                ? `${formatDate(member.dateOfBirth, "long")} (${member.age} years old)`
                : "Unknown"}
            </dd>
            <dt className="text-muted-foreground font-medium">Gender</dt>
            <dd className="capitalize">{member.legalGender}</dd>
          </dl>
        </section>

        <MemberTagsSection memberId={memberId} appliedTags={member.tags} />
        <MemberGroupsSection memberId={memberId} />
      </PageWrapper>
    );
  }

  if (isError) return <ErrorPage error={error} />;

  return <LoadingPage title={`Member with ID: ${memberId}`} />;
}
