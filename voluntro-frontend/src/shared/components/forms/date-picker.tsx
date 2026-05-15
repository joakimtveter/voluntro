import { format, isValid, parse } from "date-fns";
import { CalendarIcon } from "lucide-react";
import { useEffect, useId, useState } from "react";

import { Calendar } from "#/shared/components/ui/calendar.tsx";
import { Field, FieldDescription, FieldLabel } from "#/shared/components/ui/field.tsx";
import {
  InputGroup,
  InputGroupAddon,
  InputGroupButton,
  InputGroupInput,
} from "#/shared/components/ui/input-group.tsx";
import { Popover, PopoverContent, PopoverTrigger } from "#/shared/components/ui/popover.tsx";
import { DATE_DISPLAY_FORMAT, DATE_FORMAT } from "#/shared/constants/constants.ts";
import { cn } from "#/shared/lib/utils.ts";

const CALENDAR_START_MONTH = new Date(1900, 0);
const CALENDAR_END_MONTH = new Date();

type DatePickerProps = {
  label: string;
  id?: string;
  name?: string;
  className?: string;
  value: string;
  placeholder?: string;
  onChange: (isoDate: string) => void;
  onBlur?: () => void;
  helpText?: string;
  errorMessage?: string;
};

function toISODate(date: Date): string {
  return format(date, DATE_FORMAT);
}

function parseISODate(value: string): Date | undefined {
  if (!value) return undefined;
  const date = parse(value, DATE_FORMAT, new Date());
  return isValid(date) ? date : undefined;
}

function parseDisplayDate(value: string): Date | undefined {
  if (!value) return undefined;
  const date = parse(value, DATE_DISPLAY_FORMAT, new Date());
  if (!isValid(date)) return undefined;
  // Reject lenient parses ("1.1.1" → year 1) by requiring the input to round-trip.
  if (format(date, DATE_DISPLAY_FORMAT) !== value) return undefined;
  return date;
}

function formatISOForDisplay(value: string): string {
  const parsed = parseISODate(value);
  return parsed ? format(parsed, DATE_DISPLAY_FORMAT) : "";
}

export function DatePicker(props: DatePickerProps) {
  const {
    label,
    id: providedId,
    name,
    className,
    placeholder = "dd.mm.yyyy",
    value,
    onChange,
    onBlur,
    helpText,
    errorMessage,
  } = props;
  const [open, setOpen] = useState(false);
  const [inputValue, setInputValue] = useState(() => formatISOForDisplay(value));
  const [month, setMonth] = useState<Date | undefined>(() => parseISODate(value));

  const generatedId = useId();
  const id = providedId ?? generatedId;

  const selectedDate = parseISODate(value);
  const hasError = !!errorMessage;
  const renderDescription = !!helpText || hasError;

  useEffect(() => {
    const parsed = parseISODate(value);
    setInputValue((current) => {
      const currentParsed = parseDisplayDate(current);
      const currentISO = currentParsed ? toISODate(currentParsed) : "";
      // Preserve in-progress user input that already represents `value`.
      if (currentISO === value) return current;
      return parsed ? format(parsed, DATE_DISPLAY_FORMAT) : "";
    });
    if (parsed) setMonth(parsed);
  }, [value]);

  const handleInputChange = (next: string) => {
    setInputValue(next);
    if (!next) {
      onChange("");
      return;
    }
    const parsed = parseDisplayDate(next);
    if (parsed) {
      setMonth(parsed);
      onChange(toISODate(parsed));
    } else if (value) {
      // Invalidate the form value so validation reflects the bad input.
      onChange("");
    }
  };

  const handleBlur = () => {
    if (inputValue && !parseDisplayDate(inputValue)) {
      setInputValue(formatISOForDisplay(value));
    }
    onBlur?.();
  };

  return (
    <Field className={cn("w-44", className)} data-component="DatePicker">
      <FieldLabel htmlFor={id}>{label}</FieldLabel>
      <InputGroup>
        <InputGroupInput
          id={id}
          name={name}
          value={inputValue}
          placeholder={placeholder}
          onChange={(e) => handleInputChange(e.target.value)}
          onBlur={handleBlur}
          onKeyDown={(e) => {
            if (e.key === "ArrowDown") {
              e.preventDefault();
              setOpen(true);
            }
          }}
          aria-invalid={hasError}
        />
        <InputGroupAddon align="inline-end">
          <Popover open={open} onOpenChange={setOpen}>
            <PopoverTrigger
              render={
                <InputGroupButton variant="ghost" size="icon-xs" aria-label="Select date">
                  <CalendarIcon />
                  <span className="sr-only">Select date</span>
                </InputGroupButton>
              }
            />
            <PopoverContent
              className="w-auto overflow-hidden p-0"
              align="end"
              alignOffset={-8}
              sideOffset={10}
            >
              <Calendar
                mode="single"
                captionLayout="dropdown"
                startMonth={CALENDAR_START_MONTH}
                endMonth={CALENDAR_END_MONTH}
                selected={selectedDate}
                month={month}
                onMonthChange={setMonth}
                onSelect={(date) => {
                  if (date) {
                    const iso = toISODate(date);
                    onChange(iso);
                    setInputValue(format(date, DATE_DISPLAY_FORMAT));
                    setMonth(date);
                  }
                  setOpen(false);
                }}
              />
            </PopoverContent>
          </Popover>
        </InputGroupAddon>
      </InputGroup>
      {renderDescription && <FieldDescription>{errorMessage ?? helpText}</FieldDescription>}
    </Field>
  );
}
