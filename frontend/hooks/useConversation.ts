"use client";

import { useEffect, useState } from "react";
import { useRealtime } from "@/components/shared/RealtimeProvider";

export type Message = {
  id: string;
  conversationId: string;
  direction: "Inbound" | "Outbound";
  body: string;
  createdAt: string;
};

/** Live message stream for a single conversation transcript (PLAN §3.8). */
export function useConversation(conversationId: string, initial: Message[] = []) {
  const { connection } = useRealtime();
  const [messages, setMessages] = useState<Message[]>(initial);

  useEffect(() => {
    setMessages(initial);
  }, [conversationId, initial]);

  useEffect(() => {
    if (!connection) return;

    const onMessage = (payload: { conversationId: string; message: Message }) => {
      if (payload.conversationId === conversationId) {
        setMessages((prev) => [...prev, payload.message]);
      }
    };

    connection.on("NewConversationMessage", onMessage);
    return () => connection.off("NewConversationMessage", onMessage);
  }, [connection, conversationId]);

  const appendMessage = (message: Message) => {
    setMessages((prev) =>
      prev.some((m) => m.id === message.id) ? prev : [...prev, message],
    );
  };

  return { messages, appendMessage };
}
