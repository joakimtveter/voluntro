import { useId } from "react";

import { Field, FieldDescription, FieldLabel } from "#/shared/components/ui/field";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "#/shared/components/ui/select";

type Option<T extends string> = {
  value: T;
  label: string;
};

type SingleSelectProps<T extends string = string> = {
  id?: string;
  label: string;
  placeholder?: string;
  helpText?: string;
  errorMessage?: string;
  options: Option<T>[];
  value: T;
  onChange: (value: T) => void;
  onBlur: () => void;
};

export default function SingleSelect<T extends string = string>(props: SingleSelectProps<T>) {
  const {
    label,
    id: providedId,
    helpText,
    errorMessage,
    placeholder,
    options,
    value,
    onChange,
    onBlur,
  } = props;

  const generatedId = useId();
  const id = providedId ?? generatedId;
  const descriptionId = `${id}-description`;

  const hasError = !!errorMessage;
  const renderDescription = !!helpText || hasError;

  return (
    <Field className="w-full max-w-48" data-invalid={hasError}>
      <FieldLabel htmlFor={id}>{label}</FieldLabel>
      <Select value={value} onValueChange={(val) => onChange(val as T)}>
        <SelectTrigger
          id={id}
          aria-invalid={hasError}
          aria-describedby={renderDescription ? descriptionId : undefined}
          onBlur={onBlur}
        >
          <SelectValue placeholder={placeholder}>
              {options.find((o) => o.value === value)?.label}
            </SelectValue>
        </SelectTrigger>
        <SelectContent>
          <SelectGroup>
            {options.map((option) => (
              <SelectItem key={option.value} value={option.value}>
                {option.label}
              </SelectItem>
            ))}
          </SelectGroup>
        </SelectContent>
      </Select>
      {renderDescription && <FieldDescription id={descriptionId}>{errorMessage ?? helpText}</FieldDescription>}
    </Field>
  );
}
