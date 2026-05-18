import type { ComponentProps, ReactNode } from "react";

import { Button } from "#/shared/components/ui/button.tsx";
import { useFormContext } from "#/shared/hooks/form-context.tsx";

export function FormActions({ children }: { children: ReactNode }) {
  return <div className="mt-4 flex justify-end gap-3">{children}</div>;
}

export function SubmitButton({ children = "Save", ...props }: ComponentProps<typeof Button>) {
  const form = useFormContext();
  return (
    <form.Subscribe selector={(s) => s.isSubmitting}>
      {(isSubmitting) => (
        <Button type="submit" disabled={isSubmitting} {...props}>
          {isSubmitting ? "Saving…" : children}
        </Button>
      )}
    </form.Subscribe>
  );
}

export function ResetButton({ children = "Reset", ...props }: ComponentProps<typeof Button>) {
  const form = useFormContext();
  return (
    <form.Subscribe selector={(s) => s.isPristine}>
      {(isPristine) => (
        <Button
          type="button"
          variant="outline"
          disabled={isPristine}
          onClick={() => form.reset()}
          {...props}
        >
          {children}
        </Button>
      )}
    </form.Subscribe>
  );
}
