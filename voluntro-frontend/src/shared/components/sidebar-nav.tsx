import { Link } from "@tanstack/react-router";

type NavItem = {
  title: string;
  to: string;
};

export function SidebarNav({ items }: { items: NavItem[] }) {
  return (
    <nav className="flex flex-col gap-2 py-4">
      {items.map((item) => (
        <Link
          key={item.to}
          to={item.to}
          className="hover:bg-muted [&.active]:bg-secondary block px-4 py-2 [&.active]:font-medium"
        >
          {item.title}
        </Link>
      ))}
    </nav>
  );
}
