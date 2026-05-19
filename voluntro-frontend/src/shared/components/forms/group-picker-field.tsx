import { useMemo, useState } from "react";

import { useGroupQuery } from "#/domains/groups/use-groups.ts";
import { ComboboxBase } from "#/shared/components/forms/combobox-base.tsx";
import { useFieldContext } from "#/shared/hooks/form-context.tsx";

type GroupPickerFieldProps = {
  label?: string;
  placeholder?: string;
  groupId?: string;
};

export default function GroupPickerField(props: GroupPickerFieldProps) {
  const { label = "Parent group", placeholder = "Select parent group", groupId } = props;
  const field = useFieldContext<string>();
  const [search, setSearch] = useState("");
  const { data: items, isPending } = useGroupQuery(search);
  const options = useMemo(
    () => (items ?? []).filter((i) => i.id !== groupId).map((i) => ({ label: i.label, value: i.id })),
    [items, groupId],
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
