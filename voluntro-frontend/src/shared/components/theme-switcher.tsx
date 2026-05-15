import { Moon, Sun } from "lucide-react";

import { Button } from "#/shared/components/ui/button.tsx";
import { useTheme } from "#/shared/hooks/useTheme.ts";
import { THEMES, type Theme } from "#/shared/lib/theme.ts";

const THEME_ICONS: Record<Theme, React.ReactNode> = {
  light: <Sun className="size-4" />,
  dark: <Moon className="size-4" />,
};

const THEME_LABELS: Record<Theme, string> = {
  light: "Light",
  dark: "Dark",
};

export function ThemeSwitcher() {
  const { theme, setTheme } = useTheme();

  return (
    <div role="group" aria-label="Theme" className="flex items-center gap-1">
      {THEMES.map((t) => (
        <Button
          key={t}
          variant={theme === t ? "default" : "ghost"}
          size="icon-sm"
          aria-label={THEME_LABELS[t]}
          aria-pressed={theme === t}
          onClick={() => setTheme(t)}
        >
          {THEME_ICONS[t]}
        </Button>
      ))}
    </div>
  );
}
