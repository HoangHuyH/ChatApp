# AI Chat Bubble Setup Guide

## Overview
This feature adds a floating AI chat bubble to your ChatApp that allows users to interact with Google's Gemini AI model through a simple chat interface.

## Features
- 🤖 Floating AI chat bubble in the bottom-right corner
- 💬 Clean, modern chat interface
- 🎨 Responsive design that works on mobile and desktop
- ⚡ Real-time typing indicators
- 🔒 User authentication support (optional)
- 📱 Mobile-responsive design

## Setup Instructions

### 1. Get Google AI Studio API Key
1. Go to [Google AI Studio](https://makersuite.google.com/app/apikey)
2. Sign in with your Google account
3. Click "Create API Key"
4. Copy the generated API key

### 2. Configure Environment Variables
1. Open the `.env` file in your project root
2. Replace `your_google_ai_studio_api_key_here` with your actual API key:
   ```
   GOOGLE_AI_API_KEY=your_actual_api_key_here
   AI_CHAT_MODEL=gemini-pro
   ```

### 3. Test the Feature
1. Run your application
2. Navigate to any page
3. Look for the purple robot icon in the bottom-right corner
4. Click it to open the chat window
5. Type a message and press Enter or click Send

## Files Added/Modified

### New Files:
- `wwwroot/css/ai-chat.css` - Styling for the chat bubble and window
- `wwwroot/js/ai-chat.js` - JavaScript functionality for the chat interface
- `Controllers/AiChatController.cs` - API controller for handling AI requests

### Modified Files:
- `.env` - Updated with Google AI Studio configuration
- `Pages/Shared/_Layout.cshtml` - Added CSS and JS references
- `Program.cs` - Added HttpClient service registration

## Usage

### Basic Chat
- Click the robot icon to open/close the chat
- Type messages and get AI responses
- The conversation history is maintained during the session

### Features
- **Auto-scroll**: Messages automatically scroll to the bottom
- **Typing indicators**: Shows when the AI is generating a response
- **Mobile responsive**: Works well on all screen sizes
- **Click outside to close**: Click anywhere outside the chat to close it
- **Keyboard shortcuts**: Press Enter to send messages

## Security Notes

### API Key Security
- ⚠️ **Never commit your actual API key to version control**
- The API key should be kept in the `.env` file (which should be in `.gitignore`)
- In production, use environment variables or secure configuration

### Authentication
- The AI chat currently requires user authentication (controlled by `[Authorize]` attribute)
- To allow anonymous access, remove the `[Authorize]` attribute from `AiChatController`

## Customization

### Styling
Edit `wwwroot/css/ai-chat.css` to customize:
- Colors and gradients
- Chat bubble size and position
- Chat window dimensions
- Message styling

### Functionality
Edit `wwwroot/js/ai-chat.js` to customize:
- Welcome message
- Response handling
- Conversation history length
- Error messages

### API Configuration
Edit `Controllers/AiChatController.cs` to:
- Change AI model parameters
- Add conversation context
- Implement rate limiting
- Add custom prompt engineering

## Troubleshooting

### "AI chat is not configured yet" message
- Check that your API key is set in the `.env` file
- Ensure the API key is not the placeholder value
- Restart the application after changing the `.env` file

### Chat bubble not appearing
- Check browser console for JavaScript errors
- Ensure all CSS and JS files are loading correctly
- Verify Bootstrap Icons are loading (used for the robot icon)

### API errors
- Check the application logs for detailed error messages
- Verify your Google AI Studio API key is valid and has quota
- Ensure your internet connection allows API calls

## API Rate Limits
- Google AI Studio has rate limits for API calls
- Free tier: 60 requests per minute
- Consider implementing client-side rate limiting for production use

## Future Enhancements
- Add conversation persistence to database
- Implement streaming responses for longer replies
- Add file upload support
- Add conversation history management
- Implement user-specific conversation contexts
