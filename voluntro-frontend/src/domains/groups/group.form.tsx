import { useNavigate } from "@tanstack/react-router";

import { groupFormValidationSchema, type GroupFormValues } from "#/domains/groups/group.schema.ts";
import { useCreateGroup, useUpdateGroup } from "#/domains/groups/use-groups.ts";
import Form from "#/shared/components/forms/form.tsx";
import { useAppForm } from "#/shared/hooks/use-form.tsx";

type GroupFormProps = {
  defaultValues?: GroupFormValues;
  groupId?: string;
};

export default function GroupForm(props: GroupFormProps) {
  const { defaultValues, groupId = "" } = props;
  const { mutate: create } = useCreateGroup();
  const { mutate: update } = useUpdateGroup(groupId);
  const navigate = useNavigate();

  const form = useAppForm({
    defaultValues: {
      name: defaultValues?.name ?? "",
      description: defaultValues?.description ?? "",
      parentGroupId: defaultValues?.parentGroupId ?? null,
    },
    validators: {
      onSubmit: groupFormValidationSchema,
    },
    onSubmit: ({ value }) => {
      const payload = groupFormValidationSchema.parse(value);
      if (groupId) {
        update(payload, {
          onSuccess: () => {
            form.reset();
            navigate({ to: "/groups/$groupId", params: { groupId } });
          },
        });
      } else {
        create(payload, {
          onSuccess: () => {
            form.reset();
            navigate({ to: "/groups", search: { page: 1, pageSize: 20 } });
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
        <form.AppField name="name" children={(field) => <field.TextField label="Group name" />} />
        <form.AppField
          name="description"
          children={(field) => <field.TextArea label="Description" />}
        />
        <form.FormActions>
          <form.SubmitButton />
          <form.ResetButton />
        </form.FormActions>
      </form.AppForm>
    </Form>
  );
}
