# Ghi chú Triển khai Dự án Ứng dụng Nhắn tin

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
