import type { Address } from "#/shared/types/address.types.ts";

export type VenueBrief = {
  id: string;
  name: string;
  isDeleted: boolean;
  createdAt: string;
  updatedAt: string;
};

export type Venue = {
  description: string;
  address: Address;
} & VenueBrief;
