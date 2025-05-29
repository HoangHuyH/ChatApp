// AI Chat Bubble Functionality
class AIChatBubble {    constructor() {
        this.isOpen = false;
        this.apiKey = null;
        this.apiModel = 'gemini-2.0-flash';
        this.apiConfigured = false;
        this.messages = [];
        this.init();
    }

    init() {
        this.createBubbleHTML();
        this.bindEvents();
        this.loadApiConfig();
    }

    createBubbleHTML() {
        // Create the AI chat bubble
        const bubble = document.createElement('div');
        bubble.className = 'ai-chat-bubble';
        bubble.innerHTML = '<i class="bi bi-robot"></i>';
        
        // Create the AI chat window
        const chatWindow = document.createElement('div');
        chatWindow.className = 'ai-chat-window';
        chatWindow.innerHTML = `
            <div class="ai-chat-header">
                <h5><i class="bi bi-robot me-2"></i>AI Assistant</h5>
                <button class="ai-chat-close" type="button">
                    <i class="bi bi-x"></i>
                </button>
            </div>
            <div class="ai-chat-messages" id="aiChatMessages">
                <div class="ai-message">
                    <div>Hello! I'm your AI assistant. How can I help you today?</div>
                    <div class="message-time">${this.getCurrentTime()}</div>
                </div>
            </div>
            <div class="ai-chat-input">
                <div class="ai-input-group">
                    <input type="text" id="aiChatInput" placeholder="Type your message..." maxlength="500">
                    <button class="ai-send-btn" id="aiSendBtn" type="button">
                        <i class="bi bi-send"></i>
                    </button>
                </div>
            </div>
        `;

        // Add to page
        document.body.appendChild(bubble);
        document.body.appendChild(chatWindow);

        // Store references
        this.bubble = bubble;
        this.chatWindow = chatWindow;
        this.messagesContainer = document.getElementById('aiChatMessages');
        this.chatInput = document.getElementById('aiChatInput');
        this.sendBtn = document.getElementById('aiSendBtn');
    }

    bindEvents() {
        // Bubble click to toggle
        this.bubble.addEventListener('click', () => this.toggleChat());
        
        // Close button
        this.chatWindow.querySelector('.ai-chat-close').addEventListener('click', () => this.closeChat());
        
        // Send button
        this.sendBtn.addEventListener('click', () => this.sendMessage());
        
        // Enter key in input
        this.chatInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.sendMessage();
            }
        });

        // Close when clicking outside
        document.addEventListener('click', (e) => {
            if (this.isOpen && 
                !this.chatWindow.contains(e.target) && 
                !this.bubble.contains(e.target)) {
                this.closeChat();
            }
        });
    }    loadApiConfig() {
        // Check API configuration status from backend
        this.checkApiConfiguration();
    }    async checkApiConfiguration() {
        try {
            const response = await fetch('/api/aichat/status', {
                credentials: 'same-origin' // Include cookies for authentication
            });
            if (response.ok) {
                const data = await response.json();
                this.apiConfigured = data.configured;
                this.apiModel = data.model;
                
                if (!this.apiConfigured) {
                    console.warn('AI Chat: API not configured properly');
                }
            } else if (response.status === 401) {
                console.log('AI Chat: Authentication required');
                this.apiConfigured = false;
            }
        } catch (error) {
            console.error('AI Chat: Failed to check API configuration', error);
            this.apiConfigured = false;
        }
    }

    toggleChat() {
        if (this.isOpen) {
            this.closeChat();
        } else {
            this.openChat();
        }
    }

    openChat() {
        this.isOpen = true;
        this.bubble.classList.add('active');
        this.chatWindow.classList.add('show');
        
        // Focus input after animation
        setTimeout(() => {
            this.chatInput.focus();
        }, 300);
    }

    closeChat() {
        this.isOpen = false;
        this.bubble.classList.remove('active');
        this.chatWindow.classList.remove('show');
    }

    async sendMessage() {
        const message = this.chatInput.value.trim();
        if (!message) return;

        // Disable input while processing
        this.setInputState(false);
        
        // Add user message
        this.addMessage(message, 'user');
        this.chatInput.value = '';

        // Show typing indicator
        this.showTypingIndicator();

        try {
            // Send to AI API
            const response = await this.callAIAPI(message);
            this.hideTypingIndicator();
            this.addMessage(response, 'ai');
        } catch (error) {
            this.hideTypingIndicator();
            this.addMessage('Sorry, I\'m having trouble connecting right now. Please try again later.', 'ai');
            console.error('AI Chat Error:', error);
        }

        // Re-enable input
        this.setInputState(true);
        this.chatInput.focus();
    }    async callAIAPI(message) {
        try {
            // Get anti-forgery token if available
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            
            const headers = {
                'Content-Type': 'application/json',
            };
            
            if (token) {
                headers['RequestVerificationToken'] = token;
            }

            const response = await fetch('/api/aichat', {
                method: 'POST',
                headers: headers,
                credentials: 'same-origin', // Include cookies for authentication
                body: JSON.stringify({
                    message: message,
                    conversationHistory: this.messages.slice(-10) // Last 10 messages for context
                })
            });

            if (!response.ok) {
                if (response.status === 401) {
                    throw new Error('Authentication required');
                }
                throw new Error('Failed to get AI response');
            }

            const data = await response.json();
            return data.response;
        } catch (error) {
            console.error('AI API Error:', error);
            
            // Show specific error for authentication
            if (error.message === 'Authentication required') {
                return "Please log in to use the AI chat feature.";
            }
            
            // Fallback to simple responses if API fails
            const responses = {
                hello: "Hello! How are you doing today?",
                help: "I'm here to help! What would you like assistance with?",
                weather: "I don't have access to real-time weather data, but you can check your local weather service for accurate information.",
                time: `The current time is ${new Date().toLocaleTimeString()}.`,
                default: "I'm sorry, I'm having trouble connecting to my AI service right now. Please try again later."
            };

            const lowerMessage = message.toLowerCase();
            for (const [keyword, response] of Object.entries(responses)) {
                if (lowerMessage.includes(keyword)) {
                    return response;
                }
            }

            return responses.default;
        }
    }

    addMessage(message, sender) {
        const messageDiv = document.createElement('div');
        messageDiv.className = sender === 'user' ? 'user-message' : 'ai-message';
        
        messageDiv.innerHTML = `
            <div>${this.escapeHtml(message)}</div>
            <div class="message-time">${this.getCurrentTime()}</div>
        `;

        this.messagesContainer.appendChild(messageDiv);
        this.scrollToBottom();

        // Store message in conversation history
        this.messages.push({
            sender: sender,
            message: message,
            timestamp: new Date()
        });

        // Keep only last 50 messages
        if (this.messages.length > 50) {
            this.messages = this.messages.slice(-50);
        }
    }

    showTypingIndicator() {
        const typingDiv = document.createElement('div');
        typingDiv.className = 'ai-typing';
        typingDiv.id = 'aiTypingIndicator';
        typingDiv.innerHTML = `
            <div class="ai-typing-dots">
                <div class="ai-typing-dot"></div>
                <div class="ai-typing-dot"></div>
                <div class="ai-typing-dot"></div>
            </div>
            <span style="margin-left: 8px; font-size: 12px; color: #666;">AI is typing...</span>
        `;

        this.messagesContainer.appendChild(typingDiv);
        this.scrollToBottom();
    }

    hideTypingIndicator() {
        const typingIndicator = document.getElementById('aiTypingIndicator');
        if (typingIndicator) {
            typingIndicator.remove();
        }
    }

    setInputState(enabled) {
        this.chatInput.disabled = !enabled;
        this.sendBtn.disabled = !enabled;
    }

    scrollToBottom() {
        this.messagesContainer.scrollTop = this.messagesContainer.scrollHeight;
    }

    getCurrentTime() {
        return new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

// Initialize AI Chat Bubble when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    // Only initialize if user is logged in (optional check)
    const aiChatBubble = new AIChatBubble();
    
    // Make it globally accessible for debugging
    window.aiChatBubble = aiChatBubble;
    
    console.log('AI Chat Bubble initialized');
});

// Export for module usage if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AIChatBubble;
}
