import { createLink, type LinkComponent } from "@tanstack/react-router";
import { type VariantProps } from "class-variance-authority";
import type { ComponentProps, Ref } from "react";

import { buttonVariants } from "#/shared/components/ui/button";
import { cn } from "#/shared/lib/utils";

type IconLinkButtonBaseProps = Omit<ComponentProps<"a">, "size"> &
  Pick<VariantProps<typeof buttonVariants>, "variant"> & {
    size?: "icon-xs" | "icon-sm" | "icon" | "icon-lg";
    ref?: Ref<HTMLAnchorElement>;
    "aria-label": string;
  };

function IconLinkButtonBase({
  className,
  variant = "ghost",
  size = "icon",
  ...props
}: IconLinkButtonBaseProps) {
  return (
    <a data-slot="button" className={cn(buttonVariants({ variant, size }), className)} {...props} />
  );
}

const CreatedIconLinkButton = createLink(IconLinkButtonBase);

export const IconLinkButton: LinkComponent<typeof IconLinkButtonBase> = (props) => {
  return <CreatedIconLinkButton preload="intent" {...props} />;
};
