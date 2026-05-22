import { useMemo, useState } from "react";

import { useMemberComboboxQuery } from "#/domains/members/use-members.ts";
import { ComboboxBase } from "#/shared/components/forms/combobox-base.tsx";
import { useFieldContext } from "#/shared/hooks/form-context.tsx";

type MemberPickerFieldProps = {
  label?: string;
  placeholder?: string;
  memberIds?: string[];
};

export default function MemberPickerField(props: MemberPickerFieldProps) {
  const { label = "Member", placeholder = "Select member", memberIds } = props;
  const field = useFieldContext<string>();
  const [search, setSearch] = useState("");
  const { data: items, isPending } = useMemberComboboxQuery(search);
  const options = useMemo(
    () =>
      (items ?? [])
        .filter((i) => !memberIds?.includes(i.id))
        .map((i) => ({ label: i.label, value: i.id })),
    [items, memberIds],
  );
  const selectedOption = options.find((o) => o.value === field.state.value) ?? null;

  return (
    <ComboboxBase
      label={label}
      placeholder={placeholder}
      options={options}
      value={selectedOption}
      onValueChange={(option) => field.handleChange(option?.value ?? "")}
      onSearchChange={setSearch}
      onBlur={field.handleBlur}
      loading={isPending}
    />
  );
}
