import { membershipFormValidationSchema } from "#/domains/membership/membership.schema.ts";
import { useAddMembership } from "#/domains/membership/use-membership.ts";
import Form from "#/shared/components/forms/form.tsx";
import { Button } from "#/shared/components/ui/button.tsx";
import { useAppForm } from "#/shared/hooks/use-form.tsx";

type AddMemberToGroupFormProps = {
  groupId: string;
  filterMemberIds: string[];
};

export default function AddMemberToGroupForm(props: AddMemberToGroupFormProps) {
  const { groupId, filterMemberIds } = props;
  const { mutate } = useAddMembership();

  const form = useAppForm({
    defaultValues: { groupId, memberId: "" },
    validators: { onSubmit: membershipFormValidationSchema },
    onSubmit: ({ value }) => {
      const payload = membershipFormValidationSchema.parse(value);
      mutate(payload, { onSuccess: () => form.reset() });
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
      <div className="flex items-center gap-3">
        <form.AppField
          name="memberId"
          children={(field) => <field.MemberPicker memberIds={filterMemberIds} />}
        />
        <Button type="submit" className="mt-4">
          Add member
        </Button>
      </div>
    </Form>
  );
}
