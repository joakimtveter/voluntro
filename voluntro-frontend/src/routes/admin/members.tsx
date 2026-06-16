import { createFileRoute } from "@tanstack/react-router";
import { flexRender, getCoreRowModel, useReactTable } from "@tanstack/react-table";
import { EyeIcon, PencilIcon, ShieldAlertIcon, Trash2Icon, Undo2Icon, XIcon } from "lucide-react";
import { useMemo, useState } from "react";
import * as z from "zod";


import {
  buildMemberColumns,
  GENDER_OPTIONS,
  memberColumnHelper,
  SORT_OPTIONS,
  type GenderValue,
  type Option,
  type SortValue,
  type TagFilterMode,
} from "#/domains/members/member-table.tsx";
import {
  useAdminMembers,
  useGdprDeleteMember,
  useRestoreMember,
} from "#/domains/members/use-members-admin.ts";
import { useDeleteMember } from "#/domains/members/use-members.ts";
import { useGetTags } from "#/domains/tags/use-tags.ts";
import { ConfirmDialog } from "#/shared/components/confirm-dialog.tsx";
import IconButton from "#/shared/components/icon-button.tsx";
import PageWrapper from "#/shared/components/page-wrapper.tsx";
import {
  Combobox,
  ComboboxChip,
  ComboboxChips,
  ComboboxContent,
  ComboboxEmpty,
  ComboboxInput,
  ComboboxItem,
  ComboboxList,
  ComboboxTrigger,
} from "#/shared/components/ui/combobox.tsx";
import { IconLinkButton } from "#/shared/components/ui/icon-link-button.tsx";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "#/shared/components/ui/select.tsx";
import { TablePagination } from "#/shared/components/ui/table-pagination.tsx";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "#/shared/components/ui/table.tsx";
import { formatName } from "#/shared/lib/formatName.ts";
import ErrorPage from "#/shared/pages/error-page.tsx";
import LoadingPage from "#/shared/pages/loading-page.tsx";

const adminMembersSearchSchema = z.object({
  sort: z
    .enum([
      "lastName-asc",
      "lastName-desc",
      "firstName-asc",
      "firstName-desc",
      "dateOfBirth-asc",
      "dateOfBirth-desc",
      "createdAt-asc",
      "createdAt-desc",
      "updatedAt-asc",
      "updatedAt-desc",
    ])
    .optional()
    .catch(undefined),
  gender: z.enum(["all", "female", "male", "unknown"]).optional().catch(undefined),
  tagIds: z.array(z.string()).optional().catch(undefined),
  tagFilterMode: z.enum(["any", "all"]).optional().catch(undefined),
  includeDeleted: z.boolean().optional().catch(undefined),
});

export const Route = createFileRoute("/admin/members")({
  validateSearch: adminMembersSearchSchema,
  component: RouteComponent,
});

function buildColumns(nameFormat: "fl" | "lf") {
  return [
    ...buildMemberColumns(nameFormat, { showDeletedBadge: true }),
    memberColumnHelper.display({
      id: "actions",
      header: () => <span className="sr-only">Actions</span>,
      cell: ({ row }) => (
        <div className="flex justify-end gap-1">
          {row.original.isDeleted ? (
            <RestoreMemberButton
              memberId={row.original.id}
              memberName={formatName(row.original.firstName, row.original.middleNames, row.original.lastName, "fl")}
            />
          ) : (
            <>
              <IconLinkButton
                to="/members/$memberId"
                params={{ memberId: row.original.id }}
                aria-label={`View ${row.original.firstName} ${row.original.lastName}`}
              >
                <EyeIcon />
              </IconLinkButton>
              <IconLinkButton
                to="/members/$memberId/edit"
                params={{ memberId: row.original.id }}
                search={{ returnTo: "/admin/members" }}
                aria-label={`Edit ${row.original.firstName} ${row.original.lastName}`}
              >
                <PencilIcon />
              </IconLinkButton>
              <DeleteMemberButton
                memberId={row.original.id}
                memberName={formatName(row.original.firstName, row.original.middleNames, row.original.lastName, "fl")}
              />
            </>
          )}
          <GdprDeleteButton
            memberId={row.original.id}
            memberName={formatName(row.original.firstName, row.original.middleNames, row.original.lastName, "fl")}
          />
        </div>
      ),
    }),
  ];
}

function RouteComponent() {
  const {
    sort = "lastName-asc",
    gender = "all",
    tagIds = [],
    tagFilterMode = "any",
    includeDeleted = false,
  } = Route.useSearch();
  const navigate = Route.useNavigate();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  const setSort = (v: SortValue) =>
    navigate({ search: (prev) => ({ ...prev, sort: v === "lastName-asc" ? undefined : v }) });
  const setGender = (v: GenderValue) =>
    navigate({ search: (prev) => ({ ...prev, gender: v === "all" ? undefined : v }) });
  const setTagIds = (ids: string[]) =>
    navigate({ search: (prev) => ({ ...prev, tagIds: ids.length === 0 ? undefined : ids }) });
  const setTagFilterMode = (mode: TagFilterMode) =>
    navigate({ search: (prev) => ({ ...prev, tagFilterMode: mode === "any" ? undefined : mode }) });
  const setIncludeDeleted = (v: boolean) => {
    navigate({ search: (prev) => ({ ...prev, includeDeleted: v || undefined }) });
    setPage(1);
  };

  const selectedSort = SORT_OPTIONS.find((o) => o.value === sort)!;
  const selectedGender = GENDER_OPTIONS.find((o) => o.value === gender)!;
  const { data: tags } = useGetTags();

  const tagsById = useMemo(() => new Map((tags ?? []).map((t) => [t.id, t])), [tags]);
  const tagOptions: Option[] = useMemo(
    () => (tags ?? []).map((t) => ({ value: t.id, label: t.name })),
    [tags],
  );
  const selectedTags: Option[] = useMemo(
    () =>
      tagIds.flatMap((id) => {
        const t = tagsById.get(id);
        return t ? [{ value: t.id, label: t.name }] : [];
      }),
    [tagIds, tagsById],
  );
  const tagSummary =
    selectedTags.length === 0
      ? "All tags"
      : selectedTags.length === 1
        ? selectedTags[0].label
        : `${selectedTags.length} tags (${tagFilterMode === "all" ? "all" : "any"})`;

  const { data, isError, error } = useAdminMembers({
    page,
    pageSize,
    includeDeleted,
    tagIds,
    tagFilterMode: tagIds.length > 1 ? tagFilterMode : undefined,
    legalGender: gender === "all" ? undefined : gender,
    sortBy: selectedSort.sortBy,
    sortOrder: selectedSort.sortOrder,
  });

  const columns = useMemo(
    () => buildColumns(selectedSort.sortBy === "lastName" ? "lf" : "fl"),
    [selectedSort.sortBy],
  );

  const table = useReactTable({
    data: data?.items ?? [],
    columns,
    getCoreRowModel: getCoreRowModel(),
  });

  if (data) {
    return (
      <PageWrapper title="Members admin">
        <div
          role="group"
          aria-label="Filter and sort members"
          className="mb-4 flex flex-wrap items-center gap-3"
        >
          {tags && tags.length > 0 && (
            <Combobox
              items={tagOptions}
              value={selectedTags}
              onValueChange={(opts) => setTagIds(opts.map((o) => o.value))}
              multiple
            >
              <ComboboxChips
                className="border-input focus-within:border-ring focus-within:ring-ring/50 dark:bg-input/30 flex min-h-9 w-fit max-w-xl items-center gap-1.5 overflow-hidden rounded-md border bg-transparent py-1.5 pr-1 pl-0 text-sm shadow-xs transition-[color,box-shadow] focus-within:ring-3"
                aria-label={`Tags: ${tagSummary}`}
              >
                <span
                  aria-hidden
                  className="bg-muted text-muted-foreground -my-1.5 flex items-center self-stretch pr-2 pl-2.5 text-sm"
                >
                  Tags
                </span>
                {selectedTags.length === 0 ? (
                  <span className="text-muted-foreground">All tags</span>
                ) : (
                  selectedTags.map((opt) => {
                    const tag = tagsById.get(opt.value);
                    return (
                      <ComboboxChip
                        key={opt.value}
                        className="text-white"
                        style={{ backgroundColor: tag?.color }}
                      >
                        {opt.label}
                      </ComboboxChip>
                    );
                  })
                )}
                <ComboboxTrigger className="text-muted-foreground hover:text-foreground ml-auto inline-flex size-6 items-center justify-center rounded-sm" />
              </ComboboxChips>
              <ComboboxContent className="min-w-56">
                <div
                  className="bg-muted m-1 mb-0 flex rounded-md p-0.5 text-xs"
                  role="radiogroup"
                  aria-label="Tag match mode"
                >
                  <button
                    type="button"
                    role="radio"
                    aria-checked={tagFilterMode === "any"}
                    onClick={() => setTagFilterMode("any")}
                    className={`flex-1 rounded-sm px-2 py-1 transition-colors ${tagFilterMode === "any" ? "bg-background text-foreground shadow-xs" : "text-muted-foreground hover:text-foreground"}`}
                  >
                    Match any
                  </button>
                  <button
                    type="button"
                    role="radio"
                    aria-checked={tagFilterMode === "all"}
                    onClick={() => setTagFilterMode("all")}
                    className={`flex-1 rounded-sm px-2 py-1 transition-colors ${tagFilterMode === "all" ? "bg-background text-foreground shadow-xs" : "text-muted-foreground hover:text-foreground"}`}
                  >
                    Match all
                  </button>
                </div>
                <ComboboxInput placeholder="Search tags…" showTrigger={false} />
                <ComboboxEmpty>No tags found.</ComboboxEmpty>
                <ComboboxList>
                  {(item) => (
                    <ComboboxItem key={item.value} value={item}>
                      {item.label}
                    </ComboboxItem>
                  )}
                </ComboboxList>
                {selectedTags.length > 0 && (
                  <div className="border-border border-t p-1">
                    <button
                      type="button"
                      onClick={() => setTagIds([])}
                      className="text-muted-foreground hover:bg-accent hover:text-accent-foreground flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm"
                    >
                      <XIcon className="size-4" />
                      Clear selection
                    </button>
                  </div>
                )}
              </ComboboxContent>
            </Combobox>
          )}

          <Select value={gender} onValueChange={(v) => setGender(v as GenderValue)}>
            <SelectTrigger
              className="overflow-hidden"
              aria-label={`Gender: ${selectedGender.label}`}
            >
              <span
                aria-hidden
                className="bg-muted text-muted-foreground -my-2 -ml-2.5 flex items-center self-stretch pr-2 pl-2.5"
              >
                Gender
              </span>
              <SelectValue>{selectedGender.label}</SelectValue>
            </SelectTrigger>
            <SelectContent>
              {GENDER_OPTIONS.map((opt) => (
                <SelectItem key={opt.value} value={opt.value}>
                  {opt.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={sort} onValueChange={(v) => setSort(v as SortValue)}>
            <SelectTrigger
              className="overflow-hidden"
              aria-label={`Sort by: ${selectedSort.label}`}
            >
              <span
                aria-hidden
                className="bg-muted text-muted-foreground -my-2 -ml-2.5 flex items-center self-stretch pr-2 pl-2.5"
              >
                Sort by
              </span>
              <SelectValue>{selectedSort.label}</SelectValue>
            </SelectTrigger>
            <SelectContent>
              {SORT_OPTIONS.map((opt) => (
                <SelectItem key={opt.value} value={opt.value}>
                  {opt.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <label className="flex cursor-pointer items-center gap-2 text-sm select-none">
            <input
              type="checkbox"
              checked={includeDeleted}
              onChange={(e) => setIncludeDeleted(e.target.checked)}
              className="accent-primary size-4 rounded"
            />
            Include deleted
          </label>
        </div>

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
                <TableRow
                  key={row.id}
                  className={row.original.isDeleted ? "opacity-60" : undefined}
                >
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

        <TablePagination
          currentPage={data.currentPage}
          totalPages={data.totalPages}
          pageSize={data.pageSize}
          hasPrevious={data.hasPrevious}
          hasNext={data.hasNext}
          onPageChange={setPage}
          onPageSizeChange={(size) => {
            setPageSize(size);
            setPage(1);
          }}
        />
      </PageWrapper>
    );
  }

  if (isError) return <ErrorPage error={error} />;

  return <LoadingPage title="Members" />;
}

function RestoreMemberButton({ memberId, memberName }: { memberId: string; memberName: string }) {
  const { mutate: restore } = useRestoreMember(memberId);
  return (
    <ConfirmDialog
      trigger={<IconButton icon={<Undo2Icon />} tooltipContent={`Restore ${memberName}`} />}
      title="Restore member"
      description={
        <>
          Restore <strong>{memberName}</strong>? They will become active again.
        </>
      }
      confirmLabel="Restore"
      onConfirm={() => restore()}
    />
  );
}

function DeleteMemberButton({ memberId, memberName }: { memberId: string; memberName: string }) {
  const { mutate: deleteMember } = useDeleteMember(memberId);
  return (
    <ConfirmDialog
      trigger={
        <IconButton
          icon={<Trash2Icon />}
          tooltipContent={`Delete ${memberName}`}
          variant="destructive"
        />
      }
      title="Delete member"
      description={
        <>
          Are you sure you want to delete <strong>{memberName}</strong>? They can be restored later.
        </>
      }
      confirmLabel="Delete"
      onConfirm={() => deleteMember()}
    />
  );
}

function GdprDeleteButton({ memberId, memberName }: { memberId: string; memberName: string }) {
  const { mutate: gdprDelete } = useGdprDeleteMember(memberId);
  return (
    <ConfirmDialog
      trigger={
        <IconButton
          icon={<ShieldAlertIcon />}
          tooltipContent={`Permanently erase ${memberName}`}
          className="bg-destructive hover:bg-destructive/70 text-white hover:!text-red-900"
        />
      }
      title="Permanently erase member data"
      description={
        <>
          This will permanently erase all personal data for <strong>{memberName}</strong> (GDPR Art.
          17). This action cannot be undone.
        </>
      }
      confirmLabel="Erase permanently"
      onConfirm={() => gdprDelete()}
    />
  );
}
