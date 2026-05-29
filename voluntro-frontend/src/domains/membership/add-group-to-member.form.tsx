import { membershipFormValidationSchema } from "#/domains/membership/membership.schema.ts";
import { useAddMembership } from "#/domains/membership/use-membership.ts";
import Form from "#/shared/components/forms/form.tsx";
import { Button } from "#/shared/components/ui/button";
import { useAppForm } from "#/shared/hooks/use-form.tsx";

type AddGroupToMemberFormProps = {
  memberId: string;
  filterGroupIds: string[];
};

export default function AddGroupToMemberForm(props: AddGroupToMemberFormProps) {
  const { memberId, filterGroupIds = [] } = props;
  const { mutate } = useAddMembership();

  const form = useAppForm({
    defaultValues: { memberId, groupId: "" },
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
          name="groupId"
          children={(field) => <field.GroupPicker filterGroupIds={filterGroupIds} />}
        />
        <Button type="submit" className="mt-4">
          Add member
        </Button>
      </div>
    </Form>
  );
}
