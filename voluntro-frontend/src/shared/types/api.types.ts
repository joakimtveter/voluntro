export type Pagination = {
  page: number;
  pageSize: number;
};

export type QueryOptions = {
  staleTime: number;
};

export type PaginatedList<T> = {
  items: Array<T>;
  currentPage: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
};

export type QueryParams = Record<
  string,
  string | number | boolean | null | undefined | Array<string | number | boolean>
>;

export type ResponseType = "json" | "text" | "blob" | "arrayBuffer";

export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  [key: string]: unknown;
};

export type ApiFetchOptions = Omit<RequestInit, "body"> & {
  query?: QueryParams;
  body?: unknown;
  responseType?: ResponseType;
};
