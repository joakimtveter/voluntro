import { useMemo } from "react";

import { useVenues } from "#/domains/venues/use-venues.ts";
import { ComboboxBase } from "#/shared/components/forms/combobox-base.tsx";
import { useFieldContext } from "#/shared/hooks/form-context.tsx";

export default function VenuePicker() {
  const field = useFieldContext<string>();
  const { data, isPending } = useVenues({ page: 1, pageSize: 200 }, { staleTime: 4 * 60 * 1000 });
  const items = data?.items ?? [];
  const options = useMemo(() => items.map((i) => ({ label: i.name, value: i.id })), [items]);
  const selectedOption = options.find((o) => o.value === field.state.value) ?? null;

  return (
    <ComboboxBase
      label="Venue"
      placeholder="Select venue"
      options={options}
      value={selectedOption}
      onValueChange={(option) => field.handleChange(option?.value ?? "")}
      onBlur={field.handleBlur}
      loading={isPending}
    />
  );
}
