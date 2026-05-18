import { useFieldGroup, type AppFieldExtendedReactFormApi } from "@tanstack/react-form";

import { FieldLegend, FieldSet } from "#/shared/components/ui/field.tsx";
import type { AutoCompleteTokens } from "#/shared/types/form.types.ts";

export type AddressFormData = {
  streetAddress: string;
  streetAddress2?: string;
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
  addressType?: "home" | "work";
};

export function AddressFields<TFormData extends { address: AddressFormData }>({
  form,
  addressType,
}: Props<TFormData>) {
  const group = useFieldGroup({
    form,
    fields: "address" as const,
    formComponents: {},
  });

  const ac = (token: string): AutoCompleteTokens =>
    (addressType ? `${addressType} ${token}` : token) as AutoCompleteTokens;

  return (
    <FieldSet className="flex flex-col gap-4">
      <FieldLegend className="text-sm font-medium">Address</FieldLegend>
      <group.AppField
        name="streetAddress"
        children={(f) => <f.TextField label="Street address" autoComplete={ac("address-line1")} />}
      />
      <group.AppField
        name="streetAddress2"
        children={(f) => <f.TextField label="Street address 2" autoComplete={ac("address-line2")} />}
      />
      <div className="flex flex-wrap gap-4">
        <div className="[flex:1_1_10ch]">
          <group.AppField
            name="postalCode"
            children={(f) => <f.TextField label="Postal code" autoComplete={ac("postal-code")} />}
          />
        </div>
        <div className="[flex:2_1_20ch]">
          <group.AppField
            name="city"
            children={(f) => <f.TextField label="City" autoComplete={ac("address-level2")} />}
          />
        </div>
      </div>
      <group.AppField
        name="country"
        children={(f) => <f.TextField label="Country" autoComplete={ac("country-name")} />}
      />
    </FieldSet>
  );
}
