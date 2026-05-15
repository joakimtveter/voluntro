type DateFormat = "short" | "default" | "long" | "full";

const DATE_FORMAT_OPTIONS: Record<DateFormat, Intl.DateTimeFormatOptions> = {
  short: { year: "2-digit", month: "2-digit", day: "2-digit" },
  default: { year: "numeric", month: "2-digit", day: "2-digit" },
  long: { year: "numeric", month: "long", day: "numeric" },
  full: { year: "numeric", month: "long", day: "numeric", weekday: "long" },
};

export function formatDate(date?: string, format: DateFormat = "default") {
  if (!date) return "";
  const d = new Date(date);

  if (isNaN(d.getTime())) return "Ikke gyldig dato";

  return Intl.DateTimeFormat("nb-NO", DATE_FORMAT_OPTIONS[format]).format(d);
}

type DateTimeFormat = "default" | "long" | "full" | "precise";

const DATE_TIME_FORMAT_OPTIONS: Record<DateTimeFormat, Intl.DateTimeFormatOptions> = {
  default: {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  },
  long: { year: "numeric", month: "long", day: "numeric", hour: "2-digit", minute: "2-digit" },
  full: {
    year: "numeric",
    month: "long",
    day: "numeric",
    weekday: "long",
    hour: "2-digit",
    minute: "2-digit",
  },
  precise: {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  },
};

export function formatDateTime(date?: string, format: DateTimeFormat = "default") {
  if (!date) return "";
  const d = new Date(date);

  if (isNaN(d.getTime())) return "Ikke gyldig dato";

  return Intl.DateTimeFormat("nb-NO", DATE_TIME_FORMAT_OPTIONS[format]).format(d);
}

export function formatTime(date?: string, precise: boolean = false) {
  if (!date) return "";
  const d = new Date(date);

  if (isNaN(d.getTime())) return "Ikke gyldig dato";

  if (precise)
    return Intl.DateTimeFormat("nb-NO", {
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
    }).format(d);

  return Intl.DateTimeFormat("nb-NO", {
    hour: "2-digit",
    minute: "2-digit",
  }).format(d);
}
