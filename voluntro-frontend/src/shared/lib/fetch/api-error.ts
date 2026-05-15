import type { ProblemDetails } from "#/shared/types/api.types.ts";

export class ApiError extends Error {
  status: number;
  statusText: string;
  url: string;
  problem?: ProblemDetails;
  responseBody?: unknown;

  constructor(args: {
    message: string;
    status: number;
    statusText: string;
    url: string;
    problem?: ProblemDetails;
    responseBody?: unknown;
  }) {
    super(args.message);

    this.name = "ApiError";
    this.status = args.status;
    this.statusText = args.statusText;
    this.url = args.url;
    this.problem = args.problem;
    this.responseBody = args.responseBody;
  }
}
