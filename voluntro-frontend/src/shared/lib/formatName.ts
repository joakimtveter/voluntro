export function formatName(
  firstName: string,
  middleNames: string | null,
  lastName: string,
  format?: "fl" | "lf",
) {
  if (format === "lf") {
    return `${lastName}, ${firstName} ${middleNames ?? ""}`.trim();
  }

  return [firstName, middleNames, lastName].join(" ");
}
