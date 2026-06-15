import { createFileRoute, Outlet } from "@tanstack/react-router";

import { SettingsLayout } from "#/shared/components/settings-layout.tsx";

export const Route = createFileRoute("/settings")({
  component: SettingLayoutComponent,
});

function SettingLayoutComponent() {
  return (
    <SettingsLayout>
      <Outlet />
    </SettingsLayout>
  );
}
