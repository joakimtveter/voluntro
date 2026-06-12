import AddGroupToMemberForm from "#/domains/group-membership/add-group-to-member.form.tsx";
import { useMemberGroups } from "#/domains/group-membership/use-group-membership.ts";
import type { GroupBrief } from "#/domains/groups/group.types.ts";
import Heading from "#/shared/components/heading.tsx";
import { Card, CardContent, CardHeader, CardTitle } from "#/shared/components/ui/card.tsx";
import { LinkButton } from "#/shared/components/ui/link-button.tsx";
import { formatDateTime } from "#/shared/lib/datetime.ts";

type MemberGroupsSectionProps = {
  memberId: string;
};

export default function MemberGroupsSection({ memberId }: MemberGroupsSectionProps) {
  const { data: groups } = useMemberGroups(memberId);

  return (
    <section className="my-6" data-component="MemberGroupsSection">
      <Heading level={2} size="xl" badge={groups?.length ?? 0}>
        Groups
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
      <AddGroupToMemberForm memberId={memberId} filterGroupIds={groups?.map((g) => g.id) ?? []} />
    </section>
  );
}
