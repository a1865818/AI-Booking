import Link from "next/link";
import { EscalationBadge } from "./EscalationBadge";
import { cn } from "@/lib/utils";

export type ConversationSummary = {
  id: string;
  customerName: string;
  preview: string;
  unread: boolean;
  escalated: boolean;
};

export function ConversationList({
  conversations,
  activeId,
}: {
  conversations: ConversationSummary[];
  activeId?: string;
}) {
  return (
    <div className="flex w-80 flex-col border-r border-border-subtle">
      {conversations.map((c) => (
        <Link
          key={c.id}
          href={`/conversations/${c.id}`}
          className={cn(
            "flex flex-col gap-1 border-b border-border-subtle p-4 transition-colors hover:bg-elevated",
            c.id === activeId && "bg-elevated",
          )}
        >
          <div className="flex items-center justify-between">
            <span className="flex items-center gap-2 font-semibold text-primary">
              {c.unread && <span className="h-2 w-2 rounded-pill bg-accent" />}
              {c.customerName}
            </span>
            {c.escalated && <EscalationBadge />}
          </div>
          <p className="truncate text-sm text-secondary">{c.preview}</p>
        </Link>
      ))}
    </div>
  );
}
