// This is a temporary file to store a fixed version of updateRoomMemberStatus

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
        }
    } else {
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
