import { createFileRoute, Link } from "@tanstack/react-router";
import { PencilIcon, Trash2Icon } from "lucide-react";

import { useDeleteTag, useGetTags } from "#/domains/tags/use-tags.ts";
import PageWrapper from "#/shared/components/page-wrapper.tsx";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogBackdrop,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogPortal,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "#/shared/components/ui/alert-dialog.tsx";
import { Button } from "#/shared/components/ui/button.tsx";
import { LinkButton } from "#/shared/components/ui/link-button.tsx";
import ErrorPage from "#/shared/pages/error-page.tsx";
import LoadingPage from "#/shared/pages/loading-page.tsx";

export const Route = createFileRoute("/admin/tags/")({
  component: RouteComponent,
});

function DeleteTagButton({ tagId, tagName }: { tagId: string; tagName: string }) {
  const { mutate: deleteTag } = useDeleteTag(tagId);
  return (
    <AlertDialog>
      <AlertDialogTrigger render={<Button variant="destructive" size="icon" aria-label={`Delete ${tagName}`} />}>
        <Trash2Icon />
      </AlertDialogTrigger>
      <AlertDialogPortal>
        <AlertDialogBackdrop />
        <AlertDialogContent>
          <AlertDialogTitle>Delete tag</AlertDialogTitle>
          <AlertDialogDescription>
            Are you sure you want to delete <strong>{tagName}</strong>? This cannot be undone.
          </AlertDialogDescription>
          <div className="flex justify-end gap-2">
            <AlertDialogCancel render={<Button variant="outline" />}>Cancel</AlertDialogCancel>
            <AlertDialogAction render={<Button variant="destructive" />} onClick={() => deleteTag()}>
              Delete
            </AlertDialogAction>
          </div>
        </AlertDialogContent>
      </AlertDialogPortal>
    </AlertDialog>
  );
}

function RouteComponent() {
  const { data: tags, isError, error } = useGetTags();

  if (tags) {
    return (
      <PageWrapper
        title="Tags"
        actions={<LinkButton to="/admin/tags/add">Add tag</LinkButton>}
      >
        {tags.length === 0 ? (
          <p className="text-muted-foreground py-10 text-center text-sm">No tags found.</p>
        ) : (
          <ul className="divide-y divide-border rounded-md border">
            {tags.map((tag) => (
              <li key={tag.id} className="flex items-center gap-3 px-4 py-3">
                <span
                  className="size-3 shrink-0 rounded-full"
                  style={{ backgroundColor: tag.color }}
                  aria-hidden
                />
                <span className="flex-1 text-sm font-medium">{tag.name}</span>
                <span className="text-muted-foreground font-mono text-xs">{tag.color}</span>
                <div className="flex gap-1">
                  <Button variant="outline" size="icon" aria-label={`Edit ${tag.name}`} render={<Link to="/admin/tags/$tagId/edit" params={{ tagId: tag.id }} />}>
                    <PencilIcon />
                  </Button>
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
