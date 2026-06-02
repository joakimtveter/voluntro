import * as z from "zod";

import { LegalGender } from "#/domains/members/member.types.ts";

export const memberFormValidationSchema = z.object({
  firstName: z.string().min(1),
  middleNames: z.string().transform((v) => (v.trim() === "" ? null : v)),
  lastName: z.string().min(1),
  dateOfBirth: z.iso
    .date()
    .or(z.literal("").transform(() => undefined))
    .refine(
      (value) => {
        if (value === undefined) return true;
        const tomorrow = new Date();
        tomorrow.setUTCDate(tomorrow.getUTCDate() + 1);
        return value <= tomorrow.toISOString().slice(0, 10);
      },
      { message: "Date of birth cannot be in the future" },
    ),
  legalGender: z.enum(LegalGender),
});

export type MemberFormValues = z.input<typeof memberFormValidationSchema>;
export type MemberPayload = z.output<typeof memberFormValidationSchema>;
