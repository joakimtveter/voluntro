import type { VariantProps } from "class-variance-authority";
import { forwardRef, type ComponentProps, type ReactNode } from "react";

import { Button, buttonVariants } from "#/shared/components/ui/button";
import { Tooltip, TooltipContent, TooltipTrigger } from "#/shared/components/ui/tooltip.tsx";

type IconSize = Extract<
  NonNullable<VariantProps<typeof buttonVariants>["size"]>,
  "icon" | `icon-${string}`
>;

type IconButtonProps = Omit<ComponentProps<typeof Button>, "children" | "size"> & {
  icon: ReactNode;
  tooltipContent: string;
  tooltipSide?: ComponentProps<typeof TooltipContent>["side"];
  size?: IconSize;
};

const IconButton = forwardRef<HTMLButtonElement, IconButtonProps>(function IconButton(
  { tooltipContent, tooltipSide, icon, variant = "outline", size = "icon", ...rest },
  ref,
) {
  const { "aria-label": ariaLabel, ...buttonRest } = rest;
  return (
    <Tooltip>
      <TooltipTrigger
        render={
          <Button
            ref={ref}
            variant={variant}
            size={size}
            aria-label={ariaLabel ?? tooltipContent}
            {...buttonRest}
          >
            {icon}
          </Button>
        }
      />
      <TooltipContent side={tooltipSide}>{tooltipContent}</TooltipContent>
    </Tooltip>
  );
});

export default IconButton;
