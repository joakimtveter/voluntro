import { createFileRoute, Outlet } from "@tanstack/react-router";

import { AdminLayout } from "#/shared/components/admin-layout.tsx";

export const Route = createFileRoute("/admin")({
  component: AdminLayoutRouteComponent,
});

function AdminLayoutRouteComponent() {
  return (
    <AdminLayout>
      <Outlet />
    </AdminLayout>
  );
}
