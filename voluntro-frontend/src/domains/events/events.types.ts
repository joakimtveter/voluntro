import type { VenueBrief } from "#/domains/venues/venue.types.ts";

export type EventBrief = {
  id: string;
  title: string;
  description: string;
  startsAt: string;
  endsAt: string;
  venueId: string;
};

export type Event = {
  venue: VenueBrief;
} & EventBrief;
