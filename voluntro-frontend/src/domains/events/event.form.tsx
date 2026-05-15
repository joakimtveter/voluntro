import { addHours, formatISO } from "date-fns";

import { eventSchema, type EventFormValues } from "#/domains/events/event.schema.ts";
import { useCreateEvent, useUpdateEvent } from "#/domains/events/use-events.ts";
import Form from "#/shared/components/forms/form.tsx";
import { useAppForm } from "#/shared/hooks/use-form.tsx";

type EventFormProps = {
  eventId?: string;
  defaultValues?: EventFormValues;
};

export default function EventForm(props: EventFormProps) {
  const { eventId = "", defaultValues } = props;
  const { mutate: update } = useUpdateEvent(eventId);
  const { mutate: create } = useCreateEvent();
  const form = useAppForm({
    defaultValues: {
      title: defaultValues?.title ?? "",
      description: defaultValues?.description ?? "",
      startsAt: formatISO(new Date(defaultValues?.startsAt ?? Date.now())),
      endsAt: defaultValues?.endsAt
        ? formatISO(new Date(defaultValues?.endsAt))
        : formatISO(addHours(Date.now(), 1)),
      venueId: defaultValues?.venueId ?? "",
    },
    validators: {
      onSubmit: eventSchema,
    },
    onSubmit: ({ value }) => {
      const payload = eventSchema.parse(value);

      if (eventId) {
        update(payload);
        form.reset();
      } else {
        create(payload);
        form.reset();
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
        <form.AppField name="title" children={(field) => <field.TextField label="Event title" />} />
        <form.AppField
          name="description"
          children={(field) => (
            <field.TextField label="Event description" helpText="Max 255 characters" />
          )}
        />
        <form.AppField
          name="startsAt"
          children={(field) => <field.DatetimePicker label="Event start" />}
        />
        <form.AppField
          name="endsAt"
          children={(field) => <field.DatetimePicker label="Event end" />}
        />
        <form.AppField name="venueId" children={(field) => <field.VenuePicker />} />

        <form.FormActions>
          <form.SubmitButton />
          <form.ResetButton />
        </form.FormActions>
      </form.AppForm>
    </Form>
  );
}
