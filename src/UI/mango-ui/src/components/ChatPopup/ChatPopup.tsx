import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useApi } from '@/hooks/useApi';
import { useAuth } from '@/hooks';
import type { ChatMessage } from '@/types/chat';
import './ChatPopup.css';

const PAGE_SIZE = 5;

export function ChatPopup() {
    const { chat: chatService } = useApi();
    const { user } = useAuth();
    const isAuthenticated = !!user;

    const [isOpen, setIsOpen] = useState(false);
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [input, setInput] = useState('');
    const [isSending, setIsSending] = useState(false);

    // Pagination state
    const [pageIndex, setPageIndex] = useState(1);
    const [hasMore, setHasMore] = useState(false);
    const [isLoadingMore, setIsLoadingMore] = useState(false);
    const [historyLoading, setHistoryLoading] = useState(false);
    const [historyError, setHistoryError] = useState<string | null>(null);

    // Refs — keep stable references to avoid stale closures
    const messagesContainerRef = useRef<HTMLDivElement>(null);
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const topSentinelRef = useRef<HTMLDivElement>(null);
    const initialLoadDone = useRef(false);
    // Stable ref to chatService — avoids useCallback dependency on a value that re-creates each render
    const chatServiceRef = useRef(chatService);
    useEffect(() => { chatServiceRef.current = chatService; }, [chatService]);
    // Stable ref to pageIndex — avoids IntersectionObserver dependency triggering reconnect on every page load
    const pageIndexRef = useRef(pageIndex);
    useEffect(() => { pageIndexRef.current = pageIndex; }, [pageIndex]);

    const scrollToBottom = useCallback(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, []);

    // Load a specific page. prepend=true adds older messages above existing ones.
    const fetchPage = useCallback(async (page: number, prepend: boolean) => {
        if (prepend) setIsLoadingMore(true);
        else setHistoryLoading(true);
        setHistoryError(null);

        const container = messagesContainerRef.current;
        const prevScrollHeight = container?.scrollHeight ?? 0;

        const result = await chatServiceRef.current.fetchChatHistory(PAGE_SIZE, page);

        if (!result.isError && result.data) {
            const { messages: newMsgs, hasMore: more } = result.data;
            setHasMore(more);
            setMessages(prev => prepend ? [...newMsgs, ...prev] : newMsgs);

            if (prepend && container) {
                // Restore scroll position so the view doesn't jump after prepend
                requestAnimationFrame(() => {
                    container.scrollTop = container.scrollHeight - prevScrollHeight;
                });
            }
        } else if (result.isError) {
            setHistoryError(result.errorMessage ?? 'Failed to load chat history.');
        }

        if (prepend) setIsLoadingMore(false);
        else setHistoryLoading(false);
    }, []); // stable — reads chatServiceRef.current at call time

    // Initial load when chat opens
    useEffect(() => {
        if (isOpen && isAuthenticated && !initialLoadDone.current) {
            initialLoadDone.current = true;
            fetchPage(1, false).then(scrollToBottom);
        }
    }, [isOpen, isAuthenticated, fetchPage, scrollToBottom]);

    // Reset when closed
    useEffect(() => {
        if (!isOpen) {
            setMessages([]);
            setPageIndex(1);
            setHasMore(false);
            setHistoryError(null);
            initialLoadDone.current = false;
        }
    }, [isOpen]);

    // IntersectionObserver — watches top sentinel to load older messages on scroll-up
    useEffect(() => {
        // Guard: don't register while loading (any kind) or no more pages
        if (!hasMore || isLoadingMore || historyLoading) return;
        const sentinel = topSentinelRef.current;
        if (!sentinel) return;

        const observer = new IntersectionObserver(
            (entries) => {
                if (entries[0].isIntersecting) {
                    // Read current page from ref — avoids stale closure & unnecessary reconnects
                    const nextPage = pageIndexRef.current + 1;
                    setPageIndex(nextPage);
                    fetchPage(nextPage, true);
                }
            },
            { root: messagesContainerRef.current, threshold: 0.1 }
        );

        observer.observe(sentinel);
        return () => observer.disconnect();
        // pageIndex intentionally omitted — read via pageIndexRef to avoid reconnect on every load
    }, [hasMore, isLoadingMore, historyLoading, fetchPage]);

    // Scroll to bottom when user sends a message
    useEffect(() => {
        if (isSending) scrollToBottom();
    }, [messages, isSending, scrollToBottom]);

    const handleSend = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!input.trim() || isSending) return;

        const userMessage: ChatMessage = {
            id: Date.now().toString(),
            role: 'User',
            content: input,
            createdAt: new Date().toISOString(),
        };

        const assistantMessageId = (Date.now() + 1).toString();
        const assistantPlaceholder: ChatMessage = {
            id: assistantMessageId,
            role: 'Assistant',
            content: '',
            createdAt: new Date().toISOString(),
        };

        setMessages(prev => [...prev, userMessage, assistantPlaceholder]);
        setInput('');
        setIsSending(true);

        let accumulated = '';
        const result = await chatServiceRef.current.sendMessage(
            { content: userMessage.content },
            (chunk) => {
                accumulated += chunk;
                setMessages(prev => prev.map(msg =>
                    msg.id === assistantMessageId
                        ? { ...msg, content: accumulated }
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

        setIsSending(false);
    };

    const formatMessageTime = (dateString: string) => {
        const date = new Date(dateString);
        const now = new Date();
        const yesterday = new Date(now);
        yesterday.setDate(now.getDate() - 1);
        const timeStr = date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        if (date.toDateString() === now.toDateString()) return timeStr;
        if (date.toDateString() === yesterday.toDateString()) return `Yesterday, ${timeStr}`;
        const dateOptions: Intl.DateTimeFormatOptions = {
            month: 'short', day: 'numeric',
            year: now.getFullYear() !== date.getFullYear() ? 'numeric' : undefined,
        };
        return `${date.toLocaleDateString([], dateOptions)}, ${timeStr}`;
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

                    <div className="chat-messages" ref={messagesContainerRef}>
                        {/* Top sentinel — IntersectionObserver triggers load-more when scrolled to top */}
                        <div ref={topSentinelRef} />

                        {isLoadingMore && (
                            <div className="chat-loading chat-loading--top">Loading older messages...</div>
                        )}
                        {historyLoading && (
                            <div className="chat-loading">Loading history...</div>
                        )}
                        {historyError && (
                            <div className="chat-error">{historyError}</div>
                        )}

                        {messages.map((msg) => {
                            const isStreamingPlaceholder =
                                isSending && msg.role === 'Assistant' && msg.content === '';
                            return (
                                <div key={msg.id} className={`chat-msg chat-msg--${msg.role.toLowerCase()}`}>
                                    <div className="chat-msg__bubble">
                                        {isStreamingPlaceholder ? '...' : msg.content}
                                    </div>
                                    <span className="chat-msg__time">
                                        {formatMessageTime(msg.createdAt)}
                                    </span>
                                </div>
                            );
                        })}

                        <div ref={messagesEndRef} />
                    </div>

                    <form className="chat-input-area" onSubmit={handleSend}>
                        <input
                            type="text"
                            placeholder="Type your message..."
                            value={input}
                            onChange={(e) => setInput(e.target.value)}
                            disabled={isSending}
                        />
                        <button type="submit" disabled={isSending || !input.trim()}>
                            {isSending ? '...' : '➤'}
                        </button>
                    </form>
                </div>
            )}
        </div>
    );
}
