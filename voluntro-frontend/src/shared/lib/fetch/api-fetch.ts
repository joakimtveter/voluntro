import { ApiError } from "#/shared/lib/fetch/api-error.ts";
import { buildUrl } from "#/shared/lib/fetch/build-url.ts";
import { isProblemDetails } from "#/shared/lib/fetch/is-problem-detail.ts";
import { parseErrorBody } from "#/shared/lib/fetch/parse-error-body.ts";
import { parseSuccessBody } from "#/shared/lib/fetch/parse-success-body.ts";
import type { ApiFetchOptions } from "#/shared/types/api.types.ts";

export async function apiFetch<T>(url: string, options: ApiFetchOptions = {}): Promise<T> {
  const { query, headers, body, responseType = "json", ...fetchOptions } = options;

  const finalUrl = buildUrl(url, query);

  const response = await fetch(finalUrl, {
    ...fetchOptions,
    headers: {
      Accept: "application/json, application/problem+json",
      ...(body !== undefined ? { "Content-Type": "application/json" } : {}),
      ...headers,

      // TODO: Add auth later here, for example:
      // Authorization: `Bearer ${token}`,
    },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (!response.ok) {
    const responseBody = await parseErrorBody(response);
    const problem = isProblemDetails(responseBody) ? responseBody : undefined;

    const message =
      problem?.detail ||
      problem?.title ||
      response.statusText ||
      `Request failed with status ${response.status}`;

    throw new ApiError({
      message,
      status: response.status,
      statusText: response.statusText,
      url: finalUrl,
      problem,
      responseBody,
    });
  }

  const responseBody = await parseSuccessBody(response, responseType);

  return responseBody as T;
}
