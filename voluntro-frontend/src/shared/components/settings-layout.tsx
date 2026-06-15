import type { ReactNode } from "react";

import { SidebarLayout } from "#/shared/components/sidebar-layout.tsx";
import { SidebarNav } from "#/shared/components/sidebar-nav.tsx";

const navItems = [
  { title: "Member types", to: "/settings/member-types" },
  { title: "Tags", to: "/settings/tags" },
];

interface LayoutProps {
  children: ReactNode;
}

export function SettingsLayout({ children }: LayoutProps) {
  return (
    <SidebarLayout sidebar={<SidebarNav items={navItems} />}>{children}</SidebarLayout>
  );
}
