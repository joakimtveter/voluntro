import { Link } from "@tanstack/react-router";
import { createColumnHelper } from "@tanstack/react-table";

import type { LegalGenderEnum, MemberBrief } from "#/domains/members/member.types.ts";
import { Badge } from "#/shared/components/ui/badge.tsx";
import { Tooltip, TooltipContent, TooltipTrigger } from "#/shared/components/ui/tooltip.tsx";
import { formatDate } from "#/shared/lib/datetime.ts";
import { formatName } from "#/shared/lib/formatName.ts";

export type SortValue =
  | "lastName-asc"
  | "lastName-desc"
  | "firstName-asc"
  | "firstName-desc"
  | "dateOfBirth-asc"
  | "dateOfBirth-desc"
  | "createdAt-asc"
  | "createdAt-desc"
  | "updatedAt-asc"
  | "updatedAt-desc";

export type GenderValue = "all" | LegalGenderEnum;
export type TagFilterMode = "any" | "all";
export type Option = { value: string; label: string };

export const SORT_OPTIONS: {
  value: SortValue;
  label: string;
  sortBy: "lastName" | "firstName" | "dateOfBirth" | "createdAt" | "updatedAt";
  sortOrder: "asc" | "desc";
}[] = [
  { value: "lastName-asc",     label: "Last name (A–Z)",                sortBy: "lastName",    sortOrder: "asc"  },
  { value: "lastName-desc",    label: "Last name (Z–A)",                sortBy: "lastName",    sortOrder: "desc" },
  { value: "firstName-asc",    label: "First name (A–Z)",               sortBy: "firstName",   sortOrder: "asc"  },
  { value: "firstName-desc",   label: "First name (Z–A)",               sortBy: "firstName",   sortOrder: "desc" },
  { value: "dateOfBirth-asc",  label: "Date of birth (oldest first)",   sortBy: "dateOfBirth", sortOrder: "asc"  },
  { value: "dateOfBirth-desc", label: "Date of birth (youngest first)", sortBy: "dateOfBirth", sortOrder: "desc" },
  { value: "createdAt-desc",   label: "Created (newest first)",         sortBy: "createdAt",   sortOrder: "desc" },
  { value: "createdAt-asc",    label: "Created (oldest first)",         sortBy: "createdAt",   sortOrder: "asc"  },
  { value: "updatedAt-desc",   label: "Updated (newest first)",         sortBy: "updatedAt",   sortOrder: "desc" },
  { value: "updatedAt-asc",    label: "Updated (oldest first)",         sortBy: "updatedAt",   sortOrder: "asc"  },
];

export const GENDER_OPTIONS: { value: GenderValue; label: string }[] = [
  { value: "all",     label: "All" },
  { value: "female",  label: "Female" },
  { value: "male",    label: "Male" },
  { value: "unknown", label: "Unknown" },
];

export const memberColumnHelper = createColumnHelper<MemberBrief>();

export function buildMemberColumns(
  nameFormat: "fl" | "lf",
  { showDeletedBadge = false }: { showDeletedBadge?: boolean } = {},
) {
  return [
    memberColumnHelper.accessor(
      (row) => [row.firstName, row.middleNames, row.lastName].filter(Boolean).join(" "),
      {
        id: "name",
        header: "Name",
        cell: ({ row }) => {
          const gender = row.original.legalGender;
          const { symbol, label, explanation } =
            gender === "female"
              ? { symbol: "♀", label: "Female", explanation: "Female" }
              : gender === "male"
                ? { symbol: "♂", label: "Male", explanation: "Male" }
                : { symbol: "?", label: "Unknown gender", explanation: "Legal gender has not been specified for this member." };
          return (
            <div className="flex items-center gap-1.5">
              <Link
                to="/members/$memberId"
                params={{ memberId: row.original.id }}
                className="font-medium hover:underline"
              >
                {formatName(row.original.firstName, row.original.middleNames, row.original.lastName, nameFormat)}
              </Link>
              <Tooltip>
                <TooltipTrigger
                  className="text-muted-foreground flex cursor-help items-center leading-none"
                  aria-label={label}
                  tabIndex={-1}
                >
                  <span aria-hidden>{symbol}</span>
                </TooltipTrigger>
                <TooltipContent>{explanation}</TooltipContent>
              </Tooltip>
              {showDeletedBadge && row.original.isDeleted && (
                <Badge variant="destructive">Deleted</Badge>
              )}
            </div>
          );
        },
      },
    ),
    memberColumnHelper.accessor("dateOfBirth", {
      header: "Date of birth",
      cell: ({ getValue }) => formatDate(getValue()),
    }),
    memberColumnHelper.accessor("tags", {
      header: "Tags",
      cell: ({ getValue }) => (
        <div className="flex flex-wrap gap-1">
          {getValue().map((tag) => (
            <Badge key={tag.id} style={{ backgroundColor: tag.color }}>
              {tag.name}
            </Badge>
          ))}
        </div>
      ),
    }),
  ];
}
