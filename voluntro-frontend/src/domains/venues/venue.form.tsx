import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";

import { useCreateVenue, useUpdateVenue } from "#/domains/venues/use-venues.ts";
import { venueFormValidationSchema } from "#/domains/venues/venue.schema.ts";
import { AddressFields } from "#/shared/components/forms/address-fields.tsx";
import Form from "#/shared/components/forms/form.tsx";
import { useAppForm } from "#/shared/hooks/use-form.tsx";
import type { Address } from "#/shared/types/address.types.ts";

type VenueFormProps = {
  defaultValues?: {
    name: string;
    description: string;
    address: Address;
  };
  venueId?: string;
};

export default function VenueForm(props: VenueFormProps) {
  const { venueId = "", defaultValues } = props;
  const { mutate: create } = useCreateVenue();
  const { mutate: update } = useUpdateVenue(venueId);
  const navigate = useNavigate();

  const form = useAppForm({
    defaultValues: {
      name: defaultValues?.name ?? "",
      description: defaultValues?.description ?? "",
      address: {
        streetAddress: defaultValues?.address.streetAddress ?? "",
        streetAddress2: defaultValues?.address.streetAddress2 ?? "",
        postalCode: defaultValues?.address.postalCode ?? "",
        city: defaultValues?.address.city ?? "",
        country: defaultValues?.address.country ?? "",
      },
    },
    validators: {
      onSubmit: venueFormValidationSchema,
    },
    onSubmit: ({ value }) => {
      const payload = venueFormValidationSchema.parse(value);
      if (venueId) {
        update(payload, {
          onSuccess: (venue) => {
            form.reset();
            navigate({ to: "/venues/$venueId", params: { venueId: venue.id } });
            toast.success("Venue updated successfully.", {
              action: {
                label: "View member",
                onClick: () => navigate({ to: "/venues/$venueId", params: { venueId: venue.id } }),
              },
            });
          },
        });
      } else {
        create(payload, {
          onSuccess: (venue) => {
            form.reset();
            navigate({ to: "/venues", search: { page: 1, pageSize: 25 } });
            toast.success("Venue created successfully.", {
              action: {
                label: "View member",
                onClick: () => navigate({ to: "/venues/$venueId", params: { venueId: venue.id } }),
              },
            });
          },
        });
      }
    },
  });

  return (
    <Form
      onSubmit={(e) => {
        e.preventDefault();
        e.stopPropagation();
        form.handleSubmit();
      }}
    >
      <form.AppForm>
        <form.AppField name="name" children={(field) => <field.TextField label="Name" />} />
        <form.AppField
          name="description"
          children={(field) => <field.TextField label="Description" />}
        />
        <AddressFields form={form} />
        <form.FormActions>
          <form.ResetButton />
          <form.SubmitButton>{venueId ? "Update Venue" : "Create venue"}</form.SubmitButton>
        </form.FormActions>
      </form.AppForm>
    </Form>
  );
}
