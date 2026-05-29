import { createFileRoute, Link } from "@tanstack/react-router";
import {
  createColumnHelper,
  flexRender,
  getCoreRowModel,
  useReactTable,
} from "@tanstack/react-table";
import { EyeIcon, PencilIcon } from "lucide-react";
import * as z from "zod";

import { useVenues, useVenuesQueryOptions } from "#/domains/venues/use-venues.ts";
import type { VenueBrief } from "#/domains/venues/venue.types.ts";
import PageWrapper from "#/shared/components/page-wrapper.tsx";
import { IconLinkButton } from "#/shared/components/ui/icon-link-button.tsx";
import { LinkButton } from "#/shared/components/ui/link-button.tsx";
import { TablePagination } from "#/shared/components/ui/table-pagination.tsx";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "#/shared/components/ui/table.tsx";
import { SITE_TITLE } from "#/shared/constants/constants.ts";
import ErrorPage from "#/shared/pages/error-page.tsx";
import LoadingPage from "#/shared/pages/loading-page.tsx";

const venuesSearchSchema = z.object({
  page: z.number().int().min(1).catch(1).default(1),
  pageSize: z.number().int().min(1).max(100).catch(25).default(25),
});
const PAGE_TITLE = "Venues";

export const Route = createFileRoute("/venues/")({
  validateSearch: venuesSearchSchema,
  loaderDeps: ({ search: { page, pageSize } }) => ({ page, pageSize }),
  loader: ({ context: { queryClient }, deps: { page, pageSize } }) =>
    queryClient.ensureQueryData(useVenuesQueryOptions({ page, pageSize })),
  component: VenuePage,
  head: () => ({
    meta: [{ title: `${PAGE_TITLE} | ${SITE_TITLE}` }],
  }),
});

const columnHelper = createColumnHelper<VenueBrief>();

const columns = [
  columnHelper.accessor("name", {
    header: "Name",
    cell: ({ row }) => (
      <Link
        to="/venues/$venueId"
        params={{ venueId: row.original.id }}
        className="font-medium hover:underline"
      >
        {row.original.name}
      </Link>
    ),
  }),
  columnHelper.display({
    id: "actions",
    header: () => <span className="sr-only">Actions</span>,
    cell: ({ row }) => (
      <div className="flex justify-end">
        <IconLinkButton
          to="/venues/$venueId"
          params={{ venueId: row.original.id }}
          aria-label={`View ${row.original.name}`}
        >
          <EyeIcon />
        </IconLinkButton>
        <IconLinkButton
          to="/venues/$venueId/edit"
          params={{ venueId: row.original.id }}
          aria-label={`Edit ${row.original.name}`}
        >
          <PencilIcon />
        </IconLinkButton>
      </div>
    ),
  }),
];

function VenuePage() {
  const { page, pageSize } = Route.useSearch();
  const navigate = Route.useNavigate();
  const { data, isError, error } = useVenues({ page, pageSize });

  const table = useReactTable({
    data: data?.items ?? [],
    columns,
    getCoreRowModel: getCoreRowModel(),
  });

  const goToPage = (p: number) => navigate({ search: (prev) => ({ ...prev, page: p }) });
  const changePageSize = (size: number) =>
    navigate({ search: (prev) => ({ ...prev, page: 1, pageSize: size }) });

  if (data) {
    return (
      <PageWrapper title="Venues" actions={<LinkButton to="/venues/add">Add a venue</LinkButton>}>
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id}>
                {headerGroup.headers.map((header) => (
                  <TableHead key={header.id}>
                    {flexRender(header.column.columnDef.header, header.getContext())}
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {table.getRowModel().rows.length > 0 ? (
              table.getRowModel().rows.map((row) => (
                <TableRow key={row.id}>
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id}>
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell
                  colSpan={columns.length}
                  className="text-muted-foreground py-10 text-center"
                >
                  No venues found.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>

        <TablePagination
          currentPage={data.currentPage}
          totalPages={data.totalPages}
          pageSize={pageSize}
          hasPrevious={data.hasPrevious}
          hasNext={data.hasNext}
          onPageChange={goToPage}
          onPageSizeChange={changePageSize}
        />
      </PageWrapper>
    );
  }

  if (isError) return <ErrorPage error={error} />;

  return <LoadingPage title={PAGE_TITLE} />;
}
