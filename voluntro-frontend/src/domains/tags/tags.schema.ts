import * as z from "zod";

import { TAG_COLORS } from "#/domains/tags/tag.constants.ts";

const tagColorValues = TAG_COLORS.map((c) => c.value) as [string, ...string[]];

export const tagFormValidationSchema = z.object({
  name: z.string().max(100),
  color: z.enum(tagColorValues),
});

export type TagFormValues = z.input<typeof tagFormValidationSchema>;
export type TagPayload = z.output<typeof tagFormValidationSchema>;
