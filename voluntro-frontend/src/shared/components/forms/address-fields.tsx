import { useFieldGroup, type AppFieldExtendedReactFormApi } from "@tanstack/react-form";

import { FieldGroup, FieldLegend, FieldSet } from "#/shared/components/ui/field.tsx";

export type AddressFormData = {
  streetAddress: string;
  streetAddress2: string;
  postalCode: string;
  city: string;
  country: string;
};

type Props<TFormData extends { address: AddressFormData }> = {
  form: AppFieldExtendedReactFormApi<
    TFormData,
    any,
    any,
    any,
    any,
    any,
    any,
    any,
    any,
    any,
    any,
    any,
    any,
    any
  >;
};

export function AddressFields<TFormData extends { address: AddressFormData }>({
  form,
}: Props<TFormData>) {
  const group = useFieldGroup({
    form,
    fields: "address" as const,
    formComponents: {},
  });

  return (
    <FieldSet className="flex flex-col gap-4">
      <FieldLegend className="text-sm font-medium">Address</FieldLegend>
      <group.AppField
        name="streetAddress"
        children={(f) => <f.TextField label="Street address" />}
      />
      <group.AppField
        name="streetAddress2"
        children={(f) => <f.TextField label="Street address 2" />}
      />
      <FieldGroup className="flex gap-4">
        <div className="w-1/4">
          <group.AppField name="postalCode" children={(f) => <f.TextField label="Postal code" />} />
        </div>
        <div className="w-3/4">
          <group.AppField name="city" children={(f) => <f.TextField label="City" />} />
        </div>
      </FieldGroup>
      <group.AppField name="country" children={(f) => <f.TextField label="Country" />} />
    </FieldSet>
  );
}
