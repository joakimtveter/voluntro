import type { VenueBrief } from "#/domains/venues/venue.types.ts";

export type EventBrief = {
  id: string;
  title: string;
  description: string;
  eventStart: string;
  eventEnd: string;
};

export type Event = {
  venueId: string;
  venue: VenueBrief;
} & EventBrief;
