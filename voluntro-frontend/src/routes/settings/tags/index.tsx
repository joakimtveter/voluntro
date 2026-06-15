import { createFileRoute, Link } from "@tanstack/react-router";
import { PencilIcon, Trash2Icon } from "lucide-react";

import { useDeleteTag, useGetTags } from "#/domains/tags/use-tags.ts";
import { ConfirmDialog } from "#/shared/components/confirm-dialog.tsx";
import IconButton from "#/shared/components/icon-button.tsx";
import PageWrapper from "#/shared/components/page-wrapper.tsx";
import { LinkButton } from "#/shared/components/ui/link-button.tsx";
import ErrorPage from "#/shared/pages/error-page.tsx";
import LoadingPage from "#/shared/pages/loading-page.tsx";

export const Route = createFileRoute("/settings/tags/")({
  component: RouteComponent,
});

function RouteComponent() {
  const { data: tags, isError, error } = useGetTags();

  if (tags) {
    return (
      <PageWrapper title="Tags" actions={<LinkButton to="/settings/tags/add">Add tag</LinkButton>}>
        {tags.length === 0 ? (
          <p className="text-muted-foreground py-10 text-center text-sm">No tags found.</p>
        ) : (
          <ul className="divide-border max-w-xl divide-y rounded-md border">
            {tags
              .slice()
              .sort((a, b) => a.name.localeCompare(b.name))
              .map((tag) => (
                <li key={tag.id} className="flex items-center gap-3 px-4 py-3">
                  <span
                    className="size-3 shrink-0 rounded-full"
                    style={{ backgroundColor: tag.color }}
                    aria-hidden
                  />
                  <span className="flex-1 text-sm font-medium">{tag.name}</span>
                  <div className="flex gap-1">
                    <IconButton
                      icon={<PencilIcon />}
                      tooltipContent={`Edit ${tag.name}`}
                      render={<Link to="/settings/tags/$tagId/edit" params={{ tagId: tag.id }} />}
                    />
                    <DeleteTagButton tagId={tag.id} tagName={tag.name} />
                  </div>
                </li>
              ))}
          </ul>
        )}
      </PageWrapper>
    );
  }

  if (isError) return <ErrorPage error={error} />;

  return <LoadingPage title="Tags" />;
}

function DeleteTagButton({ tagId, tagName }: { tagId: string; tagName: string }) {
  const { mutate: deleteTag } = useDeleteTag(tagId);
  return (
    <ConfirmDialog
      trigger={
        <IconButton
          icon={<Trash2Icon />}
          tooltipContent={`Delete ${tagName}`}
          variant="destructive"
        />
      }
      title="Delete tag"
      description={
        <>
          Are you sure you want to delete <strong>{tagName}</strong>? This cannot be undone.
        </>
      }
      confirmLabel="Delete"
      onConfirm={() => deleteTag()}
    />
  );
}
