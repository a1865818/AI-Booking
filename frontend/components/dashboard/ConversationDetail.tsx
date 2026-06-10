"use client";

import { useState } from "react";
import { AiToggle } from "./AiToggle";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";
import { apiFetch } from "@/lib/api-client";
import { getAccessToken } from "@/lib/auth";
import type { Message } from "@/hooks/useConversation";

export function ConversationDetail({
  conversationId,
  customerName,
  aiEnabled: initialAiEnabled,
  messages,
  onMessageSent,
}: {
  conversationId: string;
  customerName: string;
  aiEnabled: boolean;
  messages: Message[];
  onMessageSent?: (message: Message) => void;
}) {
  const [draft, setDraft] = useState("");
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function toggleAi(enabled: boolean) {
    await apiFetch(`/api/conversations/${conversationId}/toggle-ai`, {
      method: "POST",
      accessToken: getAccessToken() ?? undefined,
      body: JSON.stringify({ enabled }),
    });
  }

  async function sendReply(body: string) {
    setSending(true);
    setError(null);
    try {
      const sent = await apiFetch<Message>(`/api/conversations/${conversationId}/messages`, {
        method: "POST",
        accessToken: getAccessToken() ?? undefined,
        body: JSON.stringify({ body }),
      });
      onMessageSent?.(sent);
      setDraft("");
    } catch {
      setError("Could not send your reply. Try again.");
    } finally {
      setSending(false);
    }
  }

  return (
    <div className="flex flex-1 flex-col">
      <header className="flex items-center justify-between border-b border-border-subtle p-4">
        <h2 className="font-semibold text-primary">{customerName}</h2>
        <AiToggle initial={initialAiEnabled} onChange={(enabled) => void toggleAi(enabled)} />
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
        className="flex flex-col gap-2 border-t border-border-subtle p-4"
        onSubmit={(e) => {
          e.preventDefault();
          const body = draft.trim();
          if (body) void sendReply(body);
        }}
      >
        <div className="flex items-center gap-2">
          <input
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            placeholder="Reply as a human…"
            className="flex-1 rounded-md bg-elevated px-3 py-2 text-sm text-primary shadow-[var(--shadow-inset)] placeholder:text-tertiary"
          />
          <Button type="submit" disabled={!draft.trim() || sending}>
            {sending ? "Sending…" : "Send"}
          </Button>
        </div>
        {error && (
          <p role="alert" className="text-sm text-error">
            {error}
          </p>
        )}
      </form>
    </div>
  );
}
