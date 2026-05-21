import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute('/admin/venues')({
  component: RouteComponent,
})

function RouteComponent() {
  return <div>Hello "/admin/venues"!</div>
}
