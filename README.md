# ChatApp - Real-time Messaging Application

This project aims to build a simple messaging application for the Basic Network Programming course (NT106.P21.ANTT). The application will be developed in C# with ASP.NET Core, using SQLite as the database and SignalR to support real-time communication.


## 🚀 Features

### Core Messaging Features
- **Real-time Chat**: Instant messaging with SignalR for real-time communication
- **Private Messages**: One-on-one conversations between friends
- **Room Chat**: Create and manage room conversations with multiple participants
- **Message Types**: Support for text, images, files
- **Message Status**: Track message delivery status (Sent, Delivered, Read)
- **Typing Indicators**: See when someone is typing in real-time
- **File Sharing**: Upload and share various file types with size limitations

### User Management
- **User Authentication**: Secure registration and login with ASP.NET Core Identity
- **User Profiles**: Display names and user information
- **Friend System**: Send friend requests, accept/decline, and manage friendships
- **Online Status**: Real-time online/offline status tracking
- **User Search**: Find and connect with other users

### Advanced Features
- **AI Chat Assistant**: Integrated AI chatbot powered by Google's Gemini AI
- **Desktop App**: Cross-platform desktop application using Electron
- **Responsive Design**: Mobile-friendly interface with Bootstrap
- **Room-based Chat**: Organized chat rooms for better conversation management

## 🛠️ Technology Stack

### Backend
- **Framework**: ASP.NET Core 9.0
- **Database**: SQLite with Entity Framework Core
- **Real-time**: SignalR for WebSocket communication
- **Authentication**: ASP.NET Core Identity
- **API Integration**: Google AI Studio (Gemini AI)

### Frontend
- **Framework**: ASP.NET Core Razor Pages
- **Styling**: Bootstrap 5, Custom CSS
- **JavaScript**: Vanilla JS with SignalR client
- **Icons**: Bootstrap Icons

### Desktop
- **Framework**: Electron
- **Runtime**: Node.js

### Development Tools
- **IDE**: Visual Studio 2022 / VS Code
- **Package Manager**: NuGet (.NET), npm (Node.js)
- **Environment**: .NET 9.0, Node.js

## 📋 Prerequisites

Before running this application, ensure you have the following installed:

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org/) (for desktop app)
- [VS Code](https://code.visualstudio.com/)
- [Google AI Studio API Key](https://makersuite.google.com/app/apikey) (optional, for AI features)

## ⚡ Quick Start

### 1. Clone the Repository
```bash
git clone <repository-url>
cd ChatApp
```

### 2. Setup the Web Application
```bash
cd ChatApp/src/ChatApp.Web

# Restore NuGet packages
dotnet restore

# Update database
dotnet ef database update

# Run the application
dotnet run
```

The web application will be available at `https://localhost:5001` or `http://localhost:5000`.

### 3. Setup AI Chat (Optional)
1. Get your API key from [Google AI Studio](https://makersuite.google.com/app/apikey)
2. Create a `.env` file in the `ChatApp/src/ChatApp.Web` directory:
```env
GOOGLE_AI_API_KEY=your_actual_api_key_here
AI_CHAT_MODEL=gemini-pro
AI_SYSTEM_PROMPT=You are a helpful AI assistant. Be concise but helpful in your responses.
```

### 4. Setup Desktop App (Optional)
```bash
cd ChatApp-Desktop

# Install dependencies
npm install

# Build the web app first (if not already running)
# Then start the desktop app
npm start
```

## 📁 Project Structure

```
ChatApp/
├── ChatApp/src/ChatApp.Web/           # Main web application
│   ├── Controllers/                   # API controllers
│   │   ├── ChatController.cs         # Chat functionality API
│   │   └── AiChatController.cs       # AI chat integration
│   ├── Data/                         # Database context and migrations
│   │   ├── ApplicationDbContext.cs   # EF Core context
│   │   └── Migrations/               # Database migrations
│   ├── Hubs/                         # SignalR hubs
│   │   └── ChatHub.cs               # Real-time communication hub
│   ├── Models/                       # Data models and view models
│   │   ├── Entities/                # Database entities
│   │   └── ViewModels/              # View models for pages
│   ├── Pages/                        # Razor pages
│   │   ├── Index.cshtml             # Main chat interface
│   │   └── Shared/                  # Shared layouts and partials
│   ├── Services/                     # Business logic services
│   ├── wwwroot/                      # Static files
│   │   ├── css/                     # Stylesheets
│   │   ├── js/                      # JavaScript files
│   │   ├── uploads/                 # File uploads
│   │   └── lib/                     # Third-party libraries
│   └── Program.cs                    # Application entry point
├── ChatApp-Desktop/                  # Electron desktop wrapper
│   ├── main.js                      # Electron main process
│   ├── package.json                 # Node.js dependencies
│   └── build.js                     # Build script
└── Documentation/                    # Project documentation
    ├── final_design_document.md     # Detailed design document
    ├── requirements_analysis.md     # Requirements analysis
    └── database_design.md           # Database schema design
```

## 🗄️ Database Schema

The application uses SQLite with the following main entities:

- **Users**: User accounts with ASP.NET Core Identity
- **Messages**: Chat messages with support for different types
- **Groups**: Chat groups with members and permissions
- **Friendships**: Friend relationships and requests
- **Stories**: Temporary user stories
- **UserStatus**: Online status and current room tracking

## 🔧 Configuration

### Application Settings
Key configuration options in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=app.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Environment Variables
Create a `.env` file for sensitive configuration:

```env
GOOGLE_AI_API_KEY=your_google_ai_studio_api_key_here
AI_CHAT_MODEL=gemini-pro
AI_SYSTEM_PROMPT=You are a helpful AI assistant.
```

## 🚀 Deployment

### Web Application
1. **Publish the application**:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Deploy to IIS/Apache/Nginx** following ASP.NET Core deployment guides

### Desktop Application
1. **Build the web application** for production
2. **Package the Electron app**:
   ```bash
   npm run build
   ```

## 🧪 Testing

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test ChatApp.Tests
```

### Manual Testing
1. Register multiple user accounts
2. Test friend requests and acceptance
3. Create group chats and invite members
4. Test file uploads and different message types
5. Verify real-time features work across multiple browser tabs
6. Test AI chat functionality (if configured)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 API Documentation

### Chat API Endpoints

#### Get Chat History
```http
GET /Chat/GetChatHistory?userId={userId}
```

#### Send Message
```http
POST /Chat/SendMessage
Content-Type: application/json

{
  "receiverId": "user-id",
  "content": "message content",
  "messageType": "Text"
}
```

#### AI Chat
```http
POST /api/AiChat
Content-Type: application/json

{
  "message": "Hello AI",
  "conversationHistory": []
}
```

### SignalR Hub Methods

#### Client to Server
- `SendMessage(message, receiverId, messageType)`
- `JoinRoom(roomName)`
- `LeaveRoom(roomName)`
- `SendTypingIndicator(receiverId, isTyping)`

#### Server to Client
- `ReceiveMessage(message, senderId, senderName)`
- `UserJoinedRoom(userId, roomName)`
- `UserLeftRoom(userId, roomName)`
- `TypingIndicator(senderId, senderName, isTyping)`
- `OnlineUsersUpdate(onlineUsers)`

## 🐛 Troubleshooting

### Common Issues

1. **Database Connection Issues**
   - Ensure SQLite database file exists
   - Run `dotnet ef database update`

2. **SignalR Connection Problems**
   - Check browser console for errors
   - Verify authentication is working
   - Check network/firewall settings

3. **AI Chat Not Working**
   - Verify API key is set correctly in `.env` file
   - Check Google AI Studio quota and billing
   - Review server logs for API errors

4. **Desktop App Issues**
   - Ensure web app is built and running
   - Check Node.js version compatibility
   - Verify Electron dependencies are installed

### Logs and Debugging
- Application logs: Check console output when running `dotnet run`
- Browser debugging: Open browser developer tools
- SignalR debugging: Enable detailed SignalR logging in `Program.cs`

## 📄 License

This project is developed for educational purposes as part of the Basic Network Programming course.

## 🙏 Acknowledgments

- ASP.NET Core and SignalR teams for excellent documentation
- Bootstrap team for the responsive UI framework
- Google AI Studio for AI integration capabilities
- Electron team for cross-platform desktop support

---

For more detailed information, see the additional documentation files at master branch:
- [Design Document](final_design_document.md) 
- [Requirements Analysis](requirements_analysis.md)
- [AI Chat Setup](ChatApp/src/ChatApp.Web/AI_CHAT_README.md)
