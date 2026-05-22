import * as z from "zod";

import { useAddMemberToGroup } from "#/domains/groups/use-groups.ts";
import Form from "#/shared/components/forms/form.tsx";
import { Button } from "#/shared/components/ui/button";
import { useAppForm } from "#/shared/hooks/use-form.tsx";

const formValidationSchema = z.object({
  memberId: z.uuid(),
});

export type AddGroupToMemberFormValues = z.input<typeof formValidationSchema>;
export type AddGroupToMemberPayload = z.output<typeof formValidationSchema>;

type AddGroupToMemberFormProps = {
  memberId: string;
  filterGroupIds: string[];
};

export default function AddGroupToMemberForm(props: AddGroupToMemberFormProps) {
  const { memberId, filterGroupIds } = props;
  const { mutate } = useAddMemberToGroup(memberId);

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
