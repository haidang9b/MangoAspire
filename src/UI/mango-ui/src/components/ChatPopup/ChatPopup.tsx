import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useApi } from '@/hooks/useApi';
import { useAuth } from '@/hooks';
import { useTranslation } from 'react-i18next';
import type { ChatMessage } from '@/types/chat';
import './ChatPopup.css';

/**
 * Three bouncing dots shown while the assistant composes a reply.
 *
 * The answer is generated and verified server-side before the first chunk is sent, so
 * there is a real pause with nothing to render. Without this the bubble sits empty and
 * reads as a failure.
 */
function TypingIndicator() {
    return (
        <span className="chat-typing" role="status" aria-label="Assistant is typing">
            <span className="chat-typing__dot" />
            <span className="chat-typing__dot" />
            <span className="chat-typing__dot" />
        </span>
    );
}

export function ChatPopup() {
    const { chat: chatService } = useApi();
    const { user } = useAuth();
    const { t } = useTranslation();
    const isAuthenticated = !!user;
    const [isOpen, setIsOpen] = useState(false);
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [input, setInput] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [historyLoading, setHistoryLoading] = useState(false);
    const messagesEndRef = useRef<HTMLDivElement>(null);

    const scrollToBottom = () => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    };

    const loadHistory = useCallback(async () => {
        setHistoryLoading(true);
        const result = await chatService.fetchChatHistory();
        if (!result.isError && result.data) {
            // Sort by creation date
            const sorted = [...result.data].sort((a, b) =>
                new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
            );
            setMessages(sorted);
        }
        setHistoryLoading(false);
    }, [chatService]);

    useEffect(() => {
        if (isOpen) {
            scrollToBottom();
        }
    }, [isOpen, messages]);

    useEffect(() => {
        if (isOpen && isAuthenticated && messages.length === 0) {
            setTimeout(() => {
                loadHistory();
            }, 0);
        }
    }, [isOpen, isAuthenticated, messages.length, loadHistory]);

    const handleSend = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!input.trim() || isLoading) return;

        const userMessage: ChatMessage = {
            id: Date.now().toString(),
            role: 'User',
            content: input,
            createdAt: new Date().toISOString()
        };

        setMessages(prev => [...prev, userMessage]);
        setInput('');
        setIsLoading(true);

        let assistantMessageContent = '';
        const assistantMessageId = (Date.now() + 1).toString();

        setMessages(prev => [...prev, {
            id: assistantMessageId,
            role: 'Assistant',
            content: '',
            createdAt: new Date().toISOString()
        }]);

        const result = await chatService.sendMessage(
            { content: input },
            (chunk) => {
                assistantMessageContent += chunk;
                setMessages(prev => prev.map(msg =>
                    msg.id === assistantMessageId
                        ? { ...msg, content: assistantMessageContent }
                        : msg
                ));
            }
        );

        if (result.isError) {
            setMessages(prev => prev.map(msg =>
                msg.id === assistantMessageId
                    ? { ...msg, content: 'Sorry, I encountered an error. Please try again.' }
                    : msg
            ));
        }

        setIsLoading(false);
    };

    const formatMessageTime = (dateString: string) => {
        const date = new Date(dateString);
        const now = new Date();
        const yesterday = new Date(now);
        yesterday.setDate(now.getDate() - 1);

        const timeStr = date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

        if (date.toDateString() === now.toDateString()) {
            return timeStr;
        } else if (date.toDateString() === yesterday.toDateString()) {
            return `Yesterday, ${timeStr}`;
        } else {
            const dateOptions: Intl.DateTimeFormatOptions = {
                month: 'short',
                day: 'numeric',
                year: now.getFullYear() !== date.getFullYear() ? 'numeric' : undefined
            };
            return `${date.toLocaleDateString([], dateOptions)}, ${timeStr}`;
        }
    };

    if (!isAuthenticated) return null;

    return (
        <div className={`chat-popup ${isOpen ? 'chat-popup--open' : ''}`}>
            {!isOpen && (
                <button
                    className="chat-toggle-btn"
                    onClick={() => setIsOpen(true)}
                    title="Chat with Assistant"
                >
                    💬
                </button>
            )}

            {isOpen && (
                <div className="chat-window">
                    <div className="chat-header">
                        <div className="chat-header__info">
                            <span className="chat-header__icon">🤖</span>
                            <div>
                                <h3>Mango Assistant</h3>
                                <span className="chat-header__status">Online</span>
                            </div>
                        </div>
                        <button className="chat-close-btn" onClick={() => setIsOpen(false)}>✕</button>
                    </div>

                    <div className="chat-messages">
                        {historyLoading && <div className="chat-loading">Loading history...</div>}
                        {messages.map((msg, index) => {
                            // The assistant bubble is inserted optimistically and stays empty
                            // until the first chunk arrives. Restricted to the last message so
                            // an empty reply elsewhere in the history cannot start animating.
                            const isPending = isLoading
                                && msg.role === 'Assistant'
                                && !msg.content
                                && index === messages.length - 1;

                            return (
                                <div key={msg.id} className={`chat-msg chat-msg--${msg.role.toLowerCase()}`}>
                                    <div className={`chat-msg__bubble${isPending ? ' chat-msg__bubble--typing' : ''}`}>
                                        {isPending ? <TypingIndicator /> : msg.content}
                                    </div>
                                    {/* A timestamp on a reply that has not arrived yet is noise. */}
                                    {!isPending && (
                                        <span className="chat-msg__time">
                                            {formatMessageTime(msg.createdAt)}
                                        </span>
                                    )}
                                </div>
                            );
                        })}
                        <div ref={messagesEndRef} />
                    </div>

                    <form className="chat-input-area" onSubmit={handleSend}>
                        <input
                            type="text"
                            placeholder={t('common.add') || "Type your message..."}
                            value={input}
                            onChange={(e) => setInput(e.target.value)}
                            disabled={isLoading}
                        />
                        <button type="submit" disabled={isLoading || !input.trim()}>
                            {isLoading ? <span className="chat-spinner" aria-label="Sending" /> : '➤'}
                        </button>
                    </form>
                </div>
            )}
        </div>
    );
}
