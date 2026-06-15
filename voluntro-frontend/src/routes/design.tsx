import { createFileRoute } from "@tanstack/react-router";

import Heading from "#/shared/components/heading.tsx";
import { Badge } from "#/shared/components/ui/badge.tsx";

export const Route = createFileRoute("/design")({
  component: DesignPage,
});

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="space-y-4">
      <h2 className="text-muted-foreground border-border border-b pb-1 text-xs font-semibold tracking-widest uppercase">
        {title}
      </h2>
      {children}
    </section>
  );
}

function Row({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex items-baseline gap-6">
      <span className="text-muted-foreground w-24 shrink-0 text-xs tabular-nums">{label}</span>
      {children}
    </div>
  );
}

export default function DesignPage() {
  return (
    <main className="mx-auto max-w-3xl space-y-12 px-8 py-10">
      <Heading level={1}>Typography</Heading>

      <Section title="Heading — levels (default size)">
        {([1, 2, 3, 4, 5, 6] as const).map((level) => (
          <Row key={level} label={`h${level}`}>
            <Heading level={level}>The quick brown fox</Heading>
          </Row>
        ))}
      </Section>

      <Section title="Heading — size variants">
        {(["2xl", "xl", "lg", "md", "sm", "xs"] as const).map((size) => (
          <Row key={size} label={size}>
            <Heading level={2} size={size}>
              The quick brown fox
            </Heading>
          </Row>
        ))}
      </Section>

      <Section title="Heading — with badge">
        <Row label="badge">
          <Heading level={2} badge={42}>
            Members
          </Heading>
        </Row>
      </Section>

      <Section title="Body text">
        <Row label="base">
          <p className="text-base">The quick brown fox jumps over the lazy dog.</p>
        </Row>
        <Row label="sm">
          <p className="text-sm">The quick brown fox jumps over the lazy dog.</p>
        </Row>
        <Row label="xs">
          <p className="text-xs">The quick brown fox jumps over the lazy dog.</p>
        </Row>
        <Row label="muted">
          <p className="text-muted-foreground text-sm">The quick brown fox jumps over the lazy dog.</p>
        </Row>
        <Row label="medium">
          <p className="text-sm font-medium">The quick brown fox jumps over the lazy dog.</p>
        </Row>
        <Row label="semibold">
          <p className="text-sm font-semibold">The quick brown fox jumps over the lazy dog.</p>
        </Row>
        <Row label="bold">
          <p className="text-sm font-bold">The quick brown fox jumps over the lazy dog.</p>
        </Row>
        <Row label="mono">
          <p className="font-mono text-sm">The quick brown fox jumps over the lazy dog.</p>
        </Row>
      </Section>

      <Section title="Badge variants">
        <div className="flex flex-wrap gap-2">
          {(["default", "secondary", "destructive", "outline", "ghost"] as const).map((variant) => (
            <Badge key={variant} variant={variant}>
              {variant}
            </Badge>
          ))}
        </div>
      </Section>
    </main>
  );
}
