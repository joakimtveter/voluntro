import PageWrapper from "#/shared/components/page-wrapper.tsx";
import { Spinner } from "#/shared/components/ui/spinner.tsx";

type LoadingPageProps = {
  title?: string;
};
export default function LoadingPage(props: LoadingPageProps) {
  return (
    <PageWrapper title={props.title ?? ""}>
      <div>
        <Spinner />
        <p>Loading</p>
      </div>
    </PageWrapper>
  );
}
