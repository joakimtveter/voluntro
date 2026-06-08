import { tagFormValidationSchema, type TagFormValues } from "#/domains/tags/tags.schema.ts";
import { useCreateTag, useUpdateTag } from "#/domains/tags/use-tags.ts";
import Form from "#/shared/components/forms/form.tsx";
import { useAppForm } from "#/shared/hooks/use-form.tsx";

type TagFormProps = {
  tagId?: string;
  defaultValues?: TagFormValues;
};

export default function TagForm(props: TagFormProps) {
  const { tagId = "", defaultValues } = props;
  const { mutate: create } = useCreateTag();
  const { mutate: update } = useUpdateTag(tagId);

  const form = useAppForm({
    defaultValues: {
      name: defaultValues?.name ?? "",
      color: defaultValues?.color ?? "",
    },
    validators: {
      onSubmit: tagFormValidationSchema,
    },
    onSubmit: ({ value }) => {
      const payload = tagFormValidationSchema.parse(value);

      if (tagId) {
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
      data-component="TagForm"
    >
      <form.AppForm>
        <form.AppField name="name" children={(field) => <field.TextField label="Tag name" />} />
        <form.AppField name="color" children={(field) => <field.ColorPicker />} />
        <form.FormActions>
          <form.ResetButton />
          <form.SubmitButton>{tagId ? "Update tag" : "Create tag"}</form.SubmitButton>
        </form.FormActions>
      </form.AppForm>
    </Form>
  );
}
