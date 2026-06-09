"use client";

import { useApiData } from "@/hooks/useApiData";
import { ConversationList, type ConversationSummary } from "@/components/dashboard/ConversationList";
import { LoadingState, ErrorState, EmptyState } from "@/components/ui/States";

type ApiConversation = {
  id: string;
  customerName: string;
  preview: string | null;
  escalated: boolean;
};

export default function ConversationsPage() {
  const { data, loading, error, reload } = useApiData<ApiConversation[]>("/api/conversations");

  if (loading) return <LoadingState label="Loading conversations" />;
  if (error) return <ErrorState message={error} onRetry={reload} />;

  const conversations: ConversationSummary[] = (data ?? []).map((c) => ({
    id: c.id,
    customerName: c.customerName,
    preview: c.preview ?? "",
    unread: false,
    escalated: c.escalated,
  }));

  if (conversations.length === 0) {
    return <EmptyState title="No conversations yet" hint="Inbound SMS threads will appear here." />;
  }

  return (
    <div className="flex h-[calc(100vh-4rem)] -m-8">
      <ConversationList conversations={conversations} />
      <div className="flex flex-1 items-center justify-center text-sm text-tertiary">
        Select a conversation
      </div>
    </div>
  );
}
