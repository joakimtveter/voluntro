import type { QueryParams } from "#/shared/types/api.types.ts";

export function buildUrl(path: string, query?: QueryParams): string {
  const BASE_URL = import.meta.env.VITE_BASE_URL;
  if (!BASE_URL) throw new Error("No BASE_URL found in environment");

  const url = new URL(BASE_URL + path);
  if (!query) return url.toString();

  for (const [key, value] of Object.entries(query)) {
    if (value === null || value === undefined) {
      continue;
    }

    if (Array.isArray(value)) {
      for (const item of value) {
        url.searchParams.append(key, String(item));
      }
    } else {
      url.searchParams.set(key, String(value));
    }
  }

  return url.toString();
}
