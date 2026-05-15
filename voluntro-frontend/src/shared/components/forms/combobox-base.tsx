import { useId } from "react";

import {
  Combobox,
  ComboboxContent,
  ComboboxEmpty,
  ComboboxInput,
  ComboboxItem,
  ComboboxList,
} from "#/shared/components/ui/combobox";
import { Field, FieldLabel } from "#/shared/components/ui/field";

export type Option = {
  value: string;
  label: string;
};

type ComboBoxProps = {
  id?: string;
  label: string;
  placeholder: string;
  noItemsMessage?: string;
  options: Option[];
  value?: Option | null;
  onValueChange?: (value: Option | null) => void;
  onBlur?: () => void;
  loading?: boolean;
};

export function ComboboxBase(props: ComboBoxProps) {
  const {
    id: providedId,
    label,
    options,
    placeholder,
    noItemsMessage = "No items found.",
    value,
    onValueChange,
    onBlur,
    loading = false,
  } = props;

  const generatedId = useId();
  const id = providedId ?? generatedId;

  return (
    <Field>
      <FieldLabel htmlFor={id}>{label}</FieldLabel>
      <Combobox items={options} value={value} onValueChange={onValueChange}>
        <ComboboxInput id={id} placeholder={placeholder} onBlur={onBlur} disabled={loading} />
        <ComboboxContent>
          <ComboboxEmpty>{loading ? "Loading..." : noItemsMessage}</ComboboxEmpty>
          <ComboboxList>
            {(item) => (
              <ComboboxItem key={item.value} value={item}>
                {item.label}
              </ComboboxItem>
            )}
          </ComboboxList>
        </ComboboxContent>
      </Combobox>
    </Field>
  );
}
