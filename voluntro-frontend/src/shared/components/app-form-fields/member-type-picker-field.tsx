import { useMemo } from "react";

import { useGetMemberTypes } from "#/domains/member-types/use-member-types.ts";
import { ComboboxBase } from "#/shared/components/forms/combobox-base.tsx";
import { useFieldContext } from "#/shared/hooks/form-context.tsx";

type MemberTypePickerFieldProps = {
  label?: string;
  placeholder?: string;
};

export default function MemberTypePickerField({
  label = "Member type",
  placeholder = "Select member type",
}: MemberTypePickerFieldProps) {
  const field = useFieldContext<string | undefined>();
  const { data: memberTypes, isPending } = useGetMemberTypes();

  const options = useMemo(
    () => (memberTypes ?? []).map((mt) => ({ value: mt.id, label: mt.name })),
    [memberTypes],
  );

  const selectedOption = options.find((o) => o.value === field.state.value) ?? null;

  return (
    <ComboboxBase
      label={label}
      placeholder={placeholder}
      options={options}
      value={selectedOption}
      onValueChange={(option) => field.handleChange(option?.value)}
      onBlur={field.handleBlur}
      loading={isPending}
    />
  );
}
