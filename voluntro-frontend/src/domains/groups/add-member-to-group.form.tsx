import * as z from "zod";

import { useAddMemberToGroup } from "#/domains/groups/use-groups.ts";
import Form from "#/shared/components/forms/form.tsx";
import { Button } from "#/shared/components/ui/button";
import { useAppForm } from "#/shared/hooks/use-form.tsx";

const formValidationSchema = z.object({
  memberId: z.uuid(),
});

export type AddMemberToGroupFormValues = z.input<typeof formValidationSchema>;
export type AddMemberToGroupPayload = z.output<typeof formValidationSchema>;

type AddMemberToGroupFormProps = {
  groupId: string;
  filterMemberIds: string[];
};

export default function AddMemberToGroupForm(props: AddMemberToGroupFormProps) {
  const { groupId, filterMemberIds } = props;
  const { mutate } = useAddMemberToGroup(groupId);

  const form = useAppForm({
    validators: {
      onSubmit: formValidationSchema,
    },
    onSubmit: ({ value }) => {
      const payload = formValidationSchema.parse(value);
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
