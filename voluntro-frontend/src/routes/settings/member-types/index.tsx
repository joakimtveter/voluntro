import { createFileRoute, Link } from "@tanstack/react-router";
import { PencilIcon, StarIcon, Trash2Icon } from "lucide-react";

import {
  useDeleteMemberType,
  useGetMemberTypes,
  useSetDefaultMemberType,
} from "#/domains/member-types/use-member-types.ts";
import { ConfirmDialog } from "#/shared/components/confirm-dialog.tsx";
import IconButton from "#/shared/components/icon-button.tsx";
import PageWrapper from "#/shared/components/page-wrapper.tsx";
import { Badge } from "#/shared/components/ui/badge.tsx";
import { LinkButton } from "#/shared/components/ui/link-button.tsx";
import ErrorPage from "#/shared/pages/error-page.tsx";
import LoadingPage from "#/shared/pages/loading-page.tsx";

export const Route = createFileRoute("/settings/member-types/")({
  component: RouteComponent,
});

function RouteComponent() {
  const { data: memberTypes, isError, error } = useGetMemberTypes();

  if (memberTypes) {
    return (
      <PageWrapper
        title="Member Types"
        actions={<LinkButton to="/settings/member-types/add">Add member type</LinkButton>}
      >
        {memberTypes.length === 0 ? (
          <p className="text-muted-foreground py-10 text-center text-sm">No member types found.</p>
        ) : (
          <ul className="divide-border max-w-xl divide-y rounded-md border">
            {memberTypes
              .slice()
              .sort((a, b) => a.name.localeCompare(b.name))
              .map((memberType) => (
                <li key={memberType.id} className="flex items-center gap-3 px-4 py-3">
                  <span className="flex-1 text-sm font-medium">{memberType.name}</span>
                  {memberType.isDefault && <Badge variant="secondary">Default</Badge>}
                  <div className="flex gap-1">
                    <IconButton
                      icon={<PencilIcon />}
                      tooltipContent={`Edit ${memberType.name}`}
                      render={
                        <Link
                          to="/settings/member-types/$memberTypeId/edit"
                          params={{ memberTypeId: memberType.id }}
                        />
                      }
                    />
                    <SetDefaultMemberTypeButton
                      memberTypeId={memberType.id}
                      memberTypeName={memberType.name}
                      isDefault={memberType.isDefault}
                    />
                    <DeleteMemberTypeButton
                      memberTypeId={memberType.id}
                      memberTypeName={memberType.name}
                      isDefault={memberType.isDefault}
                    />
                  </div>
                </li>
              ))}
          </ul>
        )}
      </PageWrapper>
    );
  }

  if (isError) return <ErrorPage error={error} />;

  return <LoadingPage title="Member Types" />;
}

function SetDefaultMemberTypeButton({
  memberTypeId,
  memberTypeName,
  isDefault,
}: {
  memberTypeId: string;
  memberTypeName: string;
  isDefault: boolean;
}) {
  const { mutate: setDefault } = useSetDefaultMemberType(memberTypeId);
  return (
    <ConfirmDialog
      trigger={
        <IconButton
          icon={<StarIcon />}
          tooltipContent={`Set ${memberTypeName} as default`}
          disabled={isDefault}
        />
      }
      title="Set default member type"
      description={
        <>
          Set <strong>{memberTypeName}</strong> as the default member type? New members will be
          assigned this type automatically.
        </>
      }
      confirmLabel="Set as default"
      onConfirm={() => setDefault()}
    />
  );
}

function DeleteMemberTypeButton({
  memberTypeId,
  memberTypeName,
  isDefault,
}: {
  memberTypeId: string;
  memberTypeName: string;
  isDefault: boolean;
}) {
  const { mutate: deleteMemberType } = useDeleteMemberType(memberTypeId);
  return (
    <ConfirmDialog
      trigger={
        <IconButton
          icon={<Trash2Icon />}
          tooltipContent={`Delete ${memberTypeName}`}
          variant="destructive"
          disabled={isDefault}
        />
      }
      title="Delete member type"
      description={
        <>
          Are you sure you want to delete <strong>{memberTypeName}</strong>? This cannot be undone.
        </>
      }
      confirmLabel="Delete"
      onConfirm={() => deleteMemberType()}
    />
  );
}
