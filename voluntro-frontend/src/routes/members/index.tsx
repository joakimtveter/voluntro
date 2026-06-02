import { createFileRoute, Link } from "@tanstack/react-router";
import {
  createColumnHelper,
  flexRender,
  getCoreRowModel,
  useReactTable,
} from "@tanstack/react-table";
import { EyeIcon, PencilIcon } from "lucide-react";

import type { MemberBrief } from "#/domains/members/member.types.ts";
import { useMembers } from "#/domains/members/use-members.ts";
import PageWrapper from "#/shared/components/page-wrapper.tsx";
import { IconLinkButton } from "#/shared/components/ui/icon-link-button.tsx";
import { LinkButton } from "#/shared/components/ui/link-button.tsx";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "#/shared/components/ui/table.tsx";
import { formatDate } from "#/shared/lib/datetime.ts";
import { formatName } from "#/shared/lib/formatName.ts";
import ErrorPage from "#/shared/pages/error-page.tsx";
import LoadingPage from "#/shared/pages/loading-page.tsx";

export const Route = createFileRoute("/members/")({
  component: RouteComponent,
});

const columnHelper = createColumnHelper<MemberBrief>();

const columns = [
  columnHelper.accessor(
    (row) => [row.firstName, row.middleNames, row.lastName].filter(Boolean).join(" "),
    {
      id: "name",
      header: "Name",
      cell: ({ row }) => (
        <Link
          to="/members/$memberId"
          params={{ memberId: row.original.id }}
          className="font-medium hover:underline"
        >
          {formatName(
            row.original.firstName,
            row.original.middleNames,
            row.original.lastName,
            "fl",
          )}
        </Link>
      ),
    },
  ),
  columnHelper.accessor("dateOfBirth", {
    header: "Date of birth",
    cell: ({ getValue }) => formatDate(getValue()),
  }),
  columnHelper.display({
    id: "actions",
    header: () => <span className="sr-only">Actions</span>,
    cell: ({ row }) => (
      <div className="flex justify-end">
        <IconLinkButton
          to="/members/$memberId"
          params={{ memberId: row.original.id }}
          aria-label={`Edit ${row.original.firstName} ${row.original.lastName}`}
        >
          <EyeIcon />
        </IconLinkButton>
        <IconLinkButton
          to="/members/$memberId/edit"
          params={{ memberId: row.original.id }}
          aria-label={`Edit ${row.original.firstName} ${row.original.lastName}`}
        >
          <PencilIcon />
        </IconLinkButton>
      </div>
    ),
  }),
];

function RouteComponent() {
  const { data, isError, error } = useMembers({ page: 1, pageSize: 25 });

  const table = useReactTable({
    data: data?.items ?? [],
    columns,
    getCoreRowModel: getCoreRowModel(),
  });

  if (data) {
    return (
      <PageWrapper
        title="Members"
        actions={<LinkButton to="/members/add">Add a new member</LinkButton>}
      >
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
                  No members found.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </PageWrapper>
    );
  }

  if (isError) return <ErrorPage error={error} />;

  return <LoadingPage title="Members" />;
}
