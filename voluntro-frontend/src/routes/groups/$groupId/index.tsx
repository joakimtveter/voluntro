import { createFileRoute, Link } from "@tanstack/react-router";
import { AlertCircleIcon, PencilIcon, Trash2Icon } from "lucide-react";
import { Fragment } from "react";

import AddMemberToGroupForm from "#/domains/group-membership/add-member-to-group.form.tsx";
import {
  useGroupsMembers,
  useRemoveMembership,
} from "#/domains/group-membership/use-group-membership.ts";
import type { GroupAncestor, GroupBrief } from "#/domains/groups/group.types.ts";
import { useGroupById } from "#/domains/groups/use-groups.ts";
import Heading from "#/shared/components/heading.tsx";
import PageWrapper from "#/shared/components/page-wrapper.tsx";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "#/shared/components/ui/alert-dialog.tsx";
import { Alert, AlertDescription, AlertTitle } from "#/shared/components/ui/alert.tsx";
import { Badge } from "#/shared/components/ui/badge.tsx";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "#/shared/components/ui/breadcrumb.tsx";
import { Button } from "#/shared/components/ui/button.tsx";
import { Card, CardContent, CardHeader, CardTitle } from "#/shared/components/ui/card.tsx";
import { LinkButton } from "#/shared/components/ui/link-button.tsx";
import { Spinner } from "#/shared/components/ui/spinner.tsx";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "#/shared/components/ui/table.tsx";
import { formatDateTime } from "#/shared/lib/datetime.ts";
import { formatName } from "#/shared/lib/formatName.ts";
import ErrorPage from "#/shared/pages/error-page.tsx";
import LoadingPage from "#/shared/pages/loading-page.tsx";

export const Route = createFileRoute("/groups/$groupId/")({
  component: SingleGroupPage,
});

function SingleGroupPage() {
  const { groupId } = Route.useParams();
  const { data: group, isPending, isError, error } = useGroupById(groupId);
  const { mutate: removeMembership } = useRemoveMembership();
  const {
    data,
    isPending: isMembersPending,
    isError: isMembersError,
    error: membersError,
  } = useGroupsMembers(groupId, { page: 1, pageSize: 50 }, group ? group?.memberCount > 0 : false);

  if (isPending) return <LoadingPage />;
  if (isError) return <ErrorPage error={error} />;

  return (
    <PageWrapper
      breadcrumbs={
        <Breadcrumb>
          <BreadcrumbList>
            <BreadcrumbItem>
              <BreadcrumbLink render={<Link to="/groups" />}>Groups</BreadcrumbLink>
            </BreadcrumbItem>
            {group.ancestors.map((ancestor: GroupAncestor) => (
              <Fragment key={ancestor.id}>
                <BreadcrumbSeparator />
                <BreadcrumbItem>
                  <BreadcrumbLink
                    render={<Link to="/groups/$groupId" params={{ groupId: ancestor.id }} />}
                  >
                    {ancestor.name}
                  </BreadcrumbLink>
                </BreadcrumbItem>
              </Fragment>
            ))}
            <BreadcrumbSeparator />
            <BreadcrumbItem>
              <BreadcrumbPage>{group.name}</BreadcrumbPage>
            </BreadcrumbItem>
          </BreadcrumbList>
        </Breadcrumb>
      }
      title={`Group: ${group.name}`}
      subTitle={
        group.createdAt === group.updatedAt
          ? `Created: ${formatDateTime(group?.createdAt, "precise")}`
          : `Created: ${formatDateTime(group?.createdAt, "precise")}, Updated: ${formatDateTime(group?.updatedAt, "precise")}`
      }
      actions={
        <LinkButton to="/groups/$groupId/edit">
          <PencilIcon />
          Edit group
        </LinkButton>
      }
    >
      {group.description && <p className="text-foreground text-xl">{group.description}</p>}

      {group.childGroups.length > 0 && (
        <section className="my-4 flex flex-col gap-3">
          <h2 className="text-lg font-semibold">Subgroups</h2>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {group.childGroups.map((child: GroupBrief) => (
              <LinkButton
                key={child.id}
                to="/groups/$groupId"
                params={{ groupId: child.id }}
                variant="ghost"
                className="h-auto p-0 hover:bg-transparent"
              >
                <Card size="sm" className="w-full text-left transition-shadow hover:shadow-md">
                  <CardHeader>
                    <CardTitle>{child.name}</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <p className="text-muted-foreground text-xs">
                      Created {formatDateTime(child.createdAt, "precise")}
                    </p>
                  </CardContent>
                </Card>
              </LinkButton>
            ))}
          </div>
        </section>
      )}
      <section className="my-4">
        <Heading level={2} size={"xl"}>
          Members <Badge variant="default">{group.memberCount}</Badge>
        </Heading>
        {isMembersPending && <Spinner />}
        {isMembersError && (
          <Alert variant="destructive">
            <AlertCircleIcon />
            <AlertTitle>Could not get group members</AlertTitle>
            <AlertDescription>{membersError.name}</AlertDescription>
          </Alert>
        )}
        {data && data.items.length > 0 && (
          <Table className="mt-3">
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead />
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.items.map((member) => (
                <TableRow key={member.id}>
                  <TableCell>
                    <Link
                      to="/members/$memberId"
                      params={{ memberId: member.id }}
                      className="hover:underline"
                    >
                      {formatName(member.firstName, member.middleNames, member.lastName)}
                    </Link>
                  </TableCell>
                  <TableCell className="text-right">
                    <AlertDialog>
                      <AlertDialogTrigger render={<Button variant="ghost" size="sm" />}>
                        <Trash2Icon />
                        Remove
                      </AlertDialogTrigger>
                      <AlertDialogContent>
                        <AlertDialogHeader>
                          <AlertDialogTitle>Remove member from group</AlertDialogTitle>
                          <AlertDialogDescription>
                            Remove{" "}
                            <span className="text-foreground font-medium">
                              {formatName(member.firstName, member.middleNames, member.lastName)}
                            </span>{" "}
                            from <span className="text-foreground font-medium">{group.name}</span>?
                          </AlertDialogDescription>
                        </AlertDialogHeader>
                        <AlertDialogFooter>
                          <AlertDialogCancel>Cancel</AlertDialogCancel>
                          <AlertDialogAction
                            onClick={() => removeMembership({ groupId, memberId: member.id })}
                          >
                            Remove
                          </AlertDialogAction>
                        </AlertDialogFooter>
                      </AlertDialogContent>
                    </AlertDialog>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
        <AddMemberToGroupForm
          groupId={groupId}
          filterMemberIds={data?.items.map((m) => m.id) ?? []}
        />
      </section>
    </PageWrapper>
  );
}
