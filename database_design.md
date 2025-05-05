# Thiết kế Cơ sở dữ liệu (SQLite)

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
