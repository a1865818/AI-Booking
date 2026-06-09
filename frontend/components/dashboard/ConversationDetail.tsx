"use client";

import { useState } from "react";
import { AiToggle } from "./AiToggle";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";
import type { Message } from "@/hooks/useConversation";

export function ConversationDetail({
  customerName,
  messages,
}: {
  customerName: string;
  messages: Message[];
}) {
  const [draft, setDraft] = useState("");

  return (
    <div className="flex flex-1 flex-col">
      <header className="flex items-center justify-between border-b border-border-subtle p-4">
        <h2 className="font-semibold text-primary">{customerName}</h2>
        <AiToggle />
      </header>

      <div className="flex flex-1 flex-col gap-3 overflow-y-auto p-4">
        {messages.map((m) => (
          <div
            key={m.id}
            className={cn(
              "max-w-[75%] rounded-lg px-3 py-2 text-sm",
              m.direction === "Inbound"
                ? "self-start bg-elevated text-primary"
                : "self-end bg-accent text-accent-on",
            )}
          >
            {m.body}
          </div>
        ))}
      </div>

      <form
        className="flex items-center gap-2 border-t border-border-subtle p-4"
        onSubmit={(e) => {
          e.preventDefault();
          setDraft("");
        }}
      >
        <input
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          placeholder="Reply as a human…"
          className="flex-1 rounded-md bg-elevated px-3 py-2 text-sm text-primary shadow-[var(--shadow-inset)] placeholder:text-tertiary"
        />
        <Button type="submit" disabled={!draft.trim()}>
          Send
        </Button>
      </form>
    </div>
  );
}
