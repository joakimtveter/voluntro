import { format, isValid, parseISO } from "date-fns";
import { ChevronDownIcon } from "lucide-react";
import { useId, useState } from "react";

import { Button } from "#/shared/components/ui/button";
import { Calendar } from "#/shared/components/ui/calendar";
import {
  Field,
  FieldDescription,
  FieldGroup,
  FieldLabel,
  FieldLegend,
  FieldSet,
} from "#/shared/components/ui/field";
import { Input } from "#/shared/components/ui/input";
import { Popover, PopoverContent, PopoverTrigger } from "#/shared/components/ui/popover";
import { DATE_FORMAT } from "#/shared/constants/constants.ts";
import { useFieldContext } from "#/shared/hooks/form-context.tsx";

type DatePickerTimeProps = {
  id?: string;
  label: string;
  description?: string;
  dateLabel?: string;
  timeLabel?: string;
};

function parseFieldValue(value: string): { date: Date | undefined; time: string } {
  if (!value) return { date: undefined, time: "" };
  const parsed = parseISO(value);
  if (!isValid(parsed)) return { date: undefined, time: "" };
  return { date: parsed, time: format(parsed, "HH:mm") };
}

function combineDateTime(date: Date | undefined, time: string): string {
  if (!date) return "";
  return `${format(date, DATE_FORMAT)}T${time || "00:00"}`;
}

export function DatetimePicker(props: DatePickerTimeProps) {
  const { id: providedId, timeLabel = "Time", dateLabel = "Date", description, label } = props;
  const field = useFieldContext<string>();
  const [open, setOpen] = useState(false);

  const generatedId = useId();
  const id = providedId ?? generatedId;

  const { date, time } = parseFieldValue(field.state.value);

  return (
    <FieldSet>
      <FieldLegend>{label}</FieldLegend>
      {!!description && <FieldDescription>{description}</FieldDescription>}

      <FieldGroup id={id} className="max-w-xs flex-row">
        <Field>
          <FieldLabel htmlFor={`${id}-date`}>{dateLabel}</FieldLabel>
          <Popover open={open} onOpenChange={setOpen}>
            <PopoverTrigger
              render={
                <Button
                  variant="outline"
                  id={`${id}-date`}
                  className="w-32 justify-between font-normal"
                >
                  {date ? format(date, "PPP") : "Select date"}
                  <ChevronDownIcon data-icon="inline-end" />
                </Button>
              }
            />
            <PopoverContent className="w-auto overflow-hidden p-0" align="start">
              <Calendar
                mode="single"
                selected={date}
                captionLayout="dropdown"
                defaultMonth={date}
                onSelect={(selected) => {
                  field.handleChange(combineDateTime(selected, time));
                  setOpen(false);
                }}
              />
            </PopoverContent>
          </Popover>
        </Field>
        <Field className="w-32">
          <FieldLabel id={`${id}-time-label`} htmlFor={`${id}-time`}>
            {timeLabel}
          </FieldLabel>
          <Input
            className="bg-background appearance-none [&::-webkit-calendar-picker-indicator]:hidden [&::-webkit-calendar-picker-indicator]:appearance-none"
            type="time"
            id={`${id}-time`}
            aria-labelledby={`${id}-time-label`}
            value={time}
            onChange={(e) => field.handleChange(combineDateTime(date, e.target.value))}
            onBlur={field.handleBlur}
          />
        </Field>
      </FieldGroup>
    </FieldSet>
  );
}
