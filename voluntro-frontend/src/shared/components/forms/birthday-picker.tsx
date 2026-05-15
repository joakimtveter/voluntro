import { format, isValid, parse } from "date-fns";
import { useEffect, useId, useMemo, useState } from "react";

import {
  Field,
  FieldDescription,
  FieldError,
  FieldLabel,
  FieldLegend,
  FieldSet,
} from "#/shared/components/ui/field.tsx";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "#/shared/components/ui/select.tsx";
import { DATE_FORMAT } from "#/shared/constants/constants.ts";
import { useFieldContext } from "#/shared/hooks/form-context.tsx";
import { cn } from "#/shared/lib/utils.ts";

const MIN_YEAR = 1900;

const MONTHS = Array.from({ length: 12 }, (_, i) => ({
  value: String(i + 1),
  label: format(new Date(2000, i, 1), "MMMM"),
}));

type BirthdayPickerProps = {
  label: string;
  id?: string;
  className?: string;
  helpText?: string;
};

type Parts = {
  day: string;
  month: string;
  year: string;
};

function parseISODate(value: string): Date | undefined {
  if (!value) return undefined;
  const date = parse(value, DATE_FORMAT, new Date());
  return isValid(date) ? date : undefined;
}

function partsFromISO(value: string): Parts {
  const date = parseISODate(value);
  if (!date) return { day: "", month: "", year: "" };
  return {
    day: String(date.getDate()),
    month: String(date.getMonth() + 1),
    year: String(date.getFullYear()),
  };
}

function daysInMonth(month: number, year: number): number {
  // Day 0 of the next month is the last day of the current month.
  return new Date(year, month, 0).getDate();
}

function isoFromParts(parts: Parts): string {
  if (!parts.day || !parts.month || !parts.year) return "";
  const d = Number(parts.day);
  const m = Number(parts.month);
  const y = Number(parts.year);
  const date = new Date(y, m - 1, d);
  // Reject invalid combinations like Feb 30 by requiring the date to round-trip.
  if (date.getFullYear() !== y || date.getMonth() !== m - 1 || date.getDate() !== d) return "";
  return format(date, DATE_FORMAT);
}

export default function BirthdayPicker(props: BirthdayPickerProps) {
  const { label, id: providedId, className, helpText } = props;

  const field = useFieldContext<string>();
  const value = field.state.value;
  const errorMessage = field.state.meta.errors[0]?.message;

  const generatedId = useId();
  const id = providedId ?? generatedId;

  const [parts, setParts] = useState<Parts>(() => partsFromISO(value));

  useEffect(() => {
    setParts((current) => {
      // Preserve in-progress partial state if it already represents `value`.
      if (isoFromParts(current) === value) return current;
      return partsFromISO(value);
    });
  }, [value]);

  const years = useMemo(() => {
    const currentYear = new Date().getFullYear();
    return Array.from({ length: currentYear - MIN_YEAR + 1 }, (_, i) => {
      const y = currentYear - i;
      return { value: String(y), label: String(y) };
    });
  }, []);

  const monthNum = parts.month ? Number(parts.month) : undefined;
  const yearNum = parts.year ? Number(parts.year) : undefined;
  const dayCount = monthNum ? daysInMonth(monthNum, yearNum ?? 2000) : 31;
  const days = Array.from({ length: dayCount }, (_, i) => ({
    value: String(i + 1),
    label: String(i + 1),
  }));

  const update = (next: Parts) => {
    const m = next.month ? Number(next.month) : undefined;
    const y = next.year ? Number(next.year) : undefined;
    if (next.day && m) {
      const max = daysInMonth(m, y ?? 2000);
      if (Number(next.day) > max) next = { ...next, day: String(max) };
    }
    setParts(next);
    field.handleChange(isoFromParts(next));
  };

  const hasError = !!errorMessage;

  return (
    <FieldSet
      className={cn("w-full max-w-md gap-2", className)}
      data-component="BirthdayPicker"
      data-invalid={hasError}
    >
      <FieldLegend variant="label">{label}</FieldLegend>
      <div className="flex gap-2">
        <Field className="mt-0 mb-0 w-24 gap-1">
          <FieldLabel htmlFor={`${id}-day`} className="text-muted-foreground text-xs font-normal">
            Day
          </FieldLabel>
          <Select value={parts.day} onValueChange={(v) => update({ ...parts, day: v ?? "" })}>
            <SelectTrigger id={`${id}-day`} aria-invalid={hasError} onBlur={field.handleBlur}>
              <SelectValue placeholder="Day" />
            </SelectTrigger>
            <SelectContent>
              <SelectGroup>
                {days.map((d) => (
                  <SelectItem key={d.value} value={d.value}>
                    {d.label}
                  </SelectItem>
                ))}
              </SelectGroup>
            </SelectContent>
          </Select>
        </Field>
        <Field className="mt-0 mb-0 w-40 gap-1">
          <FieldLabel htmlFor={`${id}-month`} className="text-muted-foreground text-xs font-normal">
            Month
          </FieldLabel>
          <Select value={parts.month} onValueChange={(v) => update({ ...parts, month: v ?? "" })}>
            <SelectTrigger id={`${id}-month`} aria-invalid={hasError} onBlur={field.handleBlur}>
              <SelectValue placeholder="Month">
                {(v) => MONTHS.find((m) => m.value === v)?.label ?? "Month"}
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              <SelectGroup>
                {MONTHS.map((m) => (
                  <SelectItem key={m.value} value={m.value}>
                    {m.label}
                  </SelectItem>
                ))}
              </SelectGroup>
            </SelectContent>
          </Select>
        </Field>
        <Field className="mt-0 mb-0 w-28 gap-1">
          <FieldLabel htmlFor={`${id}-year`} className="text-muted-foreground text-xs font-normal">
            Year
          </FieldLabel>
          <Select value={parts.year} onValueChange={(v) => update({ ...parts, year: v ?? "" })}>
            <SelectTrigger id={`${id}-year`} aria-invalid={hasError} onBlur={field.handleBlur}>
              <SelectValue placeholder="Year" />
            </SelectTrigger>
            <SelectContent>
              <SelectGroup>
                {years.map((y) => (
                  <SelectItem key={y.value} value={y.value}>
                    {y.label}
                  </SelectItem>
                ))}
              </SelectGroup>
            </SelectContent>
          </Select>
        </Field>
      </div>
      {helpText && !hasError && <FieldDescription>{helpText}</FieldDescription>}
      {hasError && <FieldError>{errorMessage}</FieldError>}
    </FieldSet>
  );
}
