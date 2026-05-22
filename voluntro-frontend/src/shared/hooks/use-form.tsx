import { createFormHook } from "@tanstack/react-form";

import BirthdayPicker from "#/shared/components/forms/birthday-picker.tsx";
import { DatetimePicker } from "#/shared/components/forms/datetime-picker.tsx";
import { FormActions, ResetButton, SubmitButton } from "#/shared/components/forms/form-actions.tsx";
import GroupPickerField from "#/shared/components/forms/group-picker-field.tsx";
import LegalGenderPickerField from "#/shared/components/forms/legal-gender-picker-field.tsx";
import MemberPickerField from "#/shared/components/forms/member-picker-field.tsx";
import TextAreaField from "#/shared/components/forms/text-area-field.tsx";
import TextInputField from "#/shared/components/forms/text-input-field.tsx";
import VenuePickerField from "#/shared/components/forms/venue-picker-field.tsx";
import { fieldContext, formContext } from "#/shared/hooks/form-context";

export const { useAppForm } = createFormHook({
  fieldContext,
  formContext,
  fieldComponents: {
    TextField: TextInputField,
    TextArea: TextAreaField,
    DatetimePicker,
    VenuePicker: VenuePickerField,
    BirthdayPicker,
    LegalGenderPicker: LegalGenderPickerField,
    GroupPicker: GroupPickerField,
    MemberPicker: MemberPickerField,
  },
  formComponents: {
    FormActions,
    SubmitButton,
    ResetButton,
  },
});
