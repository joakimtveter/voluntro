import { useId } from "react";

import { Field, FieldDescription, FieldError, FieldLabel } from "#/shared/components/ui/field.tsx";
import { Input } from "#/shared/components/ui/input.tsx";
import { useFieldContext } from "#/shared/hooks/form-context.tsx";
import type { AutoCompleteTokens, TextFieldType } from "#/shared/types/form.types.ts";

type TextFieldProps = {
  type?: TextFieldType;
  id?: string;
  label: string;
  placeholder?: string;
  autoComplete?: AutoCompleteTokens;
  helpText?: string;
};

export default function TextField(props: TextFieldProps) {
  const {
    label,
    type = "text",
    id: providedId,
    placeholder,
    autoComplete = "on",
    helpText,
  } = props;

  const field = useFieldContext<string>();
  const generatedId = useId();
  const id = providedId ?? generatedId;
  const labelId = `${id}-label`;
  const descriptionId = `${id}-description`;
  const errorId = `${id}-error`;

  const errorMessage = field.state.meta.errors[0]?.message;
  const hasError = !!errorMessage;

  return (
    <Field data-component="TextField" data-invalid={hasError}>
      <FieldLabel id={labelId} htmlFor={id}>
        {label}
      </FieldLabel>
      <Input
        id={id}
        type={type}
        placeholder={placeholder}
        autoComplete={autoComplete}
        value={field.state.value}
        onChange={(e) => field.handleChange(e.target.value)}
        onBlur={field.handleBlur}
        aria-labelledby={labelId}
        aria-invalid={hasError}
        aria-describedby={
          [helpText && descriptionId, hasError && errorId].filter(Boolean).join(" ") || undefined
        }
      />
      {helpText && !hasError && <FieldDescription id={descriptionId}>{helpText}</FieldDescription>}
      {hasError && <FieldError id={errorId}>{errorMessage}</FieldError>}
    </Field>
  );
}
