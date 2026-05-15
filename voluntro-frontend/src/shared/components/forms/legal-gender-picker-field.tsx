import { LegalGender, type LegalGenderEnum } from "#/domains/members/member.types.ts";
import SingleSelect from "#/shared/components/forms/single-select.tsx";
import { useFieldContext } from "#/shared/hooks/form-context.tsx";

type LegalGenderPickerFieldProps = {
  label?: string;
  placeholder?: string;
};

const options = Object.values(LegalGender).map((g) => ({
  value: g,
  label: g.charAt(0).toUpperCase() + g.slice(1),
}));

export default function LegalGenderPickerField(props: LegalGenderPickerFieldProps) {
  const { label = "Legal gender", placeholder = "Select gender" } = props;
  const field = useFieldContext<LegalGenderEnum>();
  const errorMessage = field.state.meta.isTouched ? field.state.meta.errors[0]?.message : undefined;
  return (
    <SingleSelect
      label={label}
      placeholder={placeholder}
      value={field.state.value}
      errorMessage={errorMessage}
      options={options}
      onChange={field.handleChange}
      onBlur={field.handleBlur}
    />
  );
}
