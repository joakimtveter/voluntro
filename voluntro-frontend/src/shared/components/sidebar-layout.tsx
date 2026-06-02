import { MenuIcon, XIcon } from "lucide-react";
import { type ReactNode, useState } from "react";

import { Button } from "#/shared/components/ui/button";

interface SidebarLayoutProps {
  sidebar: ReactNode;
  children: ReactNode;
}

export function SidebarLayout({ sidebar, children }: SidebarLayoutProps) {
  const [open, setOpen] = useState(false);

  return (
    <div className="grid h-full grid-cols-1 grid-rows-[auto_1fr] sm:grid-cols-[auto_1fr] sm:grid-rows-1">
      <Button
        variant="outline"
        size="icon"
        onClick={() => setOpen((o) => !o)}
        aria-label={open ? "Close menu" : "Open menu"}
        className="z-50 col-start-1 row-start-1 m-2 self-start justify-self-start sm:hidden"
      >
        {open ? <XIcon className="size-5" /> : <MenuIcon className="size-5" />}
      </Button>

      <aside
        style={{ background: "hotpink" }}
        className={`z-40 col-start-1 row-start-2 min-w-50 sm:col-start-1 sm:row-start-1 sm:min-h-dvh ${open ? "block" : "hidden sm:block"}`}
      >
        {sidebar}
      </aside>

      <main className="col-start-1 row-start-2 sm:col-start-2 sm:row-start-1">{children}</main>
    </div>
  );
}
