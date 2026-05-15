import * as z from "zod";

const AddressSchema = z.object({
  streetAddress: z.string().min(1).max(100),
  streetAddress2: z
    .string()
    .max(100)
    .transform((val) => (val === "" ? undefined : val)),
  postalCode: z.string().min(1).max(100),
  city: z.string().min(1).max(100),
  country: z
    .string()
    .max(100)
    .transform((val) => (val === "" ? undefined : val)),
});

export const createVenueSchema = z.object({
  name: z.string().min(1).max(100),
  description: z.string().max(250),
  address: AddressSchema,
});

export type VenueFormValues = z.input<typeof createVenueSchema>;
export type VenuePayload = z.output<typeof createVenueSchema>;
