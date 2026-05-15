import { Link, type LinkProps } from "@tanstack/react-router";
import { type VariantProps } from "class-variance-authority";
import type { ReactNode } from "react";

import { buttonVariants } from "#/shared/components/ui/button";
import { cn } from "#/shared/lib/utils";

type LinkButtonProps = LinkProps &
  VariantProps<typeof buttonVariants> & {
    className?: string;
    children: ReactNode;
  };

export function LinkButton({ className, variant, size, children, ...props }: LinkButtonProps) {
  return (
    <Link className={cn(buttonVariants({ variant, size }), className)} {...props}>
      {children}
    </Link>
  );
}
