import type { ReactNode } from "react";

import Heading from "#/shared/components/heading.tsx";

type PageWrapperProps = {
  title: string;
  subTitle?: string;
  children?: ReactNode | ReactNode[];
  actions?: ReactNode | ReactNode[];
};

export default function PageWrapper(props: PageWrapperProps) {
  const { title, subTitle, children, actions } = props;
  return (
    <main className="m-auto w-full px-8 py-6">
      <div className="mb-2 flex items-center justify-between">
        <hgroup>
          <Heading level={1} size="xl">
            {title}
          </Heading>
          {!!subTitle && <p>{subTitle}</p>}
        </hgroup>
        {actions != undefined && <div>{actions}</div>}
      </div>
      {children}
    </main>
  );
}
