import type { ReactNode } from "react";

import { cn } from "#/shared/lib/utils";

type HeadingProps = {
  level: 1 | 2 | 3 | 4 | 5 | 6;
  size?: "xs" | "sm" | "md" | "lg" | "xl" | "2xl";
  className?: string;
  children: ReactNode | ReactNode[];
};

const sizeClasses = {
  xs: "text-sm font-semibold",
  sm: "text-base font-semibold",
  md: "text-lg font-semibold",
  lg: "text-xl font-bold",
  xl: "text-2xl font-bold",
  "2xl": "text-3xl font-bold",
} as const;

const defaultSizeByLevel: Record<number, HeadingProps["size"]> = {
  1: "2xl",
  2: "xl",
  3: "lg",
  4: "md",
  5: "sm",
  6: "xs",
};

export default function Heading({ level, size, className, children }: HeadingProps) {
  const Tag = `h${level}` as const;
  const resolvedSize = size ?? defaultSizeByLevel[level];
  return <Tag className={cn(sizeClasses[resolvedSize!], className)}>{children}</Tag>;
}
