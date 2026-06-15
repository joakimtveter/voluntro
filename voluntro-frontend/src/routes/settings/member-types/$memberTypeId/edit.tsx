import { createFileRoute } from "@tanstack/react-router";

import MemberTypeForm from "#/domains/member-types/member-type.form.tsx";
import { useGetMemberTypes } from "#/domains/member-types/use-member-types.ts";
import PageWrapper from "#/shared/components/page-wrapper.tsx";
import ErrorPage from "#/shared/pages/error-page.tsx";
import LoadingPage from "#/shared/pages/loading-page.tsx";

export const Route = createFileRoute("/settings/member-types/$memberTypeId/edit")({
  component: RouteComponent,
});

function RouteComponent() {
  const { memberTypeId } = Route.useParams();
  const { data: memberTypes, isError, error } = useGetMemberTypes();

  const memberType = memberTypes?.find((m) => m.id === memberTypeId);

  if (memberTypes) {
    if (!memberType) return <ErrorPage error={new Error("Member type not found")} />;
    return (
      <PageWrapper title={`Edit "${memberType.name}"`}>
        <MemberTypeForm memberTypeId={memberType.id} defaultValues={{ name: memberType.name }} />
      </PageWrapper>
    );
  }

  if (isError) return <ErrorPage error={error} />;

  return <LoadingPage title="Edit member type" />;
}
