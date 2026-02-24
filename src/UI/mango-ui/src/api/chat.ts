import type { AxiosInstance } from 'axios';
import type { ResultModel, ChatMessage, ChatMessageRole, PromptRequest } from '../types';
import { userManager } from '../auth';

const mapRole = (role: any): ChatMessageRole => {
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
const normalizeMessage = (msg: any): ChatMessage => ({
    id: msg.id || msg.Id,
    role: mapRole(msg.role || msg.Role),
    content: msg.content || msg.Content,
    createdAt: msg.createdAt || msg.CreatedAt
});

export const createChatApi = (instance: AxiosInstance) => ({
    fetchChatHistory: async (pageSize = 20, pageIndex = 1): Promise<ResultModel<ChatMessage[]>> => {
        try {
            // Backend returns PaginatedItems<ChatMessageDto>
            // Which is { data: [], pageIndex, pageSize, count }
            const response = await instance.get<any>('/api/chat-histories', {
                params: { pageSize, pageIndex }
            });

            const rawData = response.data;
            let messages: any[] = [];

            // Handle if the backend returns PaginatedItems directly or via an data property
            if (rawData.data && Array.isArray(rawData.data)) {
                messages = rawData.data;
            } else if (Array.isArray(rawData)) {
                messages = rawData;
            } else if (rawData.Data && Array.isArray(rawData.Data)) {
                messages = rawData.Data;
            }

            const normalizedMessages = messages.map(normalizeMessage);

            return { data: normalizedMessages, isError: false };
        } catch (error: any) {
            console.error('Chat history fetch error:', error);
            return {
                data: [],
                isError: true,
                errorMessage: error.response?.data?.message || 'Failed to fetch chat history'
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
                let boundary = buffer.lastIndexOf('\n');
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
                    } catch (e) {
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
        } catch (error: any) {
            return {
                data: undefined as void,
                isError: true,
                errorMessage: error.message || 'Failed to send message'
            };
        }
    }
});
