import { createFileRoute } from "@tanstack/react-router";
import { PencilIcon } from "lucide-react";

import type { GroupBrief } from "#/domains/groups/group.types.ts";
import { useMemberById } from "#/domains/members/use-members.ts";
import AddGroupToMemberForm from "#/domains/membership/add-group-to-member.form.tsx";
import { useMemberGroups } from "#/domains/membership/use-membership.ts";
import Heading from "#/shared/components/heading.tsx";
import PageWrapper from "#/shared/components/page-wrapper.tsx";
import { Badge } from "#/shared/components/ui/badge.tsx";
import { Card, CardContent, CardHeader, CardTitle } from "#/shared/components/ui/card.tsx";
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
  const { data: groups } = useMemberGroups(memberId);

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
          <LinkButton to="/members/$memberId/edit" params={{ memberId }}>
            <PencilIcon />
            Edit member
          </LinkButton>
        }
      >
        <section className="my-4">
          <Card>
            <CardContent>
              <dl className="grid grid-cols-[auto_1fr] gap-x-6 gap-y-3 text-sm">
                <dt className="text-muted-foreground font-medium">First name</dt>
                <dd>{member.firstName}</dd>
                {member.middleNames && (
                  <>
                    <dt className="text-muted-foreground font-medium">Middle names</dt>
                    <dd>{member.middleNames}</dd>
                  </>
                )}
                <dt className="text-muted-foreground font-medium">Last name</dt>
                <dd>{member.lastName}</dd>
                <dt className="text-muted-foreground font-medium">Date of birth</dt>
                <dd>{formatDate(member.dateOfBirth, "long")}</dd>
                <dt className="text-muted-foreground font-medium">Age</dt>
                <dd>{member.age}</dd>
                <dt className="text-muted-foreground font-medium">Gender</dt>
                <dd className="capitalize">{member.legalGender}</dd>
              </dl>
            </CardContent>
          </Card>
        </section>

        <section className="my-4">
          <Heading level={2} size="xl">
            Groups <Badge variant="default">{groups?.length ?? 0}</Badge>
          </Heading>
          {groups && groups.length > 0 && (
            <div className="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {groups.map((group: GroupBrief) => (
                <LinkButton
                  key={group.id}
                  to="/groups/$groupId"
                  params={{ groupId: group.id }}
                  variant="ghost"
                  className="h-auto p-0 hover:bg-transparent"
                >
                  <Card size="sm" className="w-full text-left transition-shadow hover:shadow-md">
                    <CardHeader>
                      <CardTitle>{group.name}</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <p className="text-muted-foreground text-xs">
                        Created {formatDateTime(group.createdAt, "precise")}
                      </p>
                    </CardContent>
                  </Card>
                </LinkButton>
              ))}
            </div>
          )}
          <AddGroupToMemberForm
            memberId={memberId}
            filterGroupIds={groups?.map((g) => g.id) ?? []}
          />
        </section>
      </PageWrapper>
    );
  }

  if (isError) return <ErrorPage error={error} />;

  return <LoadingPage title={`Member with ID: ${memberId}`} />;
}
