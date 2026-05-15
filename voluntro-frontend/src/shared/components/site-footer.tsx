export default function SiteFooter() {
  const year = new Date().getFullYear();

  return (
    <footer className="border-t border-(--line) px-4 pt-10 pb-14">
      <div className="page-wrap flex flex-col items-center justify-between gap-4 text-center sm:flex-row sm:text-left">
        <p className="m-0 text-sm">&copy; {year} Joakim Hannestad Tveter. All rights reserved.</p>
        <p className="island-kicker m-0">Built with TanStack Start</p>
      </div>
    </footer>
  );
}
