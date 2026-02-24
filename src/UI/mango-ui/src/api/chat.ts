import type { AxiosInstance } from 'axios';
import type { ResultModel, ChatMessage, ChatMessageRole, PromptRequest, PaginatedItems } from '../types';
import { userManager } from '../auth';

const mapRole = (role: string | number): ChatMessageRole => {
    if (typeof role === 'number') {
        switch (role) {
            case 0: return 'System';
            case 1: return 'User';
            case 2: return 'Assistant';
            default: return 'Assistant';
        }
    }
    if (typeof role === 'string') {
        const normalized = role.charAt(0).toUpperCase() + role.slice(1).toLowerCase();
        return (normalized === 'User' || normalized === 'Assistant' || normalized === 'System')
            ? normalized as ChatMessageRole
            : 'Assistant';
    }
    return 'Assistant';
};

// Normalize backend properties which might be PascalCase or camelCase
const normalizeMessage = (msg: Record<string, unknown>): ChatMessage => ({
    id: (msg.id || msg.Id) as string,
    role: mapRole((msg.role || msg.Role) as string | number),
    content: (msg.content || msg.Content) as string,
    createdAt: (msg.createdAt || msg.CreatedAt) as string
});

export const createChatApi = (instance: AxiosInstance) => ({
    fetchChatHistory: async (pageSize = 20, pageIndex = 1): Promise<ResultModel<ChatMessage[]>> => {
        try {
            // Backend returns PaginatedItems<ChatMessageDto>
            // Which is { data: [], pageIndex, pageSize, count }
            const response = await instance.get<ResultModel<PaginatedItems<Record<string, unknown>>>>('/api/chat-histories', {
                params: { pageSize, pageIndex }
            });

            const rawData = response.data;
            let messages: Record<string, unknown>[] = [];

            // Handle if the backend returns PaginatedItems directly or via an data property
            if (rawData.data && typeof rawData.data === 'object') {
                // If it's a ResultModel<PaginatedItems<T>>
                const data = rawData.data as { data?: unknown[] };
                if (data.data && Array.isArray(data.data)) {
                    messages = data.data as Record<string, unknown>[];
                } else if (Array.isArray(data)) {
                    messages = data as Record<string, unknown>[];
                }
            }

            const normalizedMessages = messages.map(normalizeMessage);

            return { data: normalizedMessages, isError: false };
        } catch (error: unknown) {
            const err = error as { response?: { data?: { message?: string } } };
            console.error('Chat history fetch error:', error);
            return {
                data: [],
                isError: true,
                errorMessage: err.response?.data?.message || 'Failed to fetch chat history'
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
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify(prompt)
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(errorData.message || 'Failed to send message');
            }

            if (!response.body) {
                throw new Error('No response body');
            }

            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            let buffer = '';

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                buffer += decoder.decode(value, { stream: true });

                // Process buffer for complete JSON objects (assuming one per line or separated by {})
                const boundary = buffer.lastIndexOf('\n');
                if (boundary === -1) {
                    // Try finding boundary of concatenated JSON objects if no newlines
                    // This is trickier, but ASP.NET typically uses newlines for IAsyncEnumerable
                    continue;
                }

                const completeLines = buffer.slice(0, boundary).split('\n');
                buffer = buffer.slice(boundary + 1);

                for (const line of completeLines) {
                    const trimmedLine = line.trim();
                    if (!trimmedLine) continue;

                    try {
                        const parsed = JSON.parse(trimmedLine);
                        if (parsed.content) {
                            onChunk(parsed.content);
                        }
                    } catch {
                        // If it's not JSON, might be raw text or partial
                        console.warn('Failed to parse chat chunk:', trimmedLine);
                        onChunk(trimmedLine);
                    }
                }
            }

            // Final check of remaining buffer
            if (buffer.trim()) {
                try {
                    const parsed = JSON.parse(buffer.trim());
                    if (parsed.content) onChunk(parsed.content);
                } catch {
                    onChunk(buffer.trim());
                }
            }

            return { data: undefined as void, isError: false };
        } catch (error: unknown) {
            const err = error as { message?: string };
            return {
                data: undefined as void,
                isError: true,
                errorMessage: err.message || 'Failed to send message'
            };
        }
    }
});
