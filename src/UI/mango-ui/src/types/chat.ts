export type ChatMessageRole = 'User' | 'Assistant' | 'System';

export interface ChatMessage {
    id: string;
    role: ChatMessageRole;
    content: string;
    createdAt: string;
}

export interface PromptRequest {
    content: string;
}

export interface PromptResponse {
    content: string;
}
