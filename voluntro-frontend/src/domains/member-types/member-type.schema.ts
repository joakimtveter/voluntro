import * as z from "zod";

export const memberTypeFormValidationSchema = z.object({
  name: z.string().min(1).max(100),
});

export type MemberTypeFormValues = z.input<typeof memberTypeFormValidationSchema>;
export type MemberTypePayload = z.output<typeof memberTypeFormValidationSchema>;
