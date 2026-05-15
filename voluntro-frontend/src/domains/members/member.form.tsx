import { LegalGender } from "#/domains/members/member.types.ts";
import {
  memberFormValidationSchema,
  type MemberFormValues,
} from "#/domains/members/members.schema.ts";
import { useCreateMember, useUpdateMember } from "#/domains/members/use-members.ts";
import Form from "#/shared/components/forms/form.tsx";
import { useAppForm } from "#/shared/hooks/use-form.tsx";

type MemberFormProps = {
  memberId?: string;
  defaultValues?: Omit<MemberFormValues, "middleNames"> & { middleNames?: string | null };
};

export default function MemberForm(props: MemberFormProps) {
  const { defaultValues, memberId = "" } = props;
  const { mutate: create } = useCreateMember();
  const { mutate: update } = useUpdateMember(memberId);

  const form = useAppForm({
    defaultValues: {
      firstName: defaultValues?.firstName ?? "",
      middleNames: defaultValues?.middleNames ?? "",
      lastName: defaultValues?.lastName ?? "",
      dateOfBirth: defaultValues?.dateOfBirth ?? "",
      legalGender: defaultValues?.legalGender ?? LegalGender.unknown,
    },
    validators: {
      onSubmit: memberFormValidationSchema,
    },
    onSubmit: ({ value }) => {
      const payload = memberFormValidationSchema.parse(value);

      if (memberId) {
        update(payload, {
          onSuccess: () => {
            form.reset();
          },
        });
      } else {
        create(payload, {
          onSuccess: () => {
            form.reset();
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
      data-component="MemberForm"
    >
      <form.AppForm>
        <form.AppField
          name="firstName"
          children={(field) => <field.TextField label="First name" autoComplete="given-name" />}
        />
        <form.AppField
          name="middleNames"
          children={(field) => (
            <field.TextField label="Middle names" autoComplete="additional-name" />
          )}
        />
        <form.AppField
          name="lastName"
          children={(field) => <field.TextField label="Last name" autoComplete="family-name" />}
        />

        <form.AppField
          name="dateOfBirth"
          children={(field) => <field.BirthdayPicker label="Date of birth" />}
        />
        <form.AppField name="legalGender" children={(field) => <field.LegalGenderPickerField />} />
        <form.FormActions>
          <form.ResetButton />
          <form.SubmitButton>{memberId ? "Update member" : "Create member"}</form.SubmitButton>
        </form.FormActions>
      </form.AppForm>
    </Form>
  );
}
