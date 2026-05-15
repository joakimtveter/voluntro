import { createFormHook } from "@tanstack/react-form";

import BirthdayPicker from "#/shared/components/forms/birthday-picker.tsx";
import { DatetimePicker } from "#/shared/components/forms/datetime-picker.tsx";
import { FormActions, ResetButton, SubmitButton } from "#/shared/components/forms/form-actions.tsx";
import LegalGenderPickerField from "#/shared/components/forms/legal-gender-picker-field.tsx";
import TextArea from "#/shared/components/forms/text-area.tsx";
import TextField from "#/shared/components/forms/text-field.tsx";
import VenuePicker from "#/shared/components/forms/venue-picker.tsx";
import { fieldContext, formContext } from "#/shared/hooks/form-context";

export const { useAppForm } = createFormHook({
  fieldContext,
  formContext,
  fieldComponents: {
    TextField,
    TextArea,
    DatetimePicker,
    VenuePicker,
    BirthdayPicker,
    LegalGenderPickerField,
  },
  formComponents: {
    FormActions,
    SubmitButton,
    ResetButton,
  },
});
