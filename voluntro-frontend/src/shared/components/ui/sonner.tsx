import { CircleCheckIcon, InfoIcon, Loader2Icon, OctagonXIcon, TriangleAlertIcon } from "lucide-react";
import type { CSSProperties } from "react";
import { Toaster as Sonner, type ToasterProps } from "sonner";

import { useTheme } from "#/shared/hooks/useTheme.ts";

const Toaster = ({ ...props }: ToasterProps) => {
  const { theme } = useTheme();

  return (
    <Sonner
      theme={theme}
      richColors
      className="toaster group"
      icons={{
        success: <CircleCheckIcon className="size-4 text-green-600 dark:text-green-500" />,
        info: <InfoIcon className="size-4" />,
        warning: <TriangleAlertIcon className="size-4 text-amber-600 dark:text-amber-500" />,
        error: <OctagonXIcon className="size-4" />,
        loading: <Loader2Icon className="size-4 animate-spin" />,
      }}
      toastOptions={{
        classNames: {
          toast: "border! rounded-lg! shadow-sm! px-4! py-3! items-start! gap-x-2.5! text-sm! font-sans!",
          title: "font-medium! text-sm!",
          description: "text-sm!",
          icon: "mt-0.5!",
        },
      }}
      style={{ "--border-radius": "var(--radius)" } as CSSProperties}
      {...props}
    />
  );
};

export { Toaster };
