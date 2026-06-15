import { useNavigate } from "@tanstack/react-router";

import {
  memberTypeFormValidationSchema,
  type MemberTypeFormValues,
} from "#/domains/member-types/member-type.schema.ts";
import {
  useCreateMemberType,
  useUpdateMemberType,
} from "#/domains/member-types/use-member-types.ts";
import Form from "#/shared/components/forms/form.tsx";
import { useAppForm } from "#/shared/hooks/use-form.tsx";

type MemberTypeFormProps = {
  memberTypeId?: string;
  defaultValues?: MemberTypeFormValues;
};

export default function MemberTypeForm({ memberTypeId = "", defaultValues }: MemberTypeFormProps) {
  const { mutate: create } = useCreateMemberType();
  const { mutate: update } = useUpdateMemberType(memberTypeId);
  const navigate = useNavigate();

  const form = useAppForm({
    defaultValues: {
      name: defaultValues?.name ?? "",
    },
    validators: {
      onSubmit: memberTypeFormValidationSchema,
    },
    onSubmit: ({ value }) => {
      const payload = memberTypeFormValidationSchema.parse(value);
      if (memberTypeId) {
        update(payload, {
          onSuccess: () => {
            form.reset();
            navigate({ to: "/settings/member-types" });
          },
        });
      } else {
        create(payload, {
          onSuccess: () => {
            form.reset();
            navigate({ to: "/settings/member-types" });
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
      data-component="MemberTypeForm"
    >
      <form.AppForm>
        <form.AppField
          name="name"
          children={(field) => <field.TextField label="Member type name" />}
        />
        <form.FormActions>
          <form.ResetButton />
          <form.SubmitButton>
            {memberTypeId ? "Update member type" : "Create member type"}
          </form.SubmitButton>
        </form.FormActions>
      </form.AppForm>
    </Form>
  );
}
