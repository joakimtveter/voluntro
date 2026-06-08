import { Link } from "@tanstack/react-router";
import type { ReactNode } from "react";

import { SidebarLayout } from "#/shared/components/sidebar-layout.tsx";

const navItems = [
  { title: "Members", to: "/admin/members" },
  { title: "Groups", to: "/admin/groups" },
  { title: "Events", to: "/admin/events" },
  { title: "Venues", to: "/admin/venues" },
  { title: "Tags", to: "/admin/tags" },
];

function Sidebar() {
  return (
    <nav className="flex flex-col gap-2 p-4">
      {navItems.map((item) => (
        <Link key={item.to} to={item.to} className="rounded px-3 py-2 hover:underline">
          {item.title}
        </Link>
      ))}
    </nav>
  );
}

interface AdminLayoutProps {
  children: ReactNode;
}

export function AdminLayout({ children }: AdminLayoutProps) {
  return <SidebarLayout sidebar={<Sidebar />}>{children}</SidebarLayout>;
}
