import { LegalGender, type LegalGenderEnum } from "#/domains/members/member.types.ts";
import {
  memberFormValidationSchema,
  type MemberFormValues,
} from "#/domains/members/members.schema.ts";
import { useCreateMember, useUpdateMember } from "#/domains/members/use-members.ts";
import Form from "#/shared/components/forms/form.tsx";
import { useAppForm } from "#/shared/hooks/use-form.tsx";

type MemberFormProps = {
  memberId?: string;
  defaultValues?: Omit<MemberFormValues, "middleNames" | "email"> & {
    middleNames?: string | null;
    email?: string | null;
    memberTypeId?: string;
  };
  onSuccess?: () => void;
};

export default function MemberForm(props: MemberFormProps) {
  const { defaultValues, memberId = "", onSuccess } = props;
  const { mutate: create } = useCreateMember();
  const { mutate: update } = useUpdateMember(memberId);

  const form = useAppForm({
    defaultValues: {
      firstName: defaultValues?.firstName ?? "",
      middleNames: defaultValues?.middleNames ?? "",
      lastName: defaultValues?.lastName ?? "",
      email: defaultValues?.email ?? "",
      dateOfBirth: defaultValues?.dateOfBirth ?? "",
      legalGender: (defaultValues?.legalGender?.toLowerCase() as LegalGenderEnum | undefined) ?? LegalGender.unknown,
      memberTypeId: defaultValues?.memberTypeId ?? "",
    },
    validators: {
      onSubmit: memberFormValidationSchema,
    },
    onSubmit: ({ value }) => {
      const payload = memberFormValidationSchema.parse(value);

      if (memberId) {
        update(payload, {
          onSuccess: () => {
            onSuccess?.();
            form.reset();
          },
        });
      } else {
        create(payload, {
          onSuccess: () => {
            onSuccess?.();
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
          name="email"
          children={(field) => (
            <field.TextField label="Email" type="email" autoComplete="email" />
          )}
        />

        <form.AppField
          name="dateOfBirth"
          children={(field) => <field.BirthdayPicker label="Date of birth" />}
        />
        <form.AppField name="legalGender" children={(field) => <field.LegalGenderPicker />} />
        <form.AppField name="memberTypeId" children={(field) => <field.MemberTypePicker />} />
        <form.FormActions>
          <form.ResetButton />
          <form.SubmitButton>{memberId ? "Update member" : "Create member"}</form.SubmitButton>
        </form.FormActions>
      </form.AppForm>
    </Form>
  );
}
