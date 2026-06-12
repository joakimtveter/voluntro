import { Trash2Icon } from "lucide-react";
import { useState } from "react";

import type { Tag } from "#/domains/members/member.types.ts";
import { useAddTagToMember, useRemoveTagFromMember } from "#/domains/members/use-members.ts";
import { useGetTags } from "#/domains/tags/use-tags.ts";
import { ConfirmDialog } from "#/shared/components/confirm-dialog.tsx";
import Heading from "#/shared/components/heading.tsx";
import IconButton from "#/shared/components/icon-button.tsx";
import { Button } from "#/shared/components/ui/button";
import {
  Combobox,
  ComboboxChip,
  ComboboxChips,
  ComboboxChipsInput,
  ComboboxContent,
  ComboboxEmpty,
  ComboboxItem,
  ComboboxList,
  useComboboxAnchor,
} from "#/shared/components/ui/combobox.tsx";

type Option = { value: string; label: string };

type MemberTagsSectionProps = {
  memberId: string;
  appliedTags: Tag[];
};

export default function MemberTagsSection({ memberId, appliedTags }: MemberTagsSectionProps) {
  const { data: allTags } = useGetTags();
  const { mutateAsync: addTag, isPending: isAdding } = useAddTagToMember(memberId);
  const { mutate: removeTag } = useRemoveTagFromMember(memberId);

  const [selectedOptions, setSelectedOptions] = useState<Option[]>([]);
  const anchorRef = useComboboxAnchor();

  const appliedTagIds = new Set(appliedTags.map((t) => t.id));
  const unappliedTags = allTags?.filter((t) => !appliedTagIds.has(t.id)) ?? [];
  const options: Option[] = unappliedTags.map((t) => ({ value: t.id, label: t.name }));

  const handleAdd = async () => {
    if (selectedOptions.length === 0) return;
    await Promise.all(selectedOptions.map((opt) => addTag(opt.value)));
    setSelectedOptions([]);
  };

  return (
    <section className="my-6" data-component="MemberTagsSection">
      <Heading level={2} size="xl" badge={appliedTags.length}>
        Tags
      </Heading>
      <div className="mt-3 space-y-6">
        {appliedTags.length > 0 ? (
          <ul className="divide-border max-w-lg divide-y rounded-md border">
            {appliedTags.slice().sort((a, b) => a.name.localeCompare(b.name)).map((tag) => (
              <li key={tag.id} className="flex items-center gap-3 px-4 py-3">
                <span
                  className="size-3 shrink-0 rounded-full"
                  style={{ backgroundColor: tag.color }}
                  aria-hidden
                />
                <span className="flex-1 text-sm font-medium">{tag.name}</span>
                <ConfirmDialog
                  trigger={
                    <IconButton
                      variant="destructive"
                      size="icon"
                      icon={<Trash2Icon />}
                      tooltipContent="Remove tag"
                      aria-label={`Remove tag ${tag.name}`}
                    />
                  }
                  title="Remove Tag?"
                  description={
                    <>
                      Remove <strong>{tag.name}</strong> from this member?
                    </>
                  }
                  onConfirm={() => removeTag(tag.id)}
                />
              </li>
            ))}
          </ul>
        ) : (
          <p className="text-muted-foreground text-sm">No tags applied.</p>
        )}

        {unappliedTags.length > 0 && (
          <div className="flex max-w-lg items-end gap-3">
            <div className="flex-1">
              <p className="text-muted-foreground mb-2 text-xs font-medium tracking-wide uppercase">
                Add tags
              </p>
              <Combobox
                items={options}
                value={selectedOptions}
                onValueChange={setSelectedOptions}
                multiple
              >
                <ComboboxChips ref={anchorRef}>
                  {selectedOptions.map((opt) => (
                    <ComboboxChip key={opt.value}>{opt.label}</ComboboxChip>
                  ))}
                  <ComboboxChipsInput placeholder="Search tags…" />
                </ComboboxChips>
                <ComboboxContent anchor={anchorRef}>
                  <ComboboxEmpty>No tags found.</ComboboxEmpty>
                  <ComboboxList>
                    {(item) => (
                      <ComboboxItem key={item.value} value={item}>
                        {item.label}
                      </ComboboxItem>
                    )}
                  </ComboboxList>
                </ComboboxContent>
              </Combobox>
            </div>
            <Button onClick={handleAdd} disabled={selectedOptions.length === 0 || isAdding}>
              {isAdding ? "Adding…" : "Add"}
            </Button>
          </div>
        )}
      </div>
    </section>
  );
}
