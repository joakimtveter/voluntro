export async function parseErrorBody(response: Response): Promise<string | undefined> {
  if (response.status === 204 || response.status === 205) {
    return undefined;
  }

  const contentType = response.headers.get("content-type") ?? "";
  const text = await response.text();

  if (!text) {
    return undefined;
  }

  if (
    contentType.includes("application/json") ||
    contentType.includes("application/problem+json")
  ) {
    try {
      return JSON.stringify(JSON.parse(text));
    } catch {
      return text;
    }
  }

  return text;
}
