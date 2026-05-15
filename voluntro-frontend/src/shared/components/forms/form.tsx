import type { ComponentProps } from "react";

export default function Form({ children, ...props }: ComponentProps<"form">) {
  return (
    <form {...props} data-component="Form" className="max-w-lg">
      {children}
    </form>
  );
}
