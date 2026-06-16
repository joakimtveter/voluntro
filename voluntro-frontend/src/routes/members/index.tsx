import { createFileRoute } from "@tanstack/react-router";
import { flexRender, getCoreRowModel, useReactTable } from "@tanstack/react-table";
import { EyeIcon, PencilIcon, XIcon } from "lucide-react";
import { useMemo } from "react";
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
import { useMembers } from "#/domains/members/use-members.ts";
import { useGetTags } from "#/domains/tags/use-tags.ts";
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
import { LinkButton } from "#/shared/components/ui/link-button.tsx";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "#/shared/components/ui/select.tsx";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "#/shared/components/ui/table.tsx";
import { cn } from "#/shared/lib/utils.ts";
import ErrorPage from "#/shared/pages/error-page.tsx";
import LoadingPage from "#/shared/pages/loading-page.tsx";

const membersSearchSchema = z.object({
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
});

export const Route = createFileRoute("/members/")({
  validateSearch: membersSearchSchema,
  component: MembersListPage,
});

function buildColumns(nameFormat: "fl" | "lf") {
  return [
    ...buildMemberColumns(nameFormat),
    memberColumnHelper.display({
      id: "actions",
      header: () => <span className="sr-only">Actions</span>,
      cell: ({ row }) => (
        <div className="flex justify-end">
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
            search={{ returnTo: "/members/" }}
            aria-label={`Edit ${row.original.firstName} ${row.original.lastName}`}
          >
            <PencilIcon />
          </IconLinkButton>
        </div>
      ),
    }),
  ];
}

function MembersListPage() {
  const {
    sort = "lastName-asc",
    gender = "all",
    tagIds = [],
    tagFilterMode = "any",
  } = Route.useSearch();
  const navigate = Route.useNavigate();
  const { data: tags } = useGetTags();

  const selectedSort = SORT_OPTIONS.find((o) => o.value === sort)!;
  const selectedGender = GENDER_OPTIONS.find((o) => o.value === gender)!;

  const tagsById = useMemo(() => new Map((tags ?? []).map((t) => [t.id, t])), [tags]);
  const tagOptions: Option[] = useMemo(
    () => (tags ?? []).map((t) => ({ value: t.id, label: t.name })),
    [tags],
  );
  const selectedOptions: Option[] = useMemo(
    () =>
      tagIds.flatMap((id) => {
        const t = tagsById.get(id);
        return t ? [{ value: t.id, label: t.name }] : [];
      }),
    [tagIds, tagsById],
  );

  const setSort = (v: SortValue) =>
    navigate({ search: (prev) => ({ ...prev, sort: v === "lastName-asc" ? undefined : v }) });
  const setGender = (v: GenderValue) =>
    navigate({ search: (prev) => ({ ...prev, gender: v === "all" ? undefined : v }) });
  const setTagIds = (ids: string[]) =>
    navigate({ search: (prev) => ({ ...prev, tagIds: ids.length === 0 ? undefined : ids }) });
  const setTagFilterMode = (mode: TagFilterMode) =>
    navigate({ search: (prev) => ({ ...prev, tagFilterMode: mode === "any" ? undefined : mode }) });

  const { data, isError, error } = useMembers({
    page: 1,
    pageSize: 25,
    tagIds,
    tagFilterMode: tagIds.length > 1 ? tagFilterMode : undefined,
    legalGender: gender === "all" ? undefined : gender,
    sortBy: selectedSort.sortBy,
    sortOrder: selectedSort.sortOrder,
  });

  const tagSummary =
    selectedOptions.length === 0
      ? "All tags"
      : selectedOptions.length === 1
        ? selectedOptions[0].label
        : `${selectedOptions.length} tags (${tagFilterMode === "all" ? "all" : "any"})`;

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
      <PageWrapper
        title="Members"
        actions={<LinkButton to="/members/add">Add a new member</LinkButton>}
      >
        <div
          role="group"
          aria-label="Filter and sort members"
          className="flex flex-wrap items-center gap-3"
        >
          {tags && tags.length > 0 && (
            <Combobox
              items={tagOptions}
              value={selectedOptions}
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
                {selectedOptions.length === 0 ? (
                  <span className="text-muted-foreground">All tags</span>
                ) : (
                  selectedOptions.map((opt) => {
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
                    className={cn(
                      "flex-1 rounded-sm px-2 py-1 transition-colors",
                      tagFilterMode === "any"
                        ? "bg-background text-foreground shadow-xs"
                        : "text-muted-foreground hover:text-foreground",
                    )}
                  >
                    Match any
                  </button>
                  <button
                    type="button"
                    role="radio"
                    aria-checked={tagFilterMode === "all"}
                    onClick={() => setTagFilterMode("all")}
                    className={cn(
                      "flex-1 rounded-sm px-2 py-1 transition-colors",
                      tagFilterMode === "all"
                        ? "bg-background text-foreground shadow-xs"
                        : "text-muted-foreground hover:text-foreground",
                    )}
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
                {selectedOptions.length > 0 && (
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
