import type { ResponseType } from "#/shared/types/api.types.ts";

export async function parseSuccessBody(
  response: Response,
  responseType: ResponseType,
): Promise<unknown> {
  if (response.status === 204 || response.status === 205) {
    return undefined;
  }

  switch (responseType) {
    case "json": {
      const text = await response.text();

      if (!text) {
        return undefined;
      }

      return JSON.parse(text);
    }

    case "text":
      return response.text();

    case "blob":
      return response.blob();

    case "arrayBuffer":
      return response.arrayBuffer();

    default:
      return response.text();
  }
}
