import { useId } from "react";

import { Field, FieldDescription, FieldError, FieldLabel } from "#/shared/components/ui/field.tsx";
import { Textarea } from "#/shared/components/ui/textarea.tsx";
import { useFieldContext } from "#/shared/hooks/form-context.tsx";

type TextAreaProps = {
  id?: string;
  label: string;
  helpText?: string;
  placeholder?: string;
};

export default function TextArea(props: TextAreaProps) {
  const { id: providedId, label, helpText, placeholder = "" } = props;
  const field = useFieldContext<string>();

  const errorMessage = field.state.meta.errors[0]?.message;
  const hasError = errorMessage != undefined;

  const generatedId = useId();
  const id = providedId ?? generatedId;
  const labelId = `${id}-label`;
  const descriptionId = `${id}-description`;
  const errorId = `${id}-error`;

  return (
    <Field data-component="TextArea" data-invalid={hasError}>
      <FieldLabel id={labelId} htmlFor={id}>
        {label}
      </FieldLabel>
      {helpText != undefined && <FieldDescription id={descriptionId}>{helpText}</FieldDescription>}
      <Textarea
        id={id}
        placeholder={placeholder}
        value={field.state.value}
        onChange={(e) => field.handleChange(e.target.value)}
        onBlur={field.handleBlur}
        aria-labelledby={labelId}
        aria-describedby={
          [helpText && descriptionId, hasError && errorId].filter(Boolean).join(" ") || undefined
        }
        aria-invalid={hasError}
      />
      {hasError && <FieldError id={errorId}>{errorMessage}</FieldError>}
    </Field>
  );
}
