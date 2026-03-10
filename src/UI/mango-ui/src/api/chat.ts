import type { AxiosInstance } from 'axios';
import type { ResultModel, PagedModel } from '@/types/api';
import type { ChatMessage, ChatMessageRole, PromptRequest } from '@/types/chat';
import { userManager } from '@/auth';

export interface ChatHistoryPage {
    messages: ChatMessage[];
    pageIndex: number;
    pageSize: number;
    count: number;
    hasMore: boolean;
}

// ---------- helpers ----------

const mapRole = (role: string | number): ChatMessageRole => {
    if (typeof role === 'number') {
        switch (role) {
            case 0: return 'System';
            case 1: return 'User';
            case 2: return 'Assistant';
            default: return 'Assistant';
        }
    }
    const normalized = String(role).charAt(0).toUpperCase() + String(role).slice(1).toLowerCase();
    return (normalized === 'User' || normalized === 'Assistant' || normalized === 'System')
        ? normalized as ChatMessageRole
        : 'Assistant';
};

const normalizeMessage = (msg: ChatMessage): ChatMessage => ({
    id: msg.id,
    role: mapRole(msg.role),
    content: msg.content ?? '',
    createdAt: msg.createdAt,
});

// ---------- API ----------

export const createChatApi = (instance: AxiosInstance) => ({

    fetchChatHistory: async (pageSize = 5, pageIndex = 1): Promise<ResultModel<ChatHistoryPage>> => {
        try {
            // ChatRoute returns PaginatedItems<ChatMessageDto> directly (Results.Ok — no ResultModel wrapper)
            const response = await instance.get<PagedModel<ChatMessage>>(
                '/api/chat-histories',
                { params: { pageSize, pageIndex } }
            );

            const paged = response.data;              // PagedModel<ChatMessage>
            const messages = paged.data.map(normalizeMessage);

            return {
                isError: false,
                data: {
                    messages,
                    pageIndex: paged.pageIndex,
                    pageSize: paged.pageSize,
                    count: paged.count,
                    hasMore: paged.hasNextPage,  // straight from BE
                },
            };
        } catch (error: unknown) {
            const err = error as { response?: { data?: { message?: string } } };
            console.error('Chat history fetch error:', error);
            return {
                isError: true,
                errorMessage: err.response?.data?.message ?? 'Failed to fetch chat history',
                data: { messages: [], pageIndex, pageSize, count: 0, hasMore: false },
            };
        }
    },

    sendMessage: async (prompt: PromptRequest, onChunk: (chunk: string) => void): Promise<ResultModel<void>> => {
        try {
            const chatBaseUrl = import.meta.env.VITE_CHAT_URL ?? `${instance.defaults.baseURL}/api`;
            const user = await userManager.getUser();
            const token = user?.access_token;

            const response = await fetch(`${chatBaseUrl}/chat`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`,
                },
                body: JSON.stringify(prompt),
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(errorData.message || 'Failed to send message');
            }
            if (!response.body) throw new Error('No response body');

            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            let buffer = '';

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;
                buffer += decoder.decode(value, { stream: true });

                const boundary = buffer.lastIndexOf('\n');
                if (boundary === -1) continue;

                const lines = buffer.slice(0, boundary).split('\n');
                buffer = buffer.slice(boundary + 1);

                for (const line of lines) {
                    const trimmed = line.trim();
                    if (!trimmed) continue;
                    try {
                        const parsed = JSON.parse(trimmed) as { content?: string };
                        if (parsed.content) onChunk(parsed.content);
                    } catch {
                        onChunk(trimmed);   // raw text chunk
                    }
                }
            }

            // Flush remaining buffer
            if (buffer.trim()) {
                try {
                    const parsed = JSON.parse(buffer.trim()) as { content?: string };
                    if (parsed.content) onChunk(parsed.content);
                } catch {
                    onChunk(buffer.trim());
                }
            }

            return { data: undefined as void, isError: false };
        } catch (error: unknown) {
            const err = error as { message?: string };
            return { data: undefined as void, isError: true, errorMessage: err.message ?? 'Failed to send message' };
        }
    },
});
