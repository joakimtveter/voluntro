import { createFormHook } from "@tanstack/react-form";

import BirthdayPickerField from "#/shared/components/app-form-fields/birthday-picker-field.tsx";
import { DatetimePickerField } from "#/shared/components/app-form-fields/datetime-picker-field.tsx";
import GroupPickerField from "#/shared/components/app-form-fields/group-picker-field.tsx";
import MemberTypePickerField from "#/shared/components/app-form-fields/member-type-picker-field.tsx";
import TextAreaField from "#/shared/components/app-form-fields/text-area-field.tsx";
import TextInputField from "#/shared/components/app-form-fields/text-input-field.tsx";
import VenuePickerField from "#/shared/components/app-form-fields/venue-picker-field.tsx";
import { FormActions, ResetButton, SubmitButton } from "#/shared/components/forms/form-actions.tsx";
import LegalGenderPickerField from "#/shared/components/forms/legal-gender-picker-field.tsx";
import MemberPickerField from "#/shared/components/forms/member-picker-field.tsx";
import TagColorPickerField from "#/shared/components/forms/tag-color-picker-field.tsx";
import { fieldContext, formContext } from "#/shared/hooks/form-context";

export const { useAppForm } = createFormHook({
  fieldContext,
  formContext,
  fieldComponents: {
    TextField: TextInputField,
    TextArea: TextAreaField,
    DatetimePicker: DatetimePickerField,
    VenuePicker: VenuePickerField,
    BirthdayPicker: BirthdayPickerField,
    LegalGenderPicker: LegalGenderPickerField,
    GroupPicker: GroupPickerField,
    MemberTypePicker: MemberTypePickerField,
    MemberPicker: MemberPickerField,
    ColorPicker: TagColorPickerField,
  },
  formComponents: {
    FormActions,
    SubmitButton,
    ResetButton,
  },
});
