// SignalR connection
let connection = null;
let selectedUserId = null;
let selectedGroupId = null;
let selectedRoom = null;
let typingTimer = null;
const currentUserId = document.getElementById('current-user-id')?.value;

// Initialize connection
function initializeSignalRConnection() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl('/chatHub')
        .withAutomaticReconnect()
        .build();

    // Start the connection
    startConnection();

    // Set up SignalR event handlers
    setupSignalREventHandlers();
}

// Start the connection with retry logic
function startConnection() {
    connection.start().then(() => {
        console.log("Connected to SignalR hub");
        updateConnectionStatus("Connected");
        
        // Load rooms and join default room if any
        loadAvailableRooms();
        
        // Load user groups
        loadUserGroups();
    }).catch(err => {
        console.error('SignalR Connection Error: ', err);
        updateConnectionStatus("Disconnected");
        setTimeout(startConnection, 5000);
    });
}

// Handle connection events and messages
function setupSignalREventHandlers() {
    // Connected/Disconnected Events
    connection.onreconnecting(() => {
        console.log("Reconnecting to the chat hub...");
        updateConnectionStatus("Reconnecting...");
    });

    connection.onreconnected(() => {
        console.log("Reconnected to the chat hub");
        updateConnectionStatus("Connected");
        
        // Rejoin current room if any
        if (selectedRoom) {
            connection.invoke("JoinRoom", selectedRoom);
        }
    });

    connection.onclose(() => {
        console.log("Disconnected from the chat hub");
        updateConnectionStatus("Disconnected");
    });
    
    // Receive private message
    connection.on("ReceiveMessage", (message) => {
        displayMessage(message);
        
        // If this message is from the currently selected user and it's not our own message
        if (selectedUserId === message.sender.id && !message.isOwnMessage) {
            markMessageAsRead(message.messageId);
        }
    });

    // Receive group message
    connection.on("ReceiveGroupMessage", (message) => {
        displayGroupMessage(message);
    });
    
    // Receive room message
    connection.on("ReceiveRoomMessage", (message) => {
        displayRoomMessage(message);
    });

    // Handle typing indicators
    connection.on("ReceiveTypingIndicator", (userId) => {
        displayTypingIndicator(userId);
    });

    connection.on("ReceiveGroupTypingIndicator", (groupId, userId) => {
        displayGroupTypingIndicator(groupId, userId);
    });
    
    connection.on("ReceiveRoomTypingIndicator", (roomName, userId) => {
        displayRoomTypingIndicator(roomName, userId);
    });

    // Handle user presence updates
    connection.on("UpdateFriendStatus", (userId, isOnline) => {
        updateFriendStatus(userId, isOnline);
    });

    connection.on("GetProfileInfo", (profile) => {
        // Store profile info
        window.currentUserProfile = profile;
        console.log("Profile info received:", profile);
    });
    
    // Handle room events
    connection.on("UserJoinedRoom", (user) => {
        addUserToRoom(user);
    });
    
    connection.on("UserLeftRoom", (userId) => {
        removeUserFromRoom(userId);
    });
    
    connection.on("NewRoomCreated", (room) => {
        addRoomToList(room);
    });

    // Handle message status updates
    connection.on("UpdateMessageStatus", (messageId, status) => {
        updateMessageStatus(messageId, status);
    });
    
    // Handle errors
    connection.on("OnError", (errorMessage) => {
        showErrorMessage(errorMessage);
    });
}

// Send private message
function sendPrivateMessage() {
    if (!selectedUserId) return;
    
    const messageInput = document.getElementById("messageInput");
    const content = messageInput.value.trim();
    
    if (content) {
        // Send via controller
        fetch('/Chat/SendMessage', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                content: content,
                receiverUserId: selectedUserId
            })
        })
        .catch(err => console.error('Error sending message: ', err));
        
        messageInput.value = '';
    }
}

// Send group message
function sendGroupMessage() {
    if (!selectedGroupId) return;
    
    const messageInput = document.getElementById("messageInput");
    const content = messageInput.value.trim();
    
    if (content) {
        // Send via controller
        fetch('/Chat/SendMessage', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                content: content,
                receiverGroupId: selectedGroupId
            })
        })
        .catch(err => console.error('Error sending group message: ', err));
        
        messageInput.value = '';
    }
}

// Send room message
function sendRoomMessage() {
    if (!selectedRoom) return;
    
    const messageInput = document.getElementById("messageInput");
    const content = messageInput.value.trim();
    
    if (content) {
        // Use SignalR directly for room messages
        connection.invoke("SendRoomMessage", selectedRoom, content)
            .catch(err => console.error('Error sending room message: ', err));
        
        messageInput.value = '';
    }
}

// Select user to chat with
function selectUser(userId, userName) {
    // Reset other selections
    selectedUserId = userId;
    selectedGroupId = null;
    
    if (selectedRoom) {
        connection.invoke("LeaveRoom", selectedRoom);
        selectedRoom = null;
    }
    
    // Update UI to show selected user
    document.querySelectorAll('.contact').forEach(el => el.classList.remove('active'));
    document.getElementById(`user-${userId}`).classList.add('active');
    
    document.getElementById('selected-chat-name').innerText = userName;
    
    // Clear current messages
    document.getElementById('message-list').innerHTML = '';
    
    // Load chat history
    loadChatHistory(userId);
    
    // Show chat area and hide room info
    document.getElementById('chat-area').classList.remove('d-none');
    const roomInfo = document.getElementById('room-info');
    roomInfo.classList.add('d-none');
    document.querySelector('.chat-body').classList.remove('with-room-info');
    
    // Show chat on mobile
    document.querySelector('.chat-container').classList.add('show-chat');
}

// Select group to chat with
function selectGroup(groupId, groupName) {
    // Reset other selections
    selectedGroupId = groupId;
    selectedUserId = null;
    
    if (selectedRoom) {
        connection.invoke("LeaveRoom", selectedRoom);
        selectedRoom = null;
    }
    
    // Update UI to show selected group
    document.querySelectorAll('.contact').forEach(el => el.classList.remove('active'));
    document.getElementById(`group-${groupId}`).classList.add('active');
    
    document.getElementById('selected-chat-name').innerText = groupName;
    
    // Clear current messages
    document.getElementById('message-list').innerHTML = '';
    
    // Load group chat history
    loadGroupChatHistory(groupId);
    
    // Show chat area and hide room info
    document.getElementById('chat-area').classList.remove('d-none');
    const roomInfo = document.getElementById('room-info');
    roomInfo.classList.add('d-none');
    document.querySelector('.chat-body').classList.remove('with-room-info');
    
    // Show chat on mobile
    document.querySelector('.chat-container').classList.add('show-chat');
}

// Select room to chat in
function selectRoom(roomName) {
    // Reset other selections
    selectedRoom = roomName;
    selectedUserId = null;
    selectedGroupId = null;
    
    // Join the room
    connection.invoke("JoinRoom", roomName)
        .catch(err => console.error('Error joining room: ', err));
    
    // Update UI
    document.querySelectorAll('.room-item').forEach(el => el.classList.remove('active'));
    document.querySelector(`.room-item[data-room-name="${roomName}"]`).classList.add('active');
    
    document.getElementById('selected-chat-name').innerText = roomName;
    
    // Clear current messages
    document.getElementById('message-list').innerHTML = '';
    
    // Load room messages
    loadRoomMessages(roomName);
    
    // Show chat area
    const chatArea = document.getElementById('chat-area');
    chatArea.classList.remove('d-none');
    
    // Show room info and adjust chat body
    const roomInfo = document.getElementById('room-info');
    const chatBody = document.querySelector('.chat-body');
    
    roomInfo.classList.remove('d-none');
    chatBody.classList.add('with-room-info');
    
    // Show chat on mobile
    document.querySelector('.chat-container').classList.add('show-chat');
    
    // Load users in room
    loadUsersInRoom(roomName);
}

// Load chat history for a user
function loadChatHistory(userId) {
    fetch(`/Chat/GetChatHistory?userId=${userId}`)
        .then(response => response.json())
        .then(messages => {
            const messageList = document.getElementById('message-list');
            messageList.innerHTML = '';
            
            messages.forEach(msg => {
                displayMessage({
                    messageId: msg.messageId,
                    content: msg.content,
                    messageType: msg.messageType,
                    sentAt: msg.sentAt,
                    sender: {
                        id: msg.senderId,
                        name: msg.senderName
                    },
                    isOwnMessage: msg.isOwnMessage,
                    status: msg.status
                });
            });
            
            scrollToBottom();
        })
        .catch(err => console.error('Error loading chat history: ', err));
}

// Load group chat history
function loadGroupChatHistory(groupId) {
    fetch(`/Chat/GetGroupChatHistory?groupId=${groupId}`)
        .then(response => response.json())
        .then(messages => {
            const messageList = document.getElementById('message-list');
            messageList.innerHTML = '';
            
            messages.forEach(msg => {
                displayGroupMessage({
                    messageId: msg.messageId,
                    groupId: msg.groupId,
                    content: msg.content,
                    messageType: msg.messageType,
                    sentAt: msg.sentAt,
                    sender: {
                        id: msg.senderId,
                        name: msg.senderName
                    },
                    isOwnMessage: msg.isOwnMessage
                });
            });
            
            scrollToBottom();
        })
        .catch(err => console.error('Error loading group chat history: ', err));
}

// Load room messages
function loadRoomMessages(roomName) {
    fetch(`/Chat/GetRoomMessages?roomName=${encodeURIComponent(roomName)}`)
        .then(response => response.json())
        .then(messages => {
            const messageList = document.getElementById('message-list');
            messageList.innerHTML = '';
            
            messages.forEach(msg => {
                displayRoomMessage({
                    messageId: msg.messageId,
                    content: msg.content,
                    messageType: msg.messageType,
                    sentAt: msg.sentAt,
                    sender: {
                        id: msg.senderId,
                        name: msg.senderName
                    },
                    isOwnMessage: msg.isOwnMessage
                });
            });
            
            scrollToBottom();
        })
        .catch(err => console.error('Error loading room messages: ', err));
}

// Display a message in the chat
function displayMessage(message) {
    const messageList = document.getElementById('message-list');
    const messageElement = document.createElement('div');
    
    messageElement.classList.add('message');
    if (message.isOwnMessage) {
        messageElement.classList.add('message-out');
    } else {
        messageElement.classList.add('message-in');
    }
    
    const date = new Date(message.sentAt);
    const timeString = date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    
    let content = message.content;
    
    // Handle different message types
    if (message.messageType === 'Image') {
        // Already contains HTML
    } else if (message.messageType === 'File') {
        // Already contains HTML
    }
    
    messageElement.innerHTML = `
        <div class="message-content" data-message-id="${message.messageId}">
            ${content}
            <div class="message-info">
                <span class="message-time">${timeString}</span>
                ${message.isOwnMessage ? 
                    `<span class="message-status" data-status="${message.status || 'Sent'}">
                        ${getStatusIcon(message.status || 'Sent')}
                    </span>` : ''}
            </div>
        </div>
    `;
    
    messageList.appendChild(messageElement);
    scrollToBottom();
}

// Display a group message in the chat
function displayGroupMessage(message) {
    const messageList = document.getElementById('message-list');
    const messageElement = document.createElement('div');
    
    messageElement.classList.add('message');
    if (message.isOwnMessage) {
        messageElement.classList.add('message-out');
    } else {
        messageElement.classList.add('message-in');
    }
    
    const date = new Date(message.sentAt);
    const timeString = date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    
    let content = message.content;
    
    // Handle different message types
    if (message.messageType === 'Image') {
        // Already contains HTML
    } else if (message.messageType === 'File') {
        // Already contains HTML
    }
    
    messageElement.innerHTML = `
        <div class="message-content" data-message-id="${message.messageId}">
            ${!message.isOwnMessage ? `<div class="message-sender">${message.sender.name}</div>` : ''}
            ${content}
            <div class="message-info">
                <span class="message-time">${timeString}</span>
            </div>
        </div>
    `;
    
    messageList.appendChild(messageElement);
    scrollToBottom();
}

// Display a room message in the chat
function displayRoomMessage(message) {
    const messageList = document.getElementById('message-list');
    const messageElement = document.createElement('div');
    
    messageElement.classList.add('message');
    if (message.isOwnMessage) {
        messageElement.classList.add('message-out');
    } else {
        messageElement.classList.add('message-in');
    }
    
    const date = new Date(message.sentAt);
    const timeString = date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    
    let content = message.content;
    
    messageElement.innerHTML = `
        <div class="message-content" data-message-id="${message.messageId}">
            ${!message.isOwnMessage ? `<div class="message-sender">${message.sender.name}</div>` : ''}
            ${content}
            <div class="message-info">
                <span class="message-time">${timeString}</span>
            </div>
        </div>
    `;
    
    messageList.appendChild(messageElement);
    scrollToBottom();
}

// Load available rooms
function loadAvailableRooms() {
    fetch('/Chat/GetAllRooms')
        .then(response => response.json())
        .then(rooms => {
            const roomsList = document.getElementById('rooms-list');
            if (roomsList) {
                roomsList.innerHTML = '';
                
                rooms.forEach(room => {
                    addRoomToList(room);
                });
                
                // Join first room if available
                if (rooms.length > 0 && document.getElementById('auto-join-room')?.value === 'true') {
                    selectRoom(rooms[0].name);
                }
            }
        })
        .catch(err => console.error('Error loading rooms: ', err));
}

// Load user groups
function loadUserGroups() {
    fetch('/Chat/GetUserGroups')
        .then(response => response.json())
        .then(groups => {
            const groupsList = document.getElementById('groups-list');
            if (groupsList) {
                groupsList.innerHTML = '';
                
                groups.forEach(group => {
                    const groupElement = document.createElement('div');
                    groupElement.classList.add('contact', 'list-group-item');
                    groupElement.id = `group-${group.id}`;
                    groupElement.onclick = () => selectGroup(group.id, group.name);
                    
                    groupElement.innerHTML = `
                        <div class="d-flex justify-content-between align-items-center">
                            <div>
                                <div class="contact-name">${group.name}</div>
                                <div class="small text-muted">${group.description || 'No description'}</div>
                            </div>
                            ${group.unreadCount > 0 ? 
                                `<span class="badge bg-primary rounded-pill">${group.unreadCount}</span>` : ''}
                        </div>
                    `;
                    
                    groupsList.appendChild(groupElement);
                });
            }
        })
        .catch(err => console.error('Error loading user groups: ', err));
}

// Add room to the list
function addRoomToList(room) {
    const roomsList = document.getElementById('rooms-list');
    if (roomsList) {
        // Check if room already exists
        if (document.querySelector(`.room-item[data-room-id="${room.id}"]`)) {
            return;
        }
        
        const roomElement = document.createElement('div');
        roomElement.classList.add('room-item', 'list-group-item', 'list-group-item-action');
        roomElement.setAttribute('data-room-id', room.id);
        roomElement.setAttribute('data-room-name', room.name);
        roomElement.onclick = () => selectRoom(room.name);
        
        roomElement.innerHTML = `
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <strong>${room.name}</strong>
                    ${room.description ? `<div class="small text-muted">${room.description}</div>` : ''}
                </div>
                <span class="badge bg-secondary">${room.memberCount || 0}</span>
            </div>
        `;
        
        roomsList.appendChild(roomElement);
    }
}

// Load users in room
function loadUsersInRoom(roomName) {
    // Use the SignalR Hub method
    connection.invoke("GetUsersInRoom", roomName)
        .then(users => {
            updateRoomUsersList(users);
        })
        .catch(err => console.error('Error getting users in room: ', err));
}

// Update the room users list
function updateRoomUsersList(users) {
    const usersList = document.getElementById('room-users-list');
    if (usersList) {
        usersList.innerHTML = '';
        
        users.forEach(user => {
            addUserToRoomList(user);
        });
    }
}

// Add user to room list
function addUserToRoomList(user) {
    const usersList = document.getElementById('room-users-list');
    if (usersList) {
        const userElement = document.createElement('div');
        userElement.classList.add('room-user', 'list-group-item');
        userElement.id = `room-user-${user.userId}`;
        
        userElement.innerHTML = `
            <div class="d-flex align-items-center">
                <div class="status-indicator ${user.isOnline ? 'online' : 'offline'}"></div>
                <div class="ms-2">
                    <div class="user-name">${user.displayName || user.userName}</div>
                    <div class="small text-muted">${user.device || 'Web'}</div>
                </div>
            </div>
        `;
        
        usersList.appendChild(userElement);
    }
}

// Add user to room
function addUserToRoom(user) {
    if (!selectedRoom || selectedRoom !== user.currentRoom) return;
    
    addUserToRoomList(user);
    
    // Show notification
    showNotification(`${user.displayName || user.userName} joined the room`);
}

// Remove user from room
function removeUserFromRoom(userId) {
    const userElement = document.getElementById(`room-user-${userId}`);
    if (userElement) {
        const userName = userElement.querySelector('.user-name').textContent;
        userElement.remove();
        
        // Show notification
        showNotification(`${userName} left the room`);
    }
}

// Create a new room
function createRoom() {
    const roomNameInput = document.getElementById('new-room-name');
    const roomDescInput = document.getElementById('new-room-description');
    
    const name = roomNameInput.value.trim();
    const description = roomDescInput.value.trim();
    
    if (!name) {
        showErrorMessage("Room name is required");
        return;
    }
    
    fetch('/Chat/CreateRoom', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            name: name,
            description: description
        })
    })
    .then(response => {
        if (response.ok) {
            roomNameInput.value = '';
            roomDescInput.value = '';
            
            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('createRoomModal'));
            if (modal) modal.hide();
            
            // Room will be added via SignalR event
        } else {
            response.text().then(text => showErrorMessage(text));
        }
    })
    .catch(err => console.error('Error creating room: ', err));
}

// Upload file
function uploadFile() {
    const fileInput = document.getElementById('file-upload');
    if (!fileInput.files || fileInput.files.length === 0) {
        showErrorMessage("Please select a file to upload");
        return;
    }
    
    const formData = new FormData();
    formData.append('file', fileInput.files[0]);
    
    // Add receiver info
    if (selectedUserId) {
        formData.append('receiverUserId', selectedUserId);
    } else if (selectedGroupId) {
        formData.append('receiverGroupId', selectedGroupId);
    } else if (selectedRoom) {
        formData.append('roomName', selectedRoom);
    } else {
        showErrorMessage("Please select a chat before uploading a file");
        return;
    }
    
    // Show upload progress
    const uploadButton = document.getElementById('upload-btn');
    const originalText = uploadButton.innerHTML;
    uploadButton.disabled = true;
    uploadButton.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Uploading...';
    
    fetch('/Chat/UploadFile', {
        method: 'POST',
        body: formData
    })
    .then(response => response.json())
    .then(result => {
        // Clear file input
        fileInput.value = '';
        
        // Reset button
        uploadButton.disabled = false;
        uploadButton.innerHTML = originalText;
        
        // Close modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('uploadFileModal'));
        if (modal) modal.hide();
    })
    .catch(err => {
        console.error('Error uploading file: ', err);
        
        // Reset button
        uploadButton.disabled = false;
        uploadButton.innerHTML = originalText;
        
        showErrorMessage("Error uploading file");
    });
}

// Mark a message as read
function markMessageAsRead(messageId) {
    connection.invoke("MarkMessageAsRead", messageId)
        .catch(err => console.error('Error marking message as read: ', err));
}

// Display typing indicator
function displayTypingIndicator(userId) {
    if (userId !== selectedUserId) return;
    
    const typingIndicator = document.getElementById('typing-indicator');
    typingIndicator.classList.remove('d-none');
    
    // Hide typing indicator after 3 seconds
    clearTimeout(typingTimer);
    typingTimer = setTimeout(() => {
        typingIndicator.classList.add('d-none');
    }, 3000);
}

// Display group typing indicator
function displayGroupTypingIndicator(groupId, userId) {
    if (groupId !== selectedGroupId || userId === currentUserId) return;
    
    // Get user name
    const userName = document.querySelector(`#user-${userId} .contact-name`)?.textContent || 'Someone';
    
    const typingIndicator = document.getElementById('typing-indicator');
    typingIndicator.textContent = `${userName} is typing...`;
    typingIndicator.classList.remove('d-none');
    
    // Hide typing indicator after 3 seconds
    clearTimeout(typingTimer);
    typingTimer = setTimeout(() => {
        typingIndicator.classList.add('d-none');
    }, 3000);
}

// Display room typing indicator
function displayRoomTypingIndicator(roomName, userId) {
    if (roomName !== selectedRoom || userId === currentUserId) return;
    
    // Get user name
    const userName = document.querySelector(`#room-user-${userId} .user-name`)?.textContent || 'Someone';
    
    const typingIndicator = document.getElementById('typing-indicator');
    typingIndicator.textContent = `${userName} is typing...`;
    typingIndicator.classList.remove('d-none');
    
    // Hide typing indicator after 3 seconds
    clearTimeout(typingTimer);
    typingTimer = setTimeout(() => {
        typingIndicator.classList.add('d-none');
    }, 3000);
}

// Update friend's online status
function updateFriendStatus(userId, isOnline) {
    const statusIndicator = document.querySelector(`#user-${userId} .status-indicator`);
    if (statusIndicator) {
        if (isOnline) {
            statusIndicator.classList.remove('offline');
            statusIndicator.classList.add('online');
        } else {
            statusIndicator.classList.remove('online');
            statusIndicator.classList.add('offline');
        }
    }
}

// Update message status
function updateMessageStatus(messageId, status) {
    const statusElement = document.querySelector(`.message-content[data-message-id="${messageId}"] .message-status`);
    if (statusElement) {
        statusElement.setAttribute('data-status', status);
        statusElement.innerHTML = getStatusIcon(status);
    }
}

// Get icon for message status
function getStatusIcon(status) {
    switch(status) {
        case 'Sent':
            return '<i class="bi bi-check"></i>';
        case 'Delivered':
            return '<i class="bi bi-check2"></i>';
        case 'Read':
            return '<i class="bi bi-check2-all"></i>';
        default:
            return '<i class="bi bi-clock"></i>';
    }
}

// Update connection status in UI
function updateConnectionStatus(status) {
    const statusElement = document.getElementById('connection-status');
    if (statusElement) {
        statusElement.textContent = status;
        
        if (status === 'Connected') {
            statusElement.classList.remove('text-danger', 'text-warning');
            statusElement.classList.add('text-success');
        } else if (status === 'Reconnecting...') {
            statusElement.classList.remove('text-danger', 'text-success');
            statusElement.classList.add('text-warning');
        } else {
            statusElement.classList.remove('text-success', 'text-warning');
            statusElement.classList.add('text-danger');
        }
    }
}

// Show error message
function showErrorMessage(message) {
    const errorAlert = document.getElementById('error-alert');
    if (errorAlert) {
        errorAlert.textContent = message;
        errorAlert.classList.remove('d-none');
        
        setTimeout(() => {
            errorAlert.classList.add('d-none');
        }, 5000);
    } else {
        console.error(message);
    }
}

// Show notification
function showNotification(message) {
    const notification = document.getElementById('notification');
    if (notification) {
        notification.textContent = message;
        notification.classList.remove('d-none');
        
        setTimeout(() => {
            notification.classList.add('d-none');
        }, 3000);
    }
}

// Handle typing notification
function handleTyping() {
    if (selectedUserId) {
        connection.invoke("NotifyTyping", selectedUserId)
            .catch(err => console.error('Error sending typing notification: ', err));
    } else if (selectedGroupId) {
        connection.invoke("NotifyGroupTyping", selectedGroupId)
            .catch(err => console.error('Error sending group typing notification: ', err));
    } else if (selectedRoom) {
        connection.invoke("NotifyRoomTyping", selectedRoom)
            .catch(err => console.error('Error sending room typing notification: ', err));
    }
}

// Scroll chat to bottom
function scrollToBottom() {
    const messageList = document.getElementById('message-list');
    messageList.scrollTop = messageList.scrollHeight;
}

// Handle resize events for responsive layout
function handleResize() {
    const width = window.innerWidth;
    const chatContainer = document.querySelector('.chat-container');
    const roomInfo = document.getElementById('room-info');
    const chatBody = document.querySelector('.chat-body');
    
    if (width >= 768) {
        // Reset mobile-specific classes on desktop
        chatContainer.classList.remove('show-chat');
        
        // Make sure chat area is visible if a conversation is selected
        if (selectedUserId || selectedGroupId || selectedRoom) {
            document.getElementById('chat-area').classList.remove('d-none');
        }
        
        // Handle room info visibility
        if (selectedRoom && !roomInfo.classList.contains('d-none')) {
            chatBody.classList.add('with-room-info');
        } else {
            chatBody.classList.remove('with-room-info');
        }
    }
}

// Handle room info toggle
function toggleRoomInfo() {
    const roomInfo = document.getElementById('room-info');
    const chatBody = document.querySelector('.chat-body');
    
    if (roomInfo.classList.contains('d-none')) {
        roomInfo.classList.remove('d-none');
        chatBody.classList.add('with-room-info');
    } else {
        roomInfo.classList.add('d-none');
        chatBody.classList.remove('with-room-info');
    }
}

// Document ready handler
document.addEventListener('DOMContentLoaded', function() {
    // Initialize SignalR
    initializeSignalRConnection();
    
    // Set up event handlers
    document.getElementById('send-button')?.addEventListener('click', function() {
        if (selectedUserId) {
            sendPrivateMessage();
        } else if (selectedGroupId) {
            sendGroupMessage();
        } else if (selectedRoom) {
            sendRoomMessage();
        }
    });
    
    document.getElementById('messageInput')?.addEventListener('keypress', function(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            if (selectedUserId) {
                sendPrivateMessage();
            } else if (selectedGroupId) {
                sendGroupMessage();
            } else if (selectedRoom) {
                sendRoomMessage();
            }
        } else {
            handleTyping();
        }
    });
    
    // Handle back button for mobile view
    document.querySelector('.back-button')?.addEventListener('click', function() {
        document.querySelector('.chat-container').classList.remove('show-chat');
    });
    
    // Handle room info toggle
    document.querySelector('.room-info .btn-close')?.addEventListener('click', function() {
        toggleRoomInfo();
    });
    
    // Responsive handling for window resize
    window.addEventListener('resize', handleResize);
    
    document.getElementById('create-room-btn')?.addEventListener('click', createRoom);
    document.getElementById('upload-btn')?.addEventListener('click', uploadFile);
    
    // If there's a user, group or room selected on page load, handle the chat body
    if (selectedRoom) {
        document.querySelector('.chat-body')?.classList.add('with-room-info');
    }
});