import PageWrapper from "#/shared/components/page-wrapper.tsx";

type NotFoundPageProps = {
  title?: string;
  message?: string;
};

export default function NotFoundPage(props: NotFoundPageProps) {
  const { title = "Page Not Found", message = "Unable to find page" } = props;
  return (
    <PageWrapper title={title}>
      <p>{message}</p>
    </PageWrapper>
  );
}
