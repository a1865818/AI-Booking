"use client";

import { use } from "react";
import { useApiData } from "@/hooks/useApiData";
import { ConversationList, type ConversationSummary } from "@/components/dashboard/ConversationList";
import { ConversationDetail } from "@/components/dashboard/ConversationDetail";
import { LoadingState, ErrorState } from "@/components/ui/States";
import { useConversation, type Message } from "@/hooks/useConversation";

const EMPTY_MESSAGES: Message[] = [];

type ApiConversation = { id: string; customerName: string; preview: string | null; escalated: boolean };
type ApiDetail = {
  conversation: { id: string; customerName: string; aiEnabled: boolean };
  messages: Message[];
};

export default function ConversationDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const list = useApiData<ApiConversation[]>("/api/conversations");
  const detail = useApiData<ApiDetail>(`/api/conversations/${id}`);
  const { messages, appendMessage } = useConversation(id, detail.data?.messages ?? EMPTY_MESSAGES);

  if (detail.loading) return <LoadingState label="Loading conversation" />;
  if (detail.error) return <ErrorState message={detail.error} onRetry={detail.reload} />;

  const conversations: ConversationSummary[] = (list.data ?? []).map((c) => ({
    id: c.id,
    customerName: c.customerName,
    preview: c.preview ?? "",
    unread: false,
    escalated: c.escalated,
  }));

  return (
    <div className="flex h-[calc(100vh-4rem)] -m-8">
      <ConversationList conversations={conversations} activeId={id} />
      <ConversationDetail
        conversationId={id}
        customerName={detail.data?.conversation.customerName ?? ""}
        aiEnabled={detail.data?.conversation.aiEnabled ?? true}
        messages={messages}
        onMessageSent={appendMessage}
      />
    </div>
  );
}
