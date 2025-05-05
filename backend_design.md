# Thiết kế Backend (ASP.NET Core & SignalR)

## 1. Tổng quan

Backend của ứng dụng nhắn tin sẽ được xây dựng bằng ASP.NET Core, sử dụng kiến trúc phân lớp đơn giản để tách biệt các mối quan tâm. SignalR sẽ được tích hợp để cung cấp các tính năng thời gian thực như gửi/nhận tin nhắn, cập nhật trạng thái.

## 2. Kiến trúc Tổng thể

Ứng dụng sẽ theo kiến trúc phân lớp cơ bản:

- **Presentation Layer (Lớp Trình bày):** Xử lý các yêu cầu HTTP đến và tương tác người dùng. Bao gồm ASP.NET Core Controllers (nếu dùng API/MVC) hoặc Razor Pages PageModels, và SignalR Hub.
- **Business Logic Layer (BLL - Lớp Nghiệp vụ):** Chứa logic nghiệp vụ chính của ứng dụng. Bao gồm các Services (ví dụ: `AuthService`, `ChatService`).
- **Data Access Layer (DAL - Lớp Truy cập Dữ liệu):** Chịu trách nhiệm tương tác với cơ sở dữ liệu SQLite. Sử dụng Entity Framework Core (EF Core).

```
+----------------------+
| Presentation Layer   |
| (Controllers/Pages,  |
|  SignalR Hub)        |
+----------+-----------+
           |
           v
+----------+-----------+
| Business Logic Layer |
| (Services)           |
+----------+-----------+
           |
           v
+----------+-----------+
| Data Access Layer    |
| (EF Core DbContext,  |
|  Repositories*)      |
+----------+-----------+
           |
           v
+----------+-----------+
| Database (SQLite)    |
+----------------------+
* Repository Pattern là tùy chọn, có thể dùng DbContext trực tiếp trong Services cho dự án nhỏ.
```

## 3. Data Access Layer (DAL)

- **ORM:** Entity Framework Core.
- **DbContext:** Tạo lớp `ApplicationDbContext` kế thừa từ `Microsoft.EntityFrameworkCore.DbContext`.
    - Khai báo các `DbSet<TEntity>` cho mỗi bảng trong thiết kế CSDL (`Users`, `Friendships`, `Messages`, `Groups`, `GroupMembers`, `Stories`, `UserStatus`).
    - Cấu hình kết nối đến file SQLite trong `OnConfiguring` hoặc thông qua `Startup.cs`/`Program.cs`.
    - Sử dụng Fluent API trong `OnModelCreating` để định nghĩa các mối quan hệ, khóa chính, khóa ngoại, chỉ mục, và các ràng buộc khác theo `database_design.md`.
- **Repository Pattern (Tùy chọn):** Có thể tạo các interface và implementation cho Repository (ví dụ: `IUserRepository`, `UserRepository`) để trừu tượng hóa việc truy cập dữ liệu, hoặc sử dụng DbContext trực tiếp trong các Service cho đơn giản.

## 4. Business Logic Layer (BLL) - Services

Các lớp Service sẽ chứa logic nghiệp vụ, sử dụng `ApplicationDbContext` (hoặc Repositories) để truy cập dữ liệu và có thể gọi các phương thức của SignalR Hub để gửi thông báo real-time.

- **`IAuthService` / `AuthService`:**
    - `Task<User> RegisterAsync(string username, string password, string displayName)`: Đăng ký người dùng mới (bao gồm băm mật khẩu).
    - `Task<User> LoginAsync(string username, string password)`: Xác thực đăng nhập (so sánh mật khẩu đã băm).
    - `Task<bool> RequestPasswordResetAsync(string username)`: Xử lý yêu cầu đặt lại mật khẩu (logic đơn giản cho bài tập).
    - `Task<bool> ResetPasswordAsync(string username, string token, string newPassword)`: Đặt lại mật khẩu.
- **`IUserService` / `UserService`:**
    - `Task<User> GetUserByIdAsync(int userId)`: Lấy thông tin người dùng.
    - `Task<IEnumerable<User>> SearchUsersAsync(string query)`: Tìm kiếm người dùng.
    - `Task UpdateUserStatusAsync(int userId, bool isOnline)`: Cập nhật trạng thái online/offline (có thể gọi từ Hub).
    - `Task<UserStatus> GetUserStatusAsync(int userId)`: Lấy trạng thái người dùng.
- **`IFriendshipService` / `FriendshipService`:**
    - `Task<bool> SendFriendRequestAsync(int requesterId, int receiverId)`: Gửi yêu cầu kết bạn.
    - `Task<bool> AcceptFriendRequestAsync(int friendshipId, int acceptingUserId)`: Chấp nhận yêu cầu.
    - `Task<bool> DeclineFriendRequestAsync(int friendshipId, int decliningUserId)`: Từ chối yêu cầu.
    - `Task<bool> RemoveFriendAsync(int userId, int friendId)`: Hủy kết bạn.
    - `Task<IEnumerable<Friendship>> GetPendingRequestsAsync(int userId)`: Lấy danh sách yêu cầu đang chờ.
    - `Task<IEnumerable<User>> GetFriendsAsync(int userId)`: Lấy danh sách bạn bè.
- **`IChatService` / `ChatService`:**
    - `Task<Message> SendPrivateMessageAsync(int senderId, int receiverId, string content, string messageType, string? filePath)`: Gửi tin nhắn cá nhân, lưu vào DB.
    - `Task<IEnumerable<Message>> GetPrivateChatHistoryAsync(int user1Id, int user2Id, int limit, int offset)`: Lấy lịch sử chat cá nhân.
    - `Task MarkMessageAsReadAsync(int messageId, int readerUserId)`: Đánh dấu tin nhắn đã đọc.
- **`IGroupService` / `GroupService`:**
    - `Task<Group> CreateGroupAsync(string groupName, int creatorId, IEnumerable<int> initialMemberIds)`: Tạo nhóm mới.
    - `Task<bool> AddGroupMemberAsync(int groupId, int adderUserId, int userIdToAdd)`: Thêm thành viên.
    - `Task<bool> RemoveGroupMemberAsync(int groupId, int removerUserId, int userIdToRemove)`: Xóa thành viên.
    - `Task<Message> SendGroupMessageAsync(int senderId, int groupId, string content, string messageType, string? filePath)`: Gửi tin nhắn nhóm, lưu vào DB.
    - `Task<IEnumerable<Message>> GetGroupChatHistoryAsync(int groupId, int limit, int offset)`: Lấy lịch sử chat nhóm.
    - `Task<IEnumerable<Group>> GetUserGroupsAsync(int userId)`: Lấy danh sách nhóm người dùng tham gia.
    - `Task<IEnumerable<User>> GetGroupMembersAsync(int groupId)`: Lấy danh sách thành viên nhóm.
- **`IStoryService` / `StoryService`:**
    - `Task<Story> CreateStoryAsync(int userId, string contentType, string content)`: Tạo story mới.
    - `Task<IEnumerable<Story>> GetFriendsStoriesAsync(int userId)`: Lấy story của bạn bè (chưa hết hạn).
    - `Task DeleteExpiredStoriesAsync()`: (Có thể là Background Service) Xóa các story đã hết hạn.
- **`IFileService` / `FileService`:**
    - `Task<string?> SaveFileAsync(IFormFile file, string subDirectory)`: Lưu file tải lên vào thư mục tĩnh trên server (ví dụ: `wwwroot/uploads/...`), trả về đường dẫn tương đối hoặc tuyệt đối.
    - `Task<bool> ValidateFileSize(IFormFile file, long maxSize)`: Kiểm tra kích thước file.

## 5. Presentation Layer

### 5.1. Controllers / Razor Pages

- Sử dụng ASP.NET Core Identity cho quản lý xác thực và phiên làm việc (hoặc tự xây dựng cơ chế đơn giản với cookie/JWT cho bài tập).
- Tạo các Controllers (API/MVC) hoặc PageModels (Razor Pages) tương ứng với các module chức năng:
    - `AccountController` / `AccountPages`: Xử lý đăng ký, đăng nhập, đăng xuất, quên mật khẩu.
    - `HomeController` / `IndexPage`: Trang chính sau khi đăng nhập, hiển thị danh sách bạn bè, trạng thái, story...
    - `FriendsController` / `FriendsPages`: Tìm kiếm người dùng, quản lý yêu cầu kết bạn.
    - `ChatController` / `ChatPage`: Giao diện chat cá nhân.
    - `GroupsController` / `GroupsPages`: Quản lý nhóm, giao diện chat nhóm.
    - `StoriesController` / `StoriesPages`: Đăng và xem story.
- Các action/handler method sẽ gọi các phương thức tương ứng trong lớp Services.
- Sử dụng Dependency Injection để inject các Services vào Controllers/PageModels.

### 5.2. SignalR Hub (`ChatHub`)

Kế thừa từ `Microsoft.AspNetCore.SignalR.Hub`. Hub sẽ xử lý giao tiếp real-time.

- **Quản lý Kết nối:**
    - `OnConnectedAsync()`: Khi client kết nối, lấy `UserId` từ context (ví dụ: thông qua `Context.UserIdentifier` nếu dùng Authentication), lưu map `ConnectionId` với `UserId` (có thể dùng Dictionary tĩnh hoặc cache), cập nhật trạng thái online cho user đó (`UserService.UpdateUserStatusAsync`), thông báo cho bạn bè của user đó về trạng thái online mới.
    - `OnDisconnectedAsync(Exception? exception)`: Khi client ngắt kết nối, cập nhật trạng thái offline, thông báo cho bạn bè.
- **Phương thức Client gọi Server (Server-callable methods):**
    - `Task SendPrivateMessage(int receiverUserId, string messageContent)`: Client gọi để gửi tin nhắn. Hub sẽ gọi `ChatService.SendPrivateMessageAsync`, sau đó gọi phương thức trên client của người gửi và người nhận (`ReceivePrivateMessage`).
    - `Task SendGroupMessage(int groupId, string messageContent)`: Tương tự cho tin nhắn nhóm, gửi đến tất cả client trong group SignalR tương ứng.
    - `Task NotifyTyping(int receiverUserId)`: Gửi chỉ báo đang nhập cho người nhận cụ thể.
    - `Task NotifyGroupTyping(int groupId)`: Gửi chỉ báo đang nhập cho nhóm.
    - `Task MarkAsRead(int messageId)`: Client thông báo đã đọc tin nhắn, Hub gọi `ChatService.MarkMessageAsReadAsync` và có thể thông báo lại cho người gửi.
- **Phương thức Server gọi Client (Client-callable methods):**
    - `ReceivePrivateMessage(int senderUserId, string senderDisplayName, string messageContent, DateTime sentAt, int messageId)`: Gửi tin nhắn đến client nhận.
    - `ReceiveGroupMessage(int groupId, int senderUserId, string senderDisplayName, string messageContent, DateTime sentAt, int messageId)`: Gửi tin nhắn nhóm đến các client thành viên.
    - `UpdateFriendStatus(int userId, bool isOnline)`: Cập nhật trạng thái online/offline của bạn bè trên client.
    - `ReceiveFriendRequest(int requesterId, string requesterDisplayName)`: Thông báo có yêu cầu kết bạn mới.
    - `ReceiveTypingIndicator(int senderUserId)`: Hiển thị chỉ báo đang nhập từ người dùng cụ thể.
    - `ReceiveGroupTypingIndicator(int groupId, int senderUserId)`: Hiển thị chỉ báo đang nhập trong nhóm.
    - `UpdateMessageStatus(int messageId, string status)`: Cập nhật trạng thái tin nhắn (Đã gửi, Đã xem) cho người gửi.
- **Quản lý Groups trong SignalR:**
    - Khi người dùng tham gia nhóm chat (trong ứng dụng), thêm `ConnectionId` của họ vào Group SignalR tương ứng (`Groups.AddToGroupAsync(Context.ConnectionId, $

group_{groupId}");`).
    - Khi người dùng rời nhóm hoặc bị xóa, xóa `ConnectionId` khỏi Group (`Groups.RemoveFromGroupAsync(Context.ConnectionId, $"group_{groupId}");`).

### 5.3. File Handling

- Tạo một thư mục tĩnh trong `wwwroot` (ví dụ: `wwwroot/uploads`) để lưu trữ các file được chia sẻ (hình ảnh, video, tài liệu) và ảnh story.
- Controller/PageModel xử lý việc tải lên file sẽ:
    - Nhận `IFormFile` từ request.
    - Gọi `FileService.ValidateFileSize` để kiểm tra kích thước.
    - Gọi `FileService.SaveFileAsync` để lưu file vào thư mục tĩnh, nhận lại đường dẫn.
    - Lưu đường dẫn này vào bảng `Messages` hoặc `Stories`.
- Cấu hình ASP.NET Core để phục vụ các file tĩnh từ thư mục `uploads`.

## 6. Cấu hình và Khởi tạo

- **`Program.cs` (hoặc `Startup.cs` cho .NET 5 trở về trước):**
    - Cấu hình dịch vụ (Dependency Injection):
        - Đăng ký `ApplicationDbContext` với chuỗi kết nối SQLite.
        - Đăng ký các Services (`AuthService`, `ChatService`, etc.).
        - Đăng ký ASP.NET Core Identity (nếu sử dụng).
        - Đăng ký SignalR (`services.AddSignalR();`).
        - Đăng ký các dịch vụ khác (ví dụ: `IHttpContextAccessor`).
    - Cấu hình pipeline HTTP request:
        - `UseStaticFiles()`: Để phục vụ file tĩnh (CSS, JS, và các file uploads).
        - `UseRouting()`.
        - `UseAuthentication()` và `UseAuthorization()` (nếu dùng Identity).
        - `MapRazorPages()` hoặc `MapControllers()`.
        - `MapHub<ChatHub>("/chathub")`: Map đường dẫn cho SignalR Hub.
- **`appsettings.json`:**
    - Lưu chuỗi kết nối SQLite.
    - Cấu hình giới hạn kích thước file tải lên.
    - Các cấu hình khác.

## 7. Middleware (Tùy chọn)

- Có thể tạo middleware tùy chỉnh nếu cần xử lý logic chung cho các request (ví dụ: logging, xử lý lỗi đặc biệt).

## 8. Background Services (Tùy chọn)

- Có thể tạo một `BackgroundService` (sử dụng `IHostedService`) để thực hiện các tác vụ định kỳ, ví dụ:
    - `ExpiredStoryCleanupService`: Chạy định kỳ (ví dụ: mỗi giờ) để gọi `StoryService.DeleteExpiredStoriesAsync()` xóa các story đã hết hạn khỏi cơ sở dữ liệu.

## 9. Lưu ý

- **Bảo mật:** Mặc dù yêu cầu bảo mật không cao, vẫn cần thực hiện các biện pháp cơ bản như băm mật khẩu (ASP.NET Core Identity làm điều này tự động), chống tấn công CSRF (ASP.NET Core có sẵn cơ chế), và XSS (sử dụng Razor syntax để mã hóa output).
- **Xử lý lỗi:** Implement cơ chế xử lý lỗi nhất quán, trả về thông báo lỗi phù hợp cho client.
- **Asynchronous Programming:** Sử dụng `async` và `await` một cách nhất quán trong các lớp Service và DAL để tránh chặn luồng và cải thiện khả năng đáp ứng của ứng dụng.
