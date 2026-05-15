import { AlertCircleIcon } from "lucide-react";

import PageWrapper from "#/shared/components/page-wrapper.tsx";
import { Alert, AlertDescription, AlertTitle } from "#/shared/components/ui/alert";

type ErrorPageProps = {
  title?: string;
  error: Error;
};

export default function ErrorPage(props: ErrorPageProps) {
  const { title = "Something went wrong!", error } = props;
  return (
    <PageWrapper title={title}>
      <Alert variant="destructive" className="max-w-md">
        <AlertCircleIcon />
        <AlertTitle>{error.name}</AlertTitle>
        <AlertDescription>{error.message}</AlertDescription>
      </Alert>
    </PageWrapper>
  );
}
