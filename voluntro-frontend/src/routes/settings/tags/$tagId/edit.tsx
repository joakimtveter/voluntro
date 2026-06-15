import { createFileRoute } from "@tanstack/react-router";

import TagForm from "#/domains/tags/tag.form.tsx";
import { useGetTags } from "#/domains/tags/use-tags.ts";
import PageWrapper from "#/shared/components/page-wrapper.tsx";
import ErrorPage from "#/shared/pages/error-page.tsx";
import LoadingPage from "#/shared/pages/loading-page.tsx";

export const Route = createFileRoute("/settings/tags/$tagId/edit")({
  component: RouteComponent,
});

function RouteComponent() {
  const { tagId } = Route.useParams();
  const { data: tags, isError, error } = useGetTags();

  const tag = tags?.find((t) => t.id === tagId);

  if (tags) {
    if (!tag) return <ErrorPage error={new Error(`Tag not found`)} />;
    return (
      <PageWrapper title={`Edit "${tag.name}"`}>
        <TagForm tagId={tag.id} defaultValues={{ name: tag.name, color: tag.color }} />
      </PageWrapper>
    );
  }

  if (isError) return <ErrorPage error={error} />;

  return <LoadingPage title="Edit tag" />;
}
