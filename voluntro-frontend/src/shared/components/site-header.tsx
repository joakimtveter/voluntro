import { Link } from "@tanstack/react-router";

import { ThemeSwitcher } from "#/shared/components/theme-switcher.tsx";

export default function SiteHeader() {
  return (
    <header className="sticky top-0 z-50 flex w-full justify-between border-b border-(--line) bg-(--header-bg) px-4 backdrop-blur-lg">
      <nav className="page-wrap flex flex-wrap items-center gap-x-3 gap-y-2 py-3 sm:py-4">
        <Link to="/">Home</Link>
        <Link to="/members">Members</Link>
        <Link to="/events">Events</Link>
        <Link to="/venues">Venues</Link>
        <Link to="/groups">Groups</Link>
        <Link to="/admin">Admin</Link>
      </nav>
      <ThemeSwitcher />
    </header>
  );
}
