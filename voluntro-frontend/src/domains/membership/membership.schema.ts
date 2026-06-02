import * as z from "zod";

export const membershipFormValidationSchema = z.object({
  memberId: z.uuid(),
  groupId: z.uuid(),
});

export type MembershipPayload = z.output<typeof membershipFormValidationSchema>;
