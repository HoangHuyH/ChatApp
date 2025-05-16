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

        // Load friends
        loadFriends();

        // Load pending friend requests
        loadPendingFriendRequests();
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
        
        // Reload friends list to ensure up-to-date friendship status
        loadFriends();
        
        // Rejoin current room if any
        if (selectedRoom) {
            connection.invoke("JoinRoom", selectedRoom);
        }
    });

    connection.onclose(() => {
        console.log("Disconnected from the chat hub");
        updateConnectionStatus("Disconnected");
    });
    
    // Friend request related events
    connection.on("FriendRequestReceived", (request) => {
        showFriendRequestNotification(request);
        loadPendingFriendRequests(); // Reload the pending requests list
    });
    
    connection.on("FriendRequestAccepted", (friendInfo) => {
        showNotification(`${friendInfo.friendName} accepted your friend request`);
        loadFriends(); // Refresh friends list
    });
    
    connection.on("FriendRequestRejected", (friendInfo) => {
        showNotification(`${friendInfo.friendName} rejected your friend request`);
    });

    // Receive private message
    connection.on("ReceiveMessage", (message) => {
        // Only display the message if:
        // 1. We're in a private chat with the sender (selectedUserId matches sender.id), OR
        // 2. It's our own message to the currently selected user
        if ((selectedUserId === message.sender.id && !message.isOwnMessage) || 
            (message.isOwnMessage && selectedUserId)) {
            displayMessage(message);
            
            // If this message is from the currently selected user and it's not our own message
            if (selectedUserId === message.sender.id && !message.isOwnMessage) {
                markMessageAsRead(message.messageId);
            }
        } else {
            // If we're not in the right chat context, just show a notification
            if (!message.isOwnMessage) {
                const notificationText = `New message from ${message.sender.name}`;
                showNotification(notificationText);
            }
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
    });    // Handle user presence updates
    connection.on("UpdateFriendStatus", (userId, isOnline) => {
        updateFriendStatus(userId, isOnline);
    });
    
    // Handle general user status updates
    connection.on("UpdateUserStatus", (userId, isOnline) => {
        updateFriendStatus(userId, isOnline);
    });
      // NEW HANDLER: Update group member status
    connection.on("UpdateGroupMemberStatus", (user) => {
        updateGroupMemberStatus(user);
    });
    
    // NEW HANDLER: Update room member status
    connection.on("UpdateRoomMemberStatus", (user) => {
        updateRoomMemberStatus(user);
    });
    
    // NEW HANDLER: Update room users list
    connection.on("UpdateRoomUsers", (users) => {
        updateRoomUsersList(users);
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
      // Room invite event
    connection.on("RoomInviteReceived", (invite) => {
        showRoomInviteNotification(invite);
    });
    
    // User added to room event
    connection.on("UserAddedToRoom", (data) => {
        // Refresh the room list to show newly joined rooms
        loadAvailableRooms().catch(err => console.error('Error reloading rooms after user joined:', err));
    });
    
    // Handle reloading rooms list
    connection.on("ReloadRooms", () => {
        loadAvailableRooms();
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

// Load user's friends
function loadFriends() {
    fetch('/Chat/GetFriends')
        .then(response => response.json())
        .then(friends => {
           renderFriendsList(friends);
        })
        .catch(err => {
            console.error('Error loading friends: ', err);
            showErrorMessage("Error loading friends list");
        });
}

// Optimized function to render only the friends list
function renderFriendsList(friends) {
    const friendsList = document.getElementById('friends-list');
    if (!friendsList) return;
    
    // Clear previous content
    friendsList.innerHTML = '';
    
    // Check if we have any friends
    if (!friends || friends.length === 0) {
        const noFriendsElement = document.createElement('div');
        noFriendsElement.className = 'p-3 text-center text-muted';
        noFriendsElement.textContent = 'No friends yet';
        friendsList.appendChild(noFriendsElement);
        return;
    }
    
    // Render each friend
    friends.forEach(friend => {
        const friendElement = document.createElement('div');
        friendElement.classList.add('contact', 'list-group-item');
        friendElement.id = `user-${friend.userId}`;
        friendElement.onclick = () => selectUser(friend.userId, friend.displayName);
        
        const friendSince = new Date(friend.acceptedAt).toLocaleDateString();
        
        friendElement.innerHTML = `
            <div class="d-flex align-items-center">
                <div class="status-indicator ${friend.isOnline ? 'online' : 'offline'}"></div>
                <div class="ms-2 flex-grow-1">
                    <div class="contact-name">${friend.displayName}</div>
                    <div class="small text-muted">Friends since ${friendSince}</div>
                </div>
                <div class="dropdown">
                    <button class="btn btn-sm btn-link text-muted dropdown-toggle" type="button" data-bs-toggle="dropdown" aria-expanded="false">
                        <i class="bi bi-three-dots-vertical"></i>
                    </button>
                    <ul class="dropdown-menu">
                        <li><a class="dropdown-item" href="#" onclick="event.stopPropagation(); removeFriend('${friend.friendshipId}')">Remove Friend</a></li>
                    </ul>
                </div>
            </div>
        `;
        
        friendsList.appendChild(friendElement);
    });
}

// Load pending friend requests
function loadPendingFriendRequests() {
    fetch('/Chat/GetPendingFriendRequests')
        .then(response => response.json())
        .then(requests => {
            displayPendingRequests(requests);
        })
        .catch(err => {
            console.error('Error loading friend requests: ', err);
        });
}

// Display pending friend requests
function displayPendingRequests(requests) {
    const requestsList = document.getElementById('friend-requests-list');
    const requestsBadge = document.getElementById('friend-requests-badge');
    
    if (requestsList) {
        requestsList.innerHTML = '';
        
        if (requests.length === 0) {
            requestsList.innerHTML = '<div class="p-3 text-center text-muted">No pending requests</div>';
            // Hide or update badge
            if (requestsBadge) {
                requestsBadge.style.display = 'none';
            }
            return;
        }
        
        // Update badge count
        if (requestsBadge) {
            requestsBadge.textContent = requests.length;
            requestsBadge.style.display = 'inline-block';
        }
        
        requests.forEach(request => {
            const requestElement = document.createElement('div');
            requestElement.classList.add('friend-request', 'list-group-item');
            
            const requestDate = new Date(request.requestedAt).toLocaleDateString();
            
            requestElement.innerHTML = `
                <div class="d-flex flex-column">
                    <div class="mb-2">${request.requesterName} wants to be your friend</div>
                    <div class="small text-muted mb-2">Sent ${requestDate}</div>
                    <div class="d-flex justify-content-between">
                        <button class="btn btn-sm btn-success" onclick="respondToFriendRequest(${request.friendshipId}, true)">Accept</button>
                        <button class="btn btn-sm btn-danger" onclick="respondToFriendRequest(${request.friendshipId}, false)">Decline</button>
                    </div>
                </div>
            `;
            
            requestsList.appendChild(requestElement);
        });
    }
}

// Respond to a friend request (accept or reject)
function respondToFriendRequest(friendshipId, accept) {
    fetch(`/Chat/RespondToFriendRequest?friendshipId=${friendshipId}&accept=${accept}`, {
        method: 'POST'
    })
    .then(response => {
        if (response.ok) {
            // Reload both friend requests and friends list
            loadPendingFriendRequests();
            if (accept) {
                loadFriends();
                showNotification("Friend request accepted!");
            } else {
                showNotification("Friend request declined");
            }
        } else {
            response.text().then(text => showErrorMessage(text));
        }
    })
    .catch(err => {
        console.error('Error responding to friend request: ', err);
        showErrorMessage("Error processing friend request");
    });
}

// Show friend request notification - simplified without popup
function showFriendRequestNotification(request) {
    // Don't show a popup, just load the pending requests
    loadPendingFriendRequests();
    
    // Play notification sound for awareness
    const notificationSound = document.getElementById('notification-sound');
    if (notificationSound) {
        notificationSound.play().catch(error => console.log('Error playing notification sound:', error));
    }
    
    // Show a subtle notification
    showNotification(`New friend request from ${request.fromUserName}`);
}

// Show room invite notification
function showRoomInviteNotification(invite) {
    showNotification(`${invite.inviterName} invited you to join room: ${invite.roomName}`);
    
    // Create an invitation card that appears in the notification area
    const notificationArea = document.createElement('div');
    notificationArea.className = 'alert alert-info alert-dismissible fade show';
    notificationArea.role = 'alert';
    notificationArea.innerHTML = `
        <strong>Room Invitation</strong>
        <p>${invite.inviterName} invited you to join room: <strong>${invite.roomName}</strong></p>
        <div>
            <button type="button" class="btn btn-sm btn-primary me-2" onclick="acceptRoomInvite('${invite.roomName}')">Join Room</button>
            <button type="button" class="btn btn-sm btn-secondary" onclick="this.parentNode.parentNode.remove()">Dismiss</button>
        </div>
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;
    
    // Find a good place to show the notification
    const container = document.querySelector('.chat-container') || document.body;
    const notifications = document.getElementById('notifications-container') || document.createElement('div');
    
    if (!document.getElementById('notifications-container')) {
        notifications.id = 'notifications-container';
        notifications.className = 'position-fixed bottom-0 end-0 p-3';
        notifications.style.zIndex = '1050';
        container.appendChild(notifications);
    }
    
    notifications.appendChild(notificationArea);
    
    // Auto dismiss after 30 seconds
    setTimeout(() => {
        if (notificationArea.parentNode) {
            notificationArea.remove();
        }
    }, 30000);
}

// Accept room invitation
function acceptRoomInvite(roomName) {
    // Join the room via SignalR
    connection.invoke("JoinRoom", roomName)
        .then(() => {
            // First reload the room list to ensure the room is in the UI
            loadAvailableRooms()
                .then(() => {
                    // Select the room to show it in the UI after the list has been updated
                    selectRoom(roomName);
                    showNotification(`Joined room: ${roomName}`);
                })
                .catch(err => {
                    console.error('Error loading rooms after join:', err);
                    // Still try to select the room even if reload fails
                    selectRoom(roomName);
                    showNotification(`Joined room: ${roomName}`);
                });
        })
        .catch(err => {
            console.error('Error joining room:', err);
            showErrorMessage('Failed to join room');
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
    
    // Reset any existing layout classes
    const chatBody = document.querySelector('.chat-body');
    chatBody.classList.remove('with-room-info', 'with-group-info');
    chatBody.style.width = '100%';
    
    // Hide room info panel if visible
    const roomInfo = document.getElementById('room-info');
    if (roomInfo) {
        roomInfo.classList.add('d-none');
    }
    
    // Update UI to show selected group
    document.querySelectorAll('.contact').forEach(el => el.classList.remove('active'));
    document.getElementById(`group-${groupId}`).classList.add('active');
    
    document.getElementById('selected-chat-name').innerText = groupName;
    
    // Clear current messages
    document.getElementById('message-list').innerHTML = '';
    
    // Load group chat history
    loadGroupChatHistory(groupId);
    
    // Show chat area
    document.getElementById('chat-area').classList.remove('d-none');
    
    // Show chat on mobile
    document.querySelector('.chat-container').classList.add('show-chat');
    
    // Add group members button and load group members
    updateGroupHeader(groupId, groupName);
    loadGroupMembers(groupId);
}

// Select room to chat in
function selectRoom(roomName) {
    // Reset other selections
    selectedRoom = roomName;
    selectedUserId = null;
    selectedGroupId = null;
    
    // First reset any existing layout classes
    const chatBody = document.querySelector('.chat-body');
    chatBody.classList.remove('with-room-info', 'with-group-info');
    
    // Hide group info panel if visible
    const groupInfo = document.getElementById('group-info');
    if (groupInfo) {
        groupInfo.classList.add('d-none');
    }
    
    // Join the room
    connection.invoke("JoinRoom", roomName)
        .catch(err => console.error('Error joining room: ', err));
      
    // Update UI - safely
    document.querySelectorAll('.room-item').forEach(el => el.classList.remove('active'));
    const roomElement = document.querySelector(`.room-item[data-room-name="${roomName}"]`);
    if (roomElement) {
        roomElement.classList.add('active');
    }
    
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
    
    // First ensure proper initial state
    chatBody.style.width = '100%';
    
    // Then apply changes with a slight delay to allow transitions
    setTimeout(() => {
        roomInfo.classList.remove('d-none');
        chatBody.classList.add('with-room-info');
    }, 10);
    
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
    
    // Create sender name div separately for better control
    let senderHtml = '';
    if (message.sender && message.sender.name) {
        senderHtml = `<div class="message-sender">${message.sender.name}</div>`;
    }
    
    messageElement.innerHTML = `
        <div class="message-content" data-message-id="${message.messageId}">
            ${senderHtml}
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
    
    // Create sender name div separately for better control
    let senderHtml = '';
    if (message.sender && message.sender.name) {
        senderHtml = `<div class="message-sender">${message.sender.name}</div>`;
    }
    
    messageElement.innerHTML = `
        <div class="message-content" data-message-id="${message.messageId}">
            ${senderHtml}
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
    
    // Create sender name div separately for better control
    let senderHtml = '';
    if (message.sender && message.sender.name) {
        senderHtml = `<div class="message-sender">${message.sender.name}</div>`;
    }
    
    messageElement.innerHTML = `
        <div class="message-content" data-message-id="${message.messageId}">
            ${senderHtml}
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
    return new Promise((resolve, reject) => {
        fetch('/Chat/GetAllRooms')
            .then(response => response.json())
            .then(rooms => {
                const roomsList = document.getElementById('rooms-list');
                if (roomsList) {
                    roomsList.innerHTML = '';
                    
                    rooms.forEach(room => {
                        addRoomToList(room);
                    });
                    
                    // Join first room if available and no room is currently selected or the selected room is not in the list
                    if (rooms.length > 0 && document.getElementById('auto-join-room')?.value === 'true') {
                        if (!selectedRoom || !rooms.some(r => r.name === selectedRoom)) {
                           selectRoom(rooms[0].name);
                        }
                    }
                }
                resolve(rooms);
            })
            .catch(err => {
                console.error('Error loading rooms: ', err);
                reject(err);
            });
    });
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
                    <strong style="color: black;">${room.name}</strong>
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
            // Sort users by online status first, then by name
            const sortedUsers = users.sort((a, b) => {
                // First sort by online status
                if (a.isOnline && !b.isOnline) return -1;
                if (!a.isOnline && b.isOnline) return 1;
                
                // Then sort by name
                const nameA = (a.displayName || a.userName || '').toLowerCase();
                const nameB = (b.displayName || b.userName || '').toLowerCase();
                
                return nameA.localeCompare(nameB);
            });
            
            updateRoomUsersList(sortedUsers);
        })
        .catch(err => console.error('Error getting users in room: ', err));
}

// Update the room users list
function updateRoomUsersList(users) {
    const usersList = document.getElementById('room-users-list');
    if (usersList) {
        usersList.innerHTML = '';
        
        // Remove duplicated users by userId
        const uniqueUsers = users.reduce((acc, current) => {
            const x = acc.find(user => user.userId === current.userId);
            if (!x) {
                return acc.concat([current]);
            } else {
                return acc;
            }
        }, []);
        
        // Sort users by online status and then name
        const sortedUsers = uniqueUsers.sort((a, b) => {
            // Sort by online status first
            if (a.isOnline && !b.isOnline) return -1;
            if (!a.isOnline && b.isOnline) return 1;
            
            // Then by display name
            const nameA = (a.displayName || a.userName || '').toLowerCase();
            const nameB = (b.displayName || b.userName || '').toLowerCase();
            return nameA.localeCompare(nameB);
        });
        
        // Add each unique user to the list
        sortedUsers.forEach(user => {
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
        userElement.setAttribute('data-user-id', user.userId);
          // Simple online/offline status
        let statusText = user.isOnline ? 'Online' : 'Offline';
        
        userElement.innerHTML = `
            <div class="d-flex align-items-center justify-content-between">
                <div class="d-flex align-items-center">
                    <div class="status-indicator ${user.isOnline ? 'online' : 'offline'}"></div>
                    <div class="ms-2">
                        <div class="user-name">${user.displayName || user.userName}</div>
                        <div class="small text-muted">${user.device || 'Web'}</div>
                    </div>
                </div>
                <div class="small text-muted status-text">
                    ${statusText}
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

// Update room member status when they go online or offline
function updateRoomMemberStatus(user) {
    if (!selectedRoom) return;
      const userId = user.userId || user.UserId;
    const displayName = user.displayName || user.DisplayName || user.userName || user.UserName;
    const isOnline = user.isOnline !== undefined ? user.isOnline : user.IsOnline;
    
    const userElement = document.getElementById(`room-user-${userId}`);
    if (userElement) {
        // User is already in the list, update the status
        const statusIndicator = userElement.querySelector('.status-indicator');
        if (statusIndicator) {
            statusIndicator.classList.remove('online', 'offline');
            statusIndicator.classList.add(isOnline ? 'online' : 'offline');
        }
        
        // Update status text
        const statusTextElement = userElement.querySelector('.status-text');
        if (statusTextElement) {
            let statusText = isOnline ? 'Online' : 'Offline';
            statusTextElement.textContent = statusText;
        }    } else {
        // User is not in the list yet, add them
        addUserToRoomList({
            userId: userId,
            displayName: displayName,
            userName: user.userName || user.UserName,
            isOnline: isOnline,
            device: user.device || user.Device || 'Web'
        });
    }
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

// Create a new group
function createGroup() {
    const groupNameInput = document.getElementById('new-group-name');
    const groupDescInput = document.getElementById('new-group-description');
    
    const name = groupNameInput.value.trim();
    const description = groupDescInput.value.trim();
    
    if (!name) {
        showErrorMessage("Group name is required");
        return;
    }
    
    fetch('/Chat/CreateGroup', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            Name: name,
            Description: description
        })
    })
    .then(response => {
        if (response.ok) {
            return response.json();
        } else {
            return response.text().then(text => { throw new Error(text); });
        }
    })
    .then(data => {
        groupNameInput.value = '';
        groupDescInput.value = '';
        
        // Close modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('createGroupModal'));
        if (modal) modal.hide();
        
        // Refresh the groups list
        loadUserGroups();
        
        showNotification("Group created successfully");
    })
    .catch(err => {
        console.error('Error creating group:', err);
        showErrorMessage(err.message || "Failed to create group");
    });
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
    
    // Get user's display name from the contact in the friends list
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

// Update group member's online status
function updateGroupMemberStatus(user) {    // Update in room users list if present
    const roomUserElement = document.getElementById(`room-user-${user.userId}`);
    if (roomUserElement) {
        const statusIndicator = roomUserElement.querySelector('.status-indicator');
        if (statusIndicator) {
            statusIndicator.classList.remove('online', 'offline');
            statusIndicator.classList.add(user.isOnline ? 'online' : 'offline');
        }
        
        // Update status text in room users list
        const statusTextElement = roomUserElement.querySelector('.status-text');
        if (statusTextElement) {
            let statusText = user.isOnline ? 'Online' : 'Offline';
            statusTextElement.textContent = statusText;
        }
    }
    
    // If we're in a group chat with this user, make sure they're shown with correct status
    if (selectedGroupId) {
        const groupUsersList = document.getElementById('group-users-list');
        if (groupUsersList) {
            const groupUserElement = groupUsersList.querySelector(`[data-user-id="${user.userId}"]`);
            if (groupUserElement) {
                const statusIndicator = groupUserElement.querySelector('.status-indicator');
                if (statusIndicator) {
                    statusIndicator.classList.remove('online', 'offline');
                    statusIndicator.classList.add(user.isOnline ? 'online' : 'offline');
                }
                  // Update status text in group members list
                const statusTextElement = groupUserElement.querySelector('.status-text');
                if (statusTextElement) {
                    let statusText = user.isOnline ? 'Online' : 'Offline';
                    statusTextElement.textContent = statusText;
                }
            }
        }
    }
    
    // Also update in regular contacts list if present (might be same person in contacts and groups)
    updateFriendStatus(user.userId, user.isOnline);
    
    // If the user just came online, show a subtle notification
    // Only show notification if we have a valid username to display
    const userName = user.displayName || user.userName;
    if (user.isOnline && (selectedGroupId || selectedRoom) && userName && userName !== 'undefined') {
        showNotification(`${userName} is now online`);
    } else if (!user.isOnline && (selectedGroupId || selectedRoom) && userName && userName !== 'undefined') {
        showNotification(`${userName} is now offline`);
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
        // Show room info
        chatBody.style.width = '100%'; // Reset width first
        setTimeout(() => {
            roomInfo.classList.remove('d-none');
            setTimeout(() => {
                chatBody.classList.add('with-room-info');
            }, 10);
        }, 10);
    } else {
        // Hide room info
        chatBody.classList.remove('with-room-info');
        // Wait for transition to complete before hiding
        setTimeout(() => {
            roomInfo.classList.add('d-none');
            chatBody.style.width = '100%';
        }, 300);
    }
}

// Search users by display name
function searchUsers(displayName) {
    if (!displayName || displayName.trim().length < 2) {
        // Require at least 2 characters for search
        document.getElementById('search-results').innerHTML = '';
        return;
    }
    
    fetch(`/Chat/SearchUsers?displayName=${encodeURIComponent(displayName.trim())}`)
        .then(response => response.json())
        .then(users => {
            displaySearchResults(users);
        })
        .catch(err => {
            console.error('Error searching for users: ', err);
            showErrorMessage("Error searching for users");
        });
}

// Display search results
function displaySearchResults(users) {
    const searchResults = document.getElementById('search-results');
    searchResults.innerHTML = '';
    
    if (users.length === 0) {
        searchResults.innerHTML = '<div class="p-3 text-center text-muted">No users found</div>';
        return;
    }
    
    users.forEach(user => {
        const userElement = document.createElement('div');
        userElement.classList.add('search-result', 'list-group-item');
        
        // Different UI based on friendship status
        let actionButton = '';
        
        if (!user.friendshipStatus) {
            actionButton = `<button class="btn btn-sm btn-primary" onclick="sendFriendRequest('${user.id}')">Add Friend</button>`;
        } else if (user.friendshipStatus === 'Pending') {
            actionButton = `<span class="badge bg-warning">Request Pending</span>`;
        } else if (user.friendshipStatus === 'Accepted') {
            actionButton = `<span class="badge bg-success">Friend</span>`;
        }
        
        userElement.innerHTML = `
            <div class="d-flex justify-content-between align-items-center">
                <div class="user-info">
                    <div class="user-name">${user.displayName}</div>
                </div>
                <div class="action-buttons">
                    ${actionButton}
                </div>
            </div>
        `;
        
        searchResults.appendChild(userElement);
    });
}

// Send a friend request
function sendFriendRequest(friendId) {
    fetch('/Chat/SendFriendRequest', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: `friendId=${encodeURIComponent(friendId)}`
    })
    .then(response => {
        if (response.ok) {
            showNotification("Friend request sent successfully");
            // Refresh search results to update UI
            const searchInput = document.getElementById('user-search-input');
            if (searchInput.value) {
                searchUsers(searchInput.value);
            }
        } else {
            response.text().then(text => showErrorMessage(text));
        }
    })
    .catch(err => {
        console.error('Error sending friend request: ', err);
        showErrorMessage("Error sending friend request");
    });
}

// Load group members
function loadGroupMembers(groupId) {
    fetch(`/Chat/GetGroupMembers?groupId=${groupId}`)
        .then(response => response.json())
        .then(members => {
            displayGroupMembers(groupId, members);
        })
        .catch(err => {
            console.error('Error loading group members:', err);
            showErrorMessage('Failed to load group members');
        });
}

// Display group members with admin controls
function displayGroupMembers(groupId, members) {
    const membersList = document.getElementById('group-members-list');
    if (!membersList) return;
    
    // Clear previous content
    membersList.innerHTML = '';
      // First check if current user is admin to enable/disable admin controls
    const currentUserId = document.getElementById('current-user-id').value;
    const isCurrentUserAdmin = members.some(m => m.userId === currentUserId && m.role === 'Admin');
    
    // Sort members: admins first, then online users, then offline users, all alphabetically
    const sortedMembers = members.sort((a, b) => {
        // Admins first
        if (a.role === 'Admin' && b.role !== 'Admin') return -1;
        if (a.role !== 'Admin' && b.role === 'Admin') return 1;
        
        // Then online users
        if (a.isOnline && !b.isOnline) return -1;
        if (!a.isOnline && b.isOnline) return 1;
        
        // Finally sort by name
        return a.displayName.localeCompare(b.displayName);
    });
    
    sortedMembers.forEach(member => {
        const memberElement = document.createElement('div');
        memberElement.classList.add('group-member', 'list-group-item');
        memberElement.id = `group-member-${member.userId}`;
        
        // Create admin action buttons if current user is admin
        let adminActions = '';
        if (isCurrentUserAdmin && member.userId !== currentUserId) {
            adminActions = `
                <div class="dropdown ms-2">
                    <button class="btn btn-sm btn-outline-secondary dropdown-toggle" type="button" data-bs-toggle="dropdown">
                        Actions
                    </button>
                    <ul class="dropdown-menu">
                        ${member.role === 'Member' ? 
                            `<li><a class="dropdown-item" href="#" onclick="changeGroupMemberRole(${groupId}, '${member.userId}', 'Admin')">Make Admin</a></li>` : 
                            `<li><a class="dropdown-item" href="#" onclick="changeGroupMemberRole(${groupId}, '${member.userId}', 'Member')">Remove Admin</a></li>`
                        }
                        <li><a class="dropdown-item text-danger" href="#" onclick="removeUserFromGroup(${groupId}, '${member.userId}')">Remove from Group</a></li>
                    </ul>
                </div>
            `;
        }
        
        // Allow self-removal even for non-admins
        if (!isCurrentUserAdmin && member.userId === currentUserId) {
            adminActions = `
                <button class="btn btn-sm btn-outline-danger ms-2" onclick="removeUserFromGroup(${groupId}, '${member.userId}')">
                    Leave Group
                </button>
            `;
        }        // Simple online/offline status
        let statusText = member.isOnline ? 'Online' : 'Offline';
        
        memberElement.innerHTML = `
            <div class="d-flex align-items-center justify-content-between">
                <div class="d-flex align-items-center">
                    <div class="status-indicator ${member.isOnline ? 'online' : 'offline'}"></div>
                    <div class="ms-2">
                        <div class="member-name">${member.displayName}</div>
                        <div class="small text-muted d-flex align-items-center">
                            <span class="badge ${member.role === 'Admin' ? 'bg-primary' : 'bg-secondary'} me-2">${member.role}</span>
                            <span>Joined ${new Date(member.joinedAt).toLocaleDateString()}</span>
                        </div>
                    </div>
                </div>
                <div class="d-flex align-items-center">
                    <span class="small text-muted me-2 status-text">${statusText}</span>
                    ${adminActions}
                </div>
            </div>
        `;
        
        membersList.appendChild(memberElement);
    });
    
    // Add "Invite User" button for admins
    if (isCurrentUserAdmin) {
        const inviteSection = document.createElement('div');
        inviteSection.className = 'p-3 border-top';
        inviteSection.innerHTML = `
            <button class="btn btn-primary w-100" data-bs-toggle="modal" data-bs-target="#inviteToGroupModal" onclick="prepareGroupInvite(${groupId})">
                <i class="bi bi-person-plus"></i> Invite Users
            </button>
        `;
        membersList.appendChild(inviteSection);
    }
}

// Change a member's role
function changeGroupMemberRole(groupId, userId, newRole) {
    fetch('/Chat/ChangeGroupMemberRole', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: `groupId=${groupId}&userId=${userId}&role=${newRole}`
    })
    .then(response => {
        if (response.ok) {
            showNotification(`User role updated to ${newRole}`);
            loadGroupMembers(groupId); // Refresh the members list
        } else {
            response.text().then(text => showErrorMessage(text));
        }
    })
    .catch(err => {
        console.error('Error changing role:', err);
        showErrorMessage('Failed to update role');
    });
}

// Remove a user from the group
function removeUserFromGroup(groupId, userId) {
    // Confirm before removing
    if (!confirm('Are you sure you want to remove this user from the group?')) {
        return;
    }
    
    fetch('/Chat/RemoveUserFromGroup', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: `groupId=${groupId}&userId=${userId}`
    })
    .then(response => {
        if (response.ok) {
            showNotification('User removed from group');
            
            const currentUserId = document.getElementById('current-user-id').value;
            
            // If removing self, go back to the contacts view
            if (userId === currentUserId) {
                document.querySelector('.chat-container').classList.remove('show-chat');
                selectedGroupId = null;
                document.getElementById('chat-area').classList.add('d-none');
                showNotification('You have left the group');
            } else {
                loadGroupMembers(groupId); // Refresh the members list
            }
        } else {
            response.text().then(text => showErrorMessage(text));
        }
    })
    .catch(err => {
        console.error('Error removing user:', err);
        showErrorMessage('Failed to remove user');
    });
}

// Prepare group invite modal
function prepareGroupInvite(groupId) {
    // Store the groupId for use when sending invites
    document.getElementById('invite-group-id').value = groupId;
    
    // Clear previous search results
    document.getElementById('invite-search-results').innerHTML = '';
    document.getElementById('invite-search-input').value = '';
}

// Search users for group invite
function searchUsersForInvite(query) {
    if (!query || query.trim().length < 2) return;
    
    const groupId = document.getElementById('invite-group-id').value;
    
    fetch(`/Chat/SearchUsersForGroupInvite?groupId=${groupId}&query=${encodeURIComponent(query.trim())}`)
        .then(response => response.json())
        .then(users => {
            displayInviteSearchResults(users);
        })
        .catch(err => {
            console.error('Error searching users:', err);
            showErrorMessage('Failed to search users');
        });
}

// Display invite search results
function displayInviteSearchResults(users) {
    const resultsContainer = document.getElementById('invite-search-results');
    resultsContainer.innerHTML = '';
    
    if (users.length === 0) {
        resultsContainer.innerHTML = '<div class="p-3 text-center text-muted">No users found</div>';
        return;
    }
    
    users.forEach(user => {
        const userElement = document.createElement('div');
        userElement.className = 'list-group-item d-flex justify-content-between align-items-center';
        
        userElement.innerHTML = `
            <div>
                <div>${user.displayName}</div>
                <div class="small text-muted">${user.userName}</div>
            </div>
            <button class="btn btn-sm btn-primary" onclick="addUserToGroup(${document.getElementById('invite-group-id').value}, '${user.userId}')">
                Add to Group
            </button>
        `;
        
        resultsContainer.appendChild(userElement);
    });
}

// Add user to group
function addUserToGroup(groupId, userId) {
    fetch('/Chat/AddUserToGroup', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: `groupId=${groupId}&userId=${userId}`
    })
    .then(response => {
        if (response.ok) {
            showNotification('User added to group');
            loadGroupMembers(groupId);
            
            // Clear search results
            document.getElementById('invite-search-results').innerHTML = '';
            document.getElementById('invite-search-input').value = '';
            
            // Close the modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('inviteToGroupModal'));
            if (modal) modal.hide();
        } else {
            response.text().then(text => showErrorMessage(text));
        }
    })
    .catch(err => {
        console.error('Error adding user to group:', err);
        showErrorMessage('Failed to add user to group');
    });
}

// Prepare room invite modal
function prepareRoomInvite() {
    if (!selectedRoom) return;
    
    // Store the room name for use when sending invites
    document.getElementById('invite-room-name').value = selectedRoom;
    
    // Clear previous search results
    document.getElementById('room-invite-search-results').innerHTML = '';
    document.getElementById('room-invite-search-input').value = '';
}

// Search users for room invite
function searchUsersForRoomInvite(query) {
    if (!query || query.trim().length < 2) return;
    
    const roomName = document.getElementById('invite-room-name').value;
    if (!roomName) return;
    
    fetch(`/Chat/SearchUsersForRoomInvite?roomName=${encodeURIComponent(roomName)}&query=${encodeURIComponent(query.trim())}`)
        .then(response => response.json())
        .then(users => {
            displayRoomInviteSearchResults(users);
        })
        .catch(err => {
            console.error('Error searching users:', err);
            showErrorMessage('Failed to search users');
        });
}

// Display room invite search results
function displayRoomInviteSearchResults(users) {
    const resultsContainer = document.getElementById('room-invite-search-results');
    resultsContainer.innerHTML = '';
    
    if (users.length === 0) {
        resultsContainer.innerHTML = '<div class="p-3 text-center text-muted">No users found</div>';
        return;
    }
    
    users.forEach(user => {
        const userElement = document.createElement('div');
        userElement.className = 'list-group-item d-flex justify-content-between align-items-center';
        
        userElement.innerHTML = `
            <div>
                <div>${user.displayName}</div>
                <div class="small text-muted">${user.userName}</div>
            </div>
            <button class="btn btn-sm btn-primary" onclick="inviteUserToRoom('${document.getElementById('invite-room-name').value}', '${user.userId}')">
                Invite to Room
            </button>
        `;
        
        resultsContainer.appendChild(userElement);
    });
}

// Invite user to room
function inviteUserToRoom(roomName, userId) {
    fetch('/Chat/InviteUserToRoom', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: `roomName=${encodeURIComponent(roomName)}&userId=${userId}`
    })
    .then(response => {
        if (response.ok) {
            showNotification('Invitation sent');
            
            // Clear search results
            document.getElementById('room-invite-search-results').innerHTML = '';
            document.getElementById('room-invite-search-input').value = '';
            
            // Close the modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('inviteToRoomModal'));
            if (modal) modal.hide();
        } else {
            response.text().then(text => showErrorMessage(text));
        }
    })
    .catch(err => {
        console.error('Error inviting user to room:', err);
        showErrorMessage('Failed to invite user');
    });
}

// Toggle group info panel
function toggleGroupInfo() {
    const groupInfo = document.getElementById('group-info');
    const chatBody = document.querySelector('.chat-body');
    
    if (groupInfo.classList.contains('d-none')) {
        // Show group info
        chatBody.style.width = '100%'; // Reset width first
        setTimeout(() => {
            groupInfo.classList.remove('d-none');
            setTimeout(() => {
                chatBody.classList.add('with-group-info');
            }, 10);
        }, 10);
    } else {
        // Hide group info
        chatBody.classList.remove('with-group-info');
        // Wait for transition to complete before hiding
        setTimeout(() => {
            groupInfo.classList.add('d-none');
            chatBody.style.width = '100%';
        }, 300);
    }
}

// Update group header with members button
function updateGroupHeader(groupId, groupName) {
    const headerActions = document.querySelector('.chat-header-actions');
    if (headerActions) {
        headerActions.innerHTML = `
            <button class="btn btn-sm btn-outline-secondary" onclick="toggleGroupInfo()">
                <i class="bi bi-people"></i> Members
            </button>
        `;
    }
}

// Document ready handler
document.addEventListener('DOMContentLoaded', function() {
    // Initialize SignalR
    initializeSignalRConnection();
    
    // Load friends immediately without waiting for SignalR connection
    loadFriends();
    loadPendingFriendRequests();
    
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
    document.getElementById('create-group-btn')?.addEventListener('click', createGroup);
    document.getElementById('upload-btn')?.addEventListener('click', uploadFile);
    
    // Handle user search
    document.getElementById('user-search-input')?.addEventListener('input', function() {
        searchUsers(this.value);
    });
    
    // If there's a user, group or room selected on page load, handle the chat body
    if (selectedRoom) {
        document.querySelector('.chat-body')?.classList.add('with-room-info');
    }
});