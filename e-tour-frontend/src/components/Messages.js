import React, { useState, useEffect } from 'react';
import messageService from '../services/messageService';

const Messages = () => {
    const [messages, setMessages] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [selectedMessage, setSelectedMessage] = useState(null);

    useEffect(() => {
        loadMessages();
    }, []);

    const loadMessages = async () => {
        try {
            setLoading(true);
            const data = await messageService.getUserMessages();
            setMessages(data);
            setError(null);
        } catch (err) {
            setError('Failed to load messages');
            console.error('Error loading messages:', err);
        } finally {
            setLoading(false);
        }
    };

    const handleMessageClick = async (message) => {
        setSelectedMessage(message);
        if (!message.read) {
            try {
                await messageService.markAsRead(message.id);
                setMessages(messages.map(msg => 
                    msg.id === message.id ? { ...msg, read: true } : msg
                ));
            } catch (err) {
                console.error('Error marking message as read:', err);
            }
        }
    };

    const handleDeleteMessage = async (messageId) => {
        try {
            await messageService.deleteMessage(messageId);
            setMessages(messages.filter(msg => msg.id !== messageId));
            if (selectedMessage?.id === messageId) {
                setSelectedMessage(null);
            }
        } catch (err) {
            setError('Failed to delete message');
            console.error('Error deleting message:', err);
        }
    };

    if (loading) {
        return <div className="flex items-center justify-center h-screen text-gray-600">Loading messages...</div>;
    }

    if (error) {
        return <div className="flex items-center justify-center h-screen text-red-500">{error}</div>;
    }

    return (
        <div className="flex gap-8 p-4 h-[calc(100vh-100px)]">
            <div className="flex-1 border-r border-gray-200 pr-4 overflow-y-auto">
                <h2 className="text-2xl font-bold mb-4 text-gray-800">Messages</h2>
                {messages.length === 0 ? (
                    <p className="text-center text-gray-600 py-8">No messages found</p>
                ) : (
                    messages.map(message => (
                        <div
                            key={message.id}
                            className={`relative p-4 border rounded-lg mb-4 cursor-pointer transition-all duration-200 hover:bg-gray-50 group
                                ${message.read ? 'border-gray-200' : 'border-l-4 border-l-primary-500'}
                                ${selectedMessage?.id === message.id ? 'bg-primary-50 border-primary-300' : ''}`}
                            onClick={() => handleMessageClick(message)}
                        >
                            <div className="flex justify-between mb-2">
                                <span className="font-semibold text-gray-800">{message.sender}</span>
                                <span className="text-sm text-gray-500">{new Date(message.createdAt).toLocaleDateString()}</span>
                            </div>
                            <div className="text-gray-700">{message.subject}</div>
                            <button
                                className="absolute top-4 right-4 px-2 py-1 bg-red-500 text-white text-sm rounded opacity-0 hover:bg-red-600 transition-opacity duration-200 group-hover:opacity-100"
                                onClick={(e) => {
                                    e.stopPropagation();
                                    handleDeleteMessage(message.id);
                                }}
                            >
                                Delete
                            </button>
                        </div>
                    ))
                )}
            </div>
            {selectedMessage && (
                <div className="flex-2 p-4 bg-white rounded-lg shadow-sm">
                    <div className="border-b border-gray-200 pb-4 mb-4">
                        <h3 className="text-xl font-semibold text-gray-800">{selectedMessage.subject}</h3>
                        <span className="text-sm text-gray-500">
                            {new Date(selectedMessage.createdAt).toLocaleString()}
                        </span>
                    </div>
                    <div className="text-gray-600 mb-4">
                        From: {selectedMessage.sender}
                    </div>
                    <div className="text-gray-700 leading-relaxed">
                        {selectedMessage.content}
                    </div>
                </div>
            )}
        </div>
    );
};

export default Messages; 