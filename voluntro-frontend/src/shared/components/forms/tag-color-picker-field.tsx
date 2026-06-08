import { useId } from "react";

import { TAG_COLORS } from "#/domains/tags/tag.constants.ts";
import { Badge } from "#/shared/components/ui/badge.tsx";
import { FieldError } from "#/shared/components/ui/field.tsx";
import { useFieldContext } from "#/shared/hooks/form-context.tsx";
import { cn } from "#/shared/lib/utils.ts";

export default function TagColorPickerField() {
  const field = useFieldContext<string>();
  const groupName = useId();
  const errorMessage = field.state.meta.errors[0]?.message;

  return (
    <fieldset className="mt-2 mb-4">
      <legend className="mb-3 text-sm font-medium">Color</legend>
      <div className="flex flex-wrap gap-2">
        {TAG_COLORS.map((color) => {
          const id = `${groupName}-${color.value}`;
          const isSelected = field.state.value === color.value;
          return (
            <label key={color.value} htmlFor={id} className="cursor-pointer">
              <input
                type="radio"
                id={id}
                name={groupName}
                value={color.value}
                checked={isSelected}
                onChange={() => field.handleChange(color.value)}
                onBlur={field.handleBlur}
                className="sr-only"
              />
              <Badge
                variant="outline"
                className={cn(
                  "cursor-pointer transition-all",
                  isSelected ? "ring-2 ring-offset-2" : "opacity-60 hover:opacity-90",
                )}
                style={{
                  backgroundColor: color.value,
                  borderColor: color.value,
                  color: "#fff",
                  // @ts-expect-error — CSS custom property for ring color
                  "--tw-ring-color": color.value,
                }}
              >
                {color.name}
              </Badge>
            </label>
          );
        })}
      </div>
      {errorMessage && <FieldError className="mt-2">{errorMessage}</FieldError>}
    </fieldset>
  );
}
