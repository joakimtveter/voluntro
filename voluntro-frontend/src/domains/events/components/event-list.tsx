import { Link } from "@tanstack/react-router";
import { CalendarIcon, ClockIcon } from "lucide-react";

import type { EventBrief } from "#/domains/events/events.types.ts";
import { formatDate, formatTime } from "#/shared/lib/datetime.ts";

type EventListProps = {
  events: EventBrief[];
};

export default function EventList(props: EventListProps) {
  const { events } = props;

  if (events.length === 0) return <p className="text-muted-foreground">No events found.</p>;

  return (
    <section data-component="event-list">
      <ul>
        {events.map((event) => (
          <EventCard key={event.id} event={event} />
        ))}
      </ul>
    </section>
  );
}

function EventCard({ event }: { event: EventBrief }) {
  const sameDay = event.eventStart.slice(0, 10) === event.eventEnd.slice(0, 10);

  return (
    <Link
      to="/events/$eventId"
      params={{ eventId: event.id }}
      className="bg-card hover:bg-muted/50 block rounded-lg border p-4 transition-colors"
    >
      <p className="text-foreground font-semibold">{event.title}</p>
      {event.description && (
        <p className="text-muted-foreground mt-1 line-clamp-2 text-sm">{event.description}</p>
      )}
      <div className="text-muted-foreground mt-3 flex flex-wrap gap-3 text-xs">
        <span className="flex items-center gap-1">
          <CalendarIcon className="size-3.5" />
          {sameDay
            ? formatDate(event.eventStart, "long")
            : `${formatDate(event.eventStart, "long")} – ${formatDate(event.eventEnd, "long")}`}
        </span>
        <span className="flex items-center gap-1">
          <ClockIcon className="size-3.5" />
          {formatTime(event.eventStart)} – {formatTime(event.eventEnd)}
        </span>
      </div>
    </Link>
  );
}
