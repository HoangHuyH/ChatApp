# Tài liệu Thiết kế Chi tiết - Ứng dụng Nhắn tin Web (ASP.NET Core, SQLite, SignalR)

## Mục lục

1.  [Phân tích Yêu cầu Dự án](#1-phân-tích-yêu-cầu-dự-án)
2.  [Thiết kế Cơ sở dữ liệu (SQLite)](#2-thiết-kế-cơ-sở-dữ-liệu-sqlite)
3.  [Thiết kế Backend (ASP.NET Core & SignalR)](#3-thiết-kế-backend-aspnet-core--signalr)
4.  [Thiết kế Frontend (ASP.NET Core Razor Pages & SignalR)](#4-thiết-kế-frontend-aspnet-core-razor-pages--signalr)
5.  [Cấu trúc Dự án Chi tiết](#5-cấu-trúc-dự-án-chi-tiết)
6.  [Ghi chú Triển khai Dự án](#6-ghi-chú-triển-khai-dự-án)

---



# 1. Phân tích Yêu cầu Dự án

## 1. Tổng quan

Dự án này nhằm mục đích xây dựng một ứng dụng nhắn tin web đơn giản cho môn Lập trình Mạng căn bản. Ứng dụng sẽ được phát triển bằng C# với ASP.NET Core, sử dụng SQLite làm cơ sở dữ liệu và SignalR để hỗ trợ giao tiếp thời gian thực.

## 2. Công nghệ sử dụng

- **Ngôn ngữ lập trình:** C#
- **Framework Backend:** ASP.NET Core
- **Framework Frontend:** ASP.NET Core Razor Pages (hoặc MVC)
- **Cơ sở dữ liệu:** SQLite
- **Giao tiếp Real-time:** SignalR
- **Môi trường phát triển:** Visual Studio 2022

## 3. Yêu cầu Chức năng Chi tiết

Dựa trên thông tin từ người dùng và hình ảnh cung cấp, các chức năng chính của ứng dụng bao gồm:

### 3.1. Module Xác thực Người dùng
- **Đăng ký:** Cho phép người dùng mới tạo tài khoản với tên đăng nhập, mật khẩu, và có thể là thông tin cá nhân cơ bản (tên hiển thị).
- **Đăng nhập:** Xác thực người dùng hiện tại bằng tên đăng nhập và mật khẩu.
- **Quên mật khẩu/Đặt lại mật khẩu:** Cung cấp cơ chế đơn giản để người dùng khôi phục hoặc đặt lại mật khẩu (ví dụ: qua email - mặc dù email có thể phức tạp, có thể thay bằng câu hỏi bí mật hoặc cơ chế đơn giản hơn cho bài tập).

### 3.2. Module Quản lý Danh bạ
- **Tìm kiếm người dùng:** Cho phép người dùng tìm kiếm người dùng khác trong hệ thống bằng tên đăng nhập hoặc tên hiển thị.
- **Gửi yêu cầu kết bạn:** Người dùng có thể gửi lời mời kết bạn đến người dùng khác.
- **Quản lý yêu cầu kết bạn:** Người dùng có thể xem danh sách yêu cầu kết bạn đã nhận và chấp nhận hoặc từ chối.
- **Hủy yêu cầu kết bạn:** Người dùng có thể hủy yêu cầu kết bạn đã gửi.
- **Hiển thị danh sách bạn bè:** Hiển thị danh sách những người dùng đã kết bạn.
- **Hiển thị trạng thái online/offline:** Hiển thị trạng thái hoạt động (online/offline) của bạn bè trong danh sách.

### 3.3. Module Nhắn tin Cá nhân
- **Gửi/Nhận tin nhắn văn bản:** Cho phép hai người dùng đã kết bạn gửi và nhận tin nhắn văn bản cho nhau theo thời gian thực.
- **Hiển thị lịch sử trò chuyện:** Lưu trữ và hiển thị lịch sử tin nhắn giữa hai người dùng.
- **Hiển thị trạng thái tin nhắn:** Hiển thị trạng thái của tin nhắn đã gửi (ví dụ: "Đã gửi", "Đã xem").
- **Chỉ báo "Đang nhập...":** Hiển thị thông báo khi đối phương đang soạn tin nhắn.
- **Gửi/Nhận tin nhắn thoại:** (Tính năng nâng cao) Cho phép người dùng ghi âm và gửi tin nhắn thoại ngắn. Nhận và phát lại tin nhắn thoại.

### 3.4. Module Nhắn tin Nhóm
- **Tạo nhóm:** Người dùng có thể tạo một cuộc trò chuyện nhóm mới.
- **Đặt tên nhóm:** Cho phép đặt tên cho nhóm chat.
- **Thêm/Xóa thành viên:** Người dùng tạo nhóm (quản trị viên) có thể thêm hoặc xóa thành viên khỏi nhóm.
- **Phân quyền quản trị viên:** (Đơn giản) Người tạo nhóm là quản trị viên, có thể có thêm quyền quản lý thành viên.
- **Gửi/Nhận tin nhắn trong nhóm:** Tất cả thành viên trong nhóm có thể gửi và nhận tin nhắn văn bản.

### 3.5. Module Chia sẻ File & Phương tiện
- **Gửi file:** Cho phép người dùng gửi các loại file (hình ảnh, video, tài liệu) trong cuộc trò chuyện cá nhân và nhóm.
- **Giới hạn kích thước file:** Áp dụng giới hạn về kích thước tối đa cho mỗi file tải lên.
- **Hiển thị/Tải file:** Hiển thị hình ảnh/video thu nhỏ và cho phép tải về các file đính kèm.

### 3.6. Module Thông báo & Trạng thái
- **Thông báo tin nhắn mới:** Thông báo cho người dùng (có thể là thông báo trên giao diện web) khi có tin nhắn mới.
- **Thông báo yêu cầu kết bạn:** Thông báo khi có yêu cầu kết bạn mới.
- **Cập nhật trạng thái online/offline:** Tự động cập nhật và hiển thị trạng thái online/offline của người dùng khi họ đăng nhập/đăng xuất hoặc mất kết nối.

### 3.7. Module Gọi thoại/Video (Tính năng nâng cao)
- **Thực hiện cuộc gọi:** Cho phép người dùng bắt đầu cuộc gọi thoại hoặc video với một người bạn trong danh sách.
- **Nhận/Từ chối cuộc gọi:** Người nhận có thể chấp nhận hoặc từ chối cuộc gọi đến.
- **Lưu ý:** Tính năng này phức tạp, thường yêu cầu WebRTC. Đối với bài tập cơ bản, có thể chỉ cần mô phỏng giao diện hoặc bỏ qua.

### 3.8. Module Đăng/Xem Story
- **Đăng Story:** Người dùng có thể đăng nội dung (văn bản, hình ảnh) làm story, hiển thị cho bạn bè.
- **Tự động xóa Story:** Story sẽ tự động biến mất sau 24 giờ.
- **Xem Story của bạn bè:** Người dùng có thể xem danh sách story từ bạn bè của họ.

## 4. Yêu cầu Phi chức năng

- **Giao diện người dùng:** Đơn giản, dễ sử dụng, phù hợp với ứng dụng web.
- **Hiệu năng:** Phản hồi nhanh chóng đối với các tương tác cơ bản và tin nhắn thời gian thực (trong phạm vi bài tập).
- **Khả năng mở rộng:** Thiết kế cơ bản, không yêu cầu cao về khả năng mở rộng quy mô lớn.
- **Bảo mật:** Mức độ cơ bản, chủ yếu tập trung vào xác thực người dùng và quản lý phiên làm việc. Không yêu cầu mã hóa đầu cuối hoặc các biện pháp bảo mật phức tạp.
- **Cơ sở dữ liệu:** Sử dụng SQLite, phù hợp cho ứng dụng nhỏ và đơn giản, dễ dàng triển khai.

---



# 2. Thiết kế Cơ sở dữ liệu (SQLite)

## 1. Tổng quan

Cơ sở dữ liệu được thiết kế sử dụng SQLite để lưu trữ dữ liệu cho ứng dụng nhắn tin. Thiết kế này bao gồm các bảng cần thiết để quản lý người dùng, bạn bè, tin nhắn, nhóm chat, và các tính năng khác theo yêu cầu.

## 2. Sơ đồ Quan hệ Thực thể (ERD - Mô tả)

- **Users:** Lưu trữ thông tin tài khoản người dùng.
- **Friendships:** Quản lý mối quan hệ bạn bè và yêu cầu kết bạn giữa hai người dùng.
- **Messages:** Lưu trữ nội dung tin nhắn cá nhân và tin nhắn nhóm.
- **Groups:** Lưu trữ thông tin về các nhóm chat.
- **GroupMembers:** Quản lý thành viên và vai trò trong các nhóm chat.
- **Stories:** Lưu trữ các story do người dùng đăng tải.
- **UserStatus:** (Tùy chọn) Theo dõi trạng thái online/offline của người dùng.

## 3. Chi tiết các Bảng

### 3.1. Bảng `Users`

Lưu trữ thông tin cơ bản của người dùng.

- `UserId` (INTEGER, PRIMARY KEY, AUTOINCREMENT): Khóa chính, định danh duy nhất cho mỗi người dùng.
- `Username` (TEXT, UNIQUE, NOT NULL): Tên đăng nhập, không được trùng.
- `PasswordHash` (TEXT, NOT NULL): Chuỗi băm của mật khẩu người dùng (lưu ý: cần sử dụng thuật toán băm an toàn).
- `DisplayName` (TEXT): Tên hiển thị của người dùng.
- `CreatedAt` (TEXT, NOT NULL): Thời gian tạo tài khoản (định dạng ISO8601: 'YYYY-MM-DD HH:MM:SS.SSS').

### 3.2. Bảng `Friendships`

Quản lý mối quan hệ bạn bè.

- `FriendshipId` (INTEGER, PRIMARY KEY, AUTOINCREMENT): Khóa chính.
- `User1Id` (INTEGER, NOT NULL): ID của người dùng gửi yêu cầu hoặc người dùng có ID nhỏ hơn trong cặp bạn bè.
- `User2Id` (INTEGER, NOT NULL): ID của người dùng nhận yêu cầu hoặc người dùng có ID lớn hơn.
- `Status` (TEXT, NOT NULL): Trạng thái mối quan hệ ('Pending', 'Accepted', 'Declined', 'Blocked').
    - `Pending`: User1 đã gửi yêu cầu cho User2.
    - `Accepted`: Yêu cầu đã được chấp nhận, hai người là bạn bè.
    - `Declined`: Yêu cầu bị từ chối.
    - `Blocked`: Một trong hai người chặn người kia (có thể cần thêm cột để chỉ rõ ai chặn).
- `RequestedAt` (TEXT, NOT NULL): Thời gian gửi yêu cầu kết bạn.
- `AcceptedAt` (TEXT): Thời gian chấp nhận yêu cầu (NULL nếu chưa chấp nhận).
- **Constraints:**
    - `FOREIGN KEY(User1Id) REFERENCES Users(UserId)`
    - `FOREIGN KEY(User2Id) REFERENCES Users(UserId)`
    - `UNIQUE(User1Id, User2Id)`: Đảm bảo không có cặp trùng lặp.
    - `CHECK(Status IN ('Pending', 'Accepted', 'Declined', 'Blocked'))`
    - `CHECK(User1Id < User2Id)`: Quy ước để tránh trùng lặp (UserA, UserB) và (UserB, UserA) cho mối quan hệ `Accepted`. Cần xử lý logic khi tạo/truy vấn.

### 3.3. Bảng `Messages`

Lưu trữ tin nhắn cá nhân và nhóm.

- `MessageId` (INTEGER, PRIMARY KEY, AUTOINCREMENT): Khóa chính.
- `SenderId` (INTEGER, NOT NULL): ID của người gửi tin nhắn.
- `ReceiverUserId` (INTEGER, NULL): ID của người nhận (nếu là tin nhắn cá nhân).
- `ReceiverGroupId` (INTEGER, NULL): ID của nhóm nhận (nếu là tin nhắn nhóm).
- `Content` (TEXT, NOT NULL): Nội dung tin nhắn (văn bản) hoặc mô tả/tham chiếu đến file.
- `MessageType` (TEXT, NOT NULL): Loại tin nhắn ('Text', 'Image', 'Video', 'File', 'Voice').
- `FilePath` (TEXT, NULL): Đường dẫn đến file đính kèm (nếu MessageType không phải 'Text'). Lưu ý: Việc lưu file trực tiếp vào DB không được khuyến khích; nên lưu trên hệ thống file và chỉ lưu đường dẫn.
- `SentAt` (TEXT, NOT NULL): Thời gian gửi tin nhắn.
- `Status` (TEXT, NOT NULL, DEFAULT 'Sent'): Trạng thái tin nhắn ('Sent', 'Delivered', 'Read'). Cập nhật trạng thái này cần logic phía backend và client.
- **Constraints:**
    - `FOREIGN KEY(SenderId) REFERENCES Users(UserId)`
    - `FOREIGN KEY(ReceiverUserId) REFERENCES Users(UserId)`
    - `FOREIGN KEY(ReceiverGroupId) REFERENCES Groups(GroupId)`
    - `CHECK(ReceiverUserId IS NOT NULL OR ReceiverGroupId IS NOT NULL)`: Đảm bảo tin nhắn có người nhận hoặc nhóm nhận.
    - `CHECK(MessageType IN ('Text', 'Image', 'Video', 'File', 'Voice'))`
    - `CHECK(Status IN ('Sent', 'Delivered', 'Read'))`

### 3.4. Bảng `Groups`

Lưu trữ thông tin các nhóm chat.

- `GroupId` (INTEGER, PRIMARY KEY, AUTOINCREMENT): Khóa chính.
- `GroupName` (TEXT, NOT NULL): Tên của nhóm chat.
- `CreatorId` (INTEGER, NOT NULL): ID của người tạo nhóm.
- `CreatedAt` (TEXT, NOT NULL): Thời gian tạo nhóm.
- **Constraints:**
    - `FOREIGN KEY(CreatorId) REFERENCES Users(UserId)`

### 3.5. Bảng `GroupMembers`

Quản lý thành viên trong các nhóm.

- `GroupMemberId` (INTEGER, PRIMARY KEY, AUTOINCREMENT): Khóa chính.
- `GroupId` (INTEGER, NOT NULL): ID của nhóm.
- `UserId` (INTEGER, NOT NULL): ID của thành viên.
- `Role` (TEXT, NOT NULL, DEFAULT 'Member'): Vai trò của thành viên trong nhóm ('Admin', 'Member').
- `JoinedAt` (TEXT, NOT NULL): Thời gian tham gia nhóm.
- **Constraints:**
    - `FOREIGN KEY(GroupId) REFERENCES Groups(GroupId)`
    - `FOREIGN KEY(UserId) REFERENCES Users(UserId)`
    - `UNIQUE(GroupId, UserId)`: Mỗi người dùng chỉ tham gia một nhóm một lần.
    - `CHECK(Role IN ('Admin', 'Member'))`

### 3.6. Bảng `Stories`

Lưu trữ thông tin các story.

- `StoryId` (INTEGER, PRIMARY KEY, AUTOINCREMENT): Khóa chính.
- `UserId` (INTEGER, NOT NULL): ID của người đăng story.
- `ContentType` (TEXT, NOT NULL): Loại nội dung ('Text', 'Image').
- `Content` (TEXT, NOT NULL): Nội dung text hoặc đường dẫn đến file ảnh.
- `CreatedAt` (TEXT, NOT NULL): Thời gian đăng story.
- `ExpiresAt` (TEXT, NOT NULL): Thời gian hết hạn của story (thường là CreatedAt + 24 giờ).
- **Constraints:**
    - `FOREIGN KEY(UserId) REFERENCES Users(UserId)`
    - `CHECK(ContentType IN ('Text', 'Image'))`

### 3.7. Bảng `UserStatus` (Tùy chọn)

Theo dõi trạng thái online/offline.

- `UserStatusId` (INTEGER, PRIMARY KEY, AUTOINCREMENT): Khóa chính.
- `UserId` (INTEGER, UNIQUE, NOT NULL): ID của người dùng.
- `IsOnline` (INTEGER, NOT NULL): Trạng thái online (1) hay offline (0).
- `LastSeen` (TEXT, NOT NULL): Thời điểm cuối cùng người dùng online hoặc cập nhật trạng thái.
- **Constraints:**
    - `FOREIGN KEY(UserId) REFERENCES Users(UserId)`
    - `CHECK(IsOnline IN (0, 1))`

## 4. Lưu ý

- **Kiểu dữ liệu Thời gian:** SQLite không có kiểu dữ liệu thời gian chuyên dụng. Sử dụng kiểu `TEXT` và lưu trữ dưới dạng chuỗi ISO8601 ('YYYY-MM-DD HH:MM:SS.SSS') để đảm bảo tính nhất quán và dễ dàng truy vấn, sắp xếp.
- **File Storage:** Không nên lưu trữ file lớn trực tiếp trong cơ sở dữ liệu SQLite. Thay vào đó, lưu file trên hệ thống tệp của server và chỉ lưu đường dẫn (`FilePath`) trong cơ sở dữ liệu.
- **Chỉ mục (Indexes):** Cần tạo chỉ mục cho các cột thường xuyên được sử dụng trong mệnh đề `WHERE` hoặc `JOIN` (ví dụ: `SenderId`, `ReceiverUserId`, `ReceiverGroupId` trong bảng `Messages`; `User1Id`, `User2Id` trong bảng `Friendships`; `UserId`, `GroupId` trong bảng `GroupMembers`) để cải thiện hiệu năng truy vấn.
- **Quan hệ:** Các khóa ngoại (`FOREIGN KEY`) đã được định nghĩa để đảm bảo tính toàn vẹn dữ liệu.

---



# 3. Thiết kế Backend (ASP.NET Core & SignalR)

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
    - Khi người dùng tham gia nhóm chat (trong ứng dụng), thêm `ConnectionId` của họ vào Group SignalR tương ứng (`Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");`).
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

---



# 4. Thiết kế Frontend (ASP.NET Core Razor Pages & SignalR)

## 1. Tổng quan

Frontend của ứng dụng nhắn tin sẽ được xây dựng bằng ASP.NET Core Razor Pages, kết hợp với JavaScript để xử lý tương tác người dùng động và tích hợp với SignalR client library để nhận/gửi thông điệp thời gian thực. Giao diện sẽ được thiết kế đơn giản, tập trung vào chức năng cốt lõi của ứng dụng nhắn tin.

## 2. Công nghệ và Thư viện

- **Framework:** ASP.NET Core Razor Pages
- **Ngôn ngữ:** C#, HTML, CSS, JavaScript
- **Real-time:** `@microsoft/signalr` JavaScript client library
- **Styling:** Bootstrap (hoặc CSS tùy chỉnh) để tạo giao diện nhanh chóng và đáp ứng.
- **JavaScript:** Vanilla JS hoặc một thư viện nhỏ gọn như jQuery (tùy chọn) để đơn giản hóa DOM manipulation và AJAX calls.

## 3. Cấu trúc Layout Chính (`_Layout.cshtml`)

Một layout chung sẽ được sử dụng cho các trang yêu cầu xác thực:

- **Header:** Thanh điều hướng trên cùng, có thể chứa logo, tên người dùng đăng nhập, nút đăng xuất, và có thể là ô tìm kiếm bạn bè nhanh.
- **Sidebar (Tùy chọn):** Có thể có một sidebar bên trái để hiển thị danh sách bạn bè, nhóm chat, và truy cập nhanh các chức năng.
- **Main Content Area:** Khu vực chính để hiển thị nội dung của từng trang (`@RenderBody()`).
- **Footer:** Thông tin cơ bản (nếu cần).

## 4. Các Trang (Pages) và Thành phần (Components) Chính

### 4.1. Trang Đăng nhập (`Pages/Account/Login.cshtml`)

- Form đăng nhập với các trường: Tên đăng nhập, Mật khẩu.
- Nút "Đăng nhập".
- Liên kết đến trang Đăng ký và Quên mật khẩu.
- Hiển thị thông báo lỗi nếu đăng nhập thất bại.

### 4.2. Trang Đăng ký (`Pages/Account/Register.cshtml`)

- Form đăng ký với các trường: Tên đăng nhập, Mật khẩu, Xác nhận mật khẩu, Tên hiển thị.
- Nút "Đăng ký".
- Liên kết đến trang Đăng nhập.
- Hiển thị thông báo lỗi hoặc thành công.

### 4.3. Trang Quên/Đặt lại Mật khẩu (`Pages/Account/ForgotPassword.cshtml`, `ResetPassword.cshtml`)

- Giao diện đơn giản để người dùng nhập tên đăng nhập/email (tùy theo logic backend) và sau đó nhập mật khẩu mới (nếu có token hợp lệ).

### 4.4. Trang Chính / Bảng điều khiển (`Pages/Index.cshtml` hoặc `Pages/Chat/Index.cshtml`)

Đây là giao diện cốt lõi sau khi đăng nhập, thường được chia thành nhiều phần:

- **Danh sách Bạn bè/Nhóm (Sidebar hoặc Panel riêng):**
    - Hiển thị danh sách bạn bè với tên và trạng thái online/offline (cập nhật real-time).
    - Hiển thị danh sách các nhóm chat người dùng tham gia.
    - Cho phép người dùng chọn một bạn bè hoặc nhóm để bắt đầu/tiếp tục trò chuyện.
    - Có thể có chỉ báo tin nhắn chưa đọc.
- **Khu vực Story:**
    - Hiển thị các story của bạn bè (ví dụ: dạng thumbnail tròn ở đầu trang hoặc khu vực riêng).
    - Cho phép click để xem story chi tiết (có thể mở modal hoặc trang riêng).
    - Nút để đăng story mới.
- **Khu vực Chat:**
    - Hiển thị lịch sử tin nhắn của cuộc trò chuyện đang được chọn (cá nhân hoặc nhóm).
    - Tin nhắn được hiển thị theo thứ tự thời gian, phân biệt tin nhắn của mình và của người khác.
    - Hiển thị tên người gửi (trong chat nhóm).
    - Hiển thị trạng thái tin nhắn (Đã gửi, Đã xem - cập nhật real-time).
    - Hiển thị chỉ báo "Đang nhập..." (cập nhật real-time).
    - Hiển thị các file đính kèm (hình ảnh thu nhỏ, link tải file).
    - Tự động cuộn xuống tin nhắn mới nhất.
- **Khu vực Nhập liệu Tin nhắn:**
    - Ô nhập văn bản.
    - Nút gửi tin nhắn.
    - Nút đính kèm file (mở dialog chọn file).
    - (Tùy chọn) Nút ghi âm tin nhắn thoại.

### 4.5. Trang Quản lý Bạn bè (`Pages/Friends/Index.cshtml`)

- **Tìm kiếm người dùng:** Ô tìm kiếm và nút tìm kiếm. Hiển thị kết quả tìm kiếm với nút "Gửi yêu cầu kết bạn".
- **Yêu cầu đang chờ:** Hiển thị danh sách yêu cầu kết bạn đã nhận với nút "Chấp nhận" và "Từ chối".
- **Yêu cầu đã gửi:** Hiển thị danh sách yêu cầu đã gửi với nút "Hủy yêu cầu".

### 4.6. Trang Quản lý Nhóm (`Pages/Groups/Index.cshtml`, `Create.cshtml`, `Details.cshtml`)

- **Danh sách nhóm:** Hiển thị các nhóm người dùng tham gia.
- **Tạo nhóm mới (`Create.cshtml`):** Form nhập tên nhóm, chọn bạn bè để thêm vào nhóm ban đầu.
- **Chi tiết nhóm (`Details.cshtml`):** Hiển thị thông tin nhóm, danh sách thành viên. Cho phép quản trị viên thêm/xóa thành viên (nếu có quyền).

### 4.7. Trang Đăng Story (`Pages/Stories/Create.cshtml`)

- Form cho phép nhập nội dung text hoặc tải lên hình ảnh.
- Nút "Đăng Story".

## 5. Tích hợp SignalR Client (JavaScript)

- **Kết nối:** Trong file JavaScript chính (ví dụ: `site.js` hoặc `chat.js`), khởi tạo và bắt đầu kết nối đến `ChatHub` (`/chathub`) sau khi trang tải xong và người dùng đã xác thực.
  ```javascript
  const connection = new signalR.HubConnectionBuilder()
      .withUrl("/chathub")
      .build();

  connection.start().then(() => {
      console.log("SignalR Connected.");
      // Gọi các phương thức Hub khác nếu cần sau khi kết nối
  }).catch(err => console.error(err.toString()));
  ```
- **Gửi sự kiện:** Gắn các event listener vào các phần tử UI (nút gửi tin nhắn, ô nhập liệu) để gọi các phương thức trên Hub.
  ```javascript
  // Ví dụ: Gửi tin nhắn cá nhân
  document.getElementById("sendButton").addEventListener("click", event => {
      const receiverUserId = /* Lấy ID người nhận từ UI */;
      const message = document.getElementById("messageInput").value;
      connection.invoke("SendPrivateMessage", receiverUserId, message).catch(err => console.error(err.toString()));
      document.getElementById("messageInput").value = ""; // Xóa input
      event.preventDefault();
  });

  // Ví dụ: Thông báo đang nhập
  document.getElementById("messageInput").addEventListener("input", event => {
      const receiverUserId = /* Lấy ID người nhận */;
      connection.invoke("NotifyTyping", receiverUserId).catch(err => console.error(err.toString()));
  });
  ```
- **Nhận sự kiện:** Đăng ký các handler để xử lý các phương thức được gọi từ Hub (Server gọi Client).
  ```javascript
  // Ví dụ: Nhận tin nhắn cá nhân
  connection.on("ReceivePrivateMessage", (senderUserId, senderDisplayName, messageContent, sentAt, messageId) => {
      // Logic để hiển thị tin nhắn mới trong khu vực chat
      // Cập nhật UI...
      // Có thể gọi Hub để đánh dấu đã đọc nếu cửa sổ chat đang mở
  });

  // Ví dụ: Cập nhật trạng thái bạn bè
  connection.on("UpdateFriendStatus", (userId, isOnline) => {
      // Logic để cập nhật icon trạng thái của bạn bè trong danh sách
  });

  // Ví dụ: Nhận chỉ báo đang nhập
  connection.on("ReceiveTypingIndicator", (senderUserId) => {
      // Hiển thị chỉ báo "... đang nhập" cho senderUserId
      // Cần có logic để ẩn chỉ báo sau một khoảng thời gian ngắn
  });
  ```
- **Quản lý State:** Cần có logic JavaScript để quản lý trạng thái hiện tại của giao diện (ví dụ: cuộc trò chuyện nào đang được chọn) để hiển thị đúng thông tin và gửi đúng tham số khi gọi Hub.

## 6. Xử lý File

- **Upload:** Sử dụng thẻ `<input type="file">`. Khi người dùng chọn file, dùng JavaScript để lấy đối tượng `File`, có thể kiểm tra sơ bộ kích thước/loại file trên client, sau đó gửi file lên server thông qua một form POST đến một Action/Handler riêng biệt (không qua SignalR). Backend sẽ xử lý lưu file và có thể gửi thông báo tin nhắn chứa link file qua SignalR.
- **Download/Display:** Hiển thị link tải file hoặc thẻ `<img>` với đường dẫn đến file đã lưu trên server (ví dụ: `/uploads/images/abc.jpg`).

## 7. UI/UX Considerations

- **Responsive Design:** Sử dụng Bootstrap hoặc CSS media queries để đảm bảo giao diện hiển thị tốt trên các kích thước màn hình khác nhau.
- **Thông báo:** Sử dụng các thông báo tinh tế (ví dụ: toast notifications) cho các sự kiện như tin nhắn mới (khi cửa sổ chat không active), yêu cầu kết bạn mới.
- **Loading Indicators:** Hiển thị chỉ báo tải khi đang lấy dữ liệu (ví dụ: lịch sử chat) hoặc thực hiện hành động (gửi tin nhắn).
- **Real-time Feedback:** Cập nhật trạng thái tin nhắn (Đã gửi, Đã xem), trạng thái online, chỉ báo đang nhập một cách tức thì để người dùng cảm nhận được tính real-time.

---



# 5. Cấu trúc Dự án Chi tiết (ASP.NET Core Razor Pages)

## 1. Tổng quan

Cấu trúc dự án được tổ chức theo các lớp logic (Presentation, Business Logic, Data Access) và các tính năng của ứng dụng nhắn tin. Sử dụng ASP.NET Core Razor Pages làm nền tảng chính.

## 2. Cấu trúc Thư mục Gốc

```
ChatApp/
│
├── ChatApp.sln                   # Solution file
│
└── src/
    │
    └── ChatApp.Web/              # Dự án chính ASP.NET Core
        │
        ├── ChatApp.Web.csproj    # Project file
        ├── Program.cs            # (Hoặc Startup.cs) Cấu hình ứng dụng và dịch vụ
        ├── appsettings.json      # Cấu hình ứng dụng (chuỗi kết nối, etc.)
        ├── Properties/
        │   └── launchSettings.json # Cấu hình khởi chạy
        │
        ├── wwwroot/                # Thư mục chứa file tĩnh
        │   ├── css/                # Files CSS (e.g., site.css, bootstrap.min.css)
        │   ├── js/                 # Files JavaScript (e.g., site.js, chat.js, signalr.js)
        │   ├── lib/                # Thư viện frontend (e.g., bootstrap, jquery)
        │   └── uploads/            # Thư mục lưu trữ file tải lên (cần tạo)
        │       ├── images/
        │       ├── videos/
        │       ├── files/
        │       └── stories/
        │
        ├── Data/                   # Lớp Data Access (EF Core)
        │   ├── ApplicationDbContext.cs # DbContext
        │   └── Migrations/         # Thư mục chứa các migrations của EF Core
        │
        ├── Models/                 # ViewModels, DTOs, và có thể cả Entities nếu đơn giản
        │   ├── Entities/           # Các lớp thực thể (User, Message, Group, etc.)
        │   │   ├── User.cs
        │   │   ├── Friendship.cs
        │   │   ├── Message.cs
        │   │   ├── Group.cs
        │   │   ├── GroupMember.cs
        │   │   ├── Story.cs
        │   │   └── UserStatus.cs
        │   ├── ViewModels/         # Các lớp ViewModel cho Pages
        │   │   ├── Account/        # ViewModels cho Account Pages
        │   │   │   ├── LoginViewModel.cs
        │   │   │   └── RegisterViewModel.cs
        │   │   ├── Chat/           # ViewModels cho Chat Pages
        │   │   │   └── ChatViewModel.cs
        │   │   └── ... (Các ViewModels khác cho Friends, Groups, Stories)
        │   └── DTOs/               # Data Transfer Objects (nếu cần)
        │
        ├── Services/               # Lớp Business Logic
        │   ├── Interfaces/         # Interfaces cho Services
        │   │   ├── IAuthService.cs
        │   │   ├── IUserService.cs
        │   │   ├── IFriendshipService.cs
        │   │   ├── IChatService.cs
        │   │   ├── IGroupService.cs
        │   │   ├── IStoryService.cs
        │   │   └── IFileService.cs
        │   ├── AuthService.cs      # Implementations của Services
        │   ├── UserService.cs
        │   ├── FriendshipService.cs
        │   ├── ChatService.cs
        │   ├── GroupService.cs
        │   ├── StoryService.cs
        │   └── FileService.cs
        │
        ├── Hubs/                   # SignalR Hubs
        │   └── ChatHub.cs
        │
        ├── Pages/                  # Razor Pages (Presentation Layer)
        │   ├── _ViewStart.cshtml
        │   ├── _ViewImports.cshtml
        │   ├── Index.cshtml        # Trang chính (có thể là trang chat)
        │   ├── Index.cshtml.cs
        │   ├── Privacy.cshtml
        │   ├── Privacy.cshtml.cs
        │   ├── Error.cshtml
        │   ├── Error.cshtml.cs
        │   ├── Shared/             # Layouts và Partial Views dùng chung
        │   │   ├── _Layout.cshtml
        │   │   └── _LoginPartial.cshtml
        │   ├── Account/            # Pages cho quản lý tài khoản
        │   │   ├── Login.cshtml
        │   │   ├── Login.cshtml.cs
        │   │   ├── Register.cshtml
        │   │   ├── Register.cshtml.cs
        │   │   ├── Logout.cshtml
        │   │   ├── Logout.cshtml.cs
        │   │   └── ... (ForgotPassword, ResetPassword)
        │   ├── Chat/               # Pages cho chức năng chat
        │   │   ├── Index.cshtml    # Giao diện chat chính
        │   │   ├── Index.cshtml.cs
        │   │   └── _ChatHistoryPartial.cshtml # Partial view hiển thị lịch sử chat
        │   ├── Friends/            # Pages cho quản lý bạn bè
        │   │   ├── Index.cshtml
        │   │   ├── Index.cshtml.cs
        │   │   └── _FriendRequestPartial.cshtml
        │   ├── Groups/             # Pages cho quản lý nhóm
        │   │   ├── Index.cshtml
        │   │   ├── Index.cshtml.cs
        │   │   ├── Create.cshtml
        │   │   ├── Create.cshtml.cs
        │   │   └── Details.cshtml
        │   │   └── Details.cshtml.cs
        │   └── Stories/            # Pages cho quản lý story
        │       ├── Index.cshtml    # Xem story
        │       ├── Index.cshtml.cs
        │       ├── Create.cshtml
        │       └── Create.cshtml.cs
        │
        └── BackgroundServices/     # (Tùy chọn) Các dịch vụ chạy nền
            └── ExpiredStoryCleanupService.cs
```

## 3. Giải thích Cấu trúc

- **`ChatApp.Web`:** Dự án chính chứa tất cả code của ứng dụng. Đối với một dự án nhỏ/bài tập, việc gộp tất cả vào một project là chấp nhận được. Trong các dự án lớn hơn, BLL và DAL có thể được tách thành các Class Library riêng biệt.
- **`wwwroot`:** Chứa các tài nguyên tĩnh như CSS, JavaScript, hình ảnh, và các thư viện frontend. Thư mục `uploads` được tạo thủ công hoặc bằng code để lưu file do người dùng tải lên.
- **`Data`:** Chứa `DbContext` và các migrations của Entity Framework Core, đại diện cho lớp truy cập dữ liệu.
- **`Models`:**
    - **`Entities`:** Các lớp POCO (Plain Old CLR Object) ánh xạ trực tiếp tới các bảng trong cơ sở dữ liệu.
    - **`ViewModels`:** Các lớp được thiết kế đặc biệt để truyền dữ liệu đến và đi từ các Razor Pages. Chúng có thể chứa dữ liệu từ nhiều Entities và logic trình bày cụ thể.
    - **`DTOs`:** (Data Transfer Objects) Tùy chọn, dùng để truyền dữ liệu giữa các lớp, đặc biệt là qua các ranh giới như API hoặc SignalR Hub, giúp tách biệt cấu trúc dữ liệu nội bộ và cấu trúc dữ liệu truyền đi.
- **`Services`:** Chứa các lớp logic nghiệp vụ. Mỗi service thường xử lý một lĩnh vực chức năng cụ thể (Auth, User, Chat...). Các interface được định nghĩa trong thư mục con `Interfaces` để tuân thủ Dependency Inversion Principle.
- **`Hubs`:** Chứa các lớp SignalR Hub, xử lý giao tiếp thời gian thực.
- **`Pages`:** Chứa các Razor Pages, là thành phần chính của lớp trình bày. Mỗi page bao gồm một file `.cshtml` (HTML và Razor syntax) và một file `.cshtml.cs` (PageModel chứa logic xử lý request và tương tác với Services).
    - **`Shared`:** Chứa các layout (`_Layout.cshtml`) và partial views (`_LoginPartial.cshtml`) được sử dụng lại trên nhiều trang.
    - Các thư mục con (`Account`, `Chat`, `Friends`, `Groups`, `Stories`) tổ chức các trang theo chức năng.
- **`BackgroundServices`:** (Tùy chọn) Chứa các dịch vụ chạy nền độc lập với request của người dùng, ví dụ như dọn dẹp dữ liệu cũ.

## 4. Luồng hoạt động cơ bản

1.  Người dùng truy cập một URL.
2.  ASP.NET Core Routing ánh xạ URL đến một Razor Page trong thư mục `Pages`.
3.  Phương thức Handler trong PageModel (`.cshtml.cs`) được thực thi (ví dụ: `OnGetAsync`, `OnPostAsync`).
4.  PageModel gọi các phương thức trong lớp `Services` tương ứng (được inject qua constructor) để thực hiện logic nghiệp vụ.
5.  Lớp `Services` sử dụng `ApplicationDbContext` (được inject) để truy vấn hoặc cập nhật dữ liệu trong SQLite.
6.  Nếu cần tương tác real-time (ví dụ: gửi tin nhắn), `Services` hoặc `PageModel` có thể lấy `IHubContext<ChatHub>` (được inject) để gọi các phương thức trên `ChatHub` hoặc trực tiếp đến các client.
7.  `ChatHub` xử lý các kết nối và giao tiếp hai chiều với các client JavaScript thông qua SignalR.
8.  PageModel chuẩn bị dữ liệu (thường là thông qua ViewModel) và trả về cho file `.cshtml`.
9.  File `.cshtml` render HTML cuối cùng để gửi về trình duyệt người dùng.
10. JavaScript trong trình duyệt (trong `wwwroot/js`) kết nối đến `ChatHub`, gửi sự kiện (ví dụ: người dùng gõ tin nhắn) và nhận sự kiện (ví dụ: có tin nhắn mới) để cập nhật UI động.

---



# 6. Ghi chú Triển khai Dự án

## 1. Tổng quan

Tài liệu này cung cấp các ghi chú, lưu ý quan trọng và các phương pháp hay nhất cần xem xét trong quá trình triển khai thực tế ứng dụng nhắn tin dựa trên các thiết kế đã được đề ra (Yêu cầu, CSDL, Backend, Frontend, Cấu trúc dự án).

## 2. Môi trường và Công cụ

- **IDE:** Visual Studio 2022
- **SDK:** .NET 6 hoặc mới hơn (khuyến nghị LTS)
- **Database Tool:** DB Browser for SQLite hoặc tích hợp trong Visual Studio để xem/quản lý file SQLite.
- **Quản lý gói:** NuGet (cho backend), npm hoặc libman (cho frontend JS libraries).

## 3. Lưu ý về Cơ sở dữ liệu (SQLite & EF Core)

- **Migrations:** Sử dụng EF Core Migrations (`Add-Migration`, `Update-Database`) để quản lý schema CSDL. Tạo migration ban đầu sau khi định nghĩa các Entities và DbContext.
- **Seeding Data:** Cân nhắc tạo dữ liệu mẫu (ví dụ: tài khoản admin, một vài người dùng) bằng cách sử dụng phương thức `OnModelCreating` trong `DbContext` hoặc một dịch vụ riêng biệt chạy khi khởi động ứng dụng (trong môi trường Development).
- **Kiểu dữ liệu Thời gian:** Luôn lưu trữ và truy vấn thời gian dưới dạng UTC (`DateTime.UtcNow`) và chuyển đổi sang giờ địa phương khi hiển thị trên UI nếu cần. Sử dụng định dạng ISO 8601 (`YYYY-MM-DD HH:MM:SS.SSS`) khi lưu vào cột TEXT.
- **Concurrency:** SQLite có cơ chế khóa ở cấp độ file. Đối với ứng dụng web có nhiều người dùng đồng thời, điều này có thể gây tắc nghẽn, mặc dù EF Core và ASP.NET Core giúp giảm thiểu phần nào. Với quy mô bài tập, vấn đề này ít nghiêm trọng, nhưng cần lưu ý nếu mở rộng.
- **Chỉ mục (Indexes):** Đảm bảo đã định nghĩa các chỉ mục cần thiết trong `OnModelCreating` cho các cột thường dùng trong `WHERE` và `JOIN` để tối ưu hiệu năng truy vấn, đặc biệt là trên các bảng lớn như `Messages`.
- **Đường dẫn File DB:** Cấu hình đường dẫn đến file `.db` trong `appsettings.json`. Đảm bảo ứng dụng có quyền ghi vào thư mục chứa file DB khi triển khai.

## 4. Lưu ý về Backend (ASP.NET Core & Services)

- **Async/Await:** Sử dụng `async` và `await` một cách nhất quán cho tất cả các hoạt động I/O (đặc biệt là tương tác CSDL và gọi Hub) để giữ cho ứng dụng có khả năng đáp ứng cao.
- **Dependency Injection (DI):** Tận dụng tối đa DI của ASP.NET Core. Đăng ký `DbContext`, các `Services`, `IHubContext`, `IHttpContextAccessor` (nếu cần) trong `Program.cs`. Inject dependencies qua constructor.
- **ViewModels/DTOs:** Sử dụng ViewModels cho Razor Pages và DTOs (nếu cần) cho SignalR Hub hoặc API để tránh lộ cấu trúc Entity và chỉ truyền dữ liệu cần thiết.
- **Password Hashing:** Nếu không dùng ASP.NET Core Identity, hãy chọn một thư viện băm mật khẩu mạnh và đáng tin cậy (ví dụ: `BCrypt.Net` hoặc `libsodium`). Không bao giờ lưu mật khẩu dạng plain text.
- **Authorization:** Sử dụng thuộc tính `[Authorize]` trên các Pages hoặc Hubs yêu cầu người dùng đăng nhập. Lấy thông tin người dùng đã xác thực từ `HttpContext.User` (trong Pages/Controllers) hoặc `Context.UserIdentifier` / `Context.User` (trong Hubs).
- **Error Handling:** Implement middleware xử lý lỗi toàn cục để bắt các exception chưa được xử lý, ghi log và trả về trang lỗi thân thiện hoặc thông báo lỗi JSON phù hợp.
- **File Service:** Logic lưu file cần đảm bảo tên file là duy nhất (ví dụ: sử dụng GUID) để tránh ghi đè và xử lý các vấn đề bảo mật cơ bản (không cho phép lưu file vào các thư mục nhạy cảm, kiểm tra loại file nếu cần).

## 5. Lưu ý về Frontend (Razor Pages & JavaScript)

- **SignalR Client:** Bao gồm thư viện `@microsoft/signalr` JavaScript (qua npm/yarn hoặc CDN/libman). Khởi tạo kết nối sau khi trang tải và người dùng đã xác thực.
- **UI Updates:** Logic cập nhật DOM khi nhận message từ Hub cần hiệu quả. Tránh thao tác DOM quá nhiều. Cân nhắc sử dụng template literals hoặc các thư viện nhỏ để render HTML động.
- **State Management:** Quản lý trạng thái phía client (ví dụ: `currentChatUserId`, `currentChatGroupId`) để biết đang ở cuộc trò chuyện nào, từ đó hiển thị đúng dữ liệu và gửi đúng tham số lên Hub.
- **Scrolling:** Tự động cuộn xuống cuối khu vực chat khi có tin nhắn mới hoặc khi mở cuộc trò chuyện.
- **Client-side Validation:** Thực hiện validation cơ bản trên client (ví dụ: trường bắt buộc) trước khi gửi form để cải thiện trải nghiệm người dùng, nhưng luôn phải validate lại ở backend.
- **Security:** Sử dụng Razor syntax (`@`) để tự động mã hóa output, giúp chống XSS. Cẩn thận khi chèn HTML động từ dữ liệu người dùng (phải được sanitize).

## 6. Lưu ý về SignalR (`ChatHub`)

- **User Mapping:** Cần một cơ chế tin cậy để map `UserId` với (các) `ConnectionId` của họ. Một `Dictionary<int, HashSet<string>>` tĩnh hoặc sử dụng một dịch vụ quản lý kết nối có thể phù hợp cho bài tập. Lưu ý xử lý race conditions nếu dùng dictionary tĩnh.
- **Connection Lifecycle:** Xử lý logic trong `OnConnectedAsync` và `OnDisconnectedAsync` một cách cẩn thận: cập nhật trạng thái online/offline, thông báo cho bạn bè, thêm/xóa khỏi các SignalR Groups tương ứng với các nhóm chat người dùng tham gia.
- **Group Management:** Thêm/Xóa connection khỏi SignalR Groups (`Groups.AddToGroupAsync`, `Groups.RemoveFromGroupAsync`) khi người dùng tham gia/rời nhóm chat trong ứng dụng hoặc khi kết nối/ngắt kết nối.
- **Targeting Clients:** Sử dụng `Clients.User(userId)` để gửi đến tất cả connection của một user cụ thể, `Clients.Group(groupName)` để gửi đến một nhóm, `Clients.Client(connectionId)` để gửi đến một connection cụ thể, `Clients.AllExcept(...)`...
- **Typing Indicators:** Logic gửi `NotifyTyping` từ client nên có debounce (chỉ gửi sau một khoảng thời gian ngắn không gõ) để tránh gửi quá nhiều request. Logic nhận `ReceiveTypingIndicator` nên có timeout để tự động ẩn chỉ báo nếu không nhận được tin nhắn hoặc thông báo typing mới.
- **Message Status (Sent/Delivered/Read):**
    - **Sent:** Mặc định khi lưu vào DB.
    - **Delivered:** Server có thể gửi xác nhận lại cho người gửi khi message được gửi thành công đến `ReceivePrivateMessage`/`ReceiveGroupMessage` trên client nhận (nếu client đó đang online).
    - **Read:** Client nhận gửi một message riêng lên Hub (`MarkAsRead`) khi người dùng thực sự thấy tin nhắn. Hub cập nhật DB và thông báo lại cho người gửi (`UpdateMessageStatus`).

## 7. Lưu ý về Tính năng Cụ thể

- **Stories:** Chức năng xóa story hết hạn nên được thực hiện bởi một Background Service (`IHostedService`) chạy định kỳ để không ảnh hưởng đến request thông thường.
- **File Sharing:** Giới hạn kích thước file cần được kiểm tra cả ở client (sơ bộ) và backend (bắt buộc). Cấu hình giới hạn kích thước request trong ASP.NET Core nếu cần.
- **Voice Messages:** Yêu cầu sử dụng MediaRecorder API trong trình duyệt để ghi âm, gửi file âm thanh lên server, và thẻ `<audio>` để phát lại. Đây là tính năng phức tạp hơn.
- **Video/Voice Call:** Tính năng này rất phức tạp, thường yêu cầu WebRTC và một STUN/TURN server. Nên bỏ qua hoặc chỉ mô phỏng giao diện cho bài tập cơ bản.

## 8. Triển khai (Deployment)

- **SQLite File Path:** Đảm bảo đường dẫn đến file SQLite là đúng trong môi trường triển khai và ứng dụng có quyền ghi.
- **Static Files:** Đảm bảo server được cấu hình để phục vụ file tĩnh từ `wwwroot` và thư mục `uploads`.
- **HTTPS:** Luôn sử dụng HTTPS trong môi trường production.

## 9. Kiểm thử (Testing)

- Mặc dù không phải yêu cầu chính, việc viết Unit Test cho các lớp Service (sử dụng mocking cho DbContext/Repositories) và Integration Test có thể giúp đảm bảo chất lượng.
- Kiểm thử thủ công kỹ lưỡng các luồng chức năng chính, đặc biệt là các tương tác real-time.


