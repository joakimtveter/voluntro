import * as z from "zod";

export const groupFormValidationSchema = z.object({
  name: z.string().min(1),
  description: z.string(),
  parentGroupId: z.uuid().nullable(),
});

export type GroupFormValues = z.input<typeof groupFormValidationSchema>;
export type GroupPayload = z.output<typeof groupFormValidationSchema>;
