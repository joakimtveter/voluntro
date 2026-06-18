import { TanStackDevtools } from "@tanstack/react-devtools";
import { FormDevtoolsPanel } from "@tanstack/react-form-devtools";
import type { QueryClient } from "@tanstack/react-query";
import { ReactQueryDevtoolsPanel } from "@tanstack/react-query-devtools";
import { HeadContent, Scripts, createRootRouteWithContext } from "@tanstack/react-router";
import { TanStackRouterDevtoolsPanel } from "@tanstack/react-router-devtools";
import type { ReactNode } from "react";

import SiteFooter from "#/shared/components/site-footer.tsx";
import SiteHeader from "#/shared/components/site-header.tsx";
import { Toaster } from "#/shared/components/ui/sonner.tsx";
import { TooltipProvider } from "#/shared/components/ui/tooltip";
import NotFoundPage from "#/shared/pages/not-found-page.tsx";

import appCss from "../styles.css?url";

const THEME_INIT_SCRIPT = `(function(){try{var t=localStorage.getItem('theme');if(t==='light'||t==='dark')document.documentElement.setAttribute('data-theme',t);}catch(e){}})();`;

export const Route = createRootRouteWithContext<{ queryClient: QueryClient }>()({
  head: () => ({
    meta: [
      {
        charSet: "utf-8",
      },
      {
        name: "viewport",
        content: "width=device-width, initial-scale=1",
      },
      {
        title: "Voluntro CRM",
      },
    ],
    links: [
      {
        rel: "stylesheet",
        href: appCss,
      },
    ],
  }),
  shellComponent: RootDocument,
  notFoundComponent: () => <NotFoundPage />,
});

function RootDocument({ children }: { children: ReactNode }) {
  return (
    <html lang="en" data-theme="light" suppressHydrationWarning>
      <head>
        <script dangerouslySetInnerHTML={{ __html: THEME_INIT_SCRIPT }} />
        <HeadContent />
      </head>
      <body className="flex h-dvh flex-col overflow-hidden font-sans wrap-anywhere antialiased selection:bg-[rgba(79,184,178,0.24)]">
        <TooltipProvider>
          <SiteHeader />
          <div className="flex-1 overflow-y-auto">{children}</div>
          <SiteFooter />
          <Toaster />
          {import.meta.env.DEV && (
            <TanStackDevtools
              config={{
                position: "bottom-right",
              }}
              plugins={[
                {
                  name: "Tanstack Router",
                  render: <TanStackRouterDevtoolsPanel />,
                },
                {
                  name: "Tanstack Query",
                  render: <ReactQueryDevtoolsPanel />,
                },
                {
                  name: "Tanstack Form",
                  render: <FormDevtoolsPanel />,
                },
              ]}
            />
          )}
          <Scripts />
        </TooltipProvider>
      </body>
    </html>
  );
}
