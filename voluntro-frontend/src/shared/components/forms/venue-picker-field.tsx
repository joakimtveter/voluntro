import { useMemo, useState } from "react";

import { useVenueComboboxQuery } from "#/domains/venues/use-venues.ts";
import { ComboboxBase } from "#/shared/components/forms/combobox-base.tsx";
import { useFieldContext } from "#/shared/hooks/form-context.tsx";

type VenuePickerFieldProps = {
  label?: string;
  placeholder?: string;
};

export default function VenuePickerField(props: VenuePickerFieldProps) {
  const { label = "Venue", placeholder = "Select venue" } = props;

  const field = useFieldContext<string>();
  const [search, setSearch] = useState("");
  const { data: items, isPending } = useVenueComboboxQuery(search);
  const options = useMemo(
    () => (items ?? []).map((i) => ({ label: i.label, value: i.id })),
    [items],
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
