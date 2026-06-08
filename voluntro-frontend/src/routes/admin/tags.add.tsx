import { createFileRoute } from "@tanstack/react-router";

import TagForm from "#/domains/tags/tag.form.tsx";
import PageWrapper from "#/shared/components/page-wrapper.tsx";

export const Route = createFileRoute("/admin/tags/add")({
  component: RouteComponent,
});

function RouteComponent() {
  return (
    <PageWrapper title="Add tag">
      <TagForm />
    </PageWrapper>
  );
}
