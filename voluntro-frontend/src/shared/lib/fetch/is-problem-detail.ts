import type { ProblemDetails } from "#/shared/types/api.types.ts";

export function isProblemDetails(value: unknown): value is ProblemDetails {
  return (
    typeof value === "object" &&
    value !== null &&
    ("title" in value || "detail" in value || "type" in value || "status" in value)
  );
}
